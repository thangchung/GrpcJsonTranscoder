using Google.Protobuf.Reflection;
using Grpc.Core;
using GrpcShared;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Responses;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace GrpcGateway.Middleware
{
    public class GrpcHttpGatewayMiddleware : OcelotMiddleware
    {
        private readonly OcelotRequestDelegate _next;

        public GrpcHttpGatewayMiddleware(OcelotRequestDelegate next, IOcelotLoggerFactory loggerFactory) 
            : base(loggerFactory.CreateLogger<GrpcHttpGatewayMiddleware>())
        {
            _next = next;
        }

        public async Task Invoke(DownstreamContext context, IServiceProvider serviceProvider)
        {
            var host = context.DownstreamRequest.Host;
            var port = context.DownstreamRequest.Port;
            var absolutePath = context.DownstreamRequest.AbsolutePath;

            var grpcClient = $"{host}:{port}";

            var myServices = new List<MyServiceInfo>();

            using (var stream = typeof(Greeter.GreeterClient).Assembly.GetManifestResourceStream("GrpcShared.greet.pb"))
            {
                var fileDescriptorSet = Serializer.Deserialize<FileDescriptorSet>(stream);
                var fileDescriptorProtos = fileDescriptorSet.Files.Where(f => f.Package == "Greet");

                foreach (var fileDescriptorProto in fileDescriptorProtos)
                {
                    MyServiceInfo myService;

                    foreach (var service in fileDescriptorProto.Services)
                    {
                        myService = new MyServiceInfo
                        {
                            Name = $"{fileDescriptorProto.Package}.{service.Name}",
                            CSharpNameSpace = fileDescriptorProto.Options.CsharpNamespace,
                            ServiceType = Type.GetType($"{fileDescriptorProto.Options.CsharpNamespace}.{service.Name}, {fileDescriptorProto.Options.CsharpNamespace}")
                        };

                        MyMethodInfo myMethod;

                        foreach (var method in service.Methods)
                        {
                            myMethod = new MyMethodInfo
                            {
                                Name = method.Name,
                                AbsolutePath = $"/{myService.Name}/{method.Name}",
                                InputType = Type.GetType($"{myService.CSharpNameSpace}.{method.InputType.Split(".").Last()}, {myService.CSharpNameSpace}"),
                                OutputType = Type.GetType($"{myService.CSharpNameSpace}.{method.OutputType.Split(".").Last()}, {myService.CSharpNameSpace}")
                            };

                            myService.Methods.Add(myMethod);
                        }

                        myServices.Add(myService);
                    }
                }

                var channel = new Channel(grpcClient, ChannelCredentials.Insecure);
                /*var client = typeof(TService)
                    .GetConstructor(new[] { typeof(Channel) })
                    .Invoke(new object[] { channel });*/
            }

            OkResponse<GrpcHttpContent> httpResponse;
            httpResponse = new OkResponse<GrpcHttpContent>(new GrpcHttpContent(myServices));

            context.HttpContext.Response.ContentType = "application/json";
            context.DownstreamResponse = new DownstreamResponse(httpResponse.Data, HttpStatusCode.OK, httpResponse.Data.Headers, "GrpcHttpGatewayMiddleware");
        }
    }

    public class MyServiceInfo
    {
        public string Name { get; set; }
        public Type ServiceType { get; set; }
        public string CSharpNameSpace { get; set; }
        public List<MyMethodInfo> Methods { get; set; } = new List<MyMethodInfo>();
    }

    public class MyMethodInfo
    {
        public string AbsolutePath { get; set; }
        public string Name { get; set; }
        public Type InputType { get; set; }
        public Type OutputType { get; set; }
    }

    public class GrpcHttpContent : HttpContent
    {
        private string result;

        public GrpcHttpContent(string result)
        {
            this.result = result;
        }

        public GrpcHttpContent(object result)
        {
            this.result = Newtonsoft.Json.JsonConvert.SerializeObject(result);
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            var writer = new StreamWriter(stream);
            await writer.WriteAsync(result);
            await writer.FlushAsync();
        }

        protected override bool TryComputeLength(out long length)
        {
            length = result.Length;
            return true;
        }
    }
}
