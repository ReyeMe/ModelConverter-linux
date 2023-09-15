namespace ModelConverter.PluginLoader
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Reflection;

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
        /// Get collection of all plugins
        /// </summary>
        /// <returns>Collection of <see cref="Plugin"/> descriptors</returns>
        [RequiresUnreferencedCode("Calls System.Reflection.Assembly.LoadFile(String)")]
        internal static Dictionary<string, Plugin> GetPlugins()
        {
            Dictionary<string, Plugin> result = new Dictionary<string, Plugin>();

            foreach (string file in Directory.GetFiles(Loader.PluginDirectory, "*.dll", SearchOption.TopDirectoryOnly))
            {
                Assembly pluginAssembly = Assembly.LoadFile(file);
                IEnumerable<Type> pluginTypes = pluginAssembly.GetExportedTypes()
                    .Where(type => typeof(IExportPlugin).IsAssignableFrom(type) || typeof(IImportPlugin).IsAssignableFrom(type));

                foreach (Type pluginType in pluginTypes)
                {
                    PluginAttribute? attribute = pluginType.GetCustomAttribute<PluginAttribute>();

                    if (attribute != null &&
                        !string.IsNullOrEmpty(attribute.Name) &&
                        !result.ContainsKey(attribute.Name))
                    {
                        result.Add(attribute.Name, new Plugin(pluginType));
                    }
                }
            }

            return result;
        }
    }
}