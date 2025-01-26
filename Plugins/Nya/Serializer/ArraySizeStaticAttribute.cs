namespace Nya.Serializer
{
    using System;

    /// <summary>
    /// Static array size attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class ArraySizeStaticAttribute : ArraySizeAttribute
    {
        /// <summary>
        /// Initializes a new isntance of the <see cref="ArraySizeStaticAttribute"/> class
        /// </summary>
        /// <param name="size">Array size</param>
        public ArraySizeStaticAttribute(int size)
        {
            this.Size = size;
        }

        /// <summary>
        /// Gets or sets size of an array
        /// </summary>
        public int Size { get; set; }
    }
}