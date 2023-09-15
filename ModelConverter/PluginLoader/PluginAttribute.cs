namespace ModelConverter.PluginLoader
{
    using System;

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
        public PluginAttribute(string name, string description, string extension)
        {
            this.Name = name;
            this.Description = description;
            this.Extension = extension;
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
    }
}