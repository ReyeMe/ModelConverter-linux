namespace ModelConverter.Geometry
{
    using System.Collections.Generic;
    using System.Diagnostics;

    /// <summary>
    /// WaveFront model file
    /// </summary>
    [DebuggerDisplay("Name = {Name}")]
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
