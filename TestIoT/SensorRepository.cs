using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace TestIoT
{
    public class SensorRepository
    {
        private readonly IList<Sensor> _sensors;

        public event Action<Sensor> SensorUpdated;

        public SensorRepository()
        {
            _sensors = new List<Sensor>();
        }

        public IReadOnlyList<Sensor> GetAll()
        {
            return _sensors.ToImmutableList();
        }

        public Sensor GetOne(Guid id)
        {
            return _sensors.FirstOrDefault(p => p.Id == id);
        }

        public void Add(Sensor sensor)
        {
            _sensors.Add(sensor);
            sensor.SensorUpdated += OnSensorUpdated;
            SensorUpdated?.Invoke(sensor);
        }

        private void OnSensorUpdated(Sensor sensor)
        {
            SensorUpdated?.Invoke(sensor);
        }
    }
}