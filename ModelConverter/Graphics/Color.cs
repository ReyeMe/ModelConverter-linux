namespace ModelConverter.Graphics
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Color data
    /// </summary>
    public struct Color
    {
        /// <summary>
        /// Color depth for ABGR555
        /// </summary>
        private const short Depth555 = 0x1f;

        /// <summary>
        /// Prevents a default instance of the <see cref="Color"/> struct from being created.
        /// </summary>
        /// <param name="r">Red component</param>
        /// <param name="g">Green component</param>
        /// <param name="b">Blue component</param>
        /// <param name="a">Alpha component</param>
        private Color(byte r, byte g, byte b, byte a = byte.MaxValue)
        {
            this.R = r;
            this.G = g;
            this.B = b;
            this.A = a;
        }

        /// <summary>
        /// Alpha channel
        /// </summary>
        public byte A { get; private set; }

        /// <summary>
        /// Blue component
        /// </summary>
        public byte B { get; private set; }

        /// <summary>
        /// Green component
        /// </summary>
        public byte G { get; private set; }

        /// <summary>
        /// Red component
        /// </summary>
        public byte R { get; private set; }

        /// <summary>
        /// Convert from ABGR555 to ARGB
        /// </summary>
        /// <param name="value">Color value</param>
        /// <returns>Color data</returns>
        public static Color FromAbgr555(ushort value)
        {
            return Color.FromRgb(
                (byte)((((value & 0x1f)) / (float)Color.Depth555) * byte.MaxValue),
                (byte)((((value & 0x3e0) >> 5) / (float)Color.Depth555) * byte.MaxValue),
                (byte)((((value & 0x7c00) >> 10) / (float)Color.Depth555) * byte.MaxValue),
                (value & 0x8000) > 0 ? byte.MaxValue : byte.MinValue);
        }

        /// <summary>
        /// Create color from component values
        /// </summary>
        /// <param name="r">Red component</param>
        /// <param name="g">Green component</param>
        /// <param name="b">Blue component</param>
        /// <param name="a">Alpha component</param>
        /// <returns>Color data</returns>
        public static Color FromRgb(byte r, byte g, byte b, byte a = byte.MaxValue)
        {
            return new Color(r, g, b, a);
        }

        /// <summary>
        /// Unequality operator
        /// </summary>
        /// <param name="left">First object</param>
        /// <param name="right">Second object</param>
        /// <returns>True if NOT same</returns>
        public static bool operator !=(Color left, Color right)
        {
            return !(left==right);
        }

        /// <summary>
        /// Equality operator
        /// </summary>
        /// <param name="left">First object</param>
        /// <param name="right">Second object</param>
        /// <returns>True if same</returns>
        public static bool operator ==(Color left, Color right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Convert to ABGR555 format
        /// </summary>
        /// <returns>ABGR555 value</returns>
        public ushort AsAbgr555()
        {
            ushort r = (byte)(Color.Depth555 * (this.R / (float)byte.MaxValue));
            ushort g = (byte)(Color.Depth555 * (this.G / (float)byte.MaxValue));
            ushort b = (byte)(Color.Depth555 * (this.B / (float)byte.MaxValue));
            return (ushort)((this.A > 128 ? 0x8000 : 0) | (b << 10) | (g << 5) | r);
        }

        /// <summary>
        /// Custom equals
        /// </summary>
        /// <param name="obj">Other object</param>
        /// <returns>True if same</returns>
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return (obj is Color color && color.A == this.A && color.R == this.R && color.B == this.B && color.G == this.G) || base.Equals(obj);
        }

        /// <summary>
        /// Get object hash
        /// </summary>
        /// <returns>Object hash</returns>
        public override int GetHashCode()
        {
            return Tuple.Create(this.A, this.R, this.G, this.B).GetHashCode();
        }
    }
}