namespace ActDim.Three.Math
{
    /// <summary>
    /// A class representing a 4x4 matrix.
    /// Analogous to: https://threejs.org/docs/index.html#api/math/Matrix4
    /// JS Source: https://github.com/mrdoob/three.js/blob/master/src/math/Matrix4.js
    /// </summary>
    public class Matrix4
    {
        /// <summary>
        /// A column-major list of matrix values. 
        /// </summary>
        public float[] Elements { get; set; }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Matrix4()
        {
            Elements = new float[16];
        }

        /// <summary>
        /// Builds a Matrix4 from a <see cref="System.Numerics.Matrix4x4"/>.
        /// <para>
        /// three.js stores a column-major matrix with translation in the last column (te[12..14]);
        /// System.Numerics is row-major with row-vector convention and translation in the last row
        /// (M41..M43). Copying the M?? fields in order into the column-major <see cref="Elements"/>
        /// array performs the required transpose implicitly, so the represented transform is preserved.
        /// </para>
        /// </summary>
        public Matrix4(System.Numerics.Matrix4x4 m) : this()
        {
            Elements[0] = m.M11; Elements[1] = m.M12; Elements[2] = m.M13; Elements[3] = m.M14;
            Elements[4] = m.M21; Elements[5] = m.M22; Elements[6] = m.M23; Elements[7] = m.M24;
            Elements[8] = m.M31; Elements[9] = m.M32; Elements[10] = m.M33; Elements[11] = m.M34;
            Elements[12] = m.M41; Elements[13] = m.M42; Elements[14] = m.M43; Elements[15] = m.M44;
        }

        public static Matrix4 Identity()
        {
            return new Matrix4()
            {
                Elements = new float[16] { 1, 0, 0, 0,
                                           0, 1, 0, 0,
                                           0, 0, 1, 0,
                                           0, 0, 0, 1 }
            };
        }

        /// <summary>
        /// A scaling matrix. Mirrors <see cref="System.Numerics.Matrix4x4.CreateScale(System.Numerics.Vector3)"/>.
        /// </summary>
        public static Matrix4 CreateScale(Vector3 scale)
        {
            return new Matrix4(System.Numerics.Matrix4x4.CreateScale(scale.X, scale.Y, scale.Z));
        }

        /// <summary>
        /// A scaling matrix from component scales.
        /// </summary>
        public static Matrix4 CreateScale(float x, float y, float z)
        {
            return new Matrix4(System.Numerics.Matrix4x4.CreateScale(x, y, z));
        }

        /// <summary>
        /// A rotation matrix. Mirrors <see cref="System.Numerics.Matrix4x4.CreateFromQuaternion"/>.
        /// </summary>
        public static Matrix4 CreateFromQuaternion(Quaternion rotation)
        {
            return new Matrix4(System.Numerics.Matrix4x4.CreateFromQuaternion(
                new System.Numerics.Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W)));
        }

        /// <summary>
        /// A translation matrix. Mirrors <see cref="System.Numerics.Matrix4x4.CreateTranslation(System.Numerics.Vector3)"/>.
        /// </summary>
        public static Matrix4 CreateTranslation(Vector3 position)
        {
            return new Matrix4(System.Numerics.Matrix4x4.CreateTranslation(position.X, position.Y, position.Z));
        }

        /// <summary>
        /// Matrix product, delegating to System.Numerics so composition matches .NET semantics:
        /// <code>
        /// var m = Matrix4.CreateScale(scale)
        ///       * Matrix4.CreateFromQuaternion(rotation)
        ///       * Matrix4.CreateTranslation(position);
        /// </code>
        /// </summary>
        public static Matrix4 operator *(Matrix4 a, Matrix4 b)
        {
            return new Matrix4(a.ToMatrix4x4() * b.ToMatrix4x4());
        }

        public void SetPosition(Vector3 vector)
        {
            Elements[12] = vector.X;
            Elements[13] = vector.Y;
            Elements[14] = vector.Z;
        }

		public Vector3 GetPosition()
        {
            return new Vector3(Elements[12], Elements[13], Elements[14]);
        }

		public void MakeRotationFromQuaternion(Quaternion q)
        {
            var te = Elements;

            var x = q.X; var y = q.Y; var z = q.Z; var w = q.W;
            var x2 = x + x; var y2 = y + y; var z2 = z + z;
            var xx = x * x2; var xy = x * y2; var xz = x * z2;
            var yy = y * y2; var yz = y * z2; var zz = z * z2;
            var wx = w * x2; var wy = w * y2; var wz = w * z2;

            te[0] = 1 - (yy + zz);
            te[4] = xy - wz;
            te[8] = xz + wy;

            te[1] = xy + wz;
            te[5] = 1 - (xx + zz);
            te[9] = yz - wx;

            te[2] = xz - wy;
            te[6] = yz + wx;
            te[10] = 1 - (xx + yy);

            // last column
            te[3] = 0;
            te[7] = 0;
            te[11] = 0;

            // bottom row
            te[12] = 0;
            te[13] = 0;
            te[14] = 0;
            te[15] = 1;
        }

		public void Scale(Vector3 v)
        {
            var te = Elements;
            var x = v.X; var y = v.Y; var z = v.Z;

            te[0] *= x; te[4] *= y; te[8] *= z;
            te[1] *= x; te[5] *= y; te[9] *= z;
            te[2] *= x; te[6] *= y; te[10] *= z;
            te[3] *= x; te[7] *= y; te[11] *= z;
        }

		public void Compose(Vector3 position, Quaternion quaternion, Vector3 scale)
        {
            MakeRotationFromQuaternion(quaternion);
            Scale(scale);
            SetPosition(position);
        }

		public void LookAt(Vector3 eye, Vector3 target, Vector3 up)
        {
            var x = new Vector3();
            var y = new Vector3();
            var z = new Vector3();

            var te = this.Elements;

            z.SubVectors(eye, target);

            if (z.LengthSq() == 0)
            {

                // eye and target are in the same position

                z.Z = 1;

            }

            z.Normalize();
            x.CrossVectors(up, z);

            te[0] = x.X; te[4] = y.X; te[8] = z.X;
            te[1] = x.Y; te[5] = y.Y; te[9] = z.Y;
            te[2] = x.Z; te[6] = y.Z; te[10] = z.Z;
        }

		public float[] ToArray() { return Elements; }

        /// <summary>
        /// Converts back to a <see cref="System.Numerics.Matrix4x4"/> (inverse of the
        /// <see cref="Matrix4(System.Numerics.Matrix4x4)"/> constructor).
        /// </summary>
        public System.Numerics.Matrix4x4 ToMatrix4x4()
        {
            var e = Elements;
            return new System.Numerics.Matrix4x4(
                e[0], e[1], e[2], e[3],
                e[4], e[5], e[6], e[7],
                e[8], e[9], e[10], e[11],
                e[12], e[13], e[14], e[15]);
        }
    }
}
