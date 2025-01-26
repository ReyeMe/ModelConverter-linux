namespace ModelConverter.Geometry
{
    using ModelConverter.Graphics;

    /// <summary>
    /// Solid color material
    /// </summary>
    public class Material
    {
        /// <summary>
        /// Gets or sets material base color
        /// </summary>
        public Color BaseColor { get; set; } = Color.FromRgb(byte.MaxValue, byte.MaxValue, byte.MaxValue);

        /// <summary>
        /// Compare if objects are the same
        /// </summary>
        /// <param name="obj">Other object</param>
        /// <returns>True if same</returns>
        public override bool Equals(object? obj)
        {
            return obj is Material mat ? this.BaseColor == mat.BaseColor : base.Equals(obj);
        }

        /// <summary>
        /// Get object hash
        /// </summary>
        /// <returns>Object hash</returns>
        public override int GetHashCode()
        {
            return this.BaseColor.GetHashCode();
        }
    }

    /// <summary>
    /// Texture in indexed ARGB format
    /// </summary>
    public class TextureIndexedMaterial : Material
    {
        /// <summary>
        /// Texture data
        /// </summary>
        public byte[]? Data { get; set; }

        /// <summary>
        /// Texture height
        /// </summary>
        public int Height { get; set; } = 0;

        /// <summary>
        /// Texture color palette
        /// </summary>
        public Color[]? Palette { get; set; }

        /// <summary>
        /// Texture width
        /// </summary>
        public int Width { get; set; } = 0;

        /// <summary>
        /// Compare if objects are the same
        /// </summary>
        /// <param name="obj">Other object</param>
        /// <returns>True if same</returns>
        public override bool Equals(object? obj)
        {
            return obj is TextureIndexedMaterial mat ?
                (this.BaseColor == mat.BaseColor &&
                this.Width == mat.Width &&
                this.Height == mat.Height &&
                this.Palette != null &&
                mat.Palette != null &&
                this.Data != null &&
                mat.Data != null &&
                this.Data.Select((value, index) => value == mat.Data[index]).All(result => result) &&
                this.Palette.Select((value, index) => value == mat.Palette[index]).All(result => result))
                : base.Equals(obj);
        }

        /// <summary>
        /// Get object hash
        /// </summary>
        /// <returns>Object hash</returns>
        public override int GetHashCode()
        {
            int paletteHash = this.Palette?.Select(x => x.GetHashCode()).Sum() ?? 0;
            int dataHash = this.Data?.Select(x => (int)x).Sum() ?? 0;
            return Tuple.Create(this.BaseColor, this.Width, this.Height, dataHash).GetHashCode();
        }
    }

    /// <summary>
    /// Texture in ARGB format
    /// </summary>
    public class TextureMaterial : Material
    {
        /// <summary>
        /// Texture data
        /// </summary>
        public Color[]? Data { get; set; }

        /// <summary>
        /// Texture height
        /// </summary>
        public int Height { get; set; } = 0;

        /// <summary>
        /// Texture width
        /// </summary>
        public int Width { get; set; } = 0;

        /// <summary>
        /// Compare if objects are the same
        /// </summary>
        /// <param name="obj">Other object</param>
        /// <returns>True if same</returns>
        public override bool Equals(object? obj)
        {
            return obj is TextureMaterial mat ?
                (this.BaseColor == mat.BaseColor && this.Width == mat.Width && this.Height == mat.Height && this.Data != null && mat.Data != null && this.Data.Select((value, index) => value == mat.Data[index]).All(result => result))
                : base.Equals(obj);
        }

        /// <summary>
        /// Get object hash
        /// </summary>
        /// <returns>Object hash</returns>
        public override int GetHashCode()
        {
            int dataHash = this.Data?.Select(x => x.GetHashCode()).Sum() ?? 0;
            return Tuple.Create(this.BaseColor, this.Width, this.Height, dataHash).GetHashCode();
        }
    }

    /// <summary>
    /// Texture reference by path material
    /// </summary>
    public class TextureReferenceMaterial : Material
    {
        /// <summary>
        /// Gets or sets path to the texture file
        /// </summary>
        public string TexturePath { get; set; } = string.Empty;

        /// <summary>
        /// Compare if objects are the same
        /// </summary>
        /// <param name="obj">Other object</param>
        /// <returns>True if same</returns>
        public override bool Equals(object? obj)
        {
            return obj is TextureReferenceMaterial mat ? (this.BaseColor == mat.BaseColor && this.TexturePath == mat.TexturePath) : base.Equals(obj);
        }

        /// <summary>
        /// Get object hash
        /// </summary>
        /// <returns>Object hash</returns>
        public override int GetHashCode()
        {
            return Tuple.Create(this.BaseColor, this.TexturePath).GetHashCode();
        }
    }
}