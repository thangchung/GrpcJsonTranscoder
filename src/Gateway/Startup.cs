using System.IO;
using System.Threading.Tasks;
using Google.Protobuf.Reflection;
using GrpcShared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ReflectionMagic;

namespace Gateway
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMiddleware<ProxyMiddleware>();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Hello World!");
                });
            });
        }
    }

    public class ProxyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ProxyMiddleware> _logger;

        public ProxyMiddleware(RequestDelegate next, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ProxyMiddleware>();
            _next = next;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            var protobufAssembly = typeof(FileDescriptor).Assembly;
            var fileDescriptorSet = protobufAssembly.GetType("Google.Protobuf.Reflection.FileDescriptorSet");
            var parserGetter = fileDescriptorSet.GetProperty("Parser");
            var messageParser = parserGetter.GetValue(fileDescriptorSet);

            using var stream = typeof(Greeter.GreeterClient).Assembly.GetManifestResourceStream("GrpcShared.greet.pb");
            var cc = messageParser.AsDynamic().ParseFrom(ReadStream(stream));
        }

        private static byte[] ReadStream(Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }
    }
}
