using System;
using System.Collections.Generic;
using System.Threading;

namespace TestIoT
{
    public class Sensor
    {
        private const int TabSize = 100;
        private static readonly Random Random = new Random();

        private readonly Timer _timer;
        private readonly CircularBuffer<double> _buffer;

        public Guid Id { get; }
        public DateTimeOffset LastUpdate { get; private set; }

        public event Action<Sensor> SensorUpdated;

        public Sensor()
        {
            Id = Guid.NewGuid();
            LastUpdate = DateTimeOffset.UtcNow;
            _buffer = new CircularBuffer<double>(TabSize);
            _buffer.AddLast(42);
            _timer = new Timer(OnTime, null, TimeSpan.FromSeconds(Random.Next(1, 5)), Timeout.InfiniteTimeSpan);
        }

        private void OnTime(object state)
        {
            var newValue = _buffer.Last + Random.NextDouble() - 0.5;
            _buffer.AddLast(newValue);
            LastUpdate = DateTimeOffset.UtcNow;
            SensorUpdated?.Invoke(this);

            _timer.Change(TimeSpan.FromSeconds(Random.Next(1, 5)), Timeout.InfiniteTimeSpan);
        }

        public IReadOnlyList<double> GetValues() => _buffer.GetValues();
    }

    public class CircularBuffer<T>
    {
        private readonly T[] _values;
        private int _tail;
        private int _head;
        private int _size;

        public T First => _size > 0 ? _values[_tail] : throw new InvalidOperationException();
        public T Last => _size > 0 ? _values[_head] : throw new InvalidOperationException();
        public int Count => _size;

        public CircularBuffer(int size)
        {
            _values = new T[size];
            _head = 0;
            _tail = 0;
            _size = 0;
        }

        public void AddLast(T value)
        {
            if (_size > 0)
            {
                _head = (_head + 1) % _values.Length;

                if (_head == _tail)
                {
                    _tail = (_tail + 1) % _values.Length;
                }
            }
            _values[_head] = value;
            _size = Math.Min(_size + 1, _values.Length);
        }

        public IReadOnlyList<T> GetValues()
        {
            var values = new T[_size];

            Array.Copy(_values, _tail, values, 0, _size - _tail);
            Array.Copy(_values, 0, values, _size - _tail, _tail);

            return values;
        }
    }
}