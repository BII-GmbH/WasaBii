using System.Numerics;

namespace BII.WasaBii.Core
{
    
    public static class SystemQuaternionExtensions {
        
        public static bool IsNearly(this Quaternion lhs, Quaternion rhs, float equalityThreshold = 1E-30f) => 
            lhs.X.IsNearly(rhs.X, equalityThreshold) 
            && lhs.Y.IsNearly(rhs.Y, equalityThreshold) 
            && lhs.Z.IsNearly(rhs.Z, equalityThreshold)
            && lhs.W.IsNearly(rhs.W, equalityThreshold);

        public static bool IsValid(this Quaternion quaternion)
            => !(float.IsNaN(quaternion.X) || float.IsNaN(quaternion.Y) || float.IsNaN(quaternion.Z) || float.IsNaN(quaternion.W)) 
                && (quaternion.X.Sqr() + quaternion.Y.Sqr() + quaternion.Z.Sqr() + quaternion.W.Sqr()).IsNearly(1);

        public static Quaternion LerpTo(
            this Quaternion from, Quaternion to, float progress, bool shouldClamp = true
        ) => Quaternion.Lerp(from, to, shouldClamp ? progress.Clamp(0, 1) : progress);

    }

}