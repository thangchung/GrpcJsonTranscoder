using GrpcJsonTranscoder.Models;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OcelotGateway
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
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config
                        .SetBasePath(hostingContext.HostingEnvironment.ContentRootPath)
                        .AddJsonFile("appsettings.json")
                        .AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", true, true)
                        .AddJsonFile("ocelot.json")
                        .AddJsonFile($"ocelot.{hostingContext.HostingEnvironment.EnvironmentName}.json", true, true)
                        .AddEnvironmentVariables();
                })
                .ConfigureServices(services =>
                {
                    services.AddOcelot();
                })
                .Configure(app =>
                {
                    var configuration = new OcelotPipelineConfiguration
                    {
                        PreQueryStringBuilderMiddleware = async (ctx, next) =>
                        {
                            var routes = ctx.TemplatePlaceholderNameAndValues;
                            ctx.DownstreamRequest.Headers.Add(
                                "x-grpc-routes",
                                JsonConvert.SerializeObject(routes.Select(x => new NameAndValue { Name = x.Name, Value = x.Value })));
                            await next.Invoke();
                        }
                    };

                    app.UseOcelot(configuration).Wait();
                })
                .Build();
    }
}
