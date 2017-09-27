using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace TestIoT
{
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
}