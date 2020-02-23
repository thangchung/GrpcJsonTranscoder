using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Grpc.Core;

namespace GrpcJsonTranscoder.Internal.Grpc
{
    internal class MethodDescriptorCaller : ClientBase<MethodDescriptorCaller>
    {
        public MethodDescriptorCaller()
        {
        }

        public MethodDescriptorCaller(CallInvoker callInvoker) : base(callInvoker) { }

        public MethodDescriptorCaller(ChannelBase channel) : base(channel) { }

        protected MethodDescriptorCaller(ClientBaseConfiguration configuration) : base(configuration) { }

        protected override MethodDescriptorCaller NewInstance(ClientBaseConfiguration configuration)
        {
            return new MethodDescriptorCaller(configuration);
        }

        public Task<object> InvokeAsync(MethodDescriptor method, IDictionary<string, string> headers, object requestObject)
        {
            object requests;

            if (requestObject != null && typeof(IEnumerable<>).MakeGenericType(method.InputType.ClrType).IsInstanceOfType(requestObject))
            {
                requests = requestObject;
            }
            else
            {
                var arrayInstance = Array.CreateInstance(method.InputType.ClrType, 1);
                arrayInstance.SetValue(requestObject, 0);
                requests = arrayInstance;
            }

            var callGrpcAsyncCoreMethod = typeof(MethodDescriptorCaller).GetMethod("CallGrpcAsyncCore", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            var task = (Task<object>)callGrpcAsyncCoreMethod?.MakeGenericMethod(new Type[] { method.InputType.ClrType, method.OutputType.ClrType }).Invoke(this, new [] { method, headers, requests });

            return task;
        }

        [DebuggerStepThrough]
        private Task<object> CallGrpcAsyncCore<TRequest, TResponse>(MethodDescriptor method, IDictionary<string, string> headers, IEnumerable<TRequest> requests)
            where TRequest : class, IMessage<TRequest>, new()
            where TResponse : class, IMessage<TResponse>, new()
        {
            var option = CreateCallOptions(headers);
            var rpc = GrpcMethod<TRequest, TResponse>.GetMethod(method);
            switch (rpc.Type)
            {
                case MethodType.Unary:
                    var taskUnary = AsyncUnaryCall(CallInvoker, rpc, option, requests.FirstOrDefault());
                    return Task.FromResult<object>(taskUnary.Result);

                case MethodType.ClientStreaming:
                    var taskClientStreaming = AsyncClientStreamingCall(CallInvoker, rpc, option, requests);
                    return Task.FromResult<object>(taskClientStreaming.Result);

                case MethodType.ServerStreaming:
                    var taskServerStreaming = AsyncServerStreamingCall(CallInvoker, rpc, option, requests.FirstOrDefault());
                    return Task.FromResult<object>(taskServerStreaming.Result);

                case MethodType.DuplexStreaming:
                    var taskDuplexStreaming = AsyncDuplexStreamingCall(CallInvoker, rpc, option, requests);
                    return Task.FromResult<object>(taskDuplexStreaming.Result);

                default:
                    throw new NotSupportedException($"MethodType '{rpc.Type}' is not supported.");
            }
        }

        private static CallOptions CreateCallOptions(IDictionary<string, string> headers)
        {
            var meta = new Metadata();

            foreach (var (key, value) in headers)
            {
                meta.Add(key, value);
            }

            var option = new CallOptions(meta);

            return option;
        }

        private static Task<TResponse> AsyncUnaryCall<TRequest, TResponse>(CallInvoker invoker, Method<TRequest, TResponse> method, CallOptions option, TRequest request) where TRequest : class where TResponse : class
        {
            return invoker.AsyncUnaryCall(method, null, option, request).ResponseAsync;
        }

        private static async Task<TResponse> AsyncClientStreamingCall<TRequest, TResponse>(CallInvoker invoker, Method<TRequest, TResponse> method, CallOptions option, IEnumerable<TRequest> requests) where TRequest : class where TResponse : class
        {
            using var call = invoker.AsyncClientStreamingCall(method, null, option);
            if (requests != null)
            {
                foreach (var request in requests)
                {
                    await call.RequestStream.WriteAsync(request).ConfigureAwait(false);
                }
            }

            await call.RequestStream.CompleteAsync().ConfigureAwait(false);

            return call.ResponseAsync.Result;
        }

        private static async Task<IList<TResponse>> AsyncServerStreamingCall<TRequest, TResponse>(CallInvoker invoker, Method<TRequest, TResponse> method, CallOptions option, TRequest request) where TRequest : class where TResponse : class
        {
            using var call = invoker.AsyncServerStreamingCall(method, null, option, request);
            var responses = new List<TResponse>();

            while (await call.ResponseStream.MoveNext().ConfigureAwait(false))
            {
                responses.Add(call.ResponseStream.Current);
            }

            return responses;
        }

        private static async Task<IList<TResponse>> AsyncDuplexStreamingCall<TRequest, TResponse>(CallInvoker invoker, Method<TRequest, TResponse> method, CallOptions option, IEnumerable<TRequest> requests) where TRequest : class where TResponse : class
        {
            using var call = invoker.AsyncDuplexStreamingCall(method, null, option);
            if (requests != null)
            {
                foreach (var request in requests)
                {
                    await call.RequestStream.WriteAsync(request).ConfigureAwait(false);
                }
            }

            await call.RequestStream.CompleteAsync().ConfigureAwait(false);

            var responses = new List<TResponse>();

            while (await call.ResponseStream.MoveNext().ConfigureAwait(false))
            {
                responses.Add(call.ResponseStream.Current);
            }

            return responses;
        }
    }
}
