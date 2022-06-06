
#nullable enable

using System;
using System.Runtime.CompilerServices;

[assembly:InternalsVisibleTo("BII_Splines_Tests")]

namespace BII.WasaBii.Splines.Logic {
   
    /// Internal data structure all supported spline data structures are converted to.
    /// It is used for the calculations and describes the area between two spline handles (p1 and p2), 
    /// with the supporting handles p0 and p3
    internal readonly struct CatmullRomSegment<TPos, TDiff> 
        where TPos : struct 
        where TDiff : struct {

        public readonly TPos P0, P1, P2, P3;
        public TPos Start => P1;
        public TPos End => P2;
        
        internal readonly GeometricOperations<TPos, TDiff> Ops;

        public CatmullRomSegment(TPos p0, TPos p1, TPos p2, TPos p3, GeometricOperations<TPos, TDiff> ops) {
            P0 = p0;
            P1 = p1;
            P2 = p2;
            P3 = p3;
            Ops = ops;
        }

    }

    internal static class CatmullRomSegment {
        public const double EndOfSplineOvershootTolerance = 0.01;
        
        /// Given a SplineNode and a normalized location relative to that node,
        /// this method returns the four nodes around that location that are needed
        /// to calculate the position / tangent / etc. of that location on the spline.
        /// 
        /// The given location will be between the nodes P1 and P2 of the returned segment.
        /// The exact position between P1 and P2 is determined by the returned NormalizedOvershoot:
        /// 0.0f is at P1, 1.0f is at P2 and the values in between are lerped. 
        public static (CatmullRomSegment<TPos, TDiff> Segment, double NormalizedOvershoot)? 
            CatmullRomSegmentAt<TPos, TDiff>(Spline<TPos, TDiff> spline, NormalizedSplineLocation location) 
        where TPos : struct 
        where TDiff : struct {
            if(spline == null)
                throw new ArgumentNullException(nameof(spline));
            
            if(!spline.IsValid())
                throw new ArgumentException("The given spline was not valid", nameof(spline));
           
            if(double.IsNaN(location.Value))
                throw new ArgumentException("The spline location is NaN", nameof(location));
                
            if (location < 0 || location > spline.HandleCount - 1 + EndOfSplineOvershootTolerance)
                return null;
            
            var (s0, overshoot) = location >= spline.HandleCount - 1
                // The location was almost at, or slightly above the end of the spline
                // but within tolerance. The used segment automatically
                // becomes the last valid catmull rom segment.
                ? (SplineHandleIndex.At(spline.HandleCount - 2), 1.0f)
                // Otherwise the location is simply converted to a handle index and overshoot
                : location.AsHandleIndex();
            
            return (new CatmullRomSegment<TPos, TDiff>(
                spline[s0],
                spline[s0 + 1],
                spline[s0 + 2],
                spline[s0 + 3],
                spline.Ops
            ), overshoot);
        }
    }
}
