using System;
using System.Collections.Generic;
using System.Threading;
using Windows.UI.Xaml;
using Autofac;

namespace TestIoT
{
    public sealed partial class MainPage
    {
        private readonly Timer _timer;
        public SensorRepository Repository { get; set; }

        public MainPage()
        {
            InitializeComponent();
            ((App) Application.Current).Container.InjectUnsetProperties(this);
            _timer = new Timer(OnTimerCallback);
            _timer.Change(0, 100);
        }

        private void OnTimerCallback(object state)
        {
            Repository.UpdateRandomSensor();
        }

        private void OnAddSensorClick(object sender, RoutedEventArgs e)
        {
            Repository.Add(new Sensor
            {
                Id = Guid.NewGuid(),
                Values = new List<double> { 42 }
            });
        }
        
    }
}
