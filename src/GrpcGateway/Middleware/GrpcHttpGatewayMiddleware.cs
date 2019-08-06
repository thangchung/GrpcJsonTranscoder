using Google.Protobuf.Reflection;
using Grpc.Core;
using GrpcGateway.Grpc;
using GrpcShared;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Responses;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
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

        public async Task Invoke(DownstreamContext context)
        {
            var absolutePath = context.DownstreamRequest.AbsolutePath;
            var grpcClient = $"{context.DownstreamRequest.Host}:{context.DownstreamRequest.Port}";

            var methodDic = new ConcurrentDictionary<string, MethodDescriptor>();
            var assembly = typeof(Greeter.GreeterClient).Assembly;
            var types = assembly.GetTypes();
            var fileTypes = types.Where(type => type.Name.EndsWith("Reflection"));

            foreach (var type in fileTypes)
            {
                BindingFlags flag = BindingFlags.Static | BindingFlags.Public;
                var property = type.GetProperties(flag).Where(t => t.Name == "Descriptor").FirstOrDefault();
                if (property is null)
                    continue;

                if (!(property.GetValue(null) is FileDescriptor fileDescriptor))
                    continue;

                foreach (var svr in fileDescriptor.Services)
                {
                    var srvName = svr.FullName.ToUpper();
                    foreach (var method in svr.Methods)
                    {
                        methodDic.TryAdd(method.Name.ToUpper(), method);
                    }
                }
            }

            var methodCaller = methodDic.GetValueOrDefault(absolutePath.Split("/").Last().ToUpperInvariant());
            var channel = new Channel(grpcClient, ChannelCredentials.Insecure);
            var client = new MethodDescriptorClient(channel);
            var result = client.InvokeAsync(methodCaller, new Dictionary<string, string>(), new HelloRequest()); //todo: replace with dynamic

            OkResponse<GrpcHttpContent> httpResponse;
            httpResponse = new OkResponse<GrpcHttpContent>(new GrpcHttpContent(result));

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
