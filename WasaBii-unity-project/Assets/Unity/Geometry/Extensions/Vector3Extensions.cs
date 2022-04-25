using System;
using BII.WasaBii.Core;
using JetBrains.Annotations;
using UnityEngine;

namespace BII.WasaBii.Unity.Geometry {
    
    public static class Vector3Extensions {
        
        /// <summary>
        /// Returns a vector that is opposite to the mirrored vector along the center vector
        /// E.g. the vector is mirrored relative to the center
        /// </summary>
        /// <param name="centerVector">The point the other vector is mirrored on</param>
        /// <param name="mirroredVector">The point that is mirrored</param>
        [Pure] public static Vector3 OppositeOf(this Vector3 centerVector, Vector3 mirroredVector) =>
            centerVector + (centerVector - mirroredVector);

        [Pure] public static bool IsValid(this Vector3 vector)
            => !(float.IsNaN(vector.x) || float.IsNaN(vector.y) || float.IsNaN(vector.z));

        [Pure] public static float DistanceTo(this Vector3 v1, Vector3 v2)
            => Vector3.Distance(v1, v2);

        // Since rounding errors can be greater than `float.Epsilon`,
        // the threshold must also be greater.
        [Pure] public static bool IsNearly(this Vector3 lhs, Vector3 rhs, float equalityThreshold = 1E-30f) => 
            lhs.x.IsNearly(rhs.x, equalityThreshold) 
            && lhs.y.IsNearly(rhs.y, equalityThreshold) 
            && lhs.z.IsNearly(rhs.z, equalityThreshold);

        [Pure] public static float Dot(this Vector3 lhs, Vector3 rhs)
            => Vector3.Dot(lhs, rhs);

        [Pure] public static Vector3 Cross(this Vector3 lhs, Vector3 rhs)
            => Vector3.Cross(lhs, rhs);

        [Pure] public static bool PointsInSameDirectionAs(this Vector3 lhs, Vector3 rhs) => lhs.Dot(rhs) > 0;
        
        [Pure] public static Vector3 Map(this Vector3 v, Func<float, float> func) => 
            new(func(v.x), func(v.y), func(v.z));
        
        [Pure] public static Vector3 CombineWith(this Vector3 a, Vector3 b, Func<float, float, float> func) =>
            new(func(a.x, b.x), func(a.y, b.y), func(a.z, b.z));

        [Pure] public static bool IsParallelTo(this Vector3 v1, Vector3 v2) =>
            Math.Abs(Vector3.Dot(v1, v2)).IsNearly(v1.magnitude * v2.magnitude);

    }
    
}