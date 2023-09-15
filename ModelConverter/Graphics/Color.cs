namespace ModelConverter.Graphics
{
    /// <summary>
    /// Color data
    /// </summary>
    public struct Color
    {
        /// <summary>
        /// Prevents a default instance of the <see cref="Color"/> struct from being created.
        /// </summary>
        /// <param name="r">Red component</param>
        /// <param name="g">Green component</param>
        /// <param name="b">Blue component</param>
        private Color(byte r, byte g, byte b)
        {
            this.R = r;
            this.G = g;
            this.B = b;
        }

        /// <summary>
        /// Create color from component values
        /// </summary>
        /// <param name="r">Red component</param>
        /// <param name="g">Green component</param>
        /// <param name="b">Blue component</param>
        /// <returns>Color data</returns>
        public static Color FromRgb(byte r, byte g, byte b)
        {
            return new Color(r, g, b);
        }

        /// <summary>
        /// Red component
        /// </summary>
        public byte R { get; private set; }

        /// <summary>
        /// Green component
        /// </summary>
        public byte G { get; private set; }

        /// <summary>
        /// Blue component
        /// </summary>
        public byte B { get; private set; }
    }
}
