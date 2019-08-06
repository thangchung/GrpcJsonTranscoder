using Grpc.Core;
using System;
using System.Threading.Tasks;

namespace GrpcGateway.Grpc
{
    public delegate Task PipelineDelagate(MiddlewareContext context);

    public class MiddlewareContext
    {
        public IMethod Method { get; set; }

        /// <summary>
        /// Request object, null if streaming.
        /// </summary>
        public object Request { get; set; }

        /// <summary>
        /// Response object, null if streaming or on request path.
        /// </summary>
        public object Response { get; set; }

        public string Host { get; set; }

        public CallOptions Options { get; set; }

        /// <summary>
        /// Final handler of the RPC
        /// </summary>
        internal Func<Task> HandlerExecutor { get; set; }
    }

    public class Pipeline
    {
        private PipelineDelagate processChain;

        internal Pipeline(PipelineDelagate middlewareChain)
        {
            processChain = middlewareChain;
        }

        internal Task RunPipeline(MiddlewareContext context)
        {
            return processChain(context);
        }
    }
}
