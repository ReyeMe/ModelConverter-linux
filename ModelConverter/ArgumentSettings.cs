namespace ModelConverter
{
    using ParameterParser;

    /// <summary>
    /// Arguments view model
    /// </summary>
    [CmdHelp("Tool to convert between multiple 3D file formats.\nUse: dotnet ./ModelConverter.dll -i [Input path] -o [Output path]")]
    public sealed class ArgumentSettings
    {
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
        [CmdHelp("Path to the input file. (eg.: -i \"work/models/test.obj\")")]
        [CmdArgument("i")]
        public string? InputFile { get; set; }

        /// <summary>
        /// Gets or sets input file path
        /// </summary>
        [CmdHelp("Path to the ouput file. (eg.: -o \"work/result.tmf\")")]
        [CmdArgument("o")]
        public string? OuputFile { get; set; }

        /// <summary>
        /// Gets or sets model scale
        /// </summary>
        [CmdHelp("Scale models by some multiplier, default is 1.0")]
        [CmdArgument("s")]
        [CmdDoubleConverterAttribute]
        public double? Scale { get; set; } = 1.0;

        /// <summary>
        /// Gets or sets a value indicating whether to show help screen
        /// </summary>
        [CmdHelp("Shows this helps screen.")]
        [CmdArgument("-help", "h")]
        public bool ShowHelp { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to show all available plugins
        /// </summary>
        [CmdHelp("Show all available plugins.")]
        [CmdArgument("plugins")]
        public bool ShowPlugins { get; set; }
    }
}