namespace ModelConverter.Geometry
{
    using System.Collections.Generic;

    /// <summary>
    /// WaveFront model file
    /// </summary>
    public class Model
    {
        /// <summary>
        /// Gets model faces
        /// </summary>
        public List<Face> Faces { get; } = new List<Face>();

        /// <summary>
        /// Gets or sets model name
        /// </summary>
        public string Name { get; set; } = string.Empty;
    }

}
