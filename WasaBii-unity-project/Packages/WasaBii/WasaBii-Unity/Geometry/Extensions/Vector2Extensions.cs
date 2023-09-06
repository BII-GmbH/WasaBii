using System;
using System.Runtime.CompilerServices;
using BII.WasaBii.Core;
using JetBrains.Annotations;
using UnityEngine;

namespace BII.WasaBii.Unity.Geometry {
    
    public static class Vector2Extensions {
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure] public static float DistanceTo(this Vector2 v1, Vector2 v2)
            => Vector2.Distance(v1, v2);

        /// <inheritdoc cref="Vector3Extensions.IsNearly(Vector3,Vector3,float)"/>
        [Pure] public static bool IsNearly(this Vector2 lhs, Vector2 rhs, float equalityThreshold = 1E-30f) => 
            lhs.x.IsNearly(rhs.x, equalityThreshold) 
            && lhs.y.IsNearly(rhs.y, equalityThreshold);

        [Pure] public static float Cross(this Vector2 a, Vector2 b) 
            => a.x * b.y - b.x * a.y;

        [Pure] public static bool IsParallelTo(this Vector2 v1, Vector2 v2) =>
            Math.Abs(Vector2.Dot(v1, v2)).IsNearly(v1.magnitude * v2.magnitude);

    }
    
}