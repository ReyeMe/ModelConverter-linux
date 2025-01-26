namespace Nya.Serializer
{
    using System;

    /// <summary>
    /// Dynamic array size attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class ArraySizeDynamicAttribute : ArraySizeAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ArraySizeDynamicAttribute"/> class
        /// </summary>
        /// <param name="propertyName">Name of the property indicating size of the array</param>
        public ArraySizeDynamicAttribute(string propertyName)
        {
            this.PropertyName = propertyName;
        }

        /// <summary>
        /// Gets or sets property name
        /// </summary>
        public string PropertyName { get; set; }
    }
}