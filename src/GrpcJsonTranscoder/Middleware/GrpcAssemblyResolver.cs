using System.Collections.Generic;
using System.Reflection;

namespace GrpcJsonTranscoder.Middleware
{
    public class GrpcAssemblyResolver
    {
        private IList<Assembly> _assemblies = new List<Assembly>();

        public GrpcAssemblyResolver ConfigGrpcAssembly(params Assembly[] assemblies)
        {
            if (assemblies != null)
            {
                foreach (var assembly in assemblies)
                {
                    _assemblies.Add(assembly);
                }
            };
            return this;
        }

        public IEnumerable<Assembly> GetGrpcAssemblies()
        {
            return _assemblies;
        }

        public static GrpcAssemblyResolver Configuration(params Assembly[] assemblies)
        {
            var instance = new GrpcAssemblyResolver();
            return instance.ConfigGrpcAssembly(assemblies);
        }
    }
}
