using Grpc.AspNetCore.Server;
using Grpc.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TestGrpcServer
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddGrpc();
            services.TryAddSingleton(serviceProvider =>
            {
                //var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(Startup));
                var endpointDataSource = serviceProvider.GetRequiredService<EndpointDataSource>();

                var grpcEndpointMetadata = endpointDataSource.Endpoints
                    .Select(ep => ep.Metadata.GetMetadata<GrpcMethodMetadata>())
                    .Where(m => m != null)
                    .ToList();

                var serviceTypes = grpcEndpointMetadata.Select(m => m.ServiceType).Distinct().ToList();

                var serviceDescriptors = new List<Google.Protobuf.Reflection.ServiceDescriptor>();

                foreach (var serviceType in serviceTypes)
                {
                    var baseType = GetServiceBaseType(serviceType);
                    var definitionType = baseType?.DeclaringType;

                    var descriptorPropertyInfo = definitionType?.GetProperty("Descriptor", BindingFlags.Public | BindingFlags.Static);
                    if (descriptorPropertyInfo != null)
                    {
                        var serviceDescriptor = descriptorPropertyInfo.GetValue(null) as Google.Protobuf.Reflection.ServiceDescriptor;
                        if (serviceDescriptor != null)
                        {
                            serviceDescriptors.Add(serviceDescriptor);
                            continue;
                        }
                    }
                }

                return new ReflectionServiceImpl(serviceDescriptors);
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<GreeterService>();

                endpoints.MapGrpcService<ReflectionServiceImpl>();

                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
                });
            });
        }

        private static Type? GetServiceBaseType(Type serviceImplementation)
        {
            // TService is an implementation of the gRPC service. It ultimately derives from Foo.TServiceBase base class.
            // We need to access the static BindService method on Foo which implicitly derives from Object.
            var baseType = serviceImplementation.BaseType;

            // Handle services that have multiple levels of inheritence
            while (baseType?.BaseType?.BaseType != null)
            {
                baseType = baseType.BaseType;
            }

            return baseType;
        }
    }
}
