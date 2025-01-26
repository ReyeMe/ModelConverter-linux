namespace Nya
{
    using ModelConverter.Graphics;
    using Nya.Serializer;
    using SLIS = SixLabors.ImageSharp;

    /// <summary>
    /// Catgirl texture
    /// </summary>
    public class Texture
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Texture"/> class
        /// </summary>
        public Texture()
        {
            this.Data = new ushort[0];
            this.Width = 0;
            this.Height = 0;
            this.Name = string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Texture"/> class
        /// </summary>
        /// <param name="name">Texture name</param>
        /// <param name="width">bitmap width</param>
        /// <param name="height">Bitmap height</param>
        /// <param name="data">Bitmap data</param>
        public Texture(string name, ushort width, ushort height, ushort[] data) : this()
        {
            this.Name = name;
            this.Width = width;
            this.Height = height;
            this.Data = data;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Texture"/> class
        /// </summary>
        /// <param name="name">Texture name</param>
        /// <param name="bitmap">Bitmap data</param>
        public Texture(string name, SLIS.Image<SLIS.PixelFormats.Argb32> bitmap) : this()
        {
            this.Name = name;
            this.Width = (ushort)bitmap.Width;
            this.Height = (ushort)bitmap.Height;
            List<Color> colors = new List<Color>();

            for (int y = this.Height - 1; y >= 0; y--)
            {
                for (int x = 0; x < this.Width; x++)
                {
                    SLIS.PixelFormats.Argb32 color = bitmap[x, y];

                    if (color.A < 0x80)
                    {
                        colors.Add(Color.FromRgb(0,0,0,0));
                    }
                    else
                    {
                        colors.Add(Color.FromRgb(color.R, color.G, color.B));
                    }
                }
            }

            this.Data = colors.Select(color => color.AsAbgr555()).ToArray();
        }

        /// <summary>
        /// Gets or sets image data
        /// </summary>
        [ArraySizeDynamic("DataLength")]
        [FieldOrder(2)]
        public ushort[] Data { get; set; }

        /// <summary>
        /// Gets data length
        /// </summary>
        public int DataLength => this.Width * this.Height;

        /// <summary>
        /// Material name
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets image height
        /// </summary>
        [FieldOrder(1)]
        public ushort Height { get; set; }

        /// <summary>
        /// Gets image width
        /// </summary>
        [FieldOrder(0)]
        public ushort Width { get; set; }
    }
}
