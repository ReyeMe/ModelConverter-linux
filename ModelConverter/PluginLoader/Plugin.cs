namespace ModelConverter.PluginLoader
{
    using ModelConverter.Geometry;
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Reflection;

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
        /// <returns><see langword="true"/> on success</returns>
        internal bool ExportFile(Group group, string file, ArgumentSettings settings)
        {
            bool result = false;

            if (typeof(IExportPlugin).IsAssignableFrom(this.type))
            {
                IExportPlugin? exportPLugin = Activator.CreateInstance(this.type, new[] { settings }) as IExportPlugin;

                if (exportPLugin != null)
                {
                    result = exportPLugin.Export(group, file);

                    if (((object)exportPLugin) is IDisposable disposable)
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
        /// <param name="settings">Current argument settings</param>
        /// <returns>Imported <see cref="Group"/></returns>
        internal Group? ImportFile(string file, ArgumentSettings settings)
        {
            Group? result = null;

            if (typeof(IImportPlugin).IsAssignableFrom(this.type))
            {
                IImportPlugin? exportPLugin = Activator.CreateInstance(this.type, new[] { settings }) as IImportPlugin;

                if (exportPLugin != null)
                {
                    result = exportPLugin.Import(file);

                    if (((object)exportPLugin) is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
            }

            return result;
        }
    }
}