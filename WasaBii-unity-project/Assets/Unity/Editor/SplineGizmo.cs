using System.Linq;
using BII.WasaBii.Core;
using BII.WasaBii.Splines;
using BII.WasaBii.UnitSystem;
using UnityEngine;

namespace Unity.Editor {
    
    public static class SplineGizmo {

        public static void DrawSegments(Spline<Vector3, Vector3> spline, int samplesPerSegment = 10) {
            foreach (var (a, b) in spline.SampleSplinePerSegment(samplesPerSegment).Select(s => s.Position).PairwiseSliding()) 
                Gizmos.DrawLine(a, b);
        }
        
        public static void Draw(Spline<Vector3, Vector3> spline, int samplesTotal = 10) {
            foreach (var (a, b) in spline.SampleSpline(samplesTotal).Select(s => s.Position).PairwiseSliding()) 
                Gizmos.DrawLine(a, b);
        }
        
        public static void Draw(Spline<Vector3, Vector3> spline, Length desiredSampleLength) {
            foreach (var (a, b) in spline.SampleSplineEvery(desiredSampleLength).Select(s => s.Position).PairwiseSliding()) 
                Gizmos.DrawLine(a, b);
        }
        
    }
    
}