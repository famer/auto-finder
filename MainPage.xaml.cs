using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using System.Device.Location;
using Microsoft.Devices.Sensors;
using System.IO.IsolatedStorage;
using System.Windows.Threading;
using Microsoft.Phone.Tasks;// SMS
using System.Collections.ObjectModel;
using Microsoft.Advertising.Mobile.UI;


namespace Auto_finder
{
    public partial class MainPage : PhoneApplicationPage
    {
        
        bool useGeoLocation;
        GeoCoordinateWatcher geoWatcher = new GeoCoordinateWatcher(GeoPositionAccuracy.High) { /*MovementThreshold = 1*/ };
        GeoPositionStatus currentGPSStatus;
        DispatcherTimer timer = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(210) };
        double targetLongitude = 43.9789, targetLatitude = 56.2666;
        double currentLongitude, currentLatitude;
        double currentHorizontalAccuracy, currentVerticalAccuracy;

        double currentGeoCourse;

        DateTimeOffset currentGeoTime;

        bool calibrating = false;
        Compass compass;// = new Compass() { TimeBetweenUpdates = TimeSpan.FromMilliseconds(30) };
        bool isCompassDataValid;
        double currentTrueHeading, currentMagneticHeading, startAngle;
        //float startAngle;
        double currentHeadingAccuracy;

        private MediaElement ambienceSound;

        
        private ObservableCollection<Placemark> PlaceMarks = new ObservableCollection<Placemark>();
        
        bool savingPlacemark = false;
        
        // Rename
        Placemark renamingPlacemark;

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            // Extract target coordinates
            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
            settings.TryGetValue("longitude", out targetLongitude);
            settings.TryGetValue("latitude", out targetLatitude);
            settings.TryGetValue("useGeoLocation", out useGeoLocation);
            settings.TryGetValue("PlaceMarks", out PlaceMarks);

            if (PlaceMarks == null)
            {
                PlaceMarks = new ObservableCollection<Placemark>();
            }
            if (PlaceMarks.Count < 4)
            {
                PlaceMarks.Add(new Placemark() { Title = "Дом", Persist = true });
                PlaceMarks.Add(new Placemark() { Title = "Гостиница", Persist = true });
                PlaceMarks.Add(new Placemark() { Title = "Рыбное место", Persist = true });
                PlaceMarks.Add(new Placemark() { Title = "Грибное место", Persist = true });
                PlaceMarks.Add(new Placemark() { Title = "Выход", Persist = true });
            }

            
            if (useGeoLocation == false)
            {


                MessageBoxResult mb = MessageBox.Show("Для работы программы необходим доступ к GPS вашего устройства. Мы не храним, не отправляем и не передаем координаты третьим лицам. Нажмите ОК для продолжения", "Предупреждение", MessageBoxButton.OKCancel);

                if (mb == MessageBoxResult.OK)
                {
                    useGeoLocation = true;
                }
                else
                {
                    useGeoLocation = false;
                }
                settings["useGeoLocation"] = useGeoLocation;
                settings.Save();
            }

            // Set the data context of the listbox control to the sample data
            DataContext = App.ViewModel;
            this.Loaded += new RoutedEventHandler(MainPage_Loaded);

            // Positioning stuff
            geoWatcher.PositionChanged += new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(watcher_PositionChanged);
            geoWatcher.StatusChanged += new EventHandler<GeoPositionStatusChangedEventArgs>(watcher_StatusChanged);
            geoWatcher.Start();

            //if (Microsoft.Devices.Environment.DeviceType != Microsoft.Devices.DeviceType.Emulator)
            //{
            if (!Compass.IsSupported)
            {
                // The device on which the application is running does not support
                // the compass sensor. Alert the user and hide the
                // application bar.
                MessageBox.Show("Устройство не поддерживает компасс.");
                
            }
            else
            {
                // Compass stuff
                compass = new Compass() { TimeBetweenUpdates = TimeSpan.FromMilliseconds(30) };
                compass.CurrentValueChanged += new EventHandler<SensorReadingEventArgs<CompassReading>>(compass_CurrentValueChanged);
                compass.Calibrate += new EventHandler<CalibrationEventArgs>(compass_Calibrate);
                compass.Start();
            }
            //}

            // Sound stuff

            ambienceSound = new MediaElement();
            ambienceSound.AutoPlay = false;
            ambienceSound.Source = new Uri("Sounds/pointed.mp3", UriKind.Relative);
            ambienceSound.Volume = 1f;
            ambienceSound.MediaEnded += new RoutedEventHandler(ambienceSound_MediaEnded);
            LayoutRoot.Children.Add(ambienceSound);
            ambienceSound.Visibility = Visibility.Collapsed;

            // Graphical Timer stuff
            timer.Tick += new EventHandler(timer_Tick);
            timer.Start();

            FirstListBox.DataContext = PlaceMarks;
        }

        // Load data for the ViewModel Items
        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (!App.ViewModel.IsDataLoaded)
            {
                App.ViewModel.LoadData();
            }

            if (targetLatitude != 0.0 && targetLongitude != 0.0)
            {
                MaintPivot.SelectedItem = Navigation;
            }
        }

        void watcher_PositionChanged(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
            currentLongitude = e.Position.Location.Longitude;
            currentLatitude = e.Position.Location.Latitude;
            
            currentGeoCourse = e.Position.Location.Course;
            currentGeoTime = e.Position.Timestamp;

            currentHorizontalAccuracy = e.Position.Location.HorizontalAccuracy;
            currentVerticalAccuracy = e.Position.Location.VerticalAccuracy;
        }



        void watcher_StatusChanged(object sender, GeoPositionStatusChangedEventArgs e)
        {
            currentGPSStatus = e.Status;
            Deployment.Current.Dispatcher.BeginInvoke(() => MyStatusChanged(e));

        }

        void MyStatusChanged(GeoPositionStatusChangedEventArgs e)
        {
            switch (e.Status)
            {
                case GeoPositionStatus.Disabled:
                    // The location service is disabled or unsupported.
                    // Alert the user
                    //StatusTextBlock.Text = "location is unsupported on this device";
                    break;
                case GeoPositionStatus.Initializing:
                    // The location service is initializing.
                    // Disable the Start Location button
                    //StatusTextBlock.Text = "initializing location service," + accuracyText;
                    break;
                case GeoPositionStatus.NoData:
                    // The location service is working, but it cannot get location data
                    // Alert the user and enable the Stop Location button
                    //StatusTextBlock.Text = "data unavailable," + accuracyText;
                    break;
                case GeoPositionStatus.Ready:
                    // The location service is working and is receiving location data
                    // Show the current position and enable the Stop Location button
                    //StatusTextBlock.Text = "receiving data, " + accuracyText;
                    LoadingPanel.Visibility = System.Windows.Visibility.Collapsed;
                    break;

            }
        }

        void compass_CurrentValueChanged(object sender, SensorReadingEventArgs<CompassReading> e)
        {
            // Note that this event handler is called from a background thread
            // and therefore does not have access to the UI thread. To update 
            // the UI from this handler, use Dispatcher.BeginInvoke() as shown.
            // Dispatcher.BeginInvoke(() => { statusTextBlock.Text = "in CurrentValueChanged"; });


            isCompassDataValid = compass.IsDataValid;

            currentTrueHeading = e.SensorReading.TrueHeading;
            currentMagneticHeading = e.SensorReading.MagneticHeading;
            currentHeadingAccuracy = Math.Abs(e.SensorReading.HeadingAccuracy);

        }

        void compass_Calibrate(object sender, CalibrationEventArgs e)
        {
            Dispatcher.BeginInvoke(() => { calibrationStackPanel.Visibility = Visibility.Visible; });
            calibrating = true;

        }

        void timer_Tick(object sender, EventArgs e)
        {
            if (currentGPSStatus != GeoPositionStatus.Ready)
            {
                //return;
            }
            if (calibrating)
            {
                if (currentHeadingAccuracy <= 10)
                {
                    calibrationTextBlock.Foreground = new SolidColorBrush(Colors.Green);
                    calibrationTextBlock.Text = "Завершено!";
                    calibrationStackPanel.Visibility = System.Windows.Visibility.Collapsed;
                    calibrating = false;
                }
                else
                {
                    calibrationTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                    calibrationTextBlock.Text = currentHeadingAccuracy.ToString("0.0");
                }
                return;
            }
            if (targetLatitude == 0.0  && targetLongitude == 0.0)
                return;

            double dLong = (targetLongitude - currentLongitude) * (Math.PI/180);
            double dLat = (targetLatitude - currentLatitude) * (Math.PI / 180);

            double currentLatitudeRad = (currentLatitude) * (Math.PI / 180);
            double targetLatitudeRad = (targetLatitude) * (Math.PI / 180);
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Sin(dLong / 2) * Math.Sin(dLong / 2) * Math.Cos(currentLatitudeRad) * Math.Cos(targetLatitudeRad);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            double distance = 6371 * c * 1000;

            double y = Math.Sin(dLong) * Math.Cos(targetLatitudeRad);
            double x = Math.Cos(currentLatitudeRad) * Math.Sin(targetLatitudeRad) -
                    Math.Sin(currentLatitudeRad) * Math.Cos(targetLatitudeRad) * Math.Cos(dLong);

            double targetBearing = (Math.Atan2(y, x) * (180 / Math.PI) + 360) % 360;
            double targetAngle = (targetBearing - currentTrueHeading + 360) % 360;
            /*
            if (currentGeoCourse > 0.0)
            {
                targetAngle = (targetBearing - currentGeoCourse + 360) % 360;
            }
            else
            {
                targetAngle = (targetBearing - currentTrueHeading + 360) % 360;
            }*/

            // Updates distance text box
            if (distance < 1000)
            {
                distanceTextBox.Text = distance.ToString("0") + " м";
            }
            else
            {
                distanceTextBox.Text = (distance/1000).ToString("0.0") + " км";
            }


            // Map center
            map1.Center = new GeoCoordinate(currentLatitude, currentLongitude);

            map1.RenderTransform = new RotateTransform() { Angle = -targetBearing }; //, CenterX = 240, CenterY = 235

            // Moving target
            if (distance > 102)
            {
                Canvas.SetTop(targetEllipse, -62);
                pathLine.Y1 = -30;

                // Map Zoom Level

                map1.ZoomLevel = 19 - Math.Log((distance / 70.5), 2) - 1;

            }
            else
            {
                // Map Zoom Level
                map1.ZoomLevel = 17.263;
                Canvas.SetTop(targetEllipse, 204 - (int)distance * 2 - 62);
                pathLine.Y1 = 204 - (int)distance - 30;
            }

            if (distance < 7)
            {
                /*double sX = 3, sY = sX;

                ScaleTransform sc = new ScaleTransform() { ScaleX = sX, ScaleY = sY };
                targetEllipse.RenderTransform = sc;
                Canvas.SetTop(targetEllipse, 142);
                 * */
                /*
                Canvas.SetLeft(targetEllipse, 0);
                Canvas.SetTop(targetEllipse, 94);
                targetEllipse.Height = 160;
                targetEllipse.Width = 160;*/
            }
            else if (distance <= 21)
            {

                /*
                double sX = 21 / (distance), sY = sX;

                ScaleTransform sc = new ScaleTransform() { ScaleX = sX, ScaleY = sY };
                targetEllipse.RenderTransform = sc;
                 * */
            }
            else 
            {
                double sX = 1, sY = 1;

                ScaleTransform sc = new ScaleTransform() { ScaleX = sX, ScaleY = sY };
                targetEllipse.RenderTransform = sc;
                /*
                Canvas.SetLeft(targetEllipse, 46);
                targetEllipse.Height = 64;
                targetEllipse.Width = 64;
                */
            }


            // Does the animation
            Duration duration = new Duration(TimeSpan.FromSeconds(0.2));
            Storyboard sb = new Storyboard();
            sb.Duration = duration;

            DoubleAnimation da = new DoubleAnimation();
            da.Duration = duration;

            sb.Children.Add(da);

            RotateTransform rt = new RotateTransform();

            Storyboard.SetTarget(da, rt);
            Storyboard.SetTargetProperty(da, new PropertyPath("Angle"));
            da.From = startAngle;
            da.To = targetAngle;
            if (da.To - da.From > 180)
            {
                da.From += 360;
            }
            else if (da.To - da.From < -180)
            {
                da.To += 360;
            }

            Triangle.RenderTransform = rt;
            Triangle.RenderTransformOrigin = new Point(0.5, 1);

            sb.Begin();
            startAngle = targetAngle;
            

        }

       
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (currentGPSStatus == GeoPositionStatus.Ready)
            {

                MessageBoxResult mb = MessageBoxResult.None;
                if (useGeoLocation == false)
                {

                    mb = MessageBox.Show("Для работы программы необходим доступ к GPS вашего устройства. Мы не храним, не отправляем и не передаем координаты третьим лицам. Нажмите ОК для продолжения", "Предупреждение", MessageBoxButton.OKCancel);

                    if (mb == MessageBoxResult.OK)
                    {
                        useGeoLocation = true;
                        IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;

                        settings["useGeoLocation"] = useGeoLocation;
                        settings.Save();
                        mb = MessageBoxResult.None;
                    }
                    else
                    {

                        return;
                    }
                    
                }

                if (currentHorizontalAccuracy > 51)
                {

                    mb = MessageBox.Show("Я могу определить место с точностью до " + (int) currentHorizontalAccuracy
                        + ". Если этой точности не достаточно попробуйте нажать кнопку позже. Отметить место сейчас?", "Предупреждение", MessageBoxButton.OKCancel);
                }
                if (mb == MessageBoxResult.OK || mb == MessageBoxResult.None)
                {
                    targetLatitude = currentLatitude;
                    targetLongitude = currentLongitude;
                    
                    PlaceMarks.Insert(0, new Placemark() { Persist = false, DateTime = DateTime.Now, Latitude = currentLatitude, Longitude = currentLongitude });
                    if (PlaceMarks[3].Persist == false)
                    {
                        PlaceMarks.RemoveAt(3);
                    }

                    IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
                    settings["longitude"] = targetLongitude;
                    settings["latitude"] = targetLatitude;
                    settings["PlaceMarks"] = PlaceMarks;
                    
                    settings.Save();
                    MaintPivot.SelectedItem = Navigation;
                    ambienceSound.Play();
                }
            } else {
                MessageBoxResult mb = MessageBox.Show("Сейчас определить координаты невозможно попробуйте еще раз");
                if (mb != MessageBoxResult.OK)
                {
                    
                }
            }
        }

        private void resetButton_Click(object sender, RoutedEventArgs e)
        {
            targetLatitude = 0.0;
            targetLongitude = 0.0;
            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
            settings["longitude"] = 0.0;
            settings["latitude"] = 0.0;
            settings.Save();
            MaintPivot.SelectedItem = Mark;
        }

        private void calibrationButton_Click(object sender, RoutedEventArgs e)
        {
            calibrationStackPanel.Visibility = System.Windows.Visibility.Collapsed;
            calibrating = false;
        }

        private void infoButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/SettingsPage.xaml", UriKind.Relative));
        }

        

        private void ambienceSound_MediaEnded(object sender, RoutedEventArgs e)
        {
            // Loop the ambience sound.
            ambienceSound.Position = TimeSpan.Zero;
            //ambienceSound.Play();
        }

        private void sendLocationButton_Click(object sender, RoutedEventArgs e)
        {
            SmsComposeTask smsComposeTask = new SmsComposeTask();

            //smsComposeTask.To = "2065550123";
            smsComposeTask.Body = string.Format("I'm here! {0,0} {1,0}", currentLongitude, currentLatitude);

            smsComposeTask.Show();
        }

        private void enterLocationButton_Click(object sender, RoutedEventArgs e)
        {
            LocationEnterPanel.Visibility = System.Windows.Visibility.Visible;
        }

        private void submitLocationButton_Click(object sender, RoutedEventArgs e)
        {
            string coord = enterLocationTextBox.Text;

            LocationEnterPanel.Visibility = System.Windows.Visibility.Collapsed;
            string[] split = coord.Split(' ');
            if (coord == String.Empty || split.Count() < 4 || split[2] == String.Empty || split[3] == String.Empty)
            {
                MessageBox.Show("Невозможно распознать координаты в тексте. Попробуйте еще раз.");
                return;
            }
            Double.TryParse(split[2], out targetLongitude);
            Double.TryParse(split[3], out targetLatitude);

            if (targetLongitude == 0.0 || targetLatitude == 0.0)
            {
                MessageBox.Show("Невозможно распознать координаты в тексте. Попробуйте еще раз.");
                return;
            }

            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
            settings["longitude"] = targetLongitude;
            settings["latitude"] = targetLatitude;
            settings.Save();
            MaintPivot.SelectedItem = Navigation;
            ambienceSound.Play();
        }

        private void cancelLocationBox_Click(object sender, RoutedEventArgs e)
        {
            LocationEnterPanel.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void hideMapButton_Click(object sender, RoutedEventArgs e)
        {
            map1.Visibility = System.Windows.Visibility.Collapsed;
            //bottomMapRect.Visibility = System.Windows.Visibility.Collapsed;
            //upperMapRect.Visibility = System.Windows.Visibility.Collapsed;
            hideMapButton.Visibility = System.Windows.Visibility.Collapsed;
            showMapButton.Visibility = System.Windows.Visibility.Visible;
        }

        private void showMapButton_Click(object sender, RoutedEventArgs e)
        {
            map1.Visibility = System.Windows.Visibility.Visible;
            //bottomMapRect.Visibility = System.Windows.Visibility.Visible;
            //upperMapRect.Visibility = System.Windows.Visibility.Visible;
            hideMapButton.Visibility = System.Windows.Visibility.Visible;
            showMapButton.Visibility = System.Windows.Visibility.Collapsed;
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {

            if (LocationEnterPanel.Visibility == System.Windows.Visibility.Visible && !NavigationService.CanGoBack)
            {
                e.Cancel = true;
                LocationEnterPanel.Visibility = System.Windows.Visibility.Collapsed;
            }

        }

        private void FirstListBox_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            int selectedIndex = (sender as ListBox).SelectedIndex;
            if (selectedIndex == -1) return;

            if (savingPlacemark)
            {
                if (PlaceMarks[selectedIndex].Persist != true) return;
                if (PlaceMarks[selectedIndex].Latitude == 0.0 && PlaceMarks[selectedIndex].Longitude == 0.0)
                {
                    PlaceMarks[selectedIndex].Latitude = currentLatitude;
                    PlaceMarks[selectedIndex].Longitude = currentLongitude;
                    PlaceMarks[selectedIndex].DateTime = DateTime.Now;
                    IsolatedStorageSettings.ApplicationSettings["PlaceMarks"] = PlaceMarks;
                    savingPlacemark = false;

                }
                else
                {
                    MessageBoxResult mb = MessageBox.Show("Вы уверены, что хотите сохранить место " + PlaceMarks[selectedIndex].FirstLine + "?", "Сохранить?", MessageBoxButton.OKCancel);
                    if (mb == MessageBoxResult.OK)
                    {
                        PlaceMarks[selectedIndex].Latitude = currentLatitude;
                        PlaceMarks[selectedIndex].Longitude = currentLongitude;
                        PlaceMarks[selectedIndex].DateTime = DateTime.Now;
                        IsolatedStorageSettings.ApplicationSettings["PlaceMarks"] = PlaceMarks;
                        savingPlacemark = false;

                    }
                }
                ambienceSound.Play();

            }
            else
            {
                if (PlaceMarks[selectedIndex].Latitude == 0.0 && PlaceMarks[selectedIndex].Longitude == 0.0)
                {
                    MessageBox.Show("Место не сохранено");
                    return;
                }
                MessageBoxResult mb = MessageBox.Show("Вы уверены, что хотите загрузить место " + PlaceMarks[selectedIndex].FirstLine + "?", "Загрузить?", MessageBoxButton.OKCancel);
                if (mb == MessageBoxResult.OK)
                {
                    targetLatitude = PlaceMarks[selectedIndex].Latitude;
                    targetLongitude = PlaceMarks[selectedIndex].Longitude;
                    IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
                    settings["longitude"] = targetLongitude;
                    settings["latitude"] = targetLatitude;
                    settings.Save();
                    MaintPivot.SelectedItem = Navigation;
                }
            }

        }

        private void saveLocationButton_Click(object sender, RoutedEventArgs e)
        {
            savingPlacemark = true;
            MaintPivot.SelectedItem = History;
        }

        private void renamePlacemark_Click(object sender, RoutedEventArgs e)
        {

            //ListBoxItem contextMenuListItem = FirstListBox.ItemContainerGenerator.ContainerFromItem((sender as ContextMenu).Tag) as ListBoxItem;
            MenuItem menuItem = (MenuItem)sender;
            renamingPlacemark = (menuItem.Tag as Placemark);
            //pm.Title = "lol";
            //MessageBox.Show(pm.FirstLine);
            PlacemarkNameEnterPanel.Visibility = System.Windows.Visibility.Visible;
            enterPlacemarkNameTextBox.Text = "";
            //enterPlacemarkNameTextBox.Focus();
        }

        private void cancelPlacemarkNameBox_Click(object sender, RoutedEventArgs e)
        {
            PlacemarkNameEnterPanel.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void submitPlacemarkNameButton_Click(object sender, RoutedEventArgs e)
        {
            string input = enterPlacemarkNameTextBox.Text;
            renamingPlacemark.Title = input.First().ToString().ToUpper() + String.Join("", input.Skip(1));
            PlacemarkNameEnterPanel.Visibility = System.Windows.Visibility.Collapsed;
            IsolatedStorageSettings.ApplicationSettings["PlaceMarks"] = PlaceMarks;
        }

        private void Image_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            MarketplaceDetailTask _marketPlaceDetailTask = new MarketplaceDetailTask();
            _marketPlaceDetailTask.ContentIdentifier = "e6b9e5ca-1d45-45f4-bf85-3727af9dc441";
            _marketPlaceDetailTask.Show();
        }
        
    }
}