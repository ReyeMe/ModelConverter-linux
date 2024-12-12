namespace ModelConverter.Geometry
{
    using System.Collections.Generic;

    /// <summary>
    /// WaveFront model file face
    /// </summary>
    public class Face
    {
        /// <summary>
        /// Gets or sets a value indicating whether face is double sided
        /// </summary>
        public bool IsDoubleSided { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether face is rendered as mesh
        /// </summary>
        public bool IsMesh { get; set; }

        /// <summary>
        /// Gets or sets material name
        /// </summary>
        public string Material { get; set; } = string.Empty;

        /// <summary>
        /// Gets normal vector indices
        /// </summary>
        public List<int> Normals { get; } = new List<int>();

        /// <summary>
        /// Gets UV indices
        /// </summary>
        public List<int> Uv { get; } = new List<int>();

        /// <summary>
        /// Gets vertices indices
        /// </summary>
        public List<int> Vertices { get; } = new List<int>();
    }
}