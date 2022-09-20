using System;
using System.Collections.Generic;
using BII.WasaBii.Core;
using BII.WasaBii.Splines.Maths;
using BII.WasaBii.UnitSystem;

namespace BII.WasaBii.Splines {
    
    /// Non-generic interface for use in utilities. Always implement the explicit version below.
    [MustBeSerializable]
    public interface Spline {
        
        int SegmentCount { get; }
        Length Length { get; }
        
    }

    /// The base interface for splines.
    [MustBeSerializable]
    public interface Spline<TPos, TDiff> : Spline, IEquatable<Spline<TPos, TDiff>>, WithSpline<TPos, TDiff>
        where TPos : struct 
        where TDiff : struct {
        
        IEnumerable<SplineSegment<TPos, TDiff>> Segments { get; }

        SplineSegment<TPos, TDiff> this[SplineSegmentIndex index] { get; }
        SplineSample<TPos, TDiff> this[SplineLocation location] => this[this.Normalize(location)];
        SplineSample<TPos, TDiff> this[NormalizedSplineLocation location] { get; }
        
        GeometricOperations<TPos, TDiff> Ops { get; }

        Spline<TPosNew, TDiffNew> Map<TPosNew, TDiffNew>(Func<TPos, TPosNew> positionMapping, GeometricOperations<TPosNew, TDiffNew> newOps) 
            where TPosNew : struct where TDiffNew : struct;

        Length Spline.Length => Segments.Sum(s => s.Length);

        Spline<TPos, TDiff> WithSpline<TPos, TDiff>.Spline => this;
    }

    public interface WithSpline<TPos, TDiff>
        where TPos : struct 
        where TDiff : struct {
        Spline<TPos, TDiff> Spline { get; }
    }

}