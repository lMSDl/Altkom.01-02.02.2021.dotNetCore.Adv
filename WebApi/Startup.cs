using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Models;
using Models.Fakers;
using Services;
using Services.Interfaces;
using WebApi.Controllers;
using Newtonsoft.Json;
using WebApi.Hubs;
using Microsoft.AspNetCore.ResponseCompression;

namespace WebApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddResponseCompression(options => {
                options.EnableForHttps = true;
                options.Providers.Add<GzipCompressionProvider>();       
            });
            
            services.Configure<GzipCompressionProviderOptions>( options => options.Level = System.IO.Compression.CompressionLevel.Optimal);

            services.AddSingleton<PersonFaker>()
                    .AddSingleton<IService<Person>>(x => new Service<Person>(x.GetService<PersonFaker>(), 25));

            services.AddSignalR();
            services.AddControllers()
            .AddNewtonsoftJson();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "WebApi", Version = "v1" });
            });

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "WebApi v1"));
            }

            //app.UseHttpsRedirection();
            app.UseResponseCompression();
            app.UseRouting();
            
            app.UseAuthorization();

            app.Use(async (context, next) => {
                    System.Console.WriteLine(context.GetEndpoint()?.DisplayName ?? "none");
                    await next.Invoke();
            });

            app.Use(async (context, next) => {
                    if(context.GetEndpoint()?.DisplayName.Contains(typeof(WeatherForecastController).FullName) ?? false)
                        {
                            //logika
                            context.Items["Days"] = 4;
                        }
                    await next.Invoke();
            });

            app.Map("/noEndpoint", appbuilder => appbuilder.Run(context => context.Response.WriteAsync("Hello")));

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/endpoint", context => context.Response.WriteAsync("Get Endpoint"));

                endpoints.MapHub<PeopleHub>("signalR/People");

                endpoints.MapControllers();
            });
        }
    }
}
