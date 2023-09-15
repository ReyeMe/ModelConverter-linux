namespace ModelConverter.ParameterParser
{
    using System;

    /// <summary>
    /// Name of the command line argument
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    internal sealed class CmdArgumentAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CmdArgumentAttribute"/> class
        /// </summary>
        /// <param name="name">Argument name</param>
        /// <param name="isRequired">Indicates whether argument is required</param>
        public CmdArgumentAttribute(string name = "", bool isRequired = false)
        {
            this.Name = name;
            this.IsRequired = isRequired;
        }

        /// <summary>
        /// Gets a value indicating whether argument is required
        /// </summary>
        public bool IsRequired { get; }

        /// <summary>
        /// Gets argument name
        /// </summary>
        public string Name { get; }
    }
}