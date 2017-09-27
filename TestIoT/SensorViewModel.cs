using System;
using System.Linq;

namespace TestIoT
{
    public class SensorViewModel
    {
        public Guid Id { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public double Value { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
        public double Avg { get; set; }

        public static explicit operator SensorViewModel(Sensor sensor)
        {
            if (sensor == null)
                return null;

            var values = sensor.GetValues();
            return new SensorViewModel
            {
                Id = sensor.Id,
                Timestamp = sensor.LastUpdate,
                Value = values.Last(),
                Min = values.Min(),
                Max = values.Max(),
                Avg = values.Average()
            };
        }
    }
}