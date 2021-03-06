﻿using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.HttpSys;
using Microsoft.Extensions.DependencyInjection;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Autofac;

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
}
