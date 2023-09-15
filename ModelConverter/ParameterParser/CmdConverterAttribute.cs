namespace ModelConverter.ParameterParser
{
    using System;

    /// <summary>
    /// Converter attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    internal abstract class CmdConverterAttribute : Attribute
    {
        /// <summary>
        /// Convert set of argument values into a single object
        /// </summary>
        /// <param name="values">Collection of values</param>
        /// <returns>Argument object</returns>
        public abstract object Convert(string[] values);
    }
}