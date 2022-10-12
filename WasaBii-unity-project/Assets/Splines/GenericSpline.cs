using System;
using System.Collections.Generic;
using BII.WasaBii.Core;
using BII.WasaBii.Splines.Maths;
using BII.WasaBii.UnitSystem;

namespace BII.WasaBii.Splines {
    
    /// The base interface for splines.
    [MustBeSerializable]
    public interface Spline<TPos, TDiff>
        where TPos : struct 
        where TDiff : struct {
        
        IEnumerable<SplineSegment<TPos, TDiff>> Segments { get; }
        int SegmentCount { get; }

        SplineSegment<TPos, TDiff> this[SplineSegmentIndex index] { get; }
        SplineSample<TPos, TDiff> this[SplineLocation location] => this[this.Normalize(location)];
        SplineSample<TPos, TDiff> this[NormalizedSplineLocation location] { get; }
        
        GeometricOperations<TPos, TDiff> Ops { get; }

        Spline<TPosNew, TDiffNew> Map<TPosNew, TDiffNew>(Func<TPos, TPosNew> positionMapping, GeometricOperations<TPosNew, TDiffNew> newOps) 
            where TPosNew : struct where TDiffNew : struct; 
    }

    public static class GenericSplineExtensions {
        
        public static Length Length<TPos, TDiff>(this Spline<TPos, TDiff> spline) 
        where TPos : struct where TDiff : struct => spline.Segments.Sum(s => s.Length);

    }

}