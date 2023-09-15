namespace Wavefront
{
    using ModelConverter.Geometry;
    using ModelConverter.Graphics;
    using ModelConverter.PluginLoader;
    using System.Globalization;

    [Plugin("Wavefront", "Wavefront .obj file importer", ".obj")]
    public class WavefrontImportPlugin : ModelConverter.PluginLoader.IImportPlugin
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WavefrontImportPlugin"/> class
        /// </summary>
        /// <param name="settings">Application settings</param>
        public WavefrontImportPlugin(ModelConverter.ArgumentSettings settings)
        {
            // Do nothing
        }

        /// <summary>
        /// Import from file
        /// </summary>
        /// <param name="inputFile">File path</param>
        /// <returns>Imported <see cref="Group"/></returns>
        public Group Import(string inputFile)
        {
            Group models = new Group();
            string lastMaterial = string.Empty;

            foreach (string line in File.ReadLines(inputFile).Where(line => !line.StartsWith("#") && !line.StartsWith("vp") && !line.StartsWith("l") && line.Contains(' ')))
            {
                string lineCode = line.Substring(0, line.IndexOf(' ')).Trim();

                switch (lineCode)
                {
                    case "o":
                        models.Add(new Model() { Name = line.Remove(0, 2).Trim() });
                        break;

                    case "usemtl":
                        lastMaterial = line.Substring(6).Trim();
                        break;

                    case "v":
                        models.Vertices.Add(WavefrontImportPlugin.ParseVertex(line));
                        break;

                    case "vn":
                        models.Normals.Add(WavefrontImportPlugin.ParseVertex(line));
                        break;

                    case "f":

                        if (!models.Any())
                        {
                            models.Add(new Model());
                        }

                        models.Last().Faces.Add(WavefrontImportPlugin.ParseFace(line, lastMaterial));
                        break;

                    default:
                        break;
                }
            }

            WavefrontImportPlugin.ReadMtl(models, inputFile);
            return models;
        }

        /// <summary>
        /// Get absolute path to the texture file
        /// </summary>
        /// <param name="mtlPath">MTL texture path</param>
        /// <param name="modelFolder">Model folder path</param>
        /// <returns>Absolute texture file path</returns>
        private static string? GetAbsoluteTexturePath(string mtlPath, string modelFolder)
        {
            try
            {
                if (File.Exists(mtlPath))
                {
                    return mtlPath.ToLower();
                }
            }
            catch (Exception ex)
            {
                ex.ToString();
            }

            if (!string.IsNullOrWhiteSpace(mtlPath))
            {
                return Path.Combine(modelFolder, mtlPath).ToLower();
            }

            return null;
        }

        /// <summary>
        /// parse color
        /// </summary>
        /// <param name="line">Diffuse color</param>
        /// <returns>Solid color</returns>
        private static Color ParseColor(string line)
        {
            Vector3D color = WavefrontImportPlugin.ParseVertex(line);
            return Color.FromRgb((byte)(byte.MaxValue * color.X), (byte)(byte.MaxValue * color.Y), (byte)(byte.MaxValue * color.Z));
        }

        /// <summary>
        /// Parse face line
        /// </summary>
        /// <param name="line">Face line</param>
        /// <param name="material">Current material</param>
        /// <returns>Parsed face</returns>
        private static Face ParseFace(string line, string material)
        {
            Face face = new Face { Material = material };

            foreach (string vertex in line.Substring(1).Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] components = vertex.Split(new[] { '/' }, StringSplitOptions.None);

                if (components.Any())
                {
                    int temp;

                    if (int.TryParse(components.First(), out temp))
                    {
                        face.Vertices.Add(temp - 1);
                    }

                    if (components.Length == 3 && int.TryParse(components.Last(), out temp))
                    {
                        face.Normals.Add(temp - 1);
                    }
                }
            }

            return face;
        }

        /// <summary>
        /// Parse vertex
        /// </summary>
        /// <param name="line">Vertex line</param>
        /// <returns>Parsed vertex</returns>
        private static Vector3D ParseVertex(string line)
        {
            List<double> coordinates = line
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Skip(1)
                .Take(3)
                .Select(coordinate =>
                {
                    double value = 0.0;
                    double.TryParse(coordinate, System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture, out value);
                    return value;
                })
                .ToList();

            return new Vector3D(coordinates[0], coordinates[1], coordinates[2]);
        }

        /// <summary>
        /// Read texture definition file
        /// </summary>
        /// <param name="models">Loaded models</param>
        /// <param name="waveFrontFile">Path to the WaveFront file</param>
        private static void ReadMtl(Group models, string waveFrontFile)
        {
            models.MaterialTextures = new Dictionary<string, Material>
            {
                { string.Empty, new Material { Color = Color.FromRgb(byte.MaxValue, byte.MaxValue, byte.MaxValue), TexturePath = string.Empty } }
            };

            string? modelDirectory = Path.GetDirectoryName(waveFrontFile);

            if (string.IsNullOrEmpty(modelDirectory))
            {
                return;
            }

            string mtlFile = Path.Combine(modelDirectory, Path.GetFileNameWithoutExtension(waveFrontFile) + ".mtl");

            if (File.Exists(mtlFile))
            {
                foreach (string line in File.ReadLines(mtlFile).Where(line => !string.IsNullOrEmpty(line) && line.Contains(" ")))
                {
                    string lineCode = line.Substring(0, line.IndexOf(' ')).Trim();

                    switch (lineCode.ToLower())
                    {
                        case "newmtl":
                            models.MaterialTextures.Add(line.Replace(lineCode, string.Empty).Trim(), new Material());
                            break;

                        case "kd":
                            models.MaterialTextures.Last().Value.Color = WavefrontImportPlugin.ParseColor(line);
                            break;

                        case "map_kd":
                            string? file = WavefrontImportPlugin.GetAbsoluteTexturePath(line.Replace(lineCode, string.Empty).Trim(), modelDirectory);
                            models.MaterialTextures.Last().Value.TexturePath = file ?? string.Empty;
                            break;

                        default:
                            break;
                    }
                }
            }
        }
    }
}