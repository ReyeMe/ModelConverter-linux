namespace ModelConverter.ParameterParser
{
    using System;
    using System.IO;

    /// <summary>
    /// Relative to absolute path converter attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    internal class CmdAbsolutePathConverterAttribute : CmdConverterAttribute
    {
        /// <summary>
        /// Convert set of argument values into a single object
        /// </summary>
        /// <param name="values">Collection of values</param>
        /// <returns>Argument object</returns>
        public override object Convert(string[] values)
        {
            return values?.Select(path => Path.GetFullPath(path))?.FirstOrDefault() ?? string.Empty;
        }
    }
}
