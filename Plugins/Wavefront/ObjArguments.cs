namespace Wavefront
{
    using System.Diagnostics.CodeAnalysis;
    using ModelConverter.ParameterParser;

    /// <summary>
    /// Arguments view model
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    [CmdHelp("Wavefront import plugin settings")]
    public class ObjArguments
    {
        /// <summary>
        /// Gets or sets model scale
        /// </summary>
        [CmdHelp("Scale models by some multiplier, default is 1.0")]
        [CmdArgument("scale", "s")]
        [CmdDoubleConverterAttribute]
        public double? Scale { get; set; } = 1.0;
    }
}
