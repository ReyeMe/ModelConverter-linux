namespace Nya
{
    using System.Data.SqlTypes;
    using System.Security.AccessControl;
    using ModelConverter.Geometry;
    using Nya.Serializer;

    /// <summary>
    /// Flat mesh data
    /// </summary>
    public class Mesh
    {
        /// <summary>
        /// Initializes a new <see cref="Mesh"/> class
        /// </summary>
        /// <param name="group">Model object group</param>
        /// <param name="model">Model object</param>
        /// <param name="modelTextures">Model loaded textures</param>
        /// <param name="unwrapTextures">Unwrap model textures by UV</param>
        /// <param name="uvTextures">Embed UV textures</param>
        public Mesh(Group group, Model model, List<Texture> modelTextures, bool unwrapTextures, ref List<Texture> uvTextures)
        {
            List<FaceFlags> faceFlags = new List<FaceFlags>();
            List<Polygon> facePolygons = new List<Polygon>();
            List<Vector3D> vertices = new List<Vector3D>();

            foreach (Face face in model.Faces)
            {
                (FaceFlags flags, Polygon polygon) faceData = Mesh.ConvertFace(face, group, modelTextures, unwrapTextures, ref vertices, ref uvTextures);
                faceFlags.Add(faceData.flags);
                facePolygons.Add(faceData.polygon);
            }

            this.Points = vertices.Select(point => FxVector.FromVertex(point)).ToArray();
            this.PointCount = this.Points.Length;

            this.FaceFlags = faceFlags.ToArray();
            this.Polygons = facePolygons.ToArray();
            this.PolygonCount = this.Polygons.Length;
        }

        /// <summary>
        /// Convert face from model
        /// </summary>
        /// <param name="face">Face data</param>
        /// <param name="group">Model object group</param>
        /// <param name="modelTextures">Textures from model file textures</param>
        /// <param name="unwrapTextures">Unwrap model textures by UV</param>
        /// <param name="vertices">Model vertices</param>
        /// <param name="uvTextures">Embed model vertices</param>
        private static (FaceFlags, Polygon) ConvertFace(
            Face face,
            Group group,
            List<Texture> modelTextures,
            bool unwrapTextures,
            ref List<Vector3D> vertices,
            ref List<Texture> uvTextures)
        {
            FaceFlags faceFlag = new FaceFlags();

            // Read flags
            faceFlag.HasTexture = group.MaterialTextures[face.Material] is TextureReferenceMaterial || group.MaterialTextures[face.Material] is TextureMaterial;
            faceFlag.IsDoubleSided = face.IsDoubleSided;
            faceFlag.IsHalfTransparent = face.IsHalfTransparent;
            faceFlag.HasMeshEffect = face.IsMesh;
            faceFlag.IsFlat = face.IsFlat;
            faceFlag.SortMode = face.SortMode;
            faceFlag.IsHalfBright = face.IsHalfBright;

            // Read polygon
            Polygon polygon = Mesh.ConvertPolygon(face, faceFlag, group, modelTextures, unwrapTextures, ref vertices, ref uvTextures);

            return (faceFlag, polygon);
        }

        /// <summary>
        /// Convert face polygon from model
        /// </summary>
        /// <param name="face">Face data</param>
        /// <param name="faceFlag">Face flags</param>
        /// <param name="group">Model object group</param>
        /// <param name="modelTextures">Textures from model file textures</param>
        /// <param name="unwrapTextures">Unwrap model textures by UV</param>
        /// <param name="vertices">Model vertices</param>
        /// <param name="uvTextures">Embed model vertices</param>
        private static Polygon ConvertPolygon(
            Face face,
            FaceFlags faceFlag,
            Group group,
            List<Texture> modelTextures,
            bool unwrapTextures,
            ref List<Vector3D> vertices,
            ref List<Texture> uvTextures)
        {
            if (face.Vertices.Count < 3 || face.Vertices.Count > 4)
            {
                throw new NotSupportedException("Only supported faces are triangles and quads!");
            }
            else
            {
                // We have less normals for some reason
                if (face.Normals.Count > 0 && face.Normals.Count != face.Vertices.Count)
                {
                    face.Normals.AddRange(Enumerable.Repeat(face.Normals.Last(), face.Vertices.Count - face.Normals.Count));
                }

                // We have triangle
                if (face.Vertices.Count < 4)
                {
                    face.Vertices.Add(face.Vertices.Last());
                    face.Uv.Add(face.Uv.Last());

                    if (face.Normals.Count > 0)
                    {
                        face.Normals.Add(face.Normals.Last());
                    }
                }
            }

            if (faceFlag.HasTexture)
            {
                if (unwrapTextures)
                {
                    Texture? texture = modelTextures.FirstOrDefault(material => material.Name == face.Material);

                    if (texture != null)
                    {
                        // Find corner with smalles UV coordinate
                        int faceRotation = 0;
                        Vector3D smallestUv = new Vector3D(double.MaxValue, double.MaxValue, 0.0);

                        for (int i = 0; i < 4; i++)
                        {
                            Vector3D current = group.Uv[face.Uv[i]];

                            if (smallestUv.X >= current.X && smallestUv.Y >= current.Y)
                            {
                                faceRotation = i;
                                smallestUv = current;
                            }
                        }

                        // Rotate face so that it start at the smalled UV coordinate
                        List<int> rotatedUvs = new List<int>(face.Uv);
                        List<int> rotatedNormals = new List<int>(face.Normals);
                        List<int> rotatedVertices = new List<int>(face.Vertices);

                        for (int i = 0; i < 4; i++)
                        {
                            rotatedUvs[(i + faceRotation) % 4] = face.Uv[i];
                            rotatedNormals[(i + faceRotation) % 4] = face.Normals[i];
                            rotatedVertices[(i + faceRotation) % 4] = face.Vertices[i];
                        }

                        face.Uv = rotatedUvs;
                        face.Normals = rotatedNormals;
                        face.Vertices = rotatedVertices;

                        // Generate texture
                        faceFlag.TextureId = Mesh.GetUvMappedTexture(texture, face.Uv, group.Uv, ref uvTextures);
                    }
                    else
                    {
                        faceFlag.HasTexture = false;
                        faceFlag.BaseColor = group.MaterialTextures[face.Material].BaseColor.AsAbgr555();
                    }
                }
                else
                {
                    int found = modelTextures.FindIndex(material => material.Name == face.Material);

                    if (found == -1)
                    {
                        faceFlag.HasTexture = false;
                        faceFlag.BaseColor = group.MaterialTextures[face.Material].BaseColor.AsAbgr555();
                    }
                    else
                    {
                        faceFlag.TextureId = found;
                    }
                }
            }
            else if (group.MaterialTextures.ContainsKey(face.Material))
            {
                faceFlag.BaseColor = group.MaterialTextures[face.Material].BaseColor.AsAbgr555();
            }

            Polygon polygon = new Polygon();
            List<Vector3D> points = face.Vertices.Select(point => group.Vertices[point]).ToList();

            // Get polygon clipping normal
            if (face.Normals.Count > 0)
            {
                Vector3D accumulator = new Vector3D();

                foreach(Vector3D normal in face.Normals.Select(normal => group.Normals[normal]))
                {
                    accumulator += normal;
                }

                polygon.Normal = FxVector.FromVertex((accumulator / face.Normals.Count).GetNormal());
            }
            else
            {
                Vector3D clippingNormal = Mesh.FindNewNormal(points);
                polygon.Normal = FxVector.FromVertex(clippingNormal);

                // We have no vertex normals
                if (face.Normals.Count == 0)
                {
                    face.Normals.AddRange(Enumerable.Repeat(group.Normals.Count, face.Vertices.Count));
                    group.Normals.Add(clippingNormal);
                }
            }

            // Find and add polygon points
            for (int point = 0; point < points.Count; point++)
            {
                Vector3D current = points[point];
                int existing = vertices.FindIndex(vector => (int)((vector - current).GetLength() * 100.0) <= 0);

                if (existing >= 0)
                {
                    polygon.Vertices[point] = (short)existing;
                }
                else
                {
                    polygon.Vertices[point] = (short)vertices.Count;
                    vertices.Add(current);
                }
            }

            return polygon;
        }

        /// <summary>
        /// Get UV mapped texture from base texture
        /// </summary>
        /// <param name="baseTexture">Base texture</param>
        /// <param name="uv">UV coord indicies for quad</param>
        /// <param name="uvCoords">All UV coords</param>
        /// <param name="uvTextures">UV texture atlas</param>
        /// <returns>Number of already existing or new texture</returns>
        private static int GetUvMappedTexture(Texture baseTexture, List<int> uv, List<Vector3D> uvCoords, ref List<Texture> uvTextures)
        {
            // Check if texture mapped to this region exists already
            int existing = uvTextures.FindIndex(texture => texture.UV.Select((id, i) => (uvCoords[id] - uvCoords[uv[i]]).GetLength() <= double.Epsilon).All(val => val));

            // If not, generate new texture
            if (existing == -1)
            {
                List<Vector3D> coords = uv.Select(coord => uvCoords[coord]).ToList();
                Texture unwrap = Texture.GetUnwrap(baseTexture, coords);
                unwrap.UV = uv.ToArray();
                existing = uvTextures.Count;
                uvTextures.Add(unwrap);
            }

            return existing;
        }

        /// <summary>
        /// Find new normal of polygon
        /// </summary>
        /// <param name="polygon">Polygon points</param>
        /// <returns>Polygon normal</returns>
        private static Vector3D FindNewNormal(IList<Vector3D> polygon)
        {
            // Unique point list
            List<Vector3D> unique = new List<Vector3D> { polygon.First() };

            foreach (Vector3D point in polygon.Skip(1))
            {
                if ((int)((unique.Last() - point).GetLength() * 100.0) > 0 &&
                    (int)((unique.First() - point).GetLength() * 100.0) > 0)
                {
                    unique.Add(point);
                }
            }

            if (unique.Count > 2)
            {
                Vector3D accumulator = new Vector3D();
                int counter = 0;

                for (int p = 0; p < unique.Count; p++)
                {
                    Vector3D first = polygon[p];
                    Vector3D second = polygon[(p + 1) % polygon.Count];
                    Vector3D third = polygon[(p + 2) % polygon.Count];

                    Vector3D axis1 = first - second;
                    Vector3D axis2 = third - second;
                    Vector3D cross = axis1.Cross(axis2).GetNormal();

                    if ((int)(cross.GetLength() * 100.0) > 0)
                    {
                        accumulator += cross;
                        counter++;
                    }
                }

                if (counter > 0)
                {
                    return accumulator.GetNormal();
                }

                return new Vector3D(0.0, 0.0, 1.0);
            }
            else if (unique.Count == 2)
            {
                return (polygon.Last() - polygon.First()).GetNormal();
            }
            else
            {
                return new Vector3D(0.0, 0.0, 1.0);
            }
        }

        /// <summary>
        /// Gets or sets face flags
        /// </summary>
        [ArraySizeDynamic("PolygonCount")]
        [FieldOrder(4)]
        public FaceFlags[] FaceFlags { get; set; } = Array.Empty<FaceFlags>();

        /// <summary>
        /// Gets number of points
        /// </summary>
        [FieldOrder(0)]
        public int PointCount { get; set; }

        /// <summary>
        /// Gets or sets mesh points
        /// </summary>
        [ArraySizeDynamic("PointCount")]
        [FieldOrder(2)]
        public FxVector[] Points { get; set; } = new FxVector[0];

        /// <summary>
        /// Gets number of polygons
        /// </summary>
        [FieldOrder(1)]
        public int PolygonCount { get; set; }

        /// <summary>
        /// Gets or sets mesh polygon
        /// </summary>
        [ArraySizeDynamic("PolygonCount")]
        [FieldOrder(3)]
        public Polygon[] Polygons { get; set; } = Array.Empty<Polygon>();
    }
}
