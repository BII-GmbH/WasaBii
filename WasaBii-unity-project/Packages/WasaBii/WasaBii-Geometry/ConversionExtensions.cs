#if UNITY_2022_1_OR_NEWER

namespace BII.WasaBii.Geometry {
    public static class ConversionExtensions {
        
        public static UnityEngine.Vector3 ToUnityVector(this System.Numerics.Vector3 source) =>
            new(source.X, source.Y, source.Z);
        
        public static System.Numerics.Vector3 ToSystemVector(this UnityEngine.Vector3 source) =>
            new(source.x, source.y, source.z);
        
        public static UnityEngine.Quaternion ToUnityQuaternion(this System.Numerics.Quaternion q) =>
            new(q.X, q.Y, q.Z, q.W);
        public static System.Numerics.Quaternion ToSystemQuaternion(this UnityEngine.Quaternion q) =>
            new(q.x, q.y, q.z, q.w);

    }
}

#endif