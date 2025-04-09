namespace ModelConverter
{
    using System.Diagnostics.CodeAnalysis;
    using ParameterParser;

    /// <summary>
    /// Arguments view model
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    [CmdHelp("Tool to convert between multiple 3D file formats.\nUse: dotnet ./ModelConverter.dll -i [Input path(s)] -o [Output path]")]
    public sealed class ArgumentSettings
    {
        /// <summary>
        /// Object order in group
        /// </summary>
        public enum ObjOrder
        {
            /// <summary>
            /// Keep file same order as within file
            /// </summary>
            Keep = 0,

            /// <summary>
            /// Order alphabetically
            /// </summary>
            ByName
        }

        /// <summary>
        /// How to order objects before export
        /// </summary>
        [CmdHelp("Indicates how to order loaded objects for export. Valid values are 'Keep' (default option), 'ByName' alphabetical sort.")]
        [CmdArgument("order")]
        public ObjOrder Order { get; set; }

        /// <summary>
        /// Gets or sets export plugin override
        /// </summary>
        [CmdHelp("Force export plugin regardless of file extension")]
        [CmdArgument("exp")]
        public string? ExportPluginName { get; set; }

        /// <summary>
        /// Gets or sets import plugin override
        /// </summary>
        [CmdHelp("Force import plugin regardless of file extension")]
        [CmdArgument("imp")]
        public string? ImportPluginName { get; set; }

        /// <summary>
        /// Gets or sets input file path
        /// </summary>
        [CmdHelp("Path to the input file. (eg.: -i \"work/models/test.obj\") or multiple files (eg.: -i \"work/models/test1.obj\" \"work/models/test2.obj\")")]
        [CmdArgument("i")]
        [CmdWildPathConverter]
        public string[]? InputFile { get; set; }

        /// <summary>
        /// Gets or sets input file path
        /// </summary>
        [CmdHelp("Path to the ouput file. (eg.: -o \"work/result.tmf\")")]
        [CmdArgument("o")]
        [CmdAbsolutePathConverter]
        public string? OuputFile { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to show help screen
        /// </summary>
        [CmdHelp("Shows this helps screen. To show specific plugin help add plugin name after command.")]
        [CmdArgument("help", "h")]
        public string? ShowHelp { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to show all available plugins
        /// </summary>
        [CmdHelp("Show all available plugins.")]
        [CmdArgument("plugins")]
        public bool ShowPlugins { get; set; }
    }
}