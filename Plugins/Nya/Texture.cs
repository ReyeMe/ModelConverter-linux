﻿namespace Nya
{
    using System.Reflection.Metadata;
    using ModelConverter.Geometry;
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
            this.UV = new int[]
            {
                0,
                0,
                0,
                0
            };
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

            for (int y = 0; y < this.Height; y++)
            {
                for (int x = 0; x < this.Width; x++)
                {
                    SLIS.PixelFormats.Argb32 color = bitmap[x, y];

                    if (color.A < 0x80)
                    {
                        colors.Add(Color.FromRgb(0, 0, 0, 0));
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
        /// Gets image height
        /// </summary>
        [FieldOrder(1)]
        public ushort Height { get; set; }

        /// <summary>
        /// Material name
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets or sets UV map this texture belongs to
        /// </summary>
        public int[] UV { get; set; }

        /// <summary>
        /// Gets image width
        /// </summary>
        [FieldOrder(0)]
        public ushort Width { get; set; }

        /// <summary>
        /// Image hash
        /// </summary>
        private string hash = string.Empty;

        /// <summary>
        /// Gets image hash
        /// </summary>
        public string Hash
        {
            get
            {
                if (string.IsNullOrWhiteSpace(this.hash))
                {
                    using (var sha1 = System.Security.Cryptography.SHA1.Create())
                    {
                        this.hash = string.Concat(sha1.ComputeHash(this.Data.SelectMany(pair => new byte[] { (byte)((pair >> 8) & 0xf), (byte)(pair & 0xf) }).ToArray()).Select(x => x.ToString("X2")));
                    }
                }

                return this.hash;
            }
        }

        /// <summary>
        /// Get UV unwrap texture
        /// </summary>
        /// <param name="baseTexture">base texture</param>
        /// <param name="uv">UV coords</param>
        /// <returns>Unwrapped texture</returns>
        public static Texture GetUnwrap(Texture baseTexture, List<Vector3D> uv)
        {
            // Get region bounds
            Vector3D min = new Vector3D(uv.Min(comp => comp.X), uv.Min(comp => comp.Y), 0.0);
            Vector3D max = new Vector3D(uv.Max(comp => comp.X), uv.Max(comp => comp.Y), 0.0);

            // Get unwrap texture size
            ushort width = (ushort)Math.Max(Math.Round((Math.Abs(max.X - min.X) * baseTexture.Width) / 8.0) * 8.0, 8.0);
            ushort height = (ushort)Math.Max((Math.Abs(max.Y - min.Y) * baseTexture.Height), 1.0);

            // New empty texture
            Texture unwrap = new Texture(baseTexture.Name + "+" + Guid.NewGuid().ToString(), width, height, new ushort[width * height]);

            // UV map polygon axies
            Vector3D uvTopDirection = uv[1] - uv[0];
            Vector3D uvBottomDirection = uv[2] - uv[3];
            List<ushort> data = new List<ushort>();

            // Render to rectangle
            for (int y = height - 1; y >= 0; y--)
            {
                double portionY = ((y + 1) / (double)height);

                for (int x = 0; x < width; x++)
                {
                    double portionX = ((x + 1) / (double)width);

                    // Calculate location on quad
                    Vector3D topLocation = uv[0] + (uvTopDirection * portionX);
                    Vector3D bottomLocation = uv[3] + (uvBottomDirection * portionX);
                    Vector3D uvLocation = bottomLocation + ((topLocation - bottomLocation) * portionY);

                    // Calculate location in the UV mapped texture
                    int uvX = (int)(uvLocation.X * (baseTexture.Width - 1));
                    int uvY = (baseTexture.Height - 1) - (int)(uvLocation.Y * (baseTexture.Height - 1));

                    // Handle repeating textures
                    if (uvX >= baseTexture.Width)
                    {
                        uvX %= baseTexture.Width;
                    }
                    else if (uvX < 0)
                    {
                        uvX = baseTexture.Width - (Math.Abs(uvX + 1) % baseTexture.Width) - 1;
                    }

                    if (uvY >= baseTexture.Height)
                    {
                        uvY %= baseTexture.Height;
                    }
                    else if (uvY < 0)
                    {
                        uvY = baseTexture.Height - (Math.Abs(uvY + 1) % baseTexture.Height) - 1;
                    }

                    // Write pixel to rectangle texture
                    data.Add(baseTexture.Data[(uvY * baseTexture.Width) + uvX]);
                }
            }

            unwrap.Data = data.ToArray();
            return unwrap;
        }

        /// <summary>
        /// Get base name
        /// </summary>
        /// <returns>Base name</returns>
        public string GetBaseName()
        {
            var id = this.Name.LastIndexOf('+');

            if (id > 0)
            {
                return this.Name[..id];
            }

            return this.Name;
        }
    }
}