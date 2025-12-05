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
            "\t'N'\t- Force no light\n" +
            "\t'M'\t- Enable mesh effect\n" +
            "\t'H'\t- Enable half transparency effect\n" +
            "\t'B'\t- Enable half brightness\n" +
            "\t'-'\t- Sort by nearest\n" +
            "\t'+'\t- Sort by farthest\n" +
            "\t'L'\t- Sort by last\n" +
            "\t'C'\t- Sort by center (default if -sort not set)\n";

        /// <summary>
        /// Polygon sort mode
        /// </summary>
        public enum SortModes
        {
            /// <summary>
            /// Sort by middle of polygon
            /// </summary>
            Mid = 0,

            /// <summary>
            /// Sort by fathest point of polygon
            /// </summary>
            Far,

            /// <summary>
            /// Sort by nearest point of polygon
            /// </summary>
            Near,

            /// <summary>
            /// Sort in the same way last drawn polygon was
            /// </summary>
            Last
        }

        /// <summary>
        /// Gets or sets model scale
        /// </summary>
        [CmdHelp("Scale models by some multiplier, default is 1.0")]
        [CmdArgument("scale", "s")]
        [CmdDoubleConverterAttribute]
        public double? Scale { get; set; } = 1.0;

        /// <summary>
        /// Gets or sets default sort function
        /// </summary>
        [CmdHelp("Set default sort function. [Mid/Far/Near/Last]")]
        [CmdArgument("sort", "z")]
        public SortModes? SortMode { get; set; }
    }
}