namespace Cobra
{
    using System.Runtime.InteropServices;

    /// <summary>
    /// Face flags
    /// </summary>
    [Flags]
    public enum CobraFaceFlags : ushort
    {
        /// <summary>
        /// No flags applied
        /// </summary>
        None = 0,

        /// <summary>
        /// Face is doulbe sided
        /// </summary>
        DoubleSided = 1,

        /// <summary>
        /// Face is meshed
        /// </summary>
        Meshed = 2
    }

    /// <summary>
    /// Model face
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct CobraFace
    {
        /// <summary>
        /// Normal vector
        /// </summary>
        public CobraVertex Normal;

        /// <summary>
        /// Face quad indexes
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public ushort[] Indexes;

        /// <summary>
        /// Texture index
        /// </summary>
        public ushort TextureIndex;

        /// <summary>
        /// Face flags (meshed, double-sided, etc...)
        /// </summary>
        public CobraFaceFlags Flags;
    }

    /// <summary>
    /// File header
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct CobraHeader
    {
        /// <summary>
        /// Number of texture file entries in the file
        /// </summary>
        public ushort TextureCount;

        /// <summary>
        /// Number of models in the file
        /// </summary>
        public ushort ModelCount;

        /// <summary>
        /// Reserved space for future stuff
        /// </summary>
        public uint Reserved;

        /// <summary>
        /// Face materials
        /// </summary>
        public CobraMaterial[] Materials;

        /// <summary>
        /// Model objects
        /// </summary>
        public CobraObject[] Objects;
    };

    /// <summary>
    /// Model entry header
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct CobraObject
    {
        /// <summary>
        /// Number of vertices
        /// </summary>
        public ushort VerticesCount;

        /// <summary>
        /// Number of faces
        /// </summary>
        public ushort FaceCount;

        /// <summary>
        /// object vertices
        /// </summary>
        public CobraVertex[] Vertices;

        /// <summary>
        /// Object faces
        /// </summary>
        public CobraFace[] Faces;
    };

    /// <summary>
    /// Material entry
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct CobraMaterial
    {
        /// <summary>
        /// File name or color, and some padding
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] Data;
    };

    /// <summary>
    /// Model vertice
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct CobraVertex
    {
        /// <summary>
        /// X coordinate
        /// </summary>
        public Int32 X;

        /// <summary>
        /// Y coordinate
        /// </summary>
        public Int32 Y;

        /// <summary>
        /// Z coordinate
        /// </summary>
        public Int32 Z;
    }
}