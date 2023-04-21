using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using BII.WasaBii.Core;
using BII.WasaBii.Splines.Maths;
using BII.WasaBii.UnitSystem;

namespace BII.WasaBii.Splines {
    
    [MustBeImmutable]
    public interface Spline<TPos, TDiff>
        where TPos : unmanaged 
        where TDiff : unmanaged {
        
        IEnumerable<SplineSegment<TPos, TDiff>> Segments { get; }
        int SegmentCount { get; }

        SplineSegment<TPos, TDiff> this[SplineSegmentIndex index] { get; }
        SplineSample<TPos, TDiff> this[SplineLocation location] => this[this.Normalize(location).ResultOrThrow(error => error.AsException)];
        SplineSample<TPos, TDiff> this[NormalizedSplineLocation location] { get; }

        Length DistanceFromBegin(SplineSegmentIndex index);
        
        GeometricOperations<TPos, TDiff> Ops { get; }

        [Pure] Spline<TPosNew, TDiffNew> Map<TPosNew, TDiffNew>(Func<TPos, TPosNew> positionMapping, GeometricOperations<TPosNew, TDiffNew> newOps) 
            where TPosNew : unmanaged where TDiffNew : unmanaged; 
            
        public interface Copyable : Spline<TPos, TDiff> {

            [Pure] public Spline<TPos, TDiff> Reversed { get; }

            /// Creates a new spline with a similar trajectory, but with all handle positions
            /// being moved by a certain offset which depends on the spline's tangent at these points.
            [Pure] public Spline<TPos, TDiff> CopyWithOffset(Func<TDiff, TDiff> tangentToOffset);
            
            /// Creates a new spline with the same trajectory, but with
            /// all handle positions being moved along a certain
            /// <paramref name="offset"/>, independent of the spline's
            /// tangent at these points.
            [Pure] public Spline<TPos, TDiff> CopyWithStaticOffset(TDiff offset);
        
            /// Creates a new spline with a similar trajectory,
            /// but different spacing between the handles.
            [Pure] public Spline<TPos, TDiff> CopyWithDifferentHandleDistance(Length desiredHandleDistance);
        }
    }

    public static class GenericSplineExtensions {
        
        public static Length Length<TPos, TDiff>(this Spline<TPos, TDiff> spline) 
        where TPos : unmanaged where TDiff : unmanaged => spline.Segments.Sum(s => s.Length);

    }

}