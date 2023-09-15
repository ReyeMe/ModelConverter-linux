namespace ModelConverter.Geometry
{
    using System;

    /// <summary>
    /// Simple vector
    /// </summary>
    public class Vector3D
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Vector3D"/> class
        /// </summary>
        public Vector3D()
        {
            // Do nothing
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Vector3D"/> class
        /// </summary>
        /// <param name="x">X component</param>
        /// <param name="y">Y component</param>
        /// <param name="z">Z component</param>
        public Vector3D(double x, double y, double z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        /// <summary>
        /// Gets or sets X component
        /// </summary>
        public double X { get; set; }

        /// <summary>
        /// Gets or sets Y component
        /// </summary>
        public double Y { get; set; }

        /// <summary>
        /// Gets or sets Z component
        /// </summary>
        public double Z { get; set; }

        /// <summary>
        /// Sub two vectors from each other
        /// </summary>
        /// <param name="first">First vector</param>
        /// <param name="second">Second vector</param>
        /// <returns>Sub of the vectors</returns>
        public static Vector3D operator -(Vector3D first, Vector3D second) => new(first.X - second.X, first.Y - second.Y, first.Z - second.Z);

        /// <summary>
        /// Multiply vector
        /// </summary>
        /// <param name="first">First vector</param>
        /// <param name="scale">Scale value</param>
        /// <returns>Multiply of the vector</returns>
        public static Vector3D operator *(Vector3D first, double scale) => new(first.X * scale, first.Y * scale, first.Z * scale);

        /// <summary>
        /// Divide vector
        /// </summary>
        /// <param name="first">First vector</param>
        /// <param name="scale">Scale value</param>
        /// <returns>Divide of the vector</returns>
        public static Vector3D operator /(Vector3D first, double scale) => new(first.X / scale, first.Y / scale, first.Z / scale);

        /// <summary>
        /// Add two vectors together
        /// </summary>
        /// <param name="first">First vector</param>
        /// <param name="second">Second vector</param>
        /// <returns>Sum of the vectors</returns>
        public static Vector3D operator +(Vector3D first, Vector3D second) => new(first.X + second.X, first.Y + second.Y, first.Z + second.Z);

        /// <summary>
        /// Get vector length
        /// </summary>
        /// <returns>Vector length</returns>
        public double GetLength()
        {
            return Math.Sqrt((this.X * this.X) + (this.Y * this.Y) + (this.Z * this.Z));
        }

        /// <summary>
        /// Normalize vector
        /// </summary>
        public void Normalize()
        {
            double lenght = this.GetLength();

            if (Math.Abs(lenght) > double.Epsilon)
            {
                this.X /= lenght;
                this.Y /= lenght;
                this.Z /= lenght;
            }
        }
    }
}