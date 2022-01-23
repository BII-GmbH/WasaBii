using BII.WasaBii.Core;
using JetBrains.Annotations;
using UnityEngine;

namespace BII.WasaBii.Unity.Geometry {
    
    public static class Vector4Extensions {
        
        /// <inheritdoc cref="Vector3Extensions.IsNearly(Vector3,Vector3,float)"/>
        [Pure] public static bool IsNearly(this Vector4 lhs, Vector4 rhs, float equalityThreshold = 1E-30f) => 
            lhs.x.IsNearly(rhs.x, equalityThreshold) 
            && lhs.y.IsNearly(rhs.y, equalityThreshold)
            && lhs.z.IsNearly(rhs.z, equalityThreshold)
            && lhs.w.IsNearly(rhs.w, equalityThreshold);

    }
}