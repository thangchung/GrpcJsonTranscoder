using GrpcJsonTranscoder.Middleware;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace GrpcJsonTranscoder.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddGrpcJsonTranscoder(this IServiceCollection services, Func<GrpcAssemblyResolver> addGrpcAssembly)
        {
            using (var scope = services.BuildServiceProvider().CreateScope())
            {
                var svcProvider = scope.ServiceProvider;
                var config = svcProvider.GetRequiredService<IConfiguration>();
                var section = config.GetSection("RestGrpcMapper");
                services.Configure<GrpcMapperOptions>(config.GetSection("RestGrpcMapper"));
                services.AddSingleton(resolver => addGrpcAssembly.Invoke());
                return services;
            }
        }
    }
}
