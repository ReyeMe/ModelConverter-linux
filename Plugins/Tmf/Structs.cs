namespace Tmf
{
    using System.Runtime.InteropServices;

    /// <summary>
    /// Face flags
    /// </summary>
    [Flags]
    public enum TmfFaceFlags : byte
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
    /// Model type
    /// </summary>
    public enum TmFType : byte
    {
        /// <summary>
        ///  Static model
        /// </summary>
        Static = 0
    }

    /// <summary>
    /// Model face
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct TmfFace
    {
        /// <summary>
        /// Normal vector
        /// </summary>
        public TmfVertice Normal;

        /// <summary>
        /// Face quad indexes
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public ushort[] Indexes;

        /// <summary>
        /// Face flags (meshed, double-sided, etc...)
        /// </summary>
        public TmfFaceFlags Flags;

        /// <summary>
        /// Texture index
        /// </summary>
        public byte TextureIndex;

        /// <summary>
        /// Byte padding
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] Padding;
    }

    /// <summary>
    /// File header
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct TmfHeader
    {
        /// <summary>
        /// Model file type
        /// </summary>
        public TmFType Type;

        /// <summary>
        /// Number of texture file entries in the file
        /// </summary>
        public byte TextureCount;

        /// <summary>
        /// Number of models in the file
        /// </summary>
        public byte ModelCount;

        /// <summary>
        /// Reserved sapce for future stuff
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public byte[] Reserved;

        /// <summary>
        /// Face textures
        /// </summary>
        public TmfTextureEntry[] Textures;

        /// <summary>
        /// 3D models
        /// </summary>
        public TmfModelHeader[] Models;
    };

    /// <summary>
    /// Model entry header
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct TmfModelHeader
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
        /// Model vertices
        /// </summary>
        public TmfVertice[] Vertices;

        /// <summary>
        /// Model faces
        /// </summary>
        public TmfFace[] Faces;
    };

    /// <summary>
    /// Texture file entry
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct TmfTextureEntry
    {
        /// <summary>
        /// File name
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 13)]
        public byte[] FileName;

        /// <summary>
        /// Diffuse color
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] Color;
    };

    /// <summary>
    /// Model vertice
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct TmfVertice
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