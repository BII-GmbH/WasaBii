using System.Linq;
using BII.WasaBii.Core;
using BII.WasaBii.Splines;
using BII.WasaBii.UnitSystem;
using UnityEngine;

namespace BII.WasaBii.Unity {
    
    /// <summary>
    /// Contains methods to debug unity splines as gizmos. Remember to only
    /// call these from `OnDrawGizmos()` / `OnDrawGizmosSelected()`, as you
    /// would with other <see cref="Gizmos"/>.
    /// Since gizmos cannot draw continuous curves, the splines are approximated
    /// as strips of lines. Each method provides a different way to construct
    /// these lines, depending on your desired resolution.
    /// </summary>
    public static class SplineGizmo {

        /// <summary>
        /// Draws the spline with <see cref="samplesPerSegment"/> points per segment. This means that you will see
        /// <see cref="samplesPerSegment"/> * spline.SegmentCount - 1 individual lines.
        /// </summary>
        public static void DrawSegments(Spline<Vector3, Vector3> spline, int samplesPerSegment = 10) {
            foreach (var (a, b) in spline.SampleSplinePerSegment(samplesPerSegment).Select(s => s.Position).PairwiseSliding()) 
                Gizmos.DrawLine(a, b);
        }
        
        /// <summary>
        /// Draws the spline with <see cref="samplesTotal"/> points. This means that you will see
        /// <see cref="samplesTotal"/> - 1 individual lines.
        /// </summary>
        public static void Draw(Spline<Vector3, Vector3> spline, int samplesTotal = 10) {
            foreach (var (a, b) in spline.SampleSpline(samplesTotal).Select(s => s.Position).PairwiseSliding()) 
                Gizmos.DrawLine(a, b);
        }
        
        /// <summary>
        /// Draws the spline such that each line will have a length of approximately <see cref="desiredSampleLength"/>.
        /// This means that you will see spline.Length / <see cref="desiredSampleLength"/> individual lines.
        /// </summary>
        public static void Draw(Spline<Vector3, Vector3> spline, Length desiredSampleLength) {
            foreach (var (a, b) in spline.SampleSplineEvery(desiredSampleLength).Select(s => s.Position).PairwiseSliding()) 
                Gizmos.DrawLine(a, b);
        }
        
    }
    
}