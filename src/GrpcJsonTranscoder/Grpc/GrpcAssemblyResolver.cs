using Google.Protobuf.Reflection;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace GrpcJsonTranscoder.Grpc
{
    public class GrpcAssemblyResolver
    {
        private ILogger<GrpcAssemblyResolver> _logger;
        private readonly IList<Assembly> _assemblies = new List<Assembly>();
        private ConcurrentDictionary<string, MethodDescriptor> _methodDescriptorDic;

        public GrpcAssemblyResolver ConfigGrpcAssembly(ILogger<GrpcAssemblyResolver> logger, params Assembly[] assemblies)
        {
            _logger = logger;

            if (assemblies != null)
            {
                foreach (var assembly in assemblies)
                {
                    _assemblies.Add(assembly);
                }
            };

            _methodDescriptorDic = GetMethodDescriptors(_assemblies.ToArray());

            return this;
        }

        public MethodDescriptor FindMethodDescriptor(string methodName)
        {
            _logger.LogInformation($"Finding method #{methodName} in the assembly resolver.");

            if (!_methodDescriptorDic.TryGetValue(methodName, out var methodDescriptor))
            {
                throw new System.Exception($"Could not find out method #{methodName} in the assemblies you provided.");
            }

            return methodDescriptor;
        }

        private ConcurrentDictionary<string, MethodDescriptor> GetMethodDescriptors(params Assembly[] assemblies)
        {
            var methodDescriptorDic = new ConcurrentDictionary<string, MethodDescriptor>();
            var types = assemblies.SelectMany(a => a.GetTypes());
            var fileTypes = types.Where(type => type.Name.EndsWith("Reflection"));

            foreach (var type in fileTypes)
            {
                const BindingFlags flags = BindingFlags.Static | BindingFlags.Public;
                var property = type.GetProperties(flags).FirstOrDefault(t => t.Name == "Descriptor");

                if (property is null)
                    continue;
                if (!(property.GetValue(null) is FileDescriptor fileDescriptor))
                    continue;

                foreach (var svr in fileDescriptor.Services)
                {
                    var srvName = svr.FullName.ToUpper();
                    _logger.LogInformation($"Add service name #{srvName} into the assembly resolver.");

                    foreach (var method in svr.Methods)
                    {
                        _logger.LogInformation($"Add method name #{method.Name.ToUpper()} into the assembly resolver.");
                        methodDescriptorDic.TryAdd(method.FullName.ToUpper(), method);
                    }
                }
            }

            return methodDescriptorDic;
        }
    }
}
