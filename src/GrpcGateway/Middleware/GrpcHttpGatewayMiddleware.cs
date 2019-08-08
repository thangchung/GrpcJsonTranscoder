using Google.Protobuf.Reflection;
using Grpc.Core;
using GrpcGateway.Grpc;
using GrpcShared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Responses;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.AspNetCore.WebUtilities;

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
                var flags = BindingFlags.Static | BindingFlags.Public;
                var property = type.GetProperties(flags).Where(t => t.Name == "Descriptor").FirstOrDefault();

                if (property is null) continue;
                if (!(property.GetValue(null) is FileDescriptor fileDescriptor)) continue;

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
            context.DownstreamRequest.Scheme = "http";
            var body = JsonConvert.DeserializeObject(await GetRequestJson(context), methodCaller.InputType.ClrType);
            context.DownstreamRequest.Scheme = "grpc";

            var channel = new Channel(grpcClient, ChannelCredentials.Insecure);
            var client = new MethodDescriptorClient(channel);
            var result = await client.InvokeAsync(methodCaller, new Dictionary<string, string>(), body); //todo: replace with dynamic
            var httpResponse = new OkResponse<GrpcHttpContent>(new GrpcHttpContent(result));

            context.HttpContext.Response.ContentType = "application/json";
            context.DownstreamResponse = new DownstreamResponse(httpResponse.Data, HttpStatusCode.OK, httpResponse.Data.Headers, "GrpcHttpGatewayMiddleware");
        }

        private async Task<string> GetRequestJson(DownstreamContext context)
        {
            if (context.HttpContext.Request.Method == "GET")
            {
                var o = new JObject();

                // route data
                var nameValues = context.TemplatePlaceholderNameAndValues;
                foreach (var nameValue in nameValues)
                {
                    o.Add(nameValue.Name.Replace("{", "").Replace("}", ""), nameValue.Value);
                }

                // query string
                foreach (var q in context.HttpContext.Request.Query)
                {
                    o.Add(q.Key, q.Value.ToString());
                }
                return JsonConvert.SerializeObject(o);
            }
            else
            {
                var json = string.Empty;
                var encoding = context.HttpContext.Request.GetTypedHeaders().ContentType?.Encoding ?? Encoding.UTF8;
                //var httpRequest = context.HttpContext.Request.EnableRewind();
                //context.HttpContext.Request.EnableBuffering();
                //context.HttpContext.Request.Body.Position = 0;
                using (var bb = new StreamReader(context.HttpContext.Request.Body, encoding)) {
                    var cc = await bb.ReadToEndAsync();
                }

                //httpRequest.Body.Seek(0, SeekOrigin.Begin);
                /*using (var sr = new StreamReader(httpRequest.Body, encoding))
                {
                    json = sr.ReadToEnd();
                }*/

                /*var aa = context.HttpContext.Request.ContentLength;
                using (var stream = new MemoryStream())
                {
                    // make sure that body is read from the beginning
                    context.HttpContext.Request.Body.Seek(0, SeekOrigin.Begin);
                    context.HttpContext.Request.Body.CopyTo(stream);
                    var requestBody = encoding.GetString(stream.ToArray());

                    // this is required, otherwise model binding will return null
                    context.HttpContext.Request.Body.Seek(0, SeekOrigin.Begin);
                }*/

                /*var copy = new MemoryStream();

                context.HttpContext.Request.Body.CopyTo(copy);
                context.HttpContext.Request.Body = copy;
                context.HttpContext.Response.RegisterForDispose(copy);
                context.HttpContext.Request.Body.Position = 0;

                using (var reader = new StreamReader(
                    stream: context.HttpContext.Request.Body,
                    encoding: encoding,
                    detectEncodingFromByteOrderMarks: true,
                    bufferSize: 128,
                    leaveOpen: true))
                {
                    var content = reader.ReadToEnd();
                }*/

                return json == string.Empty ? "{}" : json;
            }
        }
    }

    /*public class MyServiceInfo
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
    }*/

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
