using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Swashbuckle.AspNetCore.Swagger;

namespace TestIoT
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();
            services.AddMvcCore()
                .AddControllersAsServices()
                .AddJsonFormatters(s =>
                {
                    s.ContractResolver = new CamelCasePropertyNamesContractResolver();
                    s.Converters.Add(new StringEnumConverter());
                })
                .AddApiExplorer();
            services.AddSwaggerGen(o =>
            {
                o.SwaggerDoc("v1", new Info { Title = "My API", Version = "v1" });
            });
        }

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseSwagger();
            app.UseSwaggerUI(o =>
            {
                o.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
            });
            app.UseWebSockets();
            app.UseMvcWithDefaultRoute();
        }
    }
}