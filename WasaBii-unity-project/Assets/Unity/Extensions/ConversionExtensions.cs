using System.Numerics;
using BII.WasaBii.Core;

namespace BII.WasaBii.Unity {
    public static class ConversionExtensions {
        
        public static UnityEngine.Vector3 ToUnityVector(this Vector3 source) =>
            new UnityEngine.Vector3(source.X, source.Y, source.Z);
        
        public static Vector3 ToSystemVector(this UnityEngine.Vector3 source) =>
            new Vector3(source.x, source.y, source.z);
        
        
        public static UnityEngine.Bounds ToUnityBounds(this in Bounds source) =>
            new UnityEngine.Bounds(source.Center.ToUnityVector(), source.Size.ToUnityVector());
        
        public static Bounds ToSystemBounds(this UnityEngine.Bounds source) =>
            new Bounds(source.center.ToSystemVector(), source.size.ToSystemVector());
        
        public static UnityEngine.Quaternion ToUnityQuaternion(this System.Numerics.Quaternion q) => new UnityEngine.Quaternion(q.X, q.Y, q.Z, q.W);
        public static System.Numerics.Quaternion ToSystemQuaternion(this UnityEngine.Quaternion q) => 
            new System.Numerics.Quaternion(q.x, q.y, q.z, q.w);

    }
}