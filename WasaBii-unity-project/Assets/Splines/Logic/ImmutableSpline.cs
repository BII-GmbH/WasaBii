using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.Serialization;
using BII.WasaBii.Core;
using BII.WasaBii.Units;
using Newtonsoft.Json;

namespace BII.WasaBii.Splines.Logic {
    
    [JsonObject(IsReference = false)] // Treat as value type for serialization
    [MustBeSerializable] 
    public sealed class ImmutableSpline<TPos, TDiff> : Spline<TPos, TDiff> where TPos : struct where TDiff : struct {
        public ImmutableSpline(
            TPos startHandle, IEnumerable<TPos> handles, TPos endHandle, 
            GeometricOperations<TPos, TDiff> ops,
            SplineType? splineType = null
        ) : this(handles.Prepend(startHandle).Append(endHandle), ops, splineType){}

        public ImmutableSpline(IEnumerable<TPos> allHandlesIncludingMarginHandles, GeometricOperations<TPos, TDiff> ops, SplineType? splineType = null) {
            handles = ImmutableArray.CreateRange(allHandlesIncludingMarginHandles);
            Type = splineType ?? SplineType.Centripetal;
            _cachedSegmentLengths = new Length[this.SegmentCount()];
            this.ops = ops;
        }
        
        // The non-nullable fields are not set and thus null, but
        // they should always be set via reflection, so this is fine.
    #pragma warning disable 8618
        [JsonConstructor] private ImmutableSpline(){}
    #pragma warning restore 8618

        private readonly ImmutableArray<TPos> handles;

        public IReadOnlyList<TPos> HandlesIncludingMargin => handles;

        public SplineType Type { get; }
        
        public Spline<TPos, TDiff> Spline => this;

        private readonly GeometricOperations<TPos, TDiff> ops;
        GeometricOperations<TPos, TDiff> Spline<TPos, TDiff>.Ops => ops;

        public TPos this[SplineHandleIndex index] => handles[index];

        public SplineSegment<TPos, TDiff> this[SplineSegmentIndex index] => SplineSegment<TPos, TDiff>.From(this, index, cachedSegmentLengthOf(index)).GetOrThrow(() =>
            new ArgumentOutOfRangeException(nameof(index), index, $"Must be between 0 and {this.SegmentCount()}"));
        
        public SplineSample<TPos, TDiff> this[NormalizedSplineLocation location] => SplineSample<TPos, TDiff>.From(this, location) ??
            throw new ArgumentOutOfRangeException(
                nameof(location),
                location,
                $"Must be between 0 and {((Spline<TPos, TDiff>)this).HandleCount - 1}"
            );

        public bool Equals(Spline<TPos, TDiff> other) => 
            !ReferenceEquals(null, other) 
            && this.HandlesIncludingMargin.SequenceEqual(other.HandlesIncludingMargin)
            && Type == other.Type;

        public override bool Equals(object obj) => obj is Spline<TPos, TDiff> otherSpline && Equals(otherSpline);
        
        public override int GetHashCode() => HashCode.Combine(handles, (int)Type);
        
#region Segment Length Caching
        // The cached lengths for each segment,
        // accessed by the segment index.
        //
        // 0 (e.g. default) values are treated as "no entry"
        // and will force a cache calculation,
        // as valid segments don't have the length 0.
        //
        // Even though this array is not immutable,
        // this class is registered as an edge-case
        // since the array is only used for lazy caches.
        [NonSerialized] 
        private Length[] _cachedSegmentLengths;

        private Length cachedSegmentLengthOf(SplineSegmentIndex idx) {
            if(idx < 0 || idx >= _cachedSegmentLengths.Length) throw new ArgumentException(
                $"Tried to access segment at index {idx}, but the spline" +
                $" only has {_cachedSegmentLengths.Length} segments"
            );
                
            var cachedLength = _cachedSegmentLengths[idx.Value];
            if (cachedLength > Length.Zero) return cachedLength;
            // intentional assigment
            return _cachedSegmentLengths[idx.Value] = SplineSegmentUtils.LengthOfSegment(
                SplineSegmentUtils.CubicPolynomialFor(this, idx).GetOrThrow(() => 
                    new Exception(
                        "Could not create a cubic polynomial for this spline. " +
                        "This should not happen and indicates a bug in this method."
                    )));
        }
        
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context) => _cachedSegmentLengths = new Length[this.SegmentCount()];

        #endregion

    }
}