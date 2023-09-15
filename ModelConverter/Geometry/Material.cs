namespace ModelConverter.Geometry
{
    using ModelConverter.Graphics;

    /// <summary>
    /// MTL material
    /// </summary>
    public class Material
    {
        /// <summary>
        /// Gets or sets material color
        /// </summary>
        public Color Color { get; set; } = Color.FromRgb(byte.MaxValue, byte.MaxValue, byte.MaxValue);

        /// <summary>
        /// Gets or sets path to the texture file
        /// </summary>
        public string TexturePath { get; set; } = string.Empty;
    }

}
