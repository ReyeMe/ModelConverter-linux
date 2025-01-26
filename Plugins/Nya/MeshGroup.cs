namespace Nya
{
    using Nya.Serializer;

    /// <summary>
    /// Mesh group
    /// </summary>
    /// <typeparam name="MeshType">Mesh group type</typeparam>
    public class MeshGroup<MeshType> where MeshType : Mesh
    {
        /// <summary>
        /// Gets or sets group type
        /// </summary>
        [FieldOrder(0)]
        public int Type
        {
            get
            {
                return (typeof(MeshType) == typeof(SmoothMesh)) ? 1 : 0;
            }
        }

        /// <summary>
        /// Gets number of meshes in group
        /// </summary>
        [FieldOrder(1)]
        public int MeshCount { get; set; }

        /// <summary>
        /// Gets or sets meshes in group
        /// </summary>
        [ArraySizeDynamic("MeshCount")]
        [FieldOrder(3)]
        public MeshType[] Meshes { get; set; } = new MeshType[0];

        /// <summary>
        /// Gets number of textures in the file
        /// </summary>
        [FieldOrder(2)]
        public int TextureCount { get; set; }

        /// <summary>
        /// Gets or sets textures in the file
        /// </summary>
        [ArraySizeDynamic("TextureCount")]
        [FieldOrder(4)]
        public Texture[] Textures { get; set; } = Array.Empty<Texture>();
    }
}
