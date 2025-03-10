using System.Reflection;
using System.Runtime.Loader;

namespace MyConsoleApp.Context
{
    class PluginLoadContext : AssemblyLoadContext, IDisposable
    {
        private AssemblyDependencyResolver _resolver;

        public PluginLoadContext(string pluginPath) : base(isCollectible: true)
        {
            _resolver = new AssemblyDependencyResolver(pluginPath);
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            string assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            return assemblyPath != null ? LoadFromAssemblyPath(assemblyPath) : null;
        }

        public void Dispose()
        {
            Unload();
            GC.SuppressFinalize(this);
        }
    }
}