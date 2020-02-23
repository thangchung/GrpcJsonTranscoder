using System;
using System.Collections.Concurrent;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Grpc.Core;

namespace GrpcJsonTranscoder.Internal.Grpc
{
    internal class GrpcMethod<TRequest, TResponse> 
        where TRequest : class, IMessage<TRequest>, new()
        where TResponse : class, IMessage<TResponse>, new()
    {
        private static readonly ConcurrentDictionary<MethodDescriptor, Method<TRequest, TResponse>> _methods
            = new ConcurrentDictionary<MethodDescriptor, Method<TRequest, TResponse>>();

        public static Method<TRequest, TResponse> GetMethod(MethodDescriptor methodDescriptor)
        {
            if (_methods.TryGetValue(methodDescriptor, out var method))
                return method;

            var callingMethodType = 0;

            if (methodDescriptor.IsClientStreaming)
                callingMethodType = 1;

            if (methodDescriptor.IsServerStreaming)
                callingMethodType += 2;

            var methodType = (MethodType)Enum.ToObject(typeof(MethodType), callingMethodType);

            method = new Method<TRequest, TResponse>(
                methodType,
                methodDescriptor.Service.FullName,
                methodDescriptor.Name,
                ArgsParser<TRequest>.Marshaller,
                ArgsParser<TResponse>.Marshaller);

            _methods.TryAdd(methodDescriptor, method);

            return method;
        }
    }

    internal static class ArgsParser<T> where T : class, IMessage<T>, new()
    {
        public static readonly MessageParser<T> Parser = new MessageParser<T>(Factory<T>.CreateInstance);
        public static readonly Marshaller<T> Marshaller = Marshallers.Create(MessageExtensions.ToByteArray, Parser.ParseFrom);
    }
}
