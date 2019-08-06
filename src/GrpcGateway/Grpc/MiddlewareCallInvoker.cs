using Grpc.Core;
using System;
using System.Threading.Tasks;

namespace GrpcGateway.Grpc
{
    public sealed class MiddlewareCallInvoker : DefaultCallInvoker
    {
        private readonly Channel grpcChannel;

        /// <summary>
        /// Middleware pipeline to be executed on every server request.
        /// </summary>
        private Pipeline MiddlewarePipeline { get; set; }

        public MiddlewareCallInvoker(Channel channel) : base(channel)
        {
            this.grpcChannel = channel;
        }

        public MiddlewareCallInvoker(Channel channel, Pipeline pipeline) : this(channel)
        {
            this.MiddlewarePipeline = pipeline;
        }

        private TResponse Call<TResponse>(Func<MiddlewareContext, TResponse> call, MiddlewareContext context)
        {
            TResponse response = default(TResponse);
            if (MiddlewarePipeline != null)
            {
                context.HandlerExecutor = async () =>
                {
                    response = await Task.FromResult(call(context));
                    context.Response = response;
                };
                MiddlewarePipeline.RunPipeline(context).ConfigureAwait(false);
            }
            else
            {
                response = call(context);
            }
            return response;
        }

        public override TResponse BlockingUnaryCall<TRequest, TResponse>(Method<TRequest, TResponse> method,
            string host, CallOptions options, TRequest request)
        {
            return Call((context) => base.BlockingUnaryCall((Method<TRequest, TResponse>)context.Method, context.Host, context.Options, (TRequest)context.Request), new MiddlewareContext
            {
                Host = host,
                Method = method,
                Options = options,
                Request = request,
                Response = null
            });
        }

        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
            Method<TRequest, TResponse> method, string host, CallOptions options, TRequest request)
        {
            return Call((context) => base.AsyncUnaryCall((Method<TRequest, TResponse>)context.Method, context.Host, context.Options, (TRequest)context.Request), new MiddlewareContext
            {
                Host = host,
                Method = method,
                Options = options,
                Request = request,
                Response = null
            });
        }

        public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string host, CallOptions options,
            TRequest request)
        {
            return Call((context) => base.AsyncServerStreamingCall((Method<TRequest, TResponse>)context.Method, context.Host, context.Options, (TRequest)context.Request), new MiddlewareContext
            {
                Host = host,
                Method = method,
                Options = options,
                Request = request,
                Response = null
            });
        }

        public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string host, CallOptions options)
        {
            return Call((context) => base.AsyncClientStreamingCall((Method<TRequest, TResponse>)context.Method, context.Host, context.Options), new MiddlewareContext
            {
                Host = host,
                Method = method,
                Options = options,
                Request = null,
                Response = null
            });
        }

        public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string host, CallOptions options)
        {
            return Call((context) => base.AsyncDuplexStreamingCall((Method<TRequest, TResponse>)context.Method, context.Host, context.Options), new MiddlewareContext
            {
                Host = host,
                Method = method,
                Options = options,
                Request = null,
                Response = null
            });
        }
    }
}
