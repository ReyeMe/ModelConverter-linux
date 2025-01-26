namespace ModelConverter.ParameterParser
{
    using System;

    /// <summary>
    /// Name of the command line argument
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class)]
    public sealed class CmdHelpAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CmdArgumentAttribute"/> class
        /// </summary>
        /// <param name="text">Argument help text</param>
        public CmdHelpAttribute(string text)
        {
            this.Text = text;
        }

        /// <summary>
        /// Gets argument help text
        /// </summary>
        public string Text { get; }
    }
}