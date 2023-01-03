using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using BII.WasaBii.Core;
using BII.WasaBii.UnitSystem;
using JetBrains.Annotations;

namespace BII.WasaBii.Geometry
{
    public static class SystemQuaternionExtensions
    {
        
        [Pure] public static Quaternion Inverse(this Quaternion q) => Quaternion.Inverse(q);
        
        [Pure] public static bool IsNearly(this Quaternion self, Quaternion other, double threshold = 1E-06) =>
            self.X.IsNearly(other.X, (float)threshold)
            && self.Y.IsNearly(other.Y, (float)threshold)
            && self.Z.IsNearly(other.Z, (float)threshold)
            && self.W.IsNearly(other.W, (float)threshold);

        [Pure]
        public static Quaternion SlerpTo(this Quaternion self, Quaternion other, double progress, bool shouldClamp) =>
            Quaternion.Slerp(self, other, (float)(shouldClamp ? Math.Clamp(progress, 0, 1) : progress));

        [Pure]
        public static Quaternion Average(this IEnumerable<Quaternion> quaternions) =>
            Quaternion.Normalize(quaternions.Aggregate(Quaternion.Add));

        [Pure] public static Angle AngleOn(this Quaternion q, Vector3 axis) {
            // An arbitrary vector that is orthogonal to `axis`.
            // Taken from https://math.stackexchange.com/a/3077100
            // Proof: 
            // axis dot vec = axis.x * (axis.y + axis.z) + axis.y * (axis.z - axis.x) + axis.z * (-axis.x - axis.y)
            //     = x*y-y*x + x*z-z*x + y*z-y*z
            //     = 0
            var vec = new Vector3(axis.Y + axis.Z, axis.Z - axis.X, -axis.X - axis.Y);
            // return Vector3.SignedAngle(vec, q * vec, axis).Degrees();
            throw new NotImplementedException();
        }
    }
}