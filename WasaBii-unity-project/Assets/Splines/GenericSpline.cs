using System;
using System.Collections.Generic;
using System.Linq;
using BII.WasaBii.Core;
using BII.WasaBii.Splines.Maths;
using BII.WasaBii.UnitSystem;

namespace BII.WasaBii.Splines {
    
    /// Non-generic interface for use in utilities. Always implement the explicit version below.
    [MustBeSerializable][MustBeImmutable]
    public interface Spline {
        
        int SegmentCount { get; }
        Length Length { get; }
        
    }

    /// The base interface for splines.
    [MustBeSerializable][MustBeImmutable]
    public interface Spline<TPos, TDiff> : Spline, IEquatable<Spline<TPos, TDiff>>, WithSpline<TPos, TDiff>
        where TPos : struct 
        where TDiff : struct {
        
        IEnumerable<SplineSegment<TPos, TDiff>> Segments { get; }

        SplineSegment<TPos, TDiff> this[SplineSegmentIndex index] { get; }
        SplineSample<TPos, TDiff> this[SplineLocation location] => this[this.Normalize(location)];
        SplineSample<TPos, TDiff> this[NormalizedSplineLocation location] { get; }
        
        GeometricOperations<TPos, TDiff> Ops { get; }

        Length Spline.Length => Segments.Sum(s => s.Length);
    }

    public interface WithSpline<TPos, TDiff>
        where TPos : struct 
        where TDiff : struct {
        Spline<TPos, TDiff> Spline { get; }
    }

}