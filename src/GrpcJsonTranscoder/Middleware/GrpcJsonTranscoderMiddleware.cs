using Grpc.Core;
using GrpcJsonTranscoder.Grpc;
using GrpcJsonTranscoder.Internal.Grpc;
using GrpcJsonTranscoder.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrpcJsonTranscoder.Middleware
{
    public class GrpcJsonTranscoderMiddleware
    {
        private readonly RequestDelegate _next;

        public GrpcJsonTranscoderMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, GrpcAssemblyResolver grpcAssemblyResolver, IOptions<GrpcMapperOptions> options)
        {
            if (!context.Request.Headers.Any(h => h.Key.ToLowerInvariant() == "content-type" && h.Value == "application/grpc")) await _next(context);
            else
            {
                var path = context.Request.Path.Value;
                var methodDescriptor = grpcAssemblyResolver.FindMethodDescriptor(path.Split('/').Last().ToUpperInvariant());
                if (methodDescriptor == null) await _next(context);
                else
                {
                    string requestData;
                    if (context.Request.Method.ToLowerInvariant() == "get")
                    {
                        requestData = ParseGetJsonRequest(context);
                    }
                    else
                    {
                        requestData = await ParseOtherJsonRequest(context);
                    }

                    var grpcLookupTable = options.Value.GrpcMappers;
                    var grpcClient = grpcLookupTable.FirstOrDefault(x => x.GrpcMethod == path).GrpcHost; //todo: should catch object to throw exception

                    var channel = new Channel(grpcClient, ChannelCredentials.Insecure);
                    var client = new MethodDescriptorCaller(channel);

                    var requestObject = JsonConvert.DeserializeObject(requestData, methodDescriptor.InputType.ClrType);
                    var result = await client.InvokeAsync(methodDescriptor, GetRequestHeaders(context), requestObject);

                    await context.Response.WriteAsync(JsonConvert.SerializeObject(result));
                }
            }
        }

        private string ParseGetJsonRequest(HttpContext context)
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

        private async Task<string> ParseOtherJsonRequest(HttpContext context)
        {
            // ref at https://stackoverflow.com/questions/43403941/how-to-read-asp-net-core-response-body
            var encoding = context.Request.GetTypedHeaders().ContentType?.Encoding ?? Encoding.UTF8;
            var stream = new StreamReader(context.Request.Body, encoding);
            var json = await stream.ReadToEndAsync();
            return json == string.Empty ? "{}" : json;
        }

        private IDictionary<string, string> GetRequestHeaders(HttpContext context)
        {
            var headers = new Dictionary<string, string>();
            foreach (string key in context.Request.Headers.Keys)
            {
                if (key.StartsWith(":"))
                {
                    continue;
                }
                if (key.StartsWith("grpc-", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                else if (key.ToLowerInvariant() == "content-type" || key.ToLowerInvariant() == "authorization")
                {
                    //todo: investigate it more
                    var value = context.Request.Headers[key];
                    headers.Add(key, value.FirstOrDefault());
                }
            }

            return headers;
        }
    }
}
