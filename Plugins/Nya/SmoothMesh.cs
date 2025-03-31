namespace Nya
{
    using ModelConverter.Geometry;
    using Nya.Serializer;

    /// <summary>
    /// Smooth mesh data
    /// </summary>
    public class SmoothMesh : Mesh
    {
        /// <summary>
        /// Initializes a new <see cref="SmoothMesh"/> class
        /// </summary>
        /// <param name="group">Model object group</param>
        /// <param name="model">Model object</param>
        /// <param name="textures">Embed textures</param>
        /// <param name="settings">Converter settings</param>
        /// <param name="uvTextures">Embed UV textures</param>
        public SmoothMesh(Group group, Model model, List<Texture> textures, NyaArguments settings, ref List<Texture> uvTextures) : base(group, model, textures, settings, ref uvTextures)
        {
            this.Normals = new FxVector[this.PointCount];

            for (int f = 0; f < this.PolygonCount; f++)
            {
                for (int n = 0; n < model.Faces[f].Normals.Count; n++)
                {
                    this.Normals[this.Polygons[f].Vertices[n]] = FxVector.FromVertex(group.Normals[model.Faces[f].Normals[n]].GetNormal());
                }
            }
        }

        /// <summary>
        /// Gets or sets vertex normals
        /// </summary>
        [ArraySizeDynamic("PointCount")]
        [FieldOrder(5)]
        public FxVector[] Normals { get; set; } = Array.Empty<FxVector>();
    }
}
