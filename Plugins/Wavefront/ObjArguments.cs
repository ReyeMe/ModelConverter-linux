namespace Wavefront
{
    using System.Diagnostics.CodeAnalysis;
    using ModelConverter.ParameterParser;

    /// <summary>
    /// Arguments view model
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    [CmdHelp("Wavefront import plugin settings\n" + ObjArguments.FaceHelp)]
    public class ObjArguments
    {
        /// <summary>
        /// Face flags settings
        /// </summary>
        public const string FaceHelp = "Valid face flags: (used in material name, eg.: 'MyMaterialName_DF-')\n" +
            "\t'D'\t- Double sided face\n" +
            "\t'F'\t- Force flat shaded\n" +
            "\t'M'\t- Enable mesh effect\n" +
            "\t'H'\t- Enable half transparency effect\n" +
            "\t'B'\t- Enable half brightness\n" +
            "\t'-'\t- Sort by minimum (by center is default)\n" +
            "\t'+'\t- Sort by maximum (by center is default)\n" +
            "\t'L'\t- Sort by last (by center is default)\n";

        /// <summary>
        /// Gets or sets model scale
        /// </summary>
        [CmdHelp("Scale models by some multiplier, default is 1.0")]
        [CmdArgument("scale", "s")]
        [CmdDoubleConverterAttribute]
        public double? Scale { get; set; } = 1.0;
    }
}
