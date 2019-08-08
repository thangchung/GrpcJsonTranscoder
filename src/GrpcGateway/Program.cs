using System;
using System.IO;
using System.Threading.Tasks;
using GrpcGateway.Extensions;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

namespace GrpcGateway
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            await BuildWebHost(args).RunAsync();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseKestrel()
                .UseUrls("http://localhost:5000")
                .ConfigureKestrel(o => {
                    o.AllowSynchronousIO = false;
                })
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config
                        .SetBasePath(hostingContext.HostingEnvironment.ContentRootPath)
                        .AddJsonFile("appsettings.json", true, true)
                        .AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", true, true)
                        .AddJsonFile("ocelot.json", false, false)
                        .AddEnvironmentVariables();
                })
                .ConfigureServices(s =>
                {
                    AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                    //s.AddHttpContextAccessor();
                    //s.AddScoped<IHttpContextAccessor, HttpContextAccessor>();
                    s.AddOcelot();
                })
                .Configure(a =>
                {
                    a.UseOcelot(config => config.AddGrpcHttpGateway());
                })
                .Build();
    }
}
