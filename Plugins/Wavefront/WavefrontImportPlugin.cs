namespace Wavefront
{
    using ModelConverter.Geometry;
    using ModelConverter.Graphics;
    using ModelConverter.PluginLoader;
    using System;
    using System.Globalization;
    using System.Reflection;

    [Plugin("Wavefront", "Wavefront .obj file importer", ".obj", typeof(ObjArguments))]
    public class WavefrontImportPlugin : ModelConverter.PluginLoader.IImportPlugin
    {
        /// <summary>
        /// Model scale
        /// </summary>
        private readonly double scale = 1.0;

        /// <summary>
        /// Model default sort mode
        /// </summary>
        private readonly ObjArguments.SortModes sortModes;

        /// <summary>
        /// Initializes a new instance of the <see cref="WavefrontImportPlugin"/> class
        /// </summary>
        /// <param name="settings">Application settings</param>
        public WavefrontImportPlugin(ObjArguments settings)
        {
            this.scale = settings.Scale ?? 1.0;
            this.sortModes = settings.SortMode ?? ObjArguments.SortModes.Mid;
        }

        /// <summary>
        /// Import from file
        /// </summary>
        /// <param name="inputFile">File path</param>
        /// <returns>Imported <see cref="Group"/></returns>
        public Group? Import(string inputFile)
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
                        models.Vertices.Add(WavefrontImportPlugin.ParseVertex(line, this.scale));
                        break;

                    case "vt":
                        models.Uv.Add(WavefrontImportPlugin.ParseVertex(line));
                        break;

                    case "vn":
                        models.Normals.Add(WavefrontImportPlugin.ParseVertex(line));
                        break;

                    case "f":

                        if (!models.Any())
                        {
                            models.Add(new Model());
                        }

                        models.Last().Faces.Add(WavefrontImportPlugin.ParseFace(line, lastMaterial, this.sortModes));
                        break;

                    default:
                        break;
                }
            }

            if (!WavefrontImportPlugin.ReadMtl(models, inputFile))
            {
                var file = Path.GetFileNameWithoutExtension(inputFile) + ".mtl";
                Console.WriteLine($"Error: '{file}' file was not found!");
                return null;
            }

            // Try to rebuild missing normals
            WavefrontImportPlugin.RebuildNormals(models);

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
	    if (string.IsNullOrWhiteSpace(mtlPath)) 
		return null;

	    try
	    {
		if (Path.IsPathRooted(mtlPath) && File.Exists(mtlPath))
		{
		    return mtlPath;
		}

		string combinedPath = Path.Combine(modelFolder, mtlPath);
		return Path.GetFullPath(combinedPath);
	    }
	    catch (Exception ex)
	    {
		// Log the exception if you have a logger, otherwise:
		Console.WriteLine($"Error resolving path: {ex.Message}");
		return null;
	    }
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
        /// <param name="defaultSortMode">Default sort mode</param>
        /// <returns>Parsed face</returns>
        private static Face ParseFace(string line, string material, ObjArguments.SortModes defaultSortMode)
        {
            Face face = new Face { Material = material };

            // Read user flags
            int separator = face.Material.LastIndexOf('_');

            if (separator > 0 && face.Material[separator] == '_')
            {
                HashSet<char> flags = face.Material.Substring(separator + 1).Where(letter => char.IsLetter(letter) && char.IsUpper(letter)).ToHashSet();

                face.IsMesh = flags.Contains('M');
                face.IsDoubleSided = flags.Contains('D');
                face.IsHalfTransparent = flags.Contains('H');
                face.IsFlat = flags.Contains('F');
                face.IsHalfBright = flags.Contains('B');
                face.SortMode = flags.Contains('C') ? 0 : (flags.Contains('L') ? 3 : (flags.Contains('-') ? 2 : (flags.Contains('+') ? 1 : (int)defaultSortMode)));
                face.IsWireframe = flags.Contains('W');
                face.NoLight = flags.Contains('N');
            }

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

                    if (components.Length >= 2 && !face.IsWireframe)
                    {
                        if (int.TryParse(components[1], out temp))
                        {
                            face.Uv.Add(temp - 1);
                        }
                        else
                        {
                            face.Uv.Add(-1);
                        }
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
        /// <param name="scale">Vertex scale</param>
        /// <returns>Parsed vertex</returns>
        private static Vector3D ParseVertex(string line, double scale = 1.0)
        {
            List<double> coordinates = line
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Skip(1)
                .Take(3)
                .Select(coordinate =>
                {
                    double value = 0.0;
                    double.TryParse(coordinate, System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture, out value);
                    return value * scale;
                })
                .ToList();

            if (coordinates.Count < 3)
            {
                coordinates = coordinates.Concat(Enumerable.Repeat(0.0, 3 - coordinates.Count)).ToList();
            }

            return new Vector3D(coordinates[0], coordinates[1], coordinates[2]);
        }

        /// <summary>
        /// Read texture definition file
        /// </summary>
        /// <param name="models">Loaded models</param>
        /// <param name="waveFrontFile">Path to the WaveFront file</param>
        /// <returns><c>true</c> if file was read properly</returns>
        private static bool ReadMtl(Group models, string waveFrontFile)
        {
            models.MaterialTextures = new Dictionary<string, Material>
            {
                { string.Empty, new Material { BaseColor = Color.FromRgb(byte.MaxValue, byte.MaxValue, byte.MaxValue) } }
            };

            string? modelDirectory = Path.GetDirectoryName(waveFrontFile);

            if (string.IsNullOrEmpty(modelDirectory))
            {
                modelDirectory = System.IO.Directory.GetCurrentDirectory();
            }

            string mtlFile = Path.Combine(modelDirectory, Path.GetFileNameWithoutExtension(waveFrontFile) + ".mtl");

            if (File.Exists(mtlFile))
            {
                string lastMaterial = string.Empty;

                foreach (string line in File.ReadLines(mtlFile).Where(line => !string.IsNullOrEmpty(line) && line.Contains(" ")))
                {
                    string lineCode = line.Substring(0, line.IndexOf(' ')).Trim();

                    switch (lineCode.ToLower())
                    {
                        case "newmtl":
                            models.MaterialTextures.Add(line.Replace(lineCode, string.Empty).Trim(), new Material());
                            lastMaterial = line.Replace(lineCode, string.Empty).Trim();
                            break;

                        case "kd":

                            if (models.MaterialTextures.ContainsKey(lastMaterial))
                            {
                                models.MaterialTextures[lastMaterial].BaseColor = WavefrontImportPlugin.ParseColor(line);
                            }

                            break;

                        case "map_kd":
                            string? file = WavefrontImportPlugin.GetAbsoluteTexturePath(line.Replace(lineCode, string.Empty).Trim(), modelDirectory);

                            if (!string.IsNullOrWhiteSpace(file) && models.MaterialTextures.ContainsKey(lastMaterial))
                            {
                                Color baseColor = models.MaterialTextures.ContainsKey(lastMaterial) ? models.MaterialTextures[lastMaterial].BaseColor : Color.FromRgb(byte.MaxValue, byte.MaxValue, byte.MaxValue);

                                models.MaterialTextures[lastMaterial] = new TextureReferenceMaterial
                                {
                                    BaseColor = baseColor,
                                    TexturePath = file ?? string.Empty
                                };
                            }

                            break;

                        default:
                            break;
                    }
                }
            }
            else
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Rebuild missing normals
        /// </summary>
        /// <param name="models">Model objects</param>
        private static void RebuildNormals(Group models)
        {
            foreach (var model in models)
            {
                foreach (var face in model.Faces)
                {
                    if (!face.Normals.Any())
                    {
                        List<Vector3D> vertexNormals = new List<Vector3D>();

                        // Collect all vertex normals
                        for (int vert = 0; vert < face.Vertices.Count; vert++)
                        {
                            var segment1 = models.Vertices[face.Vertices[(vert + 1) % face.Vertices.Count]] - models.Vertices[face.Vertices[vert]];
                            var segment2 = models.Vertices[face.Vertices[vert - 1 < 0 ? (face.Vertices.Count - 1) : (vert -1)]] - models.Vertices[face.Vertices[vert]];

                            var cross = new Vector3D(0.0, 0.0, 1.0);

                            if (segment1.GetLength() > 0.0 && segment2.GetLength() > 0.0)
                            {
                                cross = segment1.GetNormal().Cross(segment2.GetNormal()).GetNormal();
                            }

                            vertexNormals.Add(cross);
                        }

                        face.Normals.AddRange(Enumerable.Repeat(models.Normals.Count, face.Vertices.Count));
                        models.Normals.Add(vertexNormals.Aggregate((first, second) => first + second).GetNormal());
                    }
                    else if (face.Normals.Count < face.Vertices.Count)
                    {
                        // Some normals are missing, just repeat last one
                        face.Normals.AddRange(Enumerable.Repeat(face.Normals.Last(), face.Vertices.Count - face.Normals.Count));
                    }
                }
            }
        }
    }
}
