using System;
using Grpc.Core;
using GrpcGateway.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

namespace GrpcGateway
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            //services.AddOcelot(Configuration);
            /*services.Scan(s =>
                s.FromCallingAssembly()
                    .AddClasses(c => c.AssignableTo(typeof(ClientBase<>)))
                    .AsImplementedInterfaces()
                    .WithScopedLifetime());*/
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            /*app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Hello World!");
            });*/
            /*app.UseOcelot(config =>
            {
                config.AddGrpcHttpGateway();
            }).Wait();*/
        }
    }
}
