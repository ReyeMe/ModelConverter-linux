namespace Tmf
{
    using ModelConverter.Geometry;
    using ModelConverter.PluginLoader;
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Text;

    /// <summary>
    /// Export plugin for Tank Game
    /// </summary>
    [Plugin("TankGame", "Export for sega Saturn game \"TankGame\"", ".TMF")]
    public class TankModelFormatExportPlugin : ModelConverter.PluginLoader.IExportPlugin
    {
        /// <summary>
        /// Vertices scale
        /// </summary>
        private const double DoubleScale = 65536.0;

        /// <summary>
        /// Initializes a new instance of the <see cref="TankModelFormatExportPlugin"/> class
        /// </summary>
        /// <param name="settings">Application settings</param>
        public TankModelFormatExportPlugin(ModelConverter.ArgumentSettings settings)
        {
            // Do nothing
        }

        /// <summary>
        /// Export group as output file
        /// </summary>
        /// <param name="group">Model group instance</param>
        /// <param name="outputFile">Output file path</param>
        /// <returns></returns>
        public bool Export(Group model, string outputFile)
        {
            try
            {
                // Check model integrity
                if (model.Count > byte.MaxValue)
                {
                    throw new Exception("Maximum number of models in one file can be 256!");
                }
                else if (model.Count == 0)
                {
                    throw new Exception("File does not contain any models");
                }
                else if (model.MaterialTextures.Count > byte.MaxValue)
                {
                    throw new Exception("Maximum number of textures refereced in one file can be 256!");
                }

                // Sort materials by putting textures first.
                // This will make it so that indexes pointing to the texture are aligned properly,
                // and not point to something that does not exist
                Dictionary<string, Material> sortedMaterials = model.MaterialTextures
                    .OrderBy(material => string.IsNullOrEmpty(material.Value.TexturePath)).ToDictionary(material => material.Key, material => material.Value);

                // Generate model binary
                TmfHeader header = new TmfHeader
                {
                    Type = TmFType.Static,
                    TextureCount = (byte)model.MaterialTextures.Count,
                    ModelCount = (byte)model.Count,
                    Reserved = Enumerable.Repeat((byte)0x00, 5).ToArray(),
                    Textures = sortedMaterials.Select(material => TankModelFormatExportPlugin.GetTextureEntry(material.Value)).ToArray(),
                    Models = model.Select(item => TankModelFormatExportPlugin.GetModelEntry(item, sortedMaterials, model.Vertices, model.Uv, model.Normals)).ToArray()
                };

                File.WriteAllBytes(outputFile, TankModelFormatExportPlugin.GetBytes(header));

                // Report model statistics
                TankModelFormatExportPlugin.GenerateReport(ref header);

                // Return success
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return false;
        }

        /// <summary>
        /// Model report
        /// </summary>
        /// <param name="header">Model header</param>
        private static void GenerateReport(ref TmfHeader header)
        {
            ConsoleColor bg = Console.BackgroundColor;
            ConsoleColor fg = Console.ForegroundColor;

            int textures = header.Textures.Count(texture => texture.FileName[0] != '\0');

            Console.WriteLine();
            Console.BackgroundColor = fg;
            Console.ForegroundColor = bg;
            Console.WriteLine(" Model export report                 ");
            Console.BackgroundColor = bg;
            Console.ForegroundColor = fg;
            Console.WriteLine(" Type:\t\t\t| {0}", header.Type.ToString());
            Console.WriteLine(" Objects:\t\t| {0}", header.ModelCount);
            Console.WriteLine(" Materials:\t\t| {0}", header.TextureCount);
            Console.WriteLine(" Textures:\t\t| {0}", textures);
            Console.WriteLine(" Total vertices:\t| {0}", header.ModelCount > 0 ? header.Models.Sum(model => model.VerticesCount) : 0);
            Console.WriteLine(" Total faces:\t\t| {0}", header.ModelCount > 0 ? header.Models.Sum(model => model.FaceCount) : 0);
            Console.WriteLine();

            if (textures > 0)
            {
                Console.BackgroundColor = fg;
                Console.ForegroundColor = bg;
                Console.WriteLine(" Used textures                       ");
                Console.BackgroundColor = bg;
                Console.ForegroundColor = fg;

                string[] names = header.Textures.Where(texture => texture.FileName[0] != '\0').Select(texture => Encoding.ASCII.GetString(texture.FileName)).ToArray();
                Console.WriteLine(" " + string.Join(", ", names));

                Console.WriteLine();
            }
        }

        /// <summary>
        /// Get bytes from object
        /// </summary>
        /// <param name="data">Object to get bytes from</param>
        /// <returns>Byte array</returns>
        private static byte[] GetBytes(object data)
        {
            List<byte> bytes = new List<byte>();
            Type dataType = data.GetType();

            if (dataType.IsValueType && !dataType.IsPrimitive && !dataType.IsEnum)
            {
                foreach (FieldInfo field in dataType.GetFields().OrderBy(field => Marshal.OffsetOf(dataType, field.Name).ToInt32()))
                {
                    object? value = field.GetValue(data);

                    if (value != null)
                    {
                        bytes.AddRange(TankModelFormatExportPlugin.GetBytes(value));
                    }
                }
            }
            else
            {
                if (dataType.IsArray)
                {
                    foreach (object item in (Array)data)
                    {
                        bytes.AddRange(TankModelFormatExportPlugin.GetBytes(item));
                    }
                }
                else if (dataType.IsEnum)
                {
                    bytes.Add((byte)data);
                }
                else
                {
                    int length = Marshal.SizeOf(data);
                    byte[] array = new byte[length];
                    IntPtr ptr = Marshal.AllocHGlobal(length);
                    Marshal.StructureToPtr(data, ptr, true);
                    Marshal.Copy(ptr, array, 0, length);
                    Marshal.FreeHGlobal(ptr);
                    bytes.AddRange(array.Reverse());
                }
            }

            return bytes.ToArray();
        }

        /// <summary>
        /// Rotate array left
        /// </summary>
        /// <typeparam name="T">Array type</typeparam>
        /// <param name="input">Input array</param>
        /// <param name="ammount">Rotate amount</param>
        /// <returns>Rotated array</returns>
        private static T[] RotateLeft<T>(IEnumerable<T> input, int ammount)
        {
            T[] array = input.ToArray();
            ammount = ammount % array.Length;
            T[] buffer = new T[ammount];
            Array.Copy(array, buffer, ammount);
            Array.Copy(array, ammount, array, 0, array.Length - ammount);
            Array.Copy(buffer, 0, array, array.Length - ammount, ammount);
            return array;
        }

        /// <summary>
        /// Polygon orientation
        /// </summary>
        /// <param name="polygon">Oriented polygon</param>
        /// <returns>True if CW</returns>
        private static bool IsClockwisePolygon(Vector3D[] polygon)
        {
            double sum = 0;

            for (int i = 0; i < polygon.Length-1; i++)
            {
                sum += (polygon[i + 1].X - polygon[i].X) * (polygon[i + 1].Y + polygon[i].Y);
            }

            return (sum > 0) ? true : false;
        }

        /// <summary>
        /// Get face entry
        /// </summary>
        /// <param name="face">Model face</param>
        /// <param name="materials">Model materials</param>
        /// <param name="vertices">Global vertices</param>
        /// <param name="uv">UV points</param>
        /// <param name="localVertices">Local vertices</param>
        /// <param name="normals">Global normals</param>
        /// <returns>Face entry</returns>
        private static TmfFace GetFaceEntry(
                    Face face,
                    Dictionary<string, Material> materials,
                    List<Vector3D> vertices,
                    List<Vector3D> uv,
                    Dictionary<int, TmfVertice> localVertices,
                    List<Vector3D> normals)
        {
            List<Vector3D> uvs = face.Uv.Any(id => id < 0) ? new() : face.Uv.Select(id => uv[id]).ToList();
            ushort[] indexes = face.Vertices
                .Select(vertice => TankModelFormatExportPlugin.GetVerticeEntry(
                    vertice,
                    localVertices,
                    vertices[vertice].X,
                    vertices[vertice].Y,
                    vertices[vertice].Z))
                .ToArray();

            if (indexes.Length < 4)
            {
                indexes = indexes.Concat(Enumerable.Repeat(indexes[indexes.Length - 1], 4 - indexes.Length)).ToArray();
            }

            if (indexes.Length != 4)
            {
                throw new Exception("All faces must be quads!");
            }

            if (uvs.Any())
            {
                uvs = uvs.Concat(Enumerable.Repeat(uvs.Last(), 4 - uvs.Count)).ToList();

                if (!TankModelFormatExportPlugin.IsClockwisePolygon(uvs.ToArray()))
                {
                    // We need to reverse polygon
                    indexes.Reverse();
                    uvs.Reverse();

                    // we need to move start back to index 0
                    indexes = TankModelFormatExportPlugin.RotateLeft(indexes, 3);
                    uvs = TankModelFormatExportPlugin.RotateLeft(uvs, 3).ToList();
                }

                // Find origin
                Vector3D max = new Vector3D(vertices.Max(v => v.X), vertices.Max(v => v.Y), vertices.Max(v => v.Z));

                // Find start
                Vector3D start = uvs.OrderBy(v => Math.Sqrt(Math.Pow(v.X - max.X, 2) + Math.Pow(v.Y - max.Y, 2) + Math.Pow(v.Z - max.Z, 2))).First();

                // Find start index
                int index = uvs.FindIndex(item => item == start);

                // Rotate to start
                uvs = TankModelFormatExportPlugin.RotateLeft(uvs.ToArray(), index + 1).ToList();
                indexes = TankModelFormatExportPlugin.RotateLeft(indexes, index + 1);
            }

            Vector3D faceVector = face.Normals.Select(normal => normals[normal]).Aggregate((a, b) => a + b);
            faceVector.Normalize();

            int materialIndex = materials.Keys.ToList().IndexOf(face.Material);

            TmfFace entry = new TmfFace
            {
                TextureIndex = (byte)Math.Max(materialIndex, 0),
                Indexes = indexes,
                Normal = TankModelFormatExportPlugin.GetVertice(faceVector.X, faceVector.Y, faceVector.Z),
                Flags = TmfFaceFlags.None,
                Padding = Enumerable.Repeat((byte)0x00, 2).ToArray()
            };

            if (face.IsMesh)
            {
                entry.Flags |= TmfFaceFlags.Meshed;
            }

            if (face.IsDoubleSided)
            {
                entry.Flags |= TmfFaceFlags.DoubleSided;
            }

            return entry;
        }

        /// <summary>
        /// Get model entry
        /// </summary>
        /// <param name="model">3D model</param>
        /// <param name="materials">Model materials</param>
        /// <param name="vertices">Global model vertices</param>
        /// <param name="uv">UV point</param>
        /// <param name="normals">Global normals</param>
        /// <returns>Model entry</returns>
        private static TmfModelHeader GetModelEntry(
            Model model,
            Dictionary<string, Material> materials,
            List<Vector3D> vertices,
            List<Vector3D> uv,
            List<Vector3D> normals)
        {
            Dictionary<int, TmfVertice> localVertices = new Dictionary<int, TmfVertice>();
            TmfFace[] faces = model.Faces.Select(face => TankModelFormatExportPlugin.GetFaceEntry(face, materials, vertices, uv, localVertices, normals)).ToArray();
            TmfVertice[] modelVertices = localVertices.Values.ToArray();

            return new TmfModelHeader
            {
                FaceCount = (ushort)faces.Length,
                VerticesCount = (ushort)modelVertices.Length,
                Vertices = modelVertices,
                Faces = faces
            };
        }

        /// <summary>
        /// Get texture entry from material
        /// </summary>
        /// <param name="material">Face material</param>
        /// <returns>Texture entry</returns>
        private static TmfTextureEntry GetTextureEntry(Material material)
        {
            TmfTextureEntry entry = new TmfTextureEntry();

            if (!string.IsNullOrWhiteSpace(material.TexturePath))
            {
                string file = Path.GetFileName(material.TexturePath).ToUpper();
                byte[] bytes = Encoding.ASCII.GetBytes(file).Take(13).ToArray();
                entry.FileName = bytes.Concat(Enumerable.Repeat((byte)'\0', 13 - bytes.Length)).ToArray();
                entry.Color = Enumerable.Repeat(byte.MaxValue, 3).ToArray();
            }
            else
            {
                entry.FileName = Enumerable.Repeat((byte)'\0', 13).ToArray();
                entry.Color = new byte[]
                {
                    material.Color.R,
                    material.Color.G,
                    material.Color.B
                };
            }

            return entry;
        }

        /// <summary>
        /// Get vertice data
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="z">Z coordinate</param>
        /// <returns>Vertice data</returns>
        private static TmfVertice GetVertice(double x, double y, double z)
        {
            return new TmfVertice
            {
                X = (Int32)(x * TankModelFormatExportPlugin.DoubleScale),
                Y = (Int32)(y * TankModelFormatExportPlugin.DoubleScale),
                Z = (Int32)(z * TankModelFormatExportPlugin.DoubleScale),
            };
        }

        /// <summary>
        /// Get vertice entry
        /// </summary>
        /// <param name="vertice">Vertice index</param>
        /// <param name="localVertices">List of local vertices</param>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="z">Z coordinate</param>
        /// <returns>Vertice entry index</returns>
        private static ushort GetVerticeEntry(int vertice, Dictionary<int, TmfVertice> localVertices, double x, double y, double z)
        {
            int result;

            if (!localVertices.ContainsKey(vertice))
            {
                localVertices.Add(vertice, TankModelFormatExportPlugin.GetVertice(x, y, z));
            }

            result = localVertices.Keys.ToList().IndexOf(vertice);

            if (result > ushort.MaxValue)
            {
                throw new Exception("Too many vertices!");
            }

            return (ushort)result;
        }
    }
}