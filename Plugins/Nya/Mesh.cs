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
            Polygon polygon = Mesh.ConvertPolygon(face, faceFlag, group, modelTextures, !settings.NoUV, settings.TextureMergeThreshold, ref vertices, ref uvTextures);

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
        /// <param name="textureMergeThreshold">Texture similarity threshold percentage (0.0 to 100.0)
        /// above which 2 textures will be considered identical</param>
        /// <param name="vertices">Model vertices</param>
        /// <param name="uvTextures">Embed model vertices</param>
        private static Polygon ConvertPolygon(
            Face face,
            FaceFlags faceFlag,
            Group group,
            List<Texture> modelTextures,
            bool unwrapTextures,
            double textureMergeThreshold,
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
                        List<int> finalUvs = new List<int>(face.Uv);
                        List<int> finalNormals = new List<int>(face.Normals);
                        List<int> finalVertices = new List<int>(face.Vertices);

                        // Canonicalize quad UV ordering so GetUnwrap sees
                        // [TL, TR, BR, BL] every time. Mirrored faces arrive here
                        // with the same 4 UV points but traced in the opposite
                        // direction from their non-mirrored counterparts; without
                        // this fix, GetUnwrap's topDir/bottomDir end up along the
                        // wrong axis on one side and the texture tile is sampled
                        // rotated 90° relative to the other side.
                        // Padded triangles keep uv[2]==uv[3] and must not be
                        // reordered by this pass.
                        List<Vector3D> rawUvs = face.Uv.Select(i => group.Uv[i]).ToList();
                        var canonicalizationResult = CanonicalizeFace(rawUvs, wasQuad);

                        // Reorder everything according to the canonical indices
                        finalUvs.Clear();
                        finalNormals.Clear();
                        finalVertices.Clear();

                        for (int i = 0; i < 4; i++)
                        {
                            int originalIndex = canonicalizationResult.NewToOldIndices[i];
                            finalUvs.Add(face.Uv[originalIndex]);
                            finalNormals.Add(face.Normals[originalIndex]);
                            finalVertices.Add(face.Vertices[originalIndex]);
                        }

                        // Generate texture
                        TextureResult result = Mesh.GetUvMappedTexture(texture, finalUvs, group.Uv, wasQuad, textureMergeThreshold, ref uvTextures);
                        faceFlag.TextureId = result.TextureId;

                        // Reorder vertices depending on how the texture matched
                        if (result.VertexPermutation != null)
                        {
                            List<int> newUvs = new List<int>(4);
                            List<int> newNormals = new List<int>(4);
                            List<int> newVertices = new List<int>(4);

                            for (int i = 0; i < 4; i++)
                            {
                                int srcIdx = result.VertexPermutation[i];
                                newUvs.Add(finalUvs[srcIdx]);
                                newNormals.Add(finalNormals[srcIdx]);
                                newVertices.Add(finalVertices[srcIdx]);
                            }

                            finalUvs = newUvs;
                            finalNormals = newNormals;
                            finalVertices = newVertices;
                        }

                        // Reinjection
                        face.Uv = finalUvs;
                        face.Normals = finalNormals;
                        face.Vertices = finalVertices;
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
        /// Specifies transforms that can be applied to a texture.
        /// </summary>
        [Flags]
        public enum UvTransform
        {
            /// <summary>
            /// Original texture, no transform applied.
            /// </summary>
            None = 0,

            /// <summary>
            /// Texture is mirrored horizontally.
            /// </summary>
            HorizontalFlip = 1,

            /// <summary>
            /// Texture is mirrored vertically.
            /// </summary>
            VerticalFlip = 2,

            /// <summary>
            /// Texture is mirrored horizontally and vertically.
            /// </summary>
            Both = HorizontalFlip | VerticalFlip
        }

        /// <summary>
        /// Represents the detailed result of a UV mapping operation.
        /// </summary>
        /// <remarks>
        /// This object is returned by <see cref="GetUvMappedTexture(Texture, List{int}, List{Vector3D}, bool, double, ref List{Texture})"/>.
        /// </remarks>
        public class TextureResult
        {
            /// <summary>
            /// Gets or sets the identifier of the texture (either already existing or newly created) 
            /// within the UV texture atlas.
            /// </summary>
            public int TextureId { get; set; }

            /// <summary>
            /// Gets or sets the vertex permutation array used to map vertices of a polygon to their canonical order.
            /// </summary>
            public int[]? VertexPermutation { get; set; } = null;
        }

        /// <summary>
        /// Get UV mapped texture from base texture
        /// </summary>
        /// <param name="baseTexture">Base texture</param>
        /// <param name="uv">UV coord indicies for quad</param>
        /// <param name="uvCoords">All UV coords</param>
        /// <param name="wasQuad">False if the polygon was a triangle before being converted to a quad.
        /// True if the polygon always was a Quad</param>
        /// <param name="textureMergeThreshold">Texture similarity threshold percentage (0.0 to 100.0)
        /// above which 2 textures will be considered identical</param>
        /// <param name="uvTextures">UV texture atlas</param>
        /// <returns>Number of already existing or new texture</returns>
        private static TextureResult GetUvMappedTexture(
            Texture baseTexture, 
            List<int> uv, 
            List<Vector3D> uvCoords,
            bool wasQuad,
            double textureMergeThreshold,
            ref List<Texture> uvTextures)
        {
            List<Vector3D> currentFaceUvs = uv.Select(coord => uvCoords[coord]).ToList();

            // In order to detect which textures are the same we first check whether their shapes are similar (including mirror versions).
            // Similar is defined as a % of their length/width
            // (fall back to half a pixel tolerance to insure pixel perfect behavior in case of high merge threshold)
            double minU = currentFaceUvs.Min(p => p.X);
            double maxU = currentFaceUvs.Max(p => p.X);
            double minV = currentFaceUvs.Min(p => p.Y);
            double maxV = currentFaceUvs.Max(p => p.Y);
            double faceWidth = maxU - minU;
            double faceHeight = maxV - minV;
            double toleranceFactor = (100.0 - textureMergeThreshold) / 100.0;
            double uEpsilon = Math.Max(faceWidth * toleranceFactor, 0.5 / baseTexture.Width);
            double vEpsilon = Math.Max(faceHeight * toleranceFactor, 0.5 / baseTexture.Height);

            int bestTextureId = -1;
            double bestSimilarityScore = -1.0;
            int[]? bestPermutation = null;
            for (int i = 0; i < uvTextures.Count; i++)
            {
                Texture existingTexture = uvTextures[i];

                if (existingTexture.GetBaseName() != baseTexture.Name)
                {
                    continue;
                }

                List<Vector3D> existingUvs = existingTexture.UV.Select(id => uvCoords[id]).ToList();
                if (Mesh.IsUvSameShape(existingUvs, currentFaceUvs, wasQuad, uEpsilon, vEpsilon, 
                  out UvTransform detectedTransform, 
                  out int[]? currentToCanonicalVertexOrder) && currentToCanonicalVertexOrder is not null)
                {
                    //The shapes are similar, now we reorder to vertices so the content of the textures can be compared
                    List<Vector3D> reorderedCurrentUvs = new List<Vector3D>(4);

                    for (int j = 0; j < 4; j++)
                    {
                        reorderedCurrentUvs.Add(currentFaceUvs[currentToCanonicalVertexOrder[j]]);
                    }

                    Texture currentUnwrap = Texture.GetUnwrap(baseTexture, reorderedCurrentUvs);

                    //And we compare the content of the textures
                    double currentScore = currentUnwrap.CalculateSimilarityTo(existingTexture);

                    if (currentScore >= textureMergeThreshold && currentScore > bestSimilarityScore)
                    {
                        bestSimilarityScore = currentScore;
                        bestTextureId = i;
                        bestPermutation = currentToCanonicalVertexOrder;

                        if (bestSimilarityScore >= 100.0)
                        {
                            break;
                        }
                    }
                }
            }

            if (bestTextureId >= 0)
            {
                return new TextureResult { TextureId = bestTextureId, VertexPermutation = bestPermutation };
            }

            // No match with existing texture, we extract a new one
            Texture newUnwrap = Texture.GetUnwrap(baseTexture, currentFaceUvs);
            newUnwrap.UV = uv.ToArray();

            int newId = uvTextures.Count;
            uvTextures.Add(newUnwrap);

            return new TextureResult { TextureId = newId };
        }

        /// <summary>
        /// Determines whether two sets of UV coordinates share the same geometric shape within a specified tolerance, 
        /// checking across multiple orientation configurations (default orientation, horizontal flip, vertical flip and both).
        /// <param name="existingUvs">The reference list of UV coordinates to compare against.</param>
        /// <param name="testedUvs">The list of UV coordinates being evaluated for a potential match.</param>
        /// <param name="wasQuad">False if the polygon was a triangle before being converted to a quad.
        /// True if the polygon always was a Quad</param>
        /// <param name="uEpsilon">The maximum allowed absolute difference along the U (X) axis.</param>
        /// <param name="vEpsilon">The maximum allowed absolute difference along the V (Y) axis.</param>
        /// <param name="transform">When this method returns, contains the <see cref="UvTransform"/> applied to achieve the match;
        /// otherwise, <c>UvTransform.None</c>.</param>
        /// <param name="currentToCanonicalVertexOrder">When this method returns, contains an array mapping the current vertices
        /// to their canonical sequence if a match is found; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> if <paramref name="testedUvs"/> matches the shape of <paramref name="existingUvs"/> under any tested transformation; otherwise, <c>false</c>.</returns>
        private static bool IsUvSameShape(
            List<Vector3D> existingUvs,
            List<Vector3D> testedUvs,
            bool wasQuad,
            double uEpsilon, 
            double vEpsilon, 
            out UvTransform transform,
            out int[]? currentToCanonicalVertexOrder)
        {
            transform = UvTransform.None;
            currentToCanonicalVertexOrder = null;

            if (existingUvs.Count != testedUvs.Count)
            {
                return false;
            }

            Vector3D originExistingUvs = existingUvs[0];
            List<Vector3D> centeredExisting = existingUvs.Select(p => 
                new Vector3D(p.X - originExistingUvs.X, p.Y - originExistingUvs.Y, p.Z)).ToList();

            var configs = new[]
            {
                (Transform: UvTransform.None,           MirrorFunc: (Func<Vector3D, Vector3D>)(p => p)),
                (Transform: UvTransform.HorizontalFlip, MirrorFunc: (p => new Vector3D(-p.X, p.Y, p.Z))),
                (Transform: UvTransform.VerticalFlip,   MirrorFunc: (p => new Vector3D(p.X, -p.Y, p.Z))),
                (Transform: UvTransform.Both,           MirrorFunc: (p => new Vector3D(-p.X, -p.Y, p.Z)))
            };

            foreach (var cfg in configs)
            {
                List<Vector3D> mirroredTestedUvs = testedUvs.Select(cfg.MirrorFunc).ToList();

                var canonicalizationResult = CanonicalizeFace(mirroredTestedUvs, wasQuad);
                List<Vector3D> canonicalizedTestedUvs = canonicalizationResult.OrderedCoords;

                Vector3D originCanonicalizedTestedUvs = canonicalizedTestedUvs[0];
                List<Vector3D> centeredCanonicalizedTestedUvs = canonicalizedTestedUvs.Select(p => 
                    new Vector3D(p.X - originCanonicalizedTestedUvs.X, p.Y - originCanonicalizedTestedUvs.Y, p.Z)).ToList();

                bool match = true;
                for (int i = 0; i < centeredExisting.Count; i++)
                {
                    if (Math.Abs(centeredExisting[i].X - centeredCanonicalizedTestedUvs[i].X) > uEpsilon ||
                        Math.Abs(centeredExisting[i].Y - centeredCanonicalizedTestedUvs[i].Y) > vEpsilon)
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                {
                    transform = cfg.Transform;
                    currentToCanonicalVertexOrder = canonicalizationResult.NewToOldIndices;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Contains the result of a polygon face canonicalization operation.
        /// </summary>
        /// <param name="OrderedCoords">The newly ordered and normalized list of 3D coordinates.</param>
        /// <param name="NewToOldIndices">An array mapping each new position index back to its original index in the source list.</param>
        public record CanonicalizationResult(List<Vector3D> OrderedCoords, int[] NewToOldIndices);

        /// <summary>
        /// Canonicalizes a polygon face by enforcing a consistent vertex order.
        /// </summary>
        /// <param name="rawCoords">The initial list of 3D vector coordinates representing the face vertices.</param>
        /// <param name="wasQuad">False if the polygon was a triangle before being converted to a quad.
        /// True if the polygon always was a Quad</param>
        /// <returns>
        /// A tuple containing:
        /// <list type="bullet">
        /// <item><description><c>orderedCoords</c>: The newly ordered and normalized list of coordinates.</description></item>
        /// <item><description><c>originalIndices</c>: An array mapping each new position back to its original index in <paramref name="rawCoords"/>.</description></item>
        /// </list>
        /// </returns>
        private static CanonicalizationResult CanonicalizeFace(List<Vector3D> rawCoords, bool wasQuad)
        {
            if (rawCoords.Count != 4)
            {
                int[] identity = Enumerable.Range(0, rawCoords.Count).ToArray();
                return new CanonicalizationResult(new List<Vector3D>(rawCoords), identity);
            }

            int vertexCount = wasQuad? 4 : 3;

            // Find corner closest to UV top-left (minU, maxV in V-up space).
            double minU = rawCoords.Min(p => p.X);
            double maxV = rawCoords.Max(p => p.Y);

            // Cyclic shift so topLeft lands at index 0.
            int topLeft = 0;
            double bestDistSq = double.MaxValue;

            for (int vertexID = 0; vertexID < vertexCount; vertexID++)
            {
                double du = rawCoords[vertexID].X - minU;
                double dv = maxV - rawCoords[vertexID].Y;
                double d = du * du + dv * dv;

                if (d < bestDistSq)
                {
                    bestDistSq = d;
                    topLeft = vertexID;
                }
            }

            int[] indices = new int[4];

            for (int vertexID = 0; vertexID < vertexCount; vertexID++)
            {
                indices[vertexID] = (topLeft + vertexID) % vertexCount;
            }

            List<Vector3D> ordered = new List<Vector3D>(4);

            for (int vertexID = 0; vertexID < vertexCount; vertexID++)
            {
                ordered.Add(rawCoords[indices[vertexID]]);
            }

            if(!wasQuad)
            {
                indices[3] = indices[2];
                ordered.Add(ordered.Last());
            }

            // Check UV winding. For a CW quad [TL, TR, BR, BL] in
            // V-up UV space, (uv[1]-uv[0]) × (uv[3]-uv[0]) has
            // negative Z. Positive Z means CCW — swap indices 1↔3
            // to convert [TL, BL, BR, TR] → [TL, TR, BR, BL].
            Vector3D e01 = ordered[1] - ordered[0];
            Vector3D e03 = ordered[3] - ordered[0];
            double signedArea = (e01.X * e03.Y) - (e01.Y * e03.X);

            if (signedArea > 0.0) // CCW → swap 1 et 3
            {
                (indices[1], indices[3]) = (indices[3], indices[1]);
                (ordered[1], ordered[3]) = (ordered[3], ordered[1]);

                //Update the duplicated last vertex when we are dealing with a triangle polygon
                if(!wasQuad)
                {
                    indices[2] = indices[3];
                    ordered[2] = ordered[3];
                }
            }

            return new CanonicalizationResult(ordered, indices);
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
