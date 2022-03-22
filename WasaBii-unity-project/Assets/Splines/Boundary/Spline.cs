using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using BII.CatmullRomSplines.Logic;
using BII.WasaBii.Core;
using BII.WasaBii.Units;

namespace BII.CatmullRomSplines {
    /// Class that contains static factory methods for building splines. 
    public static class Splines {
        
        /// Creates a spline builder for a spline that interpolates the given handles.
        /// The begin and end margin handles will be generated automatically
        /// using <see cref="EnumerableToSplineExtensions.CalculateSplineMarginHandles"/>
        [Pure]
        public static Spline<TPos, TDiff> FromInterpolating<TPos, TDiff>(
            IEnumerable<TPos> handles, PositionOperations<TPos, TDiff> ops, SplineType? type = null
        ) where TPos : struct where TDiff : struct {
            var interpolatedHandles = handles.AsReadOnlyList();
            var (beginMarginHandle, endMarginHandle) = interpolatedHandles.CalculateSplineMarginHandles(ops);
            return FromHandles(beginMarginHandle, interpolatedHandles, endMarginHandle, ops, type);
        }

        [Pure]
        public static Spline<TPos, TDiff> FromHandles<TPos, TDiff>(
            TPos beginMarginHandle, 
            IEnumerable<TPos> interpolatedHandles, 
            TPos endMarginHandle, 
            PositionOperations<TPos, TDiff> ops, 
            SplineType? type = null
        ) where TPos : struct where TDiff : struct => 
            FromHandlesIncludingMargin(
                interpolatedHandles.Prepend(beginMarginHandle).Append(endMarginHandle), 
                ops,
                type
            );

        /// Creates a spline builder for a spline with the given handles.
        /// These include the begin and end margin handles.
        [Pure]
        public static Spline<TPos, TDiff> FromHandlesIncludingMargin<TPos, TDiff>(
            IEnumerable<TPos> allHandlesIncludingMargin, 
            PositionOperations<TPos, TDiff> ops, 
            SplineType? type = null
        ) where TPos : struct where TDiff : struct 
            => new ImmutableSpline<TPos, TDiff>(allHandlesIncludingMargin, ops, type);
    }

    public interface UntypedSpline {
        
        int TotalHandleCount { get; }
        
        /// Returns the type of the spline.
        /// It determines how closely the handles are interpolated. 
        SplineType Type { get; }

    }
    
    /// Interface for implementations of catmull-rom splines
    [MustBeSerializable][MustBeImmutable]
    public interface Spline<TPos, TDiff> : UntypedSpline, IEquatable<Spline<TPos, TDiff>>, WithSpline<TPos, TDiff>
    where TPos : struct 
    where TDiff : struct {
        
        /// Retrieves the handle at the given index.
        TPos this[SplineHandleIndex index] { get; }

        SplineSegment<TPos, TDiff> this[SplineSegmentIndex index] { get; }
        SplineSample<TPos, TDiff> this[SplineLocation location] { get; }
        SplineSample<TPos, TDiff> this[NormalizedSplineLocation location] { get; }
        
        internal PositionOperations<TPos, TDiff> Ops { get; }
    }

    public interface WithUntypedSpline {
        UntypedSpline Spline { get; }
    }

    public interface WithSpline<TPos, TDiff> : WithUntypedSpline
        where TPos : struct 
        where TDiff : struct {
        Spline<TPos, TDiff> Spline { get; }
        UntypedSpline WithUntypedSpline.Spline => Spline;
    }

    public static class SplineLikeUtils {
        public static bool IsValid(this UntypedSpline spline) => spline.TotalHandleCount >= 4;

        private static T whenValidOrThrow<T, TPos, TDiff>(this Spline<TPos, TDiff> spline, Func<Spline<TPos, TDiff>, T> resultGetter) 
            where TPos : struct where TDiff : struct =>
            spline.IsValid() ? resultGetter(spline) : throw new InvalidSplineException(spline, "Not enough handles");

        public static IEnumerable<TPos> Handles<TPos, TDiff>(this Spline<TPos, TDiff> spline) where TPos : struct where TDiff : struct =>
            Enumerable.Range(1, spline.TotalHandleCount - 2).Select(idx => spline[SplineHandleIndex.At(idx)]);

