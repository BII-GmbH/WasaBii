using System;
using BII.WasaBii.Core;
using UnityEngine;

namespace BII.Utilities.Unity {
    public static class Vector3Extensions {
        /// <summary>
        /// Returns a vector that is opposite to the mirrored vector along the center vector
        /// E.g. the vector is mirrored relative to the center
        /// </summary>
        /// <param name="centerVector">The point the other vector is mirrored on</param>
        /// <param name="mirroredVector">The point that is mirrored</param>
        public static Vector3 OppositeOf(this Vector3 centerVector, Vector3 mirroredVector) =>
            centerVector + (centerVector - mirroredVector);

        public static bool IsValid(this Vector3 vector)
            => !(float.IsNaN(vector.x) || float.IsNaN(vector.y) || float.IsNaN(vector.z));

        public static float DistanceTo(this Vector3 v1, Vector3 v2)
            => Vector3.Distance(v1, v2);

        public static float DistanceTo(this Vector2 v1, Vector2 v2)
            => Vector2.Distance(v1, v2);

        
        // Since rounding errors can be greater than `float.Epsilon`,
        // the threshold must also be greater.
        public static bool IsNearly(this Vector3 lhs, Vector3 rhs, float equalityThreshold = 1E-30f) => 
            lhs.x.IsNearly(rhs.x, equalityThreshold) 
            && lhs.y.IsNearly(rhs.y, equalityThreshold) 
            && lhs.z.IsNearly(rhs.z, equalityThreshold);
        
        public static bool IsNearly(this Vector4 lhs, Vector4 rhs, float equalityThreshold = 1E-30f) => 
            lhs.x.IsNearly(rhs.x, equalityThreshold) 
            && lhs.y.IsNearly(rhs.y, equalityThreshold) 
            && lhs.z.IsNearly(rhs.z, equalityThreshold)
            && lhs.w.IsNearly(rhs.w, equalityThreshold);

        public static float Average(this Vector3 vector)
            => (vector.x + vector.y + vector.z) / 3;

        public static float Dot(this Vector3 lhs, Vector3 rhs)
            => Vector3.Dot(lhs, rhs);

        public static Vector3 Cross(this Vector3 lhs, Vector3 rhs)
            => Vector3.Cross(lhs, rhs);

        public static bool PointsInSameDirectionAs(this Vector3 lhs, Vector3 rhs) => lhs.Dot(rhs) > 0;
        
        public static Vector4 WithW(this Vector3 vec, float w) => new Vector4(vec.x, vec.y, vec.z, w);
        
        public static Vector3 NegateIf(this Vector3 value, bool shouldNegate) => shouldNegate ? -value : value;
        
        public static Vector3 Map(this Vector3 v, Func<float, float> func) => 
            new Vector3(func(v.x), func(v.y), func(v.z));
        
        public static Vector3 CombineWith(this Vector3 a, Vector3 b, Func<float, float, float> func) =>
            new Vector3(func(a.x, b.x), func(a.y, b.y), func(a.z, b.z));

        public static float SignedMagnitude(this Vector3 v)
            => v.magnitude * (v.x < 0 ^ v.y < 0 ^ v.z < 0 ? -1 : 1);
        
        public static Vector3 AsXZWithY(this Vector2 xz, float y) => new Vector3(xz.x, y, xz.y);

    }


}

