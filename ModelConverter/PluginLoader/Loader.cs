namespace ModelConverter.PluginLoader
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Loader;
    using System.Security.Policy;

    /// <summary>
    /// Plugin loader
    /// </summary>
    internal static class Loader
    {
        /// <summary>
        /// Plugins directory
        /// </summary>
        private static readonly string PluginDirectory = Path.Combine(AppContext.BaseDirectory, "Plugins");

        /// <summary>
        /// Validate and load plugin dll
        /// </summary>
        /// <param name="pluginDll">Plugin dll path</param>
        /// <returns>Loaded DLL and context</returns>
        [RequiresUnreferencedCode("Calls System.Runtime.Loader.AssemblyLoadContext.LoadFromAssemblyPath(String)")]
        private static (AssemblyLoadContext, IList<Plugin>)? ValidateAndLoadPlugin(string pluginDll)
        {
            AssemblyLoadContext context = new($"{Path.GetFileNameWithoutExtension(pluginDll)}_{Guid.NewGuid()}", true);

            try
            {
                List<Plugin> plugins = context.LoadFromAssemblyPath(pluginDll)
                    .GetExportedTypes()
                    .Where(type => typeof(IExportPlugin).IsAssignableFrom(type) || typeof(IImportPlugin).IsAssignableFrom(type))
                    .Where(type => type.GetCustomAttribute<PluginAttribute>() != null)
                    .Select(type => new Plugin(type, context))
                    .ToList();

                context.Resolving += Loader.ContextAssemblyResolving;
                return (context, plugins);
            }
            catch
            {
                context.Unload();
                return null;
            }
        }

        /// <summary>
        /// Assembly context resolve event
        /// </summary>
        /// <param name="context">Assembly context</param>
        /// <param name="assembly">Assembly to resolve</param>
        /// <returns>Resolved assembly</returns>
        private static Assembly? ContextAssemblyResolving(AssemblyLoadContext context, AssemblyName assembly)
        {
            string? pluginName = context.Name?.Substring(0, context.Name.IndexOf('_'));

            if (!string.IsNullOrWhiteSpace(pluginName))
            {
                string dll = Path.Combine(PluginDirectory, pluginName, assembly.Name + ".dll");

                if (File.Exists(dll))
                {
                    try
                    {
                        return context.LoadFromAssemblyPath(dll);
                    }
                    catch { }
                }
            }

            return null;
        }

        /// <summary>
        /// Get collection of all plugins
        /// </summary>
        /// <returns>Collection of <see cref="Plugin"/> descriptors</returns>
        [RequiresUnreferencedCode("Calls System.Reflection.Assembly.LoadFile(String)")]
        internal static Dictionary<string, Plugin> GetPlugins()
        {
            Dictionary<string, Plugin> result = new Dictionary<string, Plugin>();

            foreach (string directory in Directory.GetDirectories(Loader.PluginDirectory, "*", SearchOption.TopDirectoryOnly))
            {
                foreach (string file in Directory.GetFiles(directory, "*.dll", SearchOption.TopDirectoryOnly))
                {
                    (AssemblyLoadContext context, IList<Plugin> plugins)? pluginAssembly = Loader.ValidateAndLoadPlugin(file);

                    if (pluginAssembly != null)
                    {
                        bool loadedAny = false;

                        foreach (Plugin plugin in pluginAssembly.Value.plugins)
                        {
                            if (!result.ContainsKey(plugin.Name))
                            {
                                loadedAny = true;
                                result.Add(plugin.Name, plugin);
                            }
                        }

                        if (!loadedAny)
                        {
                            pluginAssembly.Value.context.Unload();
                        }
                    }
                }
            }

            return result;
        }
    }
}