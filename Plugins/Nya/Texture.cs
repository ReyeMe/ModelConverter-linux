namespace Nya
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

        /// <summary>
        /// Compares this texture with another one.
        /// The similarity score is based on color similarity and gradient (average detail/edge strength)
        /// between the 2 images and between sub parts of both images.
        /// </summary>
        /// <param name="other">The texture to compare to</param>
        /// <returns>A similarity score between 0.0 (completely different) and 100.0 (exactly the same)</returns>
        public double CalculateSimilarityTo(Texture other)
        {
            if (other == null)
            {
                return 0.0;
            }

            if (ReferenceEquals(this, other))
            {
                return 100.0;
            }

            return CalculateRecursiveSimilarity(this, other, 
                                                0, 0, this.Width, this.Height,
                                                0, 0, other.Width, other.Height);
        }

        /// <summary>
        /// Recursive similarity calculation using sliding windows.
        /// <param name="img1">The first image from which a region is being compared</param>
        /// <param name="img2">The second image from which a region is being compared</param>
        /// <param name="x1">x coordinate of the top left corner of the region of the first image being compared</param>
        /// <param name="y1">y coordinate of the top left corner of the region of the first image being compared</param>
        /// <param name="w1">Width of the region of the first image being compared</param>
        /// <param name="h1">Height of the region of the first image being compared</param>
        /// <param name="x2">x coordinate of the top left corner of the region of the second image being compared</param>
        /// <param name="y2">y coordinate of the top left corner of the region of the second image being compared</param>
        /// <param name="w2">Width of the region of the second image being compared</param>
        /// <param name="h2">Height of the region of the second image being compared</param>
        /// <returns>A similarity score between 0.0 (completely different) and 100.0 (exactly the same)</returns>
        /// </summary>
        private static double CalculateRecursiveSimilarity(
            Texture img1, Texture img2,
            int x1, int y1, int w1, int h1,   // région courante sur img1
            int x2, int y2, int w2, int h2)   // région courante sur img2
        {
            if (w1 * h1 < 4 || w2 * h2 < 4)
            {
                var avg1 = GetAverageColor(img1, x1, y1, w1, h1);
                var avg2 = GetAverageColor(img2, x2, y2, w2, h2);
                return ColorSimilarity(avg1, avg2);
            }

            // Mean color similarity
            var avgFullImg1 = GetAverageColor(img1, x1, y1, w1, h1);
            var avgFullImg2 = GetAverageColor(img2, x2, y2, w2, h2);
            double colorSimilarity = ColorSimilarity(avgFullImg1, avgFullImg2);

            // Structure similarity via mean gradient
            double grad1 = GetMeanGradient(img1, x1, y1, w1, h1);
            double grad2 = GetMeanGradient(img2, x2, y2, w2, h2);
            double gradientSimilarity = GradientSimilarity(grad1, grad2);

            // The deeper we go, the less relevant is structure similarity
            double depthWeight = (w1 / img1.Width)/2;
            double fullImgSimilarity = colorSimilarity * (1-depthWeight) + gradientSimilarity * (depthWeight);

            int midW1 = (w1 + 1) / 2;
            int midH1 = (h1 + 1) / 2;
            int midW2 = (w2 + 1) / 2;
            int midH2 = (h2 + 1) / 2;

            // We do the same for each quadrant of the img
            double tl = CalculateRecursiveSimilarity(img1, img2, x1, y1, midW1, midH1, x2, y2, midW2, midH2);
            double tr = CalculateRecursiveSimilarity(img1, img2, x1 + midW1, y1, w1 - midW1, midH1, x2 + midW2, y2, w2 - midW2, midH2);
            double bl = CalculateRecursiveSimilarity(img1, img2, x1, y1 + midH1, midW1, h1 - midH1, x2, y2 + midH2, midW2, h2 - midH2);
            double br = CalculateRecursiveSimilarity(img1, img2, x1 + midW1, y1 + midH1, w1 - midW1, h1 - midH1, x2 + midW2, y2 + midH2, w2 - midW2, h2 - midH2);

            double subImgSimilarity = (tl + tr + bl + br) / 4.0;

            // Weights can be adjusted as long as their sum is equal to 1.0
            return (fullImgSimilarity * 0.2 + subImgSimilarity * 0.8);
        }

        /// <summary>
        /// Computes average RGB color of a rectangular region in the texture.
        /// <param name="texture">The texture in which a region is being processed</param>
        /// <param name="startX">x coordinate of the top left corner of the region of the image</param>
        /// <param name="startY">y coordinate of the top left corner of the region of the image</param>
        /// <param name="width">width of the region of the image</param>
        /// <param name="height">height of the region of the image</param>
        /// <returns>The average color of the given region of the texture</returns>
        /// </summary>
        private static (byte R, byte G, byte B) GetAverageColor(Texture texture, int startX, int startY, int width, int height)
        {
            long sumR = 0, sumG = 0, sumB = 0;
            int count = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int px = startX + x;
                    int py = startY + y;

                    if (px < texture.Width && py < texture.Height)
                    {
                        ushort pixel = texture.Data[py * texture.Width + px];
                        byte r = (byte)((pixel & 0x1F) << 3);
                        byte g = (byte)(((pixel >> 5) & 0x1F) << 3);
                        byte b = (byte)(((pixel >> 10) & 0x1F) << 3);

                        sumR += r;
                        sumG += g;
                        sumB += b;
                        count++;
                    }
                }
            }

            if (count == 0)
            {
                return (0, 0, 0);
            }

            return (
                (byte)(sumR / count),
                (byte)(sumG / count),
                (byte)(sumB / count)
            );
        }

        /// <summary>
        /// Similarity between two RGB colors (0.0 to 100.0).
        /// Uses a simple RGB difference between the colors.
        /// <param name="c1">The first color being compared</param>
        /// <param name="c2">The second color being compared</param>
        /// <returns>A similarity score between 0.0 and 100.0</returns>
        /// </summary>
        private static double ColorSimilarity((byte R, byte G, byte B) c1, (byte R, byte G, byte B) c2)
        {
            double simR = (255.0 - Math.Abs(c1.R - c2.R)) / 255.0;
            double simG = (255.0 - Math.Abs(c1.G - c2.G)) / 255.0;
            double simB = (255.0 - Math.Abs(c1.B - c2.B)) / 255.0;

            double avgSim = (simR + simG + simB) / 3.0 * 100.0;
            const double maxDelta = 8; //Difference above which we consider 0% similarity

            return Math.Max(0, 100 * ((avgSim - (100 - maxDelta)) / maxDelta));
        }

        /// <summary>
        /// Calculate a similarity score between 0.0 and 100.0 between two gradient values.
        /// <param name="g1">The first gradient being compared</param>
        /// <param name="g2">The second gradient being compared</param>
        /// <returns>A similarity score between 0.0 (completely different) and 100.0 (identical)</returns>
        /// </summary>
        private static double GradientSimilarity(double g1, double g2)
        {
            if (g1 == 0 && g2 == 0)
            {
                return 100.0;
            }

            if (g1 == 0 || g2 == 0)
            {
                return 0.0;
            }

            double ratio = Math.Min(g1, g2) / Math.Max(g1, g2);

            return ratio * 100.0;
        }

        /// <summary>
        /// Calculates the mean gradient (average detail/edge strength) of a region.
        /// Higher value = more details/texture variation.
        /// <param name="texture">The texture in which a region is being processed</param>
        /// <param name="startX">x coordinate of the top left corner of the region of the image</param>
        /// <param name="startY">y coordinate of the top left corner of the region of the image</param>
        /// <param name="width">width of the region of the image</param>
        /// <param name="height">height of the region of the image</param>
        /// <returns>The mean gradient of the given region of the texture</returns>
        /// </summary>
        private static double GetMeanGradient(Texture texture, int startX, int startY, int width, int height)
        {
            if (width < 2 || height < 2)
            {
                return 0.0;
            }

            long totalGradient = 0;
            int count = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int px = startX + x;
                    int py = startY + y;

                    if (px >= texture.Width - 1 || py >= texture.Height - 1)
                    {
                        continue;
                    }

                    ushort p1 = texture.Data[py * texture.Width + px];           // current pixel
                    ushort p2 = texture.Data[py * texture.Width + (px + 1)];     // right
                    ushort p3 = texture.Data[(py + 1) * texture.Width + px];     // below

                    byte r1 = (byte)((p1 & 0x1F) << 3);
                    byte g1 = (byte)(((p1 >> 5) & 0x1F) << 3);
                    byte b1 = (byte)(((p1 >> 10) & 0x1F) << 3);

                    byte r2 = (byte)((p2 & 0x1F) << 3);
                    byte g2 = (byte)(((p2 >> 5) & 0x1F) << 3);
                    byte b2 = (byte)(((p2 >> 10) & 0x1F) << 3);

                    byte r3 = (byte)((p3 & 0x1F) << 3);
                    byte g3 = (byte)(((p3 >> 5) & 0x1F) << 3);
                    byte b3 = (byte)(((p3 >> 10) & 0x1F) << 3);

                    // Simple luminance approximation
                    int lum1 = (r1 * 299 + g1 * 587 + b1 * 114) / 1000;
                    int lum2 = (r2 * 299 + g2 * 587 + b2 * 114) / 1000;
                    int lum3 = (r3 * 299 + g3 * 587 + b3 * 114) / 1000;

                    totalGradient += Math.Abs(lum1 - lum2) + Math.Abs(lum1 - lum3);
                    count++;
                }
            }

            return count == 0 ? 0.0 : (double) totalGradient / count;
        }
    }
}