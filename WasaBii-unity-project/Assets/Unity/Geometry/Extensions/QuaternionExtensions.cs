using BII.WasaBii.Core;
using UnityEngine;

namespace BII.WasaBii.Unity.Geometry {
    
    public static class QuaternionExtensions {
        
        public static bool IsNearly(this Quaternion lhs, Quaternion rhs, float equalityThreshold = 1E-30f) => 
            lhs.x.IsNearly(rhs.x, equalityThreshold) 
            && lhs.y.IsNearly(rhs.y, equalityThreshold) 
            && lhs.z.IsNearly(rhs.z, equalityThreshold)
            && lhs.w.IsNearly(rhs.w, equalityThreshold);

        public static bool IsValid(this Quaternion quaternion)
            => !(float.IsNaN(quaternion.x) || float.IsNaN(quaternion.y) || float.IsNaN(quaternion.z) || float.IsNaN(quaternion.w)) 
               && (quaternion.x.Sqr() + quaternion.y.Sqr() + quaternion.z.Sqr() + quaternion.w.Sqr()).IsNearly(1);

        public static Quaternion LerpTo(
            this Quaternion from, Quaternion to, float progress, bool shouldClamp = true
        ) => shouldClamp ? Quaternion.Lerp(from, to, progress) : Quaternion.LerpUnclamped(from, to, progress);

    }
    
}