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
        [CmdHelp("Model type can be either Smooth or Flat.\nDefault value is flat.")]
        [CmdArgument("type", "t")]
        public ModelTypes ModelType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to generate fake UV mapping textures
        /// </summary>
        [CmdHelp("Makes exporter NOT generate new textures based on the UV map.")]
        [CmdArgument("no-unwrap", "w")]
        public bool NoUV { get; set; }
    }
}