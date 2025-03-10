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
        /// Gets or sets a value indicating whether this face must be flat shaded
        /// </summary>
        public bool IsFlat { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether face is rendered at half brightness
        /// </summary>
        public bool IsHalfBright { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether face is rendered as half-transparent
        /// </summary>
        public bool IsHalfTransparent { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether face is rendered as mesh
        /// </summary>
        public bool IsMesh { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether face is only a wireframe
        /// </summary>
        public bool IsWireframe { get; set; }

        /// <summary>
        /// Gets or sets material name
        /// </summary>
        public string Material { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets normal vector indices
        /// </summary>
        public List<int> Normals { get; set; } = new List<int>();

        /// <summary>
        /// Gets or sets sort mode
        /// </summary>
        public int SortMode { get; set; }

        /// <summary>
        /// Gets or sets UV indices
        /// </summary>
        public List<int> Uv { get; set; } = new List<int>();

        /// <summary>
        /// Gets or sets vertices indices
        /// </summary>
        public List<int> Vertices { get; set; } = new List<int>();
    }
}