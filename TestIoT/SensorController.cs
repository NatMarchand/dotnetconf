using System;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace TestIoT
{
    [Route("api/sensors")]
    public class SensorController : ControllerBase
    {
        private readonly SensorRepository _sensorRepository;
        private readonly IOptions<MvcJsonOptions> _jsonOptions;

        public SensorController(SensorRepository sensorRepository, IOptions<MvcJsonOptions> jsonOptions)
        {
            _sensorRepository = sensorRepository;
            _jsonOptions = jsonOptions;
        }

        [HttpGet]
        public IActionResult Get()
        {
            return Ok(_sensorRepository.GetAll().Select(s => s.Id));
        }

        [HttpGet("{id:guid}", Name = nameof(GetOne))]
        public IActionResult GetOne(Guid id)
        {
            var sensor = _sensorRepository.GetOne(id);
            if (sensor == null)
                return NotFound();

            return Ok((SensorViewModel)sensor);
        }

        [HttpPost]
        public IActionResult Create()
        {
            var sensor = new Sensor();
            _sensorRepository.Add(sensor);
            return CreatedAtRoute(nameof(GetOne), new { id = sensor.Id }, (SensorViewModel)sensor);
        }

        [Route("stream")]
        [HttpGet]
        public async Task<IActionResult> WebSocket()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                var websocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                async void OnSensorUpdated(Sensor sensor)
                {
                    if (!websocket.CloseStatus.HasValue)
                    {
                        var payload = (SensorViewModel)sensor;
                        await websocket.SendAsync(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload, _jsonOptions.Value.SerializerSettings)), WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                }

                _sensorRepository.SensorUpdated += OnSensorUpdated;
                while (!websocket.CloseStatus.HasValue)
                {
                    await Task.Delay(10);
                }
                _sensorRepository.SensorUpdated -= OnSensorUpdated;
                await websocket.CloseAsync(websocket.CloseStatus.Value, websocket.CloseStatusDescription, CancellationToken.None);
                return NoContent();
            }
            else
            {
                return BadRequest();
            }
        }
    }
}