using UnityEngine;

namespace BII.WasaBii.Unity.Geometry {
    
    public static class MatrixExtensions {
        
        public static Matrix4x4 ToUnityMatrix(this System.Numerics.Matrix4x4 m) => new Matrix4x4(
            column0: new Vector4(m.M11, m.M12, m.M13, m.M14),
            column1: new Vector4(m.M21, m.M22, m.M23, m.M24),
            column2: new Vector4(m.M31, m.M32, m.M33, m.M34),
            column3: new Vector4(m.M41, m.M42, m.M43, m.M44)
        );
        
        public static bool IsNearly(this Matrix4x4 a, Matrix4x4 b, float equalityThreshold = 1E-30f) => 
            a.GetColumn(0).IsNearly(b.GetColumn(0), equalityThreshold) &&
            a.GetColumn(1).IsNearly(b.GetColumn(1), equalityThreshold) &&
            a.GetColumn(2).IsNearly(b.GetColumn(2), equalityThreshold) &&
            a.GetColumn(3).IsNearly(b.GetColumn(3), equalityThreshold);
    }
}