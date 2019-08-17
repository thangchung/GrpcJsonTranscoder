using Grpc.Core;
using GrpcJsonTranscoder.Grpc;
using GrpcJsonTranscoder.Internal.Grpc;
using GrpcJsonTranscoder.Internal.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Ocelot.Middleware;
using Ocelot.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace GrpcJsonTranscoder
{
    public static class DownStreamContextExtensions
    {
        public static async Task HandleGrpcRequestAsync(this DownstreamContext context, Func<Task> next)
        {
            // ignore if the request is not a gRPC content type
            if (!context.HttpContext.Request.Headers.Any(h => h.Key.ToLowerInvariant() == "content-type" && h.Value == "application/grpc"))
            {
                await next.Invoke();
            }
            else
            {
                var methodPath = context.DownstreamReRoute.DownstreamPathTemplate.Value;
                var grpcAssemblyResolver = context.HttpContext.RequestServices.GetService<GrpcAssemblyResolver>();
                var methodDescriptor = grpcAssemblyResolver.FindMethodDescriptor(methodPath.Split('/').Last().ToUpperInvariant());

                if (methodDescriptor == null)
                {
                    await next.Invoke();
                }
                else
                {
                    string requestData;
                    var upstreamHeaders = new Dictionary<string, string>
                            {
                                { "x-grpc-route-data", JsonConvert.SerializeObject(context.TemplatePlaceholderNameAndValues.Select(x => new {x.Name, x.Value})) },
                                { "x-grpc-body-data", await context.DownstreamRequest.Content.ReadAsStringAsync() }
                            };
                    if (context.HttpContext.Request.Method.ToLowerInvariant() == "get")
                    {
                        requestData = context.HttpContext.ParseGetJsonRequest(upstreamHeaders);
                    }
                    else
                    {
                        requestData = context.HttpContext.ParseOtherJsonRequest(upstreamHeaders);
                    }

                    // todo: only get the first one, currently we use service mesh to LB the downstream gRPC services
                    // but we open to support it with manually LB such as Consult 
                    var downstreamAddress = context.DownstreamReRoute.DownstreamAddresses.FirstOrDefault();
                    var downstreamHost = $"{downstreamAddress.Host}:{downstreamAddress.Port}";

                    var channel = new Channel(downstreamHost, ChannelCredentials.Insecure); //todo: handle TLS with certs later
                    var client = new MethodDescriptorCaller(channel);

                    var requestObject = JsonConvert.DeserializeObject(requestData, methodDescriptor.InputType.ClrType);
                    var result = await client.InvokeAsync(methodDescriptor, context.HttpContext.GetRequestHeaders(), requestObject);
                    var response = new OkResponse<GrpcHttpContent>(new GrpcHttpContent(JsonConvert.SerializeObject(result)));
                    var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = response.Data
                    };

                    context.HttpContext.Response.ContentType = "application/json";
                    context.DownstreamResponse = new DownstreamResponse(httpResponseMessage);
                }
            }
        }
    }
}
