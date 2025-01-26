namespace ModelConverter.PluginLoader
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Reflection;
    using ModelConverter.Geometry;
    using ModelConverter.ParameterParser;

    /// <summary>
    /// Plugin descriptor
    /// </summary>
    internal sealed class Plugin
    {
        /// <summary>
        /// Plugin base type
        /// </summary>
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        private readonly Type type;

        /// <summary>
        /// Initializes a new isntance of the <see cref="Plugin"/> class
        /// </summary>
        /// <param name="pluginBaseClassType"></param>
        /// <exception cref="InvalidOperationException"></exception>
        internal Plugin([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type pluginBaseClassType)
        {
            PluginAttribute? attribute = pluginBaseClassType.GetCustomAttribute<PluginAttribute>();

            if (attribute != null)
            {
                this.Name = attribute.Name;
                this.Description = attribute.Description;
                this.Extension = attribute.Extension;

                this.type = pluginBaseClassType;
                this.CustomArgumentsViewType = attribute.CustomArguments;

                if (typeof(IExportPlugin).IsAssignableFrom(this.type))
                {
                    this.Supports |= Support.Export;
                }

                if (typeof(IImportPlugin).IsAssignableFrom(this.type))
                {
                    this.Supports |= Support.Import;
                }
            }
            else
            {
                throw new InvalidOperationException("Plugin type must have 'PluginAttribute' attribute.");
            }
        }

        /// <summary>
        /// Plugin feature support
        /// </summary>
        internal enum Support
        {
            /// <summary>
            /// Invalid value
            /// </summary>
            Invalid = 0,

            /// <summary>
            /// Supports importing
            /// </summary>
            Import = 1,

            /// <summary>
            /// Supports exporting
            /// </summary>
            Export = 2,

            /// <summary>
            /// Supports both
            /// </summary>
            Both = Import | Export,
        }

        /// <summary>
        /// Custom type of the class holding cutom plugin arguments
        /// </summary>
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        internal Type? CustomArgumentsViewType { get; }

        /// <summary>
        /// Gets plugin description
        /// </summary>
        internal string Description { get; }

        /// <summary>
        /// Gets extension this plugin applies to
        /// </summary>
        internal string Extension { get; }

        /// <summary>
        /// Gets name of the plugin
        /// </summary>
        internal string Name { get; }

        /// <summary>
        /// Gets a value indicating plugin feature support
        /// </summary>
        internal Support Supports { get; }

        /// <summary>
        /// Export file with this plugin
        /// </summary>
        /// <param name="group"><see cref="Group"/> to export</param>
        /// <param name="file">File to export to</param>
        /// <param name="settings">Current argument settings</param>
        /// <param name="args">Raw arguments</param>
        /// <returns><see langword="true"/> on success</returns>
        internal bool ExportFile(Group group, string file, ArgumentSettings settings, string[] args)
        {
            bool result = false;

            if (typeof(IExportPlugin).IsAssignableFrom(this.type))
            {
                IExportPlugin? exportPlugin = null;

                if (this.CustomArgumentsViewType != null)
                {
                    try
                    {
                        object? pluginArgs = typeof(Parser<>).MakeGenericType(this.CustomArgumentsViewType)
                            .GetMethod("Parse", BindingFlags.Public | BindingFlags.Static)?
                            .Invoke(null, new[] { args });

                        if (pluginArgs != null)
                        {
                            exportPlugin = Plugin.TryCreateInstance(this.type, new object[] { settings, pluginArgs }) as IExportPlugin;

                            // Try simpler constructor
                            if (exportPlugin == null)
                            {
                                exportPlugin = Plugin.TryCreateInstance(this.type, new object[] { pluginArgs }) as IExportPlugin;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ex.ToString();
                    }
                }

                // Try simpler constructor
                if (exportPlugin == null)
                {
                    exportPlugin = Plugin.TryCreateInstance(this.type, new object[] { settings }) as IExportPlugin;

                    // Try empty constructor
                    if (exportPlugin == null)
                    {
                        exportPlugin = Plugin.TryCreateInstance(this.type) as IExportPlugin;
                    }
                }

                if (exportPlugin != null)
                {
                    result = exportPlugin.Export(group, file);

                    if (((object)exportPlugin) is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Export file with this plugin
        /// </summary>
        /// <param name="file">File to export to</param>
        /// <param name="settings">Current parsed argument settings</param>
        /// <param name="args">Raw arguments</param>
        /// <returns>Imported <see cref="Group"/></returns>
        internal Group? ImportFile(string file, ArgumentSettings settings, string[] args)
        {
            Group? result = null;

            if (typeof(IImportPlugin).IsAssignableFrom(this.type))
            {
                IImportPlugin? importPlugin = null;

                if (this.CustomArgumentsViewType != null)
                {
                    try
                    {
                        object? pluginArgs = typeof(Parser<>).MakeGenericType(this.CustomArgumentsViewType)
                            .GetMethod("Parse", BindingFlags.Public | BindingFlags.Static)?
                            .Invoke(null, new[] { args });

                        if (pluginArgs != null)
                        {
                            importPlugin = Plugin.TryCreateInstance(this.type, new object[] { settings, pluginArgs }) as IImportPlugin;

                            // Try simpler constructor
                            if (importPlugin == null)
                            {
                                importPlugin = Plugin.TryCreateInstance(this.type, new object[] { pluginArgs }) as IImportPlugin;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ex.ToString();
                    }
                }

                // Try simpler constructor
                if (importPlugin == null)
                {
                    importPlugin = Plugin.TryCreateInstance(this.type, new object[] { settings }) as IImportPlugin;

                    // Try empty constructor
                    if (importPlugin == null)
                    {
                        importPlugin = Plugin.TryCreateInstance(this.type) as IImportPlugin;
                    }
                }

                if (importPlugin != null)
                {
                    result = importPlugin.Import(file);

                    if (((object)importPlugin) is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Print plugin specific help
        /// </summary>
        internal void PrintHelp()
        {
            if (this.CustomArgumentsViewType != null)
            {
                try
                {
                    object? pluginArgs = typeof(Parser<>).MakeGenericType(this.CustomArgumentsViewType)
                        .GetMethod("PrintHelp", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, null);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("An error has occured while retrieving help page.\n\n" + ex.ToString());
                }
            }
            else
            {
                Console.WriteLine("Plugin has no settings.");
            }
        }

        /// <summary>
        /// Create object instance from type
        /// </summary>
        /// <param name="type">Object type</param>
        /// <param name="args">Constructor arguments</param>
        /// <returns>Object instance or <see langword="null"/></returns>
        private static object? TryCreateInstance([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
        {
            try
            {
                return Activator.CreateInstance(type);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Create object instance from type
        /// </summary>
        /// <param name="type">Object type</param>
        /// <param name="args">Constructor arguments</param>
        /// <returns>Object instance or <see langword="null"/></returns>
        private static object? TryCreateInstance([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type, params object?[]? args)
        {
            try
            {
                return Activator.CreateInstance(type, args);
            }
            catch
            {
                return null;
            }
        }
    }
}