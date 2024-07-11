using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Linq;
using BII.WasaBii.Core;
using BII.WasaBii.Splines.Maths;
using BII.WasaBii.UnitSystem;

namespace BII.WasaBii.Splines {
    
    [MustBeImmutable]
    public interface Spline<TPos, TDiff, TTime, TVel>
        where TPos : unmanaged 
        where TDiff : unmanaged 
        where TTime : unmanaged, IComparable<TTime>
        where TVel : unmanaged
    {
        
        IEnumerable<SplineSegment<TPos, TDiff, TTime, TVel>> Segments { get; }
        int SegmentCount { get; }

        Length Length => Segments.Sum(s => s.Length);
        TTime TotalDuration => Segments.Aggregate(Ops.ZeroTime, (t, s) => Ops.Add(t, s.Duration));

        SplineSample<TPos, TDiff, TTime, TVel> this[TTime t] =>
            SplineSample<TPos, TDiff, TTime, TVel>.From(this, t);
        SplineSegment<TPos, TDiff, TTime, TVel> this[SplineSegmentIndex index] => Segments.Skip(index).First();
        SplineSample<TPos, TDiff, TTime, TVel> this[SplineLocation location] => this[this.NormalizeOrThrow(location)];
        SplineSample<TPos, TDiff, TTime, TVel> this[NormalizedSplineLocation location] => 
            SplineSample<TPos, TDiff, TTime, TVel>.From(this, location).GetOrThrow(() => 
                new ArgumentOutOfRangeException(
                    nameof(location),
                    location,
                    $"Must be between 0 and {SegmentCount}"
                ));

        ImmutableArray<Length> SpatialSegmentOffsets { get; }
        ImmutableArray<TTime> TemporalSegmentOffsets { get; }
        
        GeometricOperations<TPos, TDiff, TTime, TVel> Ops { get; }

        [Pure] Spline<TPosNew, TDiffNew, TTime, TVelNew> Map<TPosNew, TDiffNew, TVelNew>(
            Func<TPos, TPosNew> positionMapping, 
            GeometricOperations<TPosNew, TDiffNew, TTime, TVelNew> newOps
        ) where TPosNew : unmanaged where TDiffNew : unmanaged where TVelNew : unmanaged;

        public interface Copyable : Spline<TPos, TDiff, TTime, TVel> {

            [Pure] public Spline<TPos, TDiff, TTime, TVel> Reversed { get; }

            /// Creates a new spline with a similar trajectory, but with all handle positions
            /// being moved by a certain offset which depends on the spline's velocity at these points.
            [Pure] public Spline<TPos, TDiff, TTime, TVel> CopyWithOffset(Func<TVel, TDiff> tangentToOffset);
            
            /// Creates a new spline with the same trajectory, but with
            /// all handle positions being moved along a certain
            /// <paramref name="offset"/>, independent of the spline's
            /// tangent at these points.
            [Pure] public Spline<TPos, TDiff, TTime, TVel> CopyWithStaticOffset(TDiff offset);
        
            /// Creates a new spline with a similar trajectory,
            /// but different spacing between the handles.
            [Pure] public Spline<TPos, TDiff, TTime, TVel> CopyWithDifferentHandleDistance(Length desiredHandleDistance);
        }
    }

}