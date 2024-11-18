#if UNITY_2022_1_OR_NEWER

using System;
using System.Collections.Generic;
using System.Linq;
using BII.WasaBii.Core;
using BII.WasaBii.UnitSystem;
using JetBrains.Annotations;
using UnityEngine;

namespace BII.WasaBii.Geometry
{
    public static class UnityNumericsExtensions
    {
        [Pure] public static bool IsNearly(this Vector3 self, Vector3 other, double threshold = 1E-06) =>
            self.x.IsNearly(other.x, (float)threshold)
            && self.y.IsNearly(other.y, (float)threshold)
            && self.z.IsNearly(other.z, (float)threshold);

        [Pure] public static Vector3 LerpTo(this Vector3 from, Vector3 to, double progress, bool shouldClamp = true) =>
            shouldClamp 
                ? Vector3.Lerp(from, to, (float)progress)
                : Vector3.LerpUnclamped(from, to, (float)progress);

        [Pure] public static Vector3 SlerpTo(this Vector3 from, Vector3 to, double progress, bool shouldClamp = true) =>
            shouldClamp 
                ? Vector3.Slerp(from, to, (float)progress)
                : Vector3.SlerpUnclamped(from, to, (float)progress);

        // Note DS: We cannot simply compare two quaternions component-wise because a quaternion and its negation
        // represent the same orientation. So we could do the component-wise comparison twice with a negated and a
        // non-negated rhs and return true if either comparison is true. However, this would be too much work,
        // so we do this neat dot-product thing instead. https://gamedev.stackexchange.com/a/75108
        // Note that this will only work for valid (unit-length) quaternions.
        [Pure] public static bool IsNearly(this Quaternion lhs, Quaternion rhs, double equalityThreshold = 1E-06) => 
            MathF.Abs(Quaternion.Dot(lhs, rhs)) >= 1 - equalityThreshold;

        public static Vector3 Average(this IEnumerable<Vector3> vectors) =>
            vectors.Average((l, r) => l + r, (accum, count) => accum / count);

        public static Quaternion Average(this IEnumerable<Quaternion> quaternions) {
            var (xVals, yVals, zVals, wVals) = quaternions.Select(q => (q.x, q.y, q.z, q.w)).Unzip();
            return new Quaternion(
                xVals.Sum(),
                yVals.Sum(),
                zVals.Sum(),
                wVals.Sum()
            ).normalized;
        }

        [Pure] public static Quaternion SlerpTo(
            this Quaternion from, Quaternion to, double progress, bool shouldClamp = true
        ) => shouldClamp 
            ? Quaternion.Slerp(from, to, (float)progress) 
            : Quaternion.SlerpUnclamped(from, to, (float)progress);

        [Pure] public static Vector3 Min(this Vector3 a, Vector3 b) => Vector3.Min(a, b);
        [Pure] public static Vector3 Max(this Vector3 a, Vector3 b) => Vector3.Max(a, b);

        /// <inheritdoc cref="Vector3.Angle"/>
        [Pure] public static Angle AngleTo(this Vector3 from, Vector3 to) => Vector3.Angle(from, to).Degrees();
        
        /// <inheritdoc cref="SystemVectorExtensions.SignedAngleTo"/>
        [Pure] public static Angle SignedAngleTo(this Vector3 from, Vector3 to, Vector3 axis, Handedness handedness) {
            var ret = Vector3.SignedAngle(from, to, axis).Degrees();
            return handedness switch {
                Handedness.Left => ret,
                Handedness.Right => -ret,
                _ => throw new UnsupportedEnumValueException(handedness)
            };
        }

        /// <inheritdoc cref="SystemVectorExtensions.SignedAngleOnPlaneTo"/>
        [Pure] public static Angle SignedAngleOnPlaneTo(this Vector3 from, Vector3 to, Vector3 axis, Handedness handedness) =>
            from.ToSystemVector().SignedAngleOnPlaneTo(to.ToSystemVector(), axis.ToSystemVector(), handedness);

    }
}
#endif