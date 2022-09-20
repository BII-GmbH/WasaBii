using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using BII.WasaBii.Core;
using BII.WasaBii.Splines.Maths;
using Newtonsoft.Json;

namespace BII.WasaBii.Splines.Bezier {
    
    /// <summary>
    /// A spline that is defined by a number of points and one (quadratic curve) or two (cubic curve) control points
    /// in between each pair of points. The spline visits the former points in order. Its trajectory between two
    /// points is influenced by the control point(s) between them, as it goes "in the direction" of these, usually
    /// without actually touching them.
    ///
    /// The derivative (tangent / velocity) has a continuous direction iff every point is collinear with
    /// the two control points before and after it. Its magnitude is continuous as well iff these two
    /// control points are also at the same distance from their shared point.
    /// </summary>
    [JsonObject(IsReference = false)] // Treat as value type for serialization
    [MustBeSerializable]
    public sealed class BezierSpline<TPos, TDiff> : Spline<TPos, TDiff>, IEquatable<BezierSpline<TPos, TDiff>> where TPos : struct where TDiff : struct {

        public readonly ImmutableArray<BezierSegment<TPos, TDiff>> Segments;
        public int SegmentCount => Segments.Length;

        IEnumerable<SplineSegment<TPos, TDiff>> Spline<TPos, TDiff>.Segments =>
            Segments.Select(s => s.ToSplineSegment());

        public SplineSegment<TPos, TDiff> this[SplineSegmentIndex index] 
            => Segments[index].ToSplineSegment();
        public SplineSample<TPos, TDiff> this[SplineLocation location] => this[this.Normalize(location)];
        public SplineSample<TPos, TDiff> this[NormalizedSplineLocation location] => SplineSample<TPos, TDiff>.From(this, location) ??
            throw new ArgumentOutOfRangeException(
                nameof(location),
                location,
                $"Must be between 0 and {SegmentCount}"
            );

        public GeometricOperations<TPos, TDiff> Ops { get; }
        
        public BezierSpline(IEnumerable<BezierSegment<TPos, TDiff>> segments, GeometricOperations<TPos, TDiff> ops) {
            Segments = segments.ToImmutableArray();
            Ops = ops;
        }

        public Spline<TPosNew, TDiffNew> Map<TPosNew, TDiffNew>(
            Func<TPos, TPosNew> positionMapping, GeometricOperations<TPosNew, TDiffNew> newOps
        ) where TPosNew : struct where TDiffNew : struct
            => new BezierSpline<TPosNew, TDiffNew>(Segments.Select(s => s.Map(positionMapping, newOps)), newOps);

        public bool Equals(BezierSpline<TPos, TDiff> other) {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Segments.SequenceEqual(other.Segments) && Equals(Ops, other.Ops);
        }

        public override bool Equals(object obj) => ReferenceEquals(this, obj) || obj is BezierSpline<TPos, TDiff> other && Equals(other);
        public bool Equals(Spline<TPos, TDiff> spline) => spline is BezierSpline<TPos, TDiff> other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Segments, Ops);

        // The non-nullable fields are not set and thus null, but
        // they should always be set via reflection, so this is fine.
        #pragma warning disable 8618
        [JsonConstructor] private BezierSpline(){}
        #pragma warning restore 8618

    }

}