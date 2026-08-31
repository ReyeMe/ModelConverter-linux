namespace Nya
{
    using System.Diagnostics.CodeAnalysis;
    using ModelConverter.ParameterParser;

    /// <summary>
    /// Arguments view model
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    [CmdHelp("Nya format export plugin arguments.")]
    public class NyaArguments
    {
        /// <summary>
        /// Model shading types
        /// </summary>
        public enum ModelTypes
        {
            /// <summary>
            /// Completely ignore shading
            /// </summary>
            NoLight,

            /// <summary>
            /// Flat shaded model
            /// </summary>
            Flat,

            /// <summary>
            /// Smooth shaded model
            /// </summary>
            Smooth
        }

        /// <summary>
        /// Gets or sets a value indicating type of the model to export
        /// </summary>
        [CmdHelp("Model type can be either Smooth, Flat or NoLight.\nDefault value is NoLight.")]
        [CmdArgument("type", "t")]
        public ModelTypes ModelType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to generate fake UV mapping textures
        /// </summary>
        [CmdHelp("Makes exporter NOT generate new textures based on the UV map.")]
        [CmdArgument("no-unwrap", "w")]
        public bool NoUV { get; set; }

        /// <summary>
        /// Gets or sets the texture similarity threshold percentage (0.0-100.0) above which 2 textures will be considered identical
        /// as to reuse one texture in place of the other and therefore save space in memory.
        /// </summary>
        [CmdHelp("Texture similarity threshold percentage (0.0 to 100.0) above which 2 textures will be considered identical as to reuse one in place of the other and therefore save space in memory.\nDefault value is 100 (pixel perfect match).")]
        [CmdArgument("texture-merge-threshold", "m")]
        public double TextureMergeThreshold { get; set; } = 100.0;

    }
}