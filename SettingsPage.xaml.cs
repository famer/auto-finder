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

using System.IO.IsolatedStorage;
using Microsoft.Phone.Tasks;

namespace Auto_finder
{
    public partial class SettingsPage : PhoneApplicationPage
    {
        bool useGeoLocation;

        private MarketplaceDetailTask _marketPlaceDetailTask = new MarketplaceDetailTask();
        MarketplaceReviewTask _marketplaceReviewTask = new MarketplaceReviewTask();
        public SettingsPage()
        {
            InitializeComponent();
            
            GPSonButton.Checked +=new RoutedEventHandler(GPSonButton_Checked);
            GPSonButton.Unchecked += new RoutedEventHandler(GPSonButton_Unchecked);
        }

        void GPSonButton_Unchecked(object sender, RoutedEventArgs e)
        {
            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
            settings["useGeoLocation"] = false;
            settings.Save();
            GPSonButton.Content = "Нет";
        }

        private void GPSonButton_Checked(object sender, RoutedEventArgs e)
        {
            
            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
            settings["useGeoLocation"] = true;
            settings.Save();
            GPSonButton.Content = "Да";

        }

        private void LayoutRoot_Loaded(object sender, RoutedEventArgs e)
        {
            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
            settings.TryGetValue("useGeoLocation", out useGeoLocation);
            if (useGeoLocation)
            {
                GPSonButton.IsChecked = true;
            }
            else
            {
                GPSonButton.IsChecked = false;
            }
        }
        private void buyButton_Click(object sender, RoutedEventArgs e)
        {
            _marketPlaceDetailTask.ContentIdentifier = "e6b9e5ca-1d45-45f4-bf85-3727af9dc441";
            _marketPlaceDetailTask.Show();
        }

        private void rateButton_Click(object sender, RoutedEventArgs e)
        {
            _marketplaceReviewTask.Show();
        }

        private void shareButton_Click(object sender, RoutedEventArgs e)
        {
            EmailComposeTask emailComposeTask = new EmailComposeTask();
            //emailComposeTask.To = "chris@example.com";
            //emailComposeTask.To = saveEmailAddressTask.Email;
            emailComposeTask.Subject = "Приложение Авто поиск для Windows Phone 7";
            emailComposeTask.Body = "Попробуй одно из лучших приложений для Windows Phone 7 http://windowsphone.com/s?appid=" + _marketPlaceDetailTask.ContentIdentifier; //_marketPlaceDetailTask.ContentIdentifier
            emailComposeTask.Show();
        }
    }
}