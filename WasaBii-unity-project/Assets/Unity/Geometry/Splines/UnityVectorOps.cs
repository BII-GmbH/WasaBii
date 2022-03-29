using BII.WasaBii.CatmullRomSplines;
using BII.WasaBii.Core;
using BII.WasaBii.Units;
using UnityEngine;

namespace BII.WasaBii.Unity.Geometry.Splines {
    
    [MustBeImmutable][MustBeSerializable]
    public sealed class UnityVectorOps : PositionOperations<Vector3, Vector3> {

        public static readonly UnityVectorOps Instance = new();
        
        private UnityVectorOps() { }

        public Length Distance(Vector3 p0, Vector3 p1) => p0.DistanceTo(p1).Meters();

        public Vector3 Sub(Vector3 p0, Vector3 p1) => p0 - p1;

        public Vector3 Add(Vector3 d1, Vector3 d2) => d1 + d2;

        public Vector3 Div(Vector3 diff, double d) => diff / (float)d;

        public Vector3 Mul(Vector3 diff, double f) => diff * (float)f;

        public double Dot(Vector3 a, Vector3 b) => Vector3.Dot(a, b);

    }
    
}