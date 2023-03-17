using BII.WasaBii.Core;
using UnityEngine;

namespace BII.WasaBii.Unity.Geometry {
    
    public static class QuaternionExtensions {
        
        public static bool IsValid(this Quaternion quaternion)
            => !(float.IsNaN(quaternion.x) || float.IsNaN(quaternion.y) || float.IsNaN(quaternion.z) || float.IsNaN(quaternion.w)) 
               && (quaternion.x.Sqr() + quaternion.y.Sqr() + quaternion.z.Sqr() + quaternion.w.Sqr()).IsNearly(1);

    }
    
}