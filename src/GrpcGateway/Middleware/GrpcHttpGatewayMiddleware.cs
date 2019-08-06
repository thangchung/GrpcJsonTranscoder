using Google.Protobuf.Reflection;
using Grpc.Core;
using Grpc.Reflection.V1Alpha;
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
using static Grpc.Reflection.V1Alpha.ServerReflection;

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

            var myServices = new Dictionary<string, MyServiceInfo>();

            using (var stream = typeof(Greeter.GreeterClient).Assembly.GetManifestResourceStream("GrpcShared.greet.pb"))
            {
                var fileDescriptorSet = Serializer.Deserialize<FileDescriptorSet>(stream);
                var fileDescriptorProtos = fileDescriptorSet.Files.Where(f => f.Package == "Greet");

                //new ServerReflectionRequest().

                foreach (var fileDescriptorProto in fileDescriptorProtos)
                {
                    foreach (var service in fileDescriptorProto.Services)
                    {
                        foreach (var method in service.Methods)
                        {
                            MyServiceInfo myService;

                            myService = new MyServiceInfo
                            {
                                Name = $"{fileDescriptorProto.Package}.{service.Name}",
                                CSharpNameSpace = fileDescriptorProto.Options.CsharpNamespace,
                                ServiceType = Type.GetType($"{fileDescriptorProto.Options.CsharpNamespace}.{service.Name}, {fileDescriptorProto.Options.CsharpNamespace}")
                            };

                            MyMethodInfo myMethod;

                            myMethod = new MyMethodInfo
                            {
                                Name = method.Name,
                                AbsolutePath = $"/{myService.Name}/{method.Name}",
                                InputType = Type.GetType($"{myService.CSharpNameSpace}.{method.InputType.Split(".").Last()}, {myService.CSharpNameSpace}"),
                                OutputType = Type.GetType($"{myService.CSharpNameSpace}.{method.OutputType.Split(".").Last()}, {myService.CSharpNameSpace}")
                            };

                           myService.Methods.Add(myMethod);
                           myServices.Add($"/{myService.Name}/{method.Name}", myService);
                        }
                    }
                }

                var calledService = myServices.GetValueOrDefault(absolutePath);

                var channel = new Channel(grpcClient, ChannelCredentials.Insecure);
                var client = new ServerReflectionClient(channel);
                var response = await SingleRequestAsync(client, new ServerReflectionRequest
                {
                    ListServices = "" // Get all services
                });

                /*var client = calledService.ServiceType
                    .GetConstructor(new[] { typeof(Channel) })
                    .Invoke(new object[] { channel });

                client.GetType().InvokeMember(
                    calledService.Methods[0].Name,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod,
                    null,
                    client,
                    new[] { new HelloRequest() });*/
            }

            OkResponse<GrpcHttpContent> httpResponse;
            httpResponse = new OkResponse<GrpcHttpContent>(new GrpcHttpContent(myServices));

            context.HttpContext.Response.ContentType = "application/json";
            context.DownstreamResponse = new DownstreamResponse(httpResponse.Data, HttpStatusCode.OK, httpResponse.Data.Headers, "GrpcHttpGatewayMiddleware");
        }

        private static async Task<ServerReflectionResponse> SingleRequestAsync(ServerReflectionClient client, ServerReflectionRequest request)
        {
            var call = client.ServerReflectionInfo();
            await call.RequestStream.WriteAsync(request);
            var response = call.ResponseStream.Current;
            await call.RequestStream.CompleteAsync();
            return response;
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
