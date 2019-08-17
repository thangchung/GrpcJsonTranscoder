using Grpc.Core;
using GrpcJsonTranscoder.Grpc;
using GrpcJsonTranscoder.Internal;
using GrpcJsonTranscoder.Internal.Grpc;
using GrpcJsonTranscoder.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Ocelot.Middleware;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GrpcJsonTranscoder
{
    public static class DownStreamContextExtensions
    {
        public static async Task HandleGrpcRequestAsync(this DownstreamContext context, Func<Task> next)
        {
            // ignore if the request is not a gRPC protocol
            if (!context.HttpContext.Request.Headers.Any(h => h.Key.ToLowerInvariant() == "content-type" && h.Value == "application/grpc"))
            {
                await next.Invoke();
            }
            else
            {
                var httpContext = context.HttpContext;
                var upstreamHeaders = new Dictionary<string, string>
                            {
                                { "x-grpc-route-data", JsonConvert.SerializeObject(context.TemplatePlaceholderNameAndValues.Select(x => new NameAndValue { Name = x.Name, Value = x.Value })) },
                                { "x-grpc-body-data", await context.DownstreamRequest.Content.ReadAsStringAsync() }
                            };

                var methodPath = context.DownstreamReRoute.DownstreamPathTemplate.Value;
                var downstreamAddress = context.DownstreamReRoute.DownstreamAddresses.FirstOrDefault(); // only get the first one, currently we use Kubernetes
                var downstreamHost = $"{downstreamAddress.Host}:{downstreamAddress.Port}";

                var grpcAssemblyResolver = httpContext.RequestServices.GetService<GrpcAssemblyResolver>();
                var methodDescriptor = grpcAssemblyResolver.FindMethodDescriptor(methodPath.Split('/').Last().ToUpperInvariant());
                if (methodDescriptor == null) await next.Invoke();
                else
                {
                    string requestData;
                    if (httpContext.Request.Method.ToLowerInvariant() == "get")
                    {
                        requestData = httpContext.ParseGetJsonRequest(upstreamHeaders);
                    }
                    else
                    {
                        requestData = httpContext.ParseOtherJsonRequest(upstreamHeaders);
                    }

                    var channel = new Channel(downstreamHost, ChannelCredentials.Insecure);
                    var client = new MethodDescriptorCaller(channel);

                    var requestObject = JsonConvert.DeserializeObject(requestData, methodDescriptor.InputType.ClrType);
                    var result = await client.InvokeAsync(methodDescriptor, httpContext.GetRequestHeaders(), requestObject);

                    await httpContext.Response.WriteAsync(JsonConvert.SerializeObject(result));
                }
            }
        }
    }
}
