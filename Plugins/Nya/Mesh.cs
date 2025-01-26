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
        /// <param name="textures">Embed textures</param>
        /// <param name="uvTextures">Embed UV textures</param>
        public Mesh(Group group, Model model, List<Texture> textures, ref List<Texture> uvTextures)
        {
            List<FaceFlags> faceFlags = new List<FaceFlags>();
            List<Polygon> facePolygons = new List<Polygon>();
            List<Vector3D> vertices = new List<Vector3D>();

            foreach (Face face in model.Faces)
            {
                (FaceFlags flags, Polygon polygon) faceData = Mesh.ConvertFace(face, group, textures, ref vertices, ref uvTextures);
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
        /// <param name="textures">Embed textures</param>
        /// <param name="vertices">Model vertices</param>
        private static (FaceFlags, Polygon) ConvertFace(
            Face face,
            Group group,
            List<Texture> textures,
            ref List<Vector3D> vertices,
            ref List<Texture> uvTextures)
        {
            FaceFlags faceFlag = new FaceFlags();

            // Read flags
            faceFlag.HasTexture = group.MaterialTextures[face.Material] is TextureReferenceMaterial || group.MaterialTextures[face.Material] is TextureMaterial;

            // Read user flags
            int separator = face.Material.LastIndexOf('_');

            if (separator > 0 && face.Material[separator] == '_')
            {
                HashSet<char> flags = face.Material.Substring(separator + 1).Where(letter => char.IsLetter(letter) && char.IsUpper(letter)).ToHashSet();

                faceFlag.HasMeshEffect = flags.Contains('M');
                faceFlag.IsDoubleSided = flags.Contains('D');
                faceFlag.IsHalfTransparent = flags.Contains('H');
            }

            // Read polygon
            Polygon polygon = Mesh.ConvertPolygon(face, faceFlag, group, textures, ref vertices, ref uvTextures);

            return (faceFlag, polygon);
        }

        /// <summary>
        /// Convert face polygon from model
        /// </summary>
        /// <param name="face">Face data</param>
        /// <param name="faceFlag">Face flags</param>
        /// <param name="group">Model object group</param>
        /// <param name="textures">Embed textures</param>
        /// <param name="vertices">Model vertices</param>
        private static Polygon ConvertPolygon(
            Face face,
            FaceFlags faceFlag,
            Group group,
            List<Texture> textures,
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

            Polygon polygon = new Polygon();

            // Get polygon clipping normal
            List<Vector3D> points = face.Vertices.Select(point => group.Vertices[point]).ToList();
            Vector3D clippingNormal = Mesh.FindNewNormal(points);
            polygon.Normal = FxVector.FromVertex(clippingNormal);

            // We have no vertex normals
            if (face.Normals.Count == 0)
            {
                face.Normals.AddRange(Enumerable.Repeat(group.Normals.Count, face.Vertices.Count));
                group.Normals.Add(clippingNormal);
            }

            if (faceFlag.HasTexture)
            {
                /* 0, 1,
                1, 1,
                1, 0,*/
                // TODO: UV mapping
                faceFlag.TextureId = uvTextures.FindIndex(material => material.Name == face.Material);
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
