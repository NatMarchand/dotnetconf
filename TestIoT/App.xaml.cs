using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.HttpSys;
using Microsoft.Extensions.DependencyInjection;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace TestIoT
{
    sealed partial class App
    {
        private readonly IContainer _container;

        public ILifetimeScope Container => _container;

        public App()
        {
            InitializeComponent();
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterType<SensorRepository>().SingleInstance();
            _container = containerBuilder.Build();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            if (Window.Current.Content == null)
            {
                Window.Current.Content = new MainPage();
            }

            if (e.PrelaunchActivated == false)
            {
                Window.Current.Activate();
            }

            var webhost = BuildWebHost();
            webhost.RunAsync();
        }

        private IWebHost BuildWebHost()
        {
            return WebHost.CreateDefaultBuilder()
                .UseStartup<Startup>()
                .UseContentRoot(Windows.ApplicationModel.Package.Current.InstalledLocation.Path)
                .UseHttpSys(options =>
                {
                    options.Authentication.Schemes = AuthenticationSchemes.None;
                    options.Authentication.AllowAnonymous = true;
                    options.MaxConnections = 100;
                    options.MaxRequestBodySize = 30000000;
                    options.UrlPrefixes.Add("http://localhost:5000");
                })
                .ConfigureServices(s =>
                {
                    s.AddSingleton<IServiceProviderFactory<IServiceCollection>>(new AutofacServiceProviderFactory(_container));
                })
                .Build();
        }
    }


    public class AutofacServiceProviderFactory : IServiceProviderFactory<IServiceCollection>
    {
        private readonly IContainer _container;

        public AutofacServiceProviderFactory(IContainer container)
        {
            _container = container;
        }

        public IServiceCollection CreateBuilder(IServiceCollection services)
        {
            return services;
        }

        public IServiceProvider CreateServiceProvider(IServiceCollection services)
        {
            var cb = new ContainerBuilder();
            cb.Populate(services);
            cb.Update(_container);
            return new AutofacServiceProvider(_container);
        }
    }

    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();
            services.AddLogging();
            services.AddMvcCore()
                .AddControllersAsServices()
                .AddJsonFormatters(s =>
                {
                    s.ContractResolver = new CamelCasePropertyNamesContractResolver();
                    s.Converters.Add(new StringEnumConverter());
                });
        }

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddDebug();
            loggerFactory.AddConsole();

            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseWebSockets();
            app.UseMvcWithDefaultRoute();
        }
    }

    public class SensorViewModel
    {
        public Guid Id { get; set; }
        public double Value { get; set; }
    }

    public class Sensor
    {
        public Guid Id { get; set; }
        public IList<double> Values { get; set; }
    }

    public class SensorRepository
    {
        private static readonly Random Random = new Random();

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
            SensorUpdated?.Invoke(sensor);
        }

        public void UpdateRandomSensor()
        {
            if (_sensors.Any())
            {
                var s = _sensors[Random.Next(0, _sensors.Count)];
                s.Values.Add(s.Values.Last() + Random.NextDouble() - 0.5);
                SensorUpdated?.Invoke(s);
            }
        }
    }

    [Route("api/sensors")]
    public class MyController : ControllerBase
    {
        private readonly SensorRepository _sensorRepository;
        private readonly IOptions<MvcJsonOptions> _jsonOptions;

        public MyController(SensorRepository sensorRepository, IOptions<MvcJsonOptions> jsonOptions)
        {
            _sensorRepository = sensorRepository;
            _jsonOptions = jsonOptions;
        }

        [HttpGet]
        public IActionResult Get()
        {
            return Ok(_sensorRepository.GetAll().Select(s => s.Id));
        }

        [HttpGet("{id:guid}")]
        public IActionResult GetOne(Guid id)
        {
            var sensor = _sensorRepository.GetOne(id);
            if (sensor == null)
                return NotFound();

            return Ok(new
            {
                Id = sensor.Id,
                Value = sensor.Values.Last(),
                Max = sensor.Values.Max(),
                Min = sensor.Values.Min(),
                Avg = sensor.Values.Average(),
            });
        }

        [Route("stream")]
        public async Task<IActionResult> WebSocket()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                var websocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                async void OnSensorUpdated(Sensor sensor)
                {
                    if (!websocket.CloseStatus.HasValue)
                    {
                        var payload = new
                        {
                            Timestamp = DateTimeOffset.UtcNow,
                            Id = sensor.Id,
                            Value = sensor.Values.Last(),
                            Max = sensor.Values.Max(),
                            Min = sensor.Values.Min(),
                            Avg = sensor.Values.Average(),
                        };
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
