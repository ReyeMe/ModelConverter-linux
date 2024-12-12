namespace ModelConverter.Geometry
{
    /// <summary>
    /// WaveFront model collection list
    /// </summary>
    public class Group : List<Model>
    {
        /// <summary>
        /// Gets or sets material texture names
        /// </summary>
        public Dictionary<string, Material> MaterialTextures { get; set; } = new Dictionary<string, Material>();

        /// <summary>
        /// Gets face normal
        /// </summary>
        public List<Vector3D> Normals { get; } = new List<Vector3D>();

        /// <summary>
        /// Gets model UV points
        /// </summary>
        public List<Vector3D> Uv { get; } = new List<Vector3D>();

        /// <summary>
        /// Gets model vertices
        /// </summary>
        public List<Vector3D> Vertices { get; } = new List<Vector3D>();
    }
}