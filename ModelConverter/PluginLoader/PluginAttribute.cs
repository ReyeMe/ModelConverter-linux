namespace ModelConverter.PluginLoader
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Plugin attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class PluginAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PluginAttribute"/> class
        /// </summary>
        /// <param name="name">Plugin name</param>
        /// <param name="description">Plugin description (version, author, eg...)</param>
        /// <param name="extension">Extension this plugin applies to (eg: ".tmf")</param>
        /// <param name="customArguments">Custom plugin arguments class type</param>
        public PluginAttribute(string name, string description, string extension, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? customArguments = null)
        {
            this.Name = name;
            this.Description = description;
            this.Extension = extension;
            this.CustomArguments = customArguments;
        }

        /// <summary>
        /// Gets plugin description
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets extension this plugin applies to
        /// </summary>
        public string Extension { get; }

        /// <summary>
        /// Gets plugin name
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets custom plugin arguments type
        /// </summary>
        public Type? CustomArguments { get; }
    }
}