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
        /// <param name="settings">Converter settings</param>
        /// <param name="uvTextures">Embed UV textures</param>
        public Mesh(Group group, Model model, List<Texture> modelTextures, NyaArguments settings, ref List<Texture> uvTextures)
        {
            List<FaceFlags> faceFlags = new List<FaceFlags>();
            List<Polygon> facePolygons = new List<Polygon>();
            List<Vector3D> vertices = new List<Vector3D>();

            foreach (Face face in model.Faces)
            {
                (FaceFlags flags, Polygon polygon) faceData = Mesh.ConvertFace(face, group, modelTextures, settings, ref vertices, ref uvTextures);
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
        /// <param name="settings">Converter settings</param>
        /// <param name="vertices">Model vertices</param>
        /// <param name="uvTextures">Embed model vertices</param>
        private static (FaceFlags, Polygon) ConvertFace(
            Face face,
            Group group,
            List<Texture> modelTextures,
            NyaArguments settings,
            ref List<Vector3D> vertices,
            ref List<Texture> uvTextures)
        {
            FaceFlags faceFlag = new FaceFlags();

            if (!group.MaterialTextures.ContainsKey(face.Material))
            {
                Console.WriteLine($"Warning: Material '{face.Material}' was not found! Replacing with purple.");
                group.MaterialTextures.Add(face.Material, new Material { BaseColor = ModelConverter.Graphics.Color.FromRgb(128, 0, 128) });
            }

            // Read flags
            faceFlag.HasTexture = group.MaterialTextures[face.Material] is TextureReferenceMaterial || group.MaterialTextures[face.Material] is TextureMaterial;
            faceFlag.IsDoubleSided = face.IsDoubleSided;
            faceFlag.IsHalfTransparent = face.IsHalfTransparent;
            faceFlag.HasMeshEffect = face.IsMesh;
            faceFlag.SortMode = face.SortMode;
            faceFlag.IsHalfBright = face.IsHalfBright;
            faceFlag.IsWireframe = face.IsWireframe;

            // Lighting
            faceFlag.IsFlat = face.IsFlat | settings.ModelType == NyaArguments.ModelTypes.NoLight;
            faceFlag.NoLight = face.NoLight | settings.ModelType == NyaArguments.ModelTypes.NoLight;

            // Read polygon
            Polygon polygon = Mesh.ConvertPolygon(face, faceFlag, group, modelTextures, !settings.NoUV, ref vertices, ref uvTextures);

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

            // Track whether this was originally a quad before triangle→quad padding,
            // so the UV canonicalization below only runs on true quads (degenerate
            // padded triangles must keep uv[2] == uv[3]).
            bool wasQuad = face.Vertices.Count == 4;

            // We have less normals for some reason
            if (face.Normals.Count > 0 && face.Normals.Count != face.Vertices.Count)
            {
                face.Normals.AddRange(Enumerable.Repeat(face.Normals.Last(), face.Vertices.Count - face.Normals.Count));
            }

            // We have triangle
            if (face.Vertices.Count < 4)
            {
                face.Vertices.Add(face.Vertices.Last());

                if (face.Uv.Count > 0)
                {
                    face.Uv.Add(face.Uv.Last());
                }

                if (face.Normals.Count > 0)
                {
                    face.Normals.Add(face.Normals.Last());
                }
            }

            if (faceFlag.HasTexture && !faceFlag.IsWireframe)
            {
                // Can unwrap only if UV is present
                if (unwrapTextures && (face.Uv?.Any() ?? false))
                {
                    Texture? texture = modelTextures.FirstOrDefault(material => material.Name == face.Material);

                    if (texture != null)
                    {
                        // Canonicalize quad UV ordering so GetUnwrap sees
                        // [TL, TR, BR, BL] every time. Mirrored faces arrive here
                        // with the same 4 UV points but traced in the opposite
                        // direction from their non-mirrored counterparts; without
                        // this fix, GetUnwrap's topDir/bottomDir end up along the
                        // wrong axis on one side and the texture tile is sampled
                        // rotated 90° relative to the other side.
                        // Padded triangles keep uv[2]==uv[3] and must not be
                        // reordered by this pass.
                        if (wasQuad)
                        {
                            // Find corner closest to UV top-left (minU, maxV in V-up space).
                            double minU = face.Uv.Select(i => group.Uv[i].X).Min();
                            double maxV = face.Uv.Select(i => group.Uv[i].Y).Max();

                            int topLeft = 0;
                            double bestDistSq = double.MaxValue;

                            for (int i = 0; i < 4; i++)
                            {
                                Vector3D c = group.Uv[face.Uv[i]];
                                double du = c.X - minU;
                                double dv = maxV - c.Y;
                                double d = (du * du) + (dv * dv);

                                if (d < bestDistSq)
                                {
                                    bestDistSq = d;
                                    topLeft = i;
                                }
                            }

                            // Cyclic shift so topLeft lands at index 0.
                            List<int> rotatedUvs = new List<int>(4);
                            List<int> rotatedNormals = new List<int>(4);
                            List<int> rotatedVertices = new List<int>(4);

                            for (int i = 0; i < 4; i++)
                            {
                                rotatedUvs.Add(face.Uv[(i + topLeft) % 4]);
                                rotatedNormals.Add(face.Normals[(i + topLeft) % 4]);
                                rotatedVertices.Add(face.Vertices[(i + topLeft) % 4]);
                            }

                            // Check UV winding. For a CW quad [TL, TR, BR, BL] in
                            // V-up UV space, (uv[1]-uv[0]) × (uv[3]-uv[0]) has
                            // negative Z. Positive Z means CCW — swap indices 1↔3
                            // to convert [TL, BL, BR, TR] → [TL, TR, BR, BL].
                            Vector3D e01 = group.Uv[rotatedUvs[1]] - group.Uv[rotatedUvs[0]];
                            Vector3D e03 = group.Uv[rotatedUvs[3]] - group.Uv[rotatedUvs[0]];
                            double signedArea = (e01.X * e03.Y) - (e01.Y * e03.X);

                            if (signedArea > 0.0)
                            {
                                (rotatedUvs[1], rotatedUvs[3]) = (rotatedUvs[3], rotatedUvs[1]);
                                (rotatedNormals[1], rotatedNormals[3]) = (rotatedNormals[3], rotatedNormals[1]);
                                (rotatedVertices[1], rotatedVertices[3]) = (rotatedVertices[3], rotatedVertices[1]);
                            }

                            face.Uv = rotatedUvs;
                            face.Normals = rotatedNormals;
                            face.Vertices = rotatedVertices;
                        }

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

                foreach (Vector3D normal in face.Normals.Select(normal => group.Normals[normal]))
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
            var createdFromBase = uvTextures.Select((texture, index) => new KeyValuePair<int, Texture>(index, texture)).Where(texture => texture.Value.GetBaseName() == baseTexture.Name).ToList();
            var existing = createdFromBase
                .Where(texture => texture.Value.UV.Select((id, i) => (uvCoords[id] - uvCoords[uv[i]]).GetLength() <= double.Epsilon).All(val => val))
                .DefaultIfEmpty(new KeyValuePair<int, Texture>(-1, baseTexture))
                .First().Key;

            // If not, generate new texture
            if (existing < 0)
            {
                List<Vector3D> coords = uv.Select(coord => uvCoords[coord]).ToList();
                Texture unwrap = Texture.GetUnwrap(baseTexture, coords);
                unwrap.UV = uv.ToArray();
                existing = uvTextures.Count;

                var found = createdFromBase.FindIndex(pair => pair.Value.Hash == unwrap.Hash);

                if (found < 0)
                {
                    uvTextures.Add(unwrap);
                }
                else
                {
                    return createdFromBase[found].Key;
                }
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