        public static IEnumerable<TPos> HandlesIncludingMargin<TPos, TDiff>(this Spline<TPos, TDiff> spline) where TPos : struct where TDiff : struct =>
            Enumerable.Range(0, spline.TotalHandleCount).Select(idx => spline[SplineHandleIndex.At(idx)]);

        public static int HandleCount(this UntypedSpline spline) =>
            spline.IsValid() ? spline.TotalHandleCount - 2 : 0;

        public static int HandleCountIncludingMargin(this UntypedSpline spline) =>
            spline.TotalHandleCount;

        public static int SegmentCount(this UntypedSpline spline) => spline.IsValid() ? spline.TotalHandleCount - 3 : 0;

        public static bool TryQuery<TPos, TDiff>(
            this Spline<TPos, TDiff> spline, NormalizedSplineLocation location, out SplineSample<TPos, TDiff> sample
        ) where TPos : struct where TDiff : struct {
            var res = SplineSample<TPos, TDiff>.From(spline, location);
            sample = res ?? new SplineSample<TPos, TDiff>();
            return res != null;
        }

        public static bool TryQuery<TPos, TDiff>(
            this Spline<TPos, TDiff> spline, SplineLocation location, out SplineSample<TPos, TDiff> sample
        ) where TPos : struct where TDiff : struct {
            var res = SplineSample<TPos, TDiff>.From(spline, location);
            sample = res ?? new SplineSample<TPos, TDiff>();
            return res != null;
        }
        
        /// Returns all the positions of spline handles that are between the given locations on the spline.
        /// The positions of the locations on the spline themselves are included at begin and end.
        /// This is a more performant way to sample a spline used for spline driving than the methods in
        /// <see cref="SplineSampleExtensions"/>.
        /// This is because sampling has to normalize the locations for every sample made,
        /// while this operation is only done twice here.
        [Pure]
        public static IEnumerable<TPos> HandlesBetween<TPos, TDiff>(
            this Spline<TPos, TDiff> spline, SplineLocation start, SplineLocation end
        ) where TPos : struct where TDiff : struct {
            var fromNormalized = spline.NormalizedLocation(start);
            var toNormalized = spline.NormalizedLocation(end);

            var allNodes = spline.Handles().ToList();

            yield return spline.PositionAtNormalizedOrThrow(fromNormalized);

            if (fromNormalized > toNormalized) {
                // Iterate from end of spline to begin
                for (var nodeIndex = allNodes.Count - 1; nodeIndex >= 0; --nodeIndex)
                    if (nodeIndex < fromNormalized && nodeIndex > toNormalized)
                        yield return allNodes[nodeIndex];
            } else {
                // Iterate from begin of spline to end
                for (var nodeIndex = 0; nodeIndex < allNodes.Count; ++nodeIndex)
                    if (nodeIndex > fromNormalized && nodeIndex < toNormalized)
                        yield return allNodes[nodeIndex];
            }

            yield return spline.PositionAtNormalizedOrThrow(toNormalized);
        }

        public static TPos BeginMarginHandle<TPos, TDiff>(this Spline<TPos, TDiff> spline)
            where TPos : struct where TDiff : struct =>
            spline.whenValidOrThrow(s => s[SplineHandleIndex.At(0)]);

        public static TPos FirstHandle<TPos, TDiff>(this Spline<TPos, TDiff> spline)
            where TPos : struct where TDiff : struct =>
            spline.whenValidOrThrow(s => s[SplineHandleIndex.At(1)]);

        public static TPos LastHandle<TPos, TDiff>(this Spline<TPos, TDiff> spline)
            where TPos : struct where TDiff : struct =>
            spline.whenValidOrThrow(s => s[SplineHandleIndex.At(s.TotalHandleCount - 2)]);

        public static TPos EndMarginHandle<TPos, TDiff>(this Spline<TPos, TDiff> spline)
            where TPos : struct where TDiff : struct =>
            spline.whenValidOrThrow(s => s[SplineHandleIndex.At(s.TotalHandleCount - 1)]);

        public static Length Length<TPos, TDiff>(this Spline<TPos, TDiff> spline, int samplesPerSegment = 10)
            where TPos : struct where TDiff : struct => spline.whenValidOrThrow(
            _ => Enumerable
                .Range(0, spline.SegmentCount())
                .Sum(idx => spline[SplineSegmentIndex.At(idx)].Length(samplesPerSegment).AsMeters())
                .Meters()
        );
    }
}