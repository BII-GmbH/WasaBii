using System;
using System.Numerics;
using BII.WasaBii.UnitSystem;

namespace BII.WasaBii.Core {

    public static class SystemVectorExtensions {

        public static Vector3 WithX(this Vector3 v, float x) =>
            new Vector3(x, v.Y, v.Z);
        
        public static Vector3 WithX(this Vector3 v, Func<float, float> mapping) =>
            new Vector3(mapping(v.X), v.Y, v.Z);

        public static Vector3 WithY(this Vector3 v, float y) => 
            new Vector3(v.X, y, v.Z);
        
        public static Vector3 WithY(this Vector3 v, Func<float, float> mapping) => 
            new Vector3(v.X, mapping(v.Y), v.Z);

        public static Vector3 WithZ(this Vector3 v, float z) => 
            new Vector3(v.X, v.Y, z);
        
        public static Vector3 WithZ(this Vector3 v, Func<float, float> mapping) => 
            new Vector3(v.X, v.Y, mapping(v.Z));

        public static Vector3 Map(this Vector3 v, Func<float, float> mapping) => 
            new Vector3(mapping(v.X), mapping(v.Y), mapping(v.Z));

        public static float DistanceTo(this Vector3 v1, Vector3 v2)
            => Vector3.Distance(v1, v2);

        public static Vector3 Normalized(this Vector3 v) => Vector3.Normalize(v);

        public static Angle AngleTo(this Vector3 a, Vector3 b) => Angles.Acos(Vector3.Dot(a, b));

        public static Vector3 Min(this Vector3 a, Vector3 b) => new(
            MathF.Min(a.X, b.X),
            MathF.Min(a.Y, b.Y),
            MathF.Min(a.Z, b.Z)
        );

        public static Vector3 Max(this Vector3 a, Vector3 b) => new(
            MathF.Max(a.X, b.X),
            MathF.Max(a.Y, b.Y),
            MathF.Max(a.Z, b.Z)
        );

        public static Vector3 LerpTo(this Vector3 from, Vector3 to, double progress, bool shouldClamp = true) =>
            Vector3.Lerp(from, to, (float)(shouldClamp ? Math.Clamp(progress, 0, 1) : progress));
        
        // https://en.wikipedia.org/wiki/Slerp
        public static Vector3 SlerpTo(this Vector3 from, Vector3 to, double progress, bool shouldClamp = true) {
            var theta = from.AngleTo(to);
            if (shouldClamp) progress = Math.Clamp(progress, 0, 1);
            return (
                (float)((1 - progress) * theta).Sin() * from
                 + (float)(progress * theta).Sin() * to
            ) / (float) theta.Sin();
        }

        /// <summary>
        /// Returns a quaternion <c>q</c> such that Vector3.Transform(<paramref name="from"/>, <c>q</c>) == <paramref name="to"/>.
        /// </summary>
        /// <param name="from">The vector to rotate to <paramref name="to"/>. **Must be normalized!**</param>
        /// <param name="to">The vector to which <paramref name="from"/> should be rotated. **Must be normalized!**</param>
        /// <param name="axisIfOpposite">In case the two vectors point in exactly opposite directions, the rotation should
        /// be 180°, but the axis is arbitrary and must be chosen. If no value is given, the Y-Axis (0,1,0) will be used.</param>
        public static Quaternion RotationTo(this Vector3 from, Vector3 to, Vector3? axisIfOpposite = null) {
            // https://stackoverflow.com/a/1171995
            var normal = Vector3.Cross(from, to);
            var dot = Vector3.Dot(from, to);
            return dot switch {
                // vectors are parallel, no rotation
                >= 0.9999f => Quaternion.Identity,
                // vectors are opposite, rotate 180° around any axis
                <= -0.9999f =>  Quaternion.CreateFromAxisAngle(axisIfOpposite ?? Vector3.UnitY, MathF.PI),
                _ => Quaternion.Normalize(new Quaternion(normal, dot))
            };
        }
    }

}