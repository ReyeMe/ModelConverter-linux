namespace Cobra
{
    using ModelConverter.Geometry;
    using ModelConverter.PluginLoader;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Text;

    /// <summary>
    /// Export plugin for Tank Game
    /// </summary>
    [Plugin("Cobra", "Export for sega Saturn game by \"Cobra!\"", ".CBR")]
    public class CobraExportPlugin : ModelConverter.PluginLoader.IExportPlugin
    {
        /// <summary>
        /// Vertices scale
        /// </summary>
        private const double DoubleScale = 65536.0;

        /// <summary>
        /// Initializes a new instance of the <see cref="TankModelFormatExportPlugin"/> class
        /// </summary>
        /// <param name="settings">Application settings</param>
        public CobraExportPlugin(ModelConverter.ArgumentSettings settings)
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
                if (model.Count > ushort.MaxValue)
                {
                    throw new Exception("Maximum number of models in one file can be 65536!");
                }
                else if (model.Count == 0)
                {
                    throw new Exception("File does not contain any models");
                }
                else if (model.MaterialTextures.Count > ushort.MaxValue)
                {
                    throw new Exception("Maximum number of textures refereced in one file can be 65536!");
                }

                CobraHeader header = new CobraHeader
                {
                    TextureCount = (byte)model.MaterialTextures.Count,
                    ModelCount = (byte)model.Count,
                    Materials = model.MaterialTextures.Select(material => CobraExportPlugin.GetTextureEntry(material.Value)).ToArray(),
                    Objects = model.Select(item => CobraExportPlugin.GetModelEntry(item, model.MaterialTextures, model.Vertices, model.Normals)).ToArray()
                };

                File.WriteAllBytes(outputFile, CobraExportPlugin.GetBytes(header));

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return false;
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
                foreach (FieldInfo field in dataType.GetFields().OrderBy(field => field.MetadataToken))
                {
                    object? value = field.GetValue(data);

                    if (value != null)
                    {
                        bytes.AddRange(CobraExportPlugin.GetBytes(value));
                    }
                }
            }
            else
            {
                if (dataType.IsArray)
                {
                    foreach (object item in (Array)data)
                    {
                        bytes.AddRange(CobraExportPlugin.GetBytes(item));
                    }
                }
                else if (dataType.IsEnum)
                {
                    bytes.AddRange(CobraExportPlugin.GetBytes((ushort)data));
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
        /// Get face entry
        /// </summary>
        /// <param name="face">Model face</param>
        /// <param name="materials">Model materials</param>
        /// <param name="vertices">Global vertices</param>
        /// <param name="localVertices">Local vertices</param>
        /// <param name="normals">Global normals</param>
        /// <returns>Face entry</returns>
        private static CobraFace GetFaceEntry(
                    Face face,
                    Dictionary<string, Material> materials,
                    List<Vector3D> vertices,
                    Dictionary<int, CobraVertex> localVertices,
                    List<Vector3D> normals)
        {
            ushort[] indexes = face.Vertices
                .Select(vertice => CobraExportPlugin.GetVerticeEntry(
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

            Vector3D faceVector = face.Normals.Select(normal => normals[normal]).Aggregate((a, b) => a + b);
            faceVector.Normalize();

            int materialIndex = materials.Keys.ToList().IndexOf(face.Material);

            if (materialIndex < 0)
            {
                if (materials.Keys.Any(material => string.IsNullOrWhiteSpace(material)))
                {
                    materialIndex =  materials.Keys.ToList().IndexOf(materials.Keys.First(material => string.IsNullOrWhiteSpace(material)));
                }
                else
                {
                    throw new Exception(string.Format("Material '{0}' is missing", face.Material));
                }
            }

            CobraFace entry = new CobraFace
            {
                TextureIndex = (byte)materialIndex,
                Indexes = indexes,
                Normal = CobraExportPlugin.GetVertice(faceVector.X, faceVector.Y, faceVector.Z),
                Flags = CobraFaceFlags.None
            };

            if (face.IsMesh)
            {
                entry.Flags |= CobraFaceFlags.Meshed;
            }

            if (face.IsDoubleSided)
            {
                entry.Flags |= CobraFaceFlags.DoubleSided;
            }

            return entry;
        }

        /// <summary>
        /// Get model entry
        /// </summary>
        /// <param name="model">3D model</param>
        /// <param name="materials">Model materials</param>
        /// <param name="vertices">Global model vertices</param>
        /// <param name="normals">Global normals</param>
        /// <returns>Model entry</returns>
        private static CobraObject GetModelEntry(
            Model model,
            Dictionary<string, Material> materials,
            List<Vector3D> vertices,
            List<Vector3D> normals)
        {
            Dictionary<int, CobraVertex> localVertices = new Dictionary<int, CobraVertex>();
            CobraFace[] faces = model.Faces.Select(face => CobraExportPlugin.GetFaceEntry(face, materials, vertices, localVertices, normals)).ToArray();
            CobraVertex[] modelVertices = localVertices.Values.ToArray();

            return new CobraObject
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
        private static CobraMaterial GetTextureEntry(Material material)
        {
            CobraMaterial entry = new();

            if (material is TextureReferenceMaterial mat &&
                !string.IsNullOrWhiteSpace(mat.TexturePath))
            {
                string file = Path.GetFileName(mat.TexturePath).ToUpper();
                byte[] bytes = Encoding.ASCII.GetBytes(file).Take(13).ToArray();
                entry.Data = bytes.Concat(Enumerable.Repeat(byte.MinValue, 16 - bytes.Length)).ToArray();
            }
            else
            {
                entry.Data = new byte[] { 0, material.BaseColor.R, material.BaseColor.G, material.BaseColor.B }.Concat(Enumerable.Repeat(byte.MinValue, 12)).ToArray();
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
        private static CobraVertex GetVertice(double x, double y, double z)
        {
            return new CobraVertex
            {
                X = (Int32)(x * CobraExportPlugin.DoubleScale),
                Y = (Int32)(y * CobraExportPlugin.DoubleScale),
                Z = (Int32)(z * CobraExportPlugin.DoubleScale),
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
        private static ushort GetVerticeEntry(int vertice, Dictionary<int, CobraVertex> localVertices, double x, double y, double z)
        {
            int result;

            if (!localVertices.ContainsKey(vertice))
            {
                localVertices.Add(vertice, CobraExportPlugin.GetVertice(x, y, z));
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