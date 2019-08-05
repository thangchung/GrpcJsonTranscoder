using Google.Protobuf.Reflection;
using GrpcShared;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ProtoBuf;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gateway.Middleware
{
    public class GrpcParserMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GrpcParserMiddleware> _logger;

        public GrpcParserMiddleware(RequestDelegate next, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<GrpcParserMiddleware>();
            _next = next;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            using var stream = typeof(Greeter.GreeterClient).Assembly.GetManifestResourceStream("GrpcShared.greet.pb");

            var fileDescriptorSet = Serializer.Deserialize<FileDescriptorSet>(stream);

            var myServices = new List<MyServiceInfo>();

            foreach (var fileDescriptorProto in fileDescriptorSet.Files)
            {
                MyServiceInfo myService;

                foreach (var service in fileDescriptorProto.Services)
                {
                    myService = new MyServiceInfo
                    {
                        Name = service.Name
                    };

                    MyMethodInfo myMethod;

                    foreach (var method in service.Methods)
                    {
                        myMethod = new MyMethodInfo
                        {
                            Name = method.Name,
                            InputType = method.InputType[1..],
                            OutputType = method.OutputType[1..]
                        };

                        myService.Methods.Add(myMethod);
                    }

                    myServices.Add(myService);
                }
            }

            var aa = myServices;

            await _next(httpContext);
        }
    }

    public class MyServiceInfo
    {
        public string Name { get; set; }
        public List<MyMethodInfo> Methods { get; set; } = new List<MyMethodInfo>();
    }

    public class MyMethodInfo
    {
        public string Name { get; set; }
        public string InputType { get; set; }
        public string OutputType { get; set; }
    }
}
