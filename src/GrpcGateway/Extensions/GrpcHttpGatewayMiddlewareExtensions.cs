using GrpcGateway.Middleware;
using Ocelot.Authentication.Middleware;
using Ocelot.Authorisation.Middleware;
using Ocelot.Cache.Middleware;
using Ocelot.DownstreamRouteFinder.Middleware;
using Ocelot.DownstreamUrlCreator.Middleware;
using Ocelot.Errors.Middleware;
using Ocelot.Headers.Middleware;
using Ocelot.LoadBalancer.Middleware;
using Ocelot.Middleware;
using Ocelot.Middleware.Pipeline;
using Ocelot.QueryStrings.Middleware;
using Ocelot.RateLimit.Middleware;
using Ocelot.Request.Middleware;
using Ocelot.Requester.Middleware;
using Ocelot.RequestId.Middleware;
using Ocelot.Responder.Middleware;
using Ocelot.Security.Middleware;
using System;
using System.Threading.Tasks;

namespace GrpcGateway.Extensions
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
            builder.UseExceptionHandlerMiddleware();

            builder.UseIfNotNull(pipelineConfiguration.PreErrorResponderMiddleware);

            builder.UseResponderMiddleware();

            builder.UseDownstreamRouteFinderMiddleware();

            builder.UseSecurityMiddleware();

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

            builder.UseClaimsToHeadersMiddleware();

            builder.UseIfNotNull(pipelineConfiguration.PreAuthorisationMiddleware);

            if (pipelineConfiguration.AuthorisationMiddleware == null)
            {
                builder.UseAuthorisationMiddleware();
            }
            else
            {
                builder.Use(pipelineConfiguration.AuthorisationMiddleware);
            }

            builder.UseClaimsToHeadersMiddleware();

            builder.UseIfNotNull(pipelineConfiguration.PreQueryStringBuilderMiddleware);

            builder.UseClaimsToQueryStringMiddleware();

            builder.UseLoadBalancingMiddleware();

            builder.UseDownstreamUrlCreatorMiddleware();

            builder.UseOutputCacheMiddleware();

            builder.UseGrpcHttpGatewayMiddleware(); // this is ours

            builder.UseHttpRequesterMiddleware();

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

        public static IOcelotPipelineBuilder UseGrpcHttpGatewayMiddleware(this IOcelotPipelineBuilder builder)
        {
            return builder.UseMiddleware<GrpcHttpGatewayMiddleware>();
        }
    }
}
