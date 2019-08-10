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
            if (!context.Request.Query.Any(q => q.Key == "grpc" && Convert.ToBoolean(q.Value) == true)) await _next(context);
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

                    var requestObject = JsonConvert.DeserializeObject(requestData, methodDescriptor.InputType.ClrType);

                    var grpcClient = grpcLookupTable.FirstOrDefault(x => x.GrpcMethod == path).GrpcHost; //todo: should catch object to throw exception

                    var channel = new Channel(grpcClient, ChannelCredentials.Insecure);

                    var client = new MethodDescriptorCaller(channel);

                    var result = await client.InvokeAsync(methodDescriptor, new Dictionary<string, string>(), requestObject);

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
    }
}
