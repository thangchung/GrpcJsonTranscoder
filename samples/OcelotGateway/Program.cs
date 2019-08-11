using GrpcJsonTranscoder.Models;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
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
                //.UseUrls("http://localhost:5000")
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config
                        .SetBasePath(hostingContext.HostingEnvironment.ContentRootPath)
                        .AddJsonFile("appsettings.json", true, true)
                        .AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", true, true)
                        .AddJsonFile("ocelot.json", false, false)
                        .AddJsonFile($"configuration.{hostingContext.HostingEnvironment.EnvironmentName}.json")
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
