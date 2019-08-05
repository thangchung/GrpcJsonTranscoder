using Ocelot.Authentication.Middleware;
using Ocelot.Authorisation.Middleware;
using Ocelot.Cache.Middleware;
using Ocelot.DownstreamUrlCreator.Middleware;
using Ocelot.Headers.Middleware;
using Ocelot.LoadBalancer.Middleware;
using Ocelot.Middleware;
using Ocelot.Middleware.Pipeline;
using Ocelot.RateLimit.Middleware;
using Ocelot.Request.Middleware;
using Ocelot.RequestId.Middleware;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Gateway.Extensions
{
    public static class GrpcHttpGatewayMiddlewareExtensions
    {
        public static OcelotPipelineConfiguration AddGrpcHttpGateway(this OcelotPipelineConfiguration config)
        {
            config.MapWhenOcelotPipeline.Add(builder => builder.AddGrpcHttpGateway(config));
            return config;
        }

        private static Func<DownstreamContext, bool> AddGrpcHttpGateway(this IOcelotPipelineBuilder builder, OcelotPipelineConfiguration pipelineConfiguration)
        {
            builder.UseHttpHeadersTransformationMiddleware();

            builder.UseDownstreamRequestInitialiser();

            builder.UseRateLimiting();

            builder.UseRequestIdMiddleware();

            builder.UseIfNotNull(pipelineConfiguration.PreAuthenticationMiddleware);

            if (pipelineConfiguration.AuthenticationMiddleware == null)
            {
                builder.UseAuthenticationMiddleware();
            }
            else
            {
                builder.Use(pipelineConfiguration.AuthenticationMiddleware);
            }

            //builder.UseClaimsBuilderMiddleware();

            builder.UseIfNotNull(pipelineConfiguration.PreAuthorisationMiddleware);

            if (pipelineConfiguration.AuthorisationMiddleware == null)
            {
                builder.UseAuthorisationMiddleware();
            }
            else
            {
                builder.Use(pipelineConfiguration.AuthorisationMiddleware);
            }

            //builder.UseHttpRequestHeadersBuilderMiddleware();

            builder.UseIfNotNull(pipelineConfiguration.PreQueryStringBuilderMiddleware);

            //builder.UseQueryStringBuilderMiddleware();

            builder.UseLoadBalancingMiddleware();

            builder.UseDownstreamUrlCreatorMiddleware();

            builder.UseOutputCacheMiddleware();

            //builder.UseGrpcHttpMiddleware();

            //builder.UseHttpRequesterMiddleware();

            return (context) =>
            {
                return context.DownstreamReRoute.DownstreamScheme.Equals("grpc", StringComparison.OrdinalIgnoreCase);
            };
        }

        private static void UseIfNotNull(this IOcelotPipelineBuilder builder,
             Func<DownstreamContext, Func<Task>, Task> middleware)
        {
            if (middleware != null)
            {
                builder.Use(middleware);
            }
        }
    }
}
