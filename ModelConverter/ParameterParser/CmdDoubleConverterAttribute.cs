namespace ModelConverter.ParameterParser
{
    using System.Globalization;
    using System.Linq;

    /// <summary>
    /// Double converter
    /// </summary>
    public class CmdDoubleConverterAttribute : CmdConverterAttribute
    {
        /// <summary>
        /// Default value
        /// </summary>
        public double Default { get; set; }

        /// <summary>
        /// Convert set of argument values into a single object
        /// </summary>
        /// <param name="values">Collection of values</param>
        /// <returns>Argument object</returns>
        public override object Convert(string[] values)
        {
            if (double.TryParse(values?.FirstOrDefault(), NumberStyles.Float, CultureInfo.InvariantCulture, out double converted))
            {
                return converted;
            }

            return this.Default;
        }
    }
}
