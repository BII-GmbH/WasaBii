using System;
using System.Numerics;

namespace BII.WasaBii.Core {

    public static class SystemVectorExtensions {

        public static Vector3 WithX(this Vector3 v, float x) =>
            new Vector3(x, v.Y, v.Z);
        
        public static Vector3 WithX(this Vector3 v, Func<float, float> mapping) =>
            new Vector3(mapping(v.X), v.Y, v.Z);

        public static Vector3 WithY(this Vector3 v, float y) => 
            new Vector3(v.X, y, v.Z);
        
        public static Vector3 WithY(this Vector3 v, Func<float, float> mapping) => 
            new Vector3(v.X, mapping(v.Y), v.Z);

        public static Vector3 WithZ(this Vector3 v, float z) => 
            new Vector3(v.X, v.Y, z);
        
        public static Vector3 WithZ(this Vector3 v, Func<float, float> mapping) => 
            new Vector3(v.X, v.Y, mapping(v.Z));

        public static Vector3 MapXYZ(this Vector3 v, Func<float, float> mapping) => 
            new Vector3(mapping(v.X), mapping(v.Y), mapping(v.Z));

        public static float DistanceTo(this Vector3 v1, Vector3 v2)
            => Vector3.Distance(v1, v2);

        public static Vector3 Normalized(this Vector3 v) => v / v.Length();
    }

}