using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using BII.WasaBii.UnitSystem;
using JetBrains.Annotations;

namespace BII.WasaBii.Geometry
{
    public static class SystemQuaternionExtensions
    {
        
        [Pure] public static Quaternion Inverse(this Quaternion q) => Quaternion.Inverse(q);
        
        // Note DS: We cannot simply compare two quaternions component-wise because a quaternion and its negation
        // represent the same orientation. So we could do the component-wise comparison twice with a negated and a
        // non-negated rhs and return true if either comparison is true. However, this would be too much work,
        // so we do this neat dot-product thing instead. https://gamedev.stackexchange.com/a/75108
        // Note that this will only work for valid (unit-length) quaternions.
        [Pure] public static bool IsNearly(this Quaternion lhs, Quaternion rhs, double equalityThreshold = 1E-06) => 
            MathF.Abs(Quaternion.Dot(lhs, rhs)) >= 1 - equalityThreshold;

        [Pure]
        public static Quaternion SlerpTo(this Quaternion self, Quaternion other, double progress, bool shouldClamp = true) =>
            Quaternion.Slerp(self, other, (float)(shouldClamp ? Math.Clamp(progress, 0, 1) : progress));

        [Pure]
        public static Quaternion Average(this IEnumerable<Quaternion> quaternions) =>
            Quaternion.Normalize(quaternions.Aggregate(Quaternion.Add));

        [Pure] public static Angle AngleOn(this Quaternion q, Vector3 axis, Handedness handedness = Handedness.Default) {
            // An arbitrary vector that is orthogonal to `axis`.
            // Taken from https://math.stackexchange.com/a/3077100
            // Proof: 
            // axis dot vec = axis.x * (axis.y + axis.z) + axis.y * (axis.z - axis.x) + axis.z * (-axis.x - axis.y)
            //     = x*y-y*x + x*z-z*x + y*z-y*z
            //     = 0
            var vec = new Vector3(axis.Y + axis.Z, axis.Z - axis.X, -axis.X - axis.Y);
            return vec.SignedAngleTo(Vector3.Transform(vec, q), axis, handedness);
        }
    }
}