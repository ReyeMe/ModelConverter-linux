namespace Nya
{
    using Nya.Serializer;

    /// <summary>
    /// Catgirl face flags
    /// </summary>
    public class FaceFlags
    {
        /// <summary>
        /// Gets or sets base color
        /// </summary>
        [FieldOrder(2)]
        public ushort BaseColor { get; set; } = 0x8000;

        /// <summary>
        /// Gets or sets flags
        /// </summary>
        [FieldOrder(0)]
        public byte Flags { get; set; } = 0x00;

        /// <summary>
        /// Gets or sets a flag indicating whether face has mesh effect
        /// </summary>
        public bool HasMeshEffect
        {
            get
            {
                return (this.Flags & 0x40) != 0;
            }

            set
            {
                this.Flags = (byte)((this.Flags & 0xbf) | (value ? 0x40 : 0));
            }
        }

        /// <summary>
        /// Gets or sets a flag indicating whether face has half transparency effect
        /// </summary>
        public bool IsHalfTransparent
        {
            get
            {
                return (this.Flags & 0x10) != 0;
            }

            set
            {
                this.Flags = (byte)((this.Flags & 0xef) | (value ? 0x10 : 0));
            }
        }

        /// <summary>
        /// Gets or sets a flag indicating whether face is double sided
        /// </summary>
        public bool IsDoubleSided
        {
            get
            {
                return (this.Flags & 0x20) != 0;
            }

            set
            {
                this.Flags = (byte)((this.Flags & 0xdf) | (value ? 0x20 : 0));
            }
        }

        /// <summary>
        /// Gets or sets a flag indicating whether face has a texture
        /// </summary>
        public bool HasTexture
        {
            get
            {
                return (this.Flags & 0x80) != 0;
            }

            set
            {
                this.Flags =  (byte)((this.Flags & 0x7f) | (value ? 0x80 : 0));
            }
        }

        /// <summary>
        /// Reserverd for future use
        /// </summary>
        [FieldOrder(1)]
        public byte Reserved { get; set; }

        /// <summary>
        /// Gets or sets texture assigned to this face
        /// </summary>
        [FieldOrder(3)]
        public int TextureId { get; set; }
    }
}
