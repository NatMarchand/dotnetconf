using System;
using System.Collections.Generic;
using System.Threading;
using Windows.UI.Xaml;
using Autofac;

namespace TestIoT
{
    public sealed partial class MainPage
    {
        public SensorRepository Repository { get; set; }

        public MainPage()
        {
            InitializeComponent();
            ((App) Application.Current).Container.InjectUnsetProperties(this);
        }
        
        private void OnAddSensorClick(object sender, RoutedEventArgs e)
        {
            Repository.Add(new Sensor());
        }
        
    }
}
