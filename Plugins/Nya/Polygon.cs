namespace Nya
{
    using Nya.Serializer;

    /// <summary>
    /// Catgirl polygon
    /// </summary>
    public class Polygon
    {
        /// <summary>
        /// Gets or sets polygon normal
        /// </summary>
        [FieldOrder(0)]
        public FxVector Normal { get; set; } = new FxVector();

        /// <summary>
        /// Gets or sets Point indicies
        /// </summary>
        [ArraySizeStatic(4)]
        [FieldOrder(1)]
        public short[] Vertices { get; set; } = new short[4];
    }
}
