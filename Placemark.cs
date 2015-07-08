using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.ComponentModel;

namespace Auto_finder
{
    //[Serializable]
    public class Placemark : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public bool Persist { get; set; }
        private string _Title;
        public string Title
        {
            get
            {
                return _Title;
            }
            set
            {
                _Title = value;
                OnPropertyChanged("FirstLine");
            }
        }
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        private DateTime _DateTime;
        public DateTime DateTime
        {
            get
            {
                return _DateTime;
            }

            set
            {
                _DateTime = value;
                OnPropertyChanged("SecondLine");
            }
        }

        private void OnPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
        public string Coordinates
        {
            get
            {
                return Longitude.ToString() + "; " + Latitude.ToString();
            }
        }

        public string FirstLine
        {
            get
            {
                return Persist ? Title : DateTime.ToString();
            }
        }

        public string SecondLine
        {
            get
            {
                return Persist ? (DateTime != DateTime.MinValue ? DateTime.ToString() : "") : "последние сохранения";
            }
        }
    }
}
