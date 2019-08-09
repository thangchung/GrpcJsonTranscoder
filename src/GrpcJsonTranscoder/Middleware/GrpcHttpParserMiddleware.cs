using Google.Protobuf.Reflection;
using Grpc.Core;
using GrpcJsonTranscoder.Grpc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GrpcJsonTranscoder.Middleware
{
    public class GrpcHttpParserMiddleware
    {
        private readonly RequestDelegate _next;
        public GrpcHttpParserMiddleware(RequestDelegate next)
        {
            _next = next;
            //AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        }

        public async Task Invoke(HttpContext context, GrpcAssemblyResolver grpcAssemblyResolver, IOptions<GrpcMapperOptions> options)
        {
            if (!context.Request.Headers.Any(h => h.Key == "x-grpc-request" && Convert.ToBoolean(h.Value) == true)) await _next(context);
            else
            {
                var path = context.Request.Path.Value;
                var methodDescriptors = GetMethodDescriptors(grpcAssemblyResolver.GetGrpcAssemblies().ToArray()); //todo: cache it

                methodDescriptors.TryGetValue(path.Split('/').Last().ToUpperInvariant(), out MethodDescriptor methodDescriptor);
                if (methodDescriptor == null) await _next(context);
                else
                {
                    string requestData;
                    if (context.Request.Method.ToLowerInvariant() == "get")
                    {
                        requestData = GetRequestJson(context);
                    }
                    else
                    {
                        requestData = await OtherRequestJson(context);
                    }

                    var grpcLookupTable = options.Value.GrpcMappers;

                    object requestObject = JsonConvert.DeserializeObject(requestData, methodDescriptor.InputType.ClrType);

                    var grpcClient = grpcLookupTable.FirstOrDefault(x => x.GrpcMethod == path).GrpcHost; //todo: should catch object to throw exception

                    var channel = new Channel(grpcClient, ChannelCredentials.Insecure);
                    var client = new MethodDescriptorCaller(channel);

                    var result = await client.InvokeAsync(methodDescriptor, new Dictionary<string, string>(), requestObject);

                    await context.Response.WriteAsync(JsonConvert.SerializeObject(result));
                }
            }
        }

        private string GetRequestJson(HttpContext context)
        {
            var o = new JObject();

            if (context.Request.Headers.ContainsKey("x-grpc-routes"))
            {
                // route data
                var nameValues = JsonConvert.DeserializeObject<List<NameAndValue>>(context.Request.Headers["x-grpc-routes"]); // work with ocelot
                foreach (var nameValue in nameValues)
                {
                    o.Add(nameValue.Name.Replace("{", "").Replace("}", ""), nameValue.Value);
                }
            }

            // query string
            foreach (var q in context.Request.Query)
            {
                o.Add(q.Key, q.Value.ToString());
            }
            return JsonConvert.SerializeObject(o);
        }

        private async Task<string> OtherRequestJson(HttpContext context)
        {
            // ref at https://stackoverflow.com/questions/43403941/how-to-read-asp-net-core-response-body
            var encoding = context.Request.GetTypedHeaders().ContentType?.Encoding ?? Encoding.UTF8;
            var stream = new StreamReader(context.Request.Body, encoding);
            var json = await stream.ReadToEndAsync();
            return json == string.Empty ? "{}" : json;
        }

        private ConcurrentDictionary<string, MethodDescriptor> GetMethodDescriptors(params Assembly[] assemblies)
        {
            var methodDic = new ConcurrentDictionary<string, MethodDescriptor>();
            var assembly = assemblies.FirstOrDefault(); //todo: loop to get all types
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

            return methodDic;
        }
    }

    public class NameAndValue
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class GrpcMapperOptions
    {
        public List<GrpcLookup> GrpcMappers { get; set; }
    }

    public class GrpcLookup
    {
        public string GrpcMethod { get; set; }
        public string GrpcHost { get; set; }
    }
}
