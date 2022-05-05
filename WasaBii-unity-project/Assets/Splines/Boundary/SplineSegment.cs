using System.Diagnostics.Contracts;
using BII.WasaBii.Splines.Logic;
using BII.WasaBii.UnitSystem;

namespace BII.WasaBii.Splines {
    public readonly struct SplineSegment<TPos, TDiff>
        where TPos : struct 
        where TDiff : struct {
        public readonly CubicPolynomial<TPos, TDiff> Polynomial;
        public readonly Length? CachedLength;

        public static SplineSegment<TPos, TDiff>? From(Spline<TPos, TDiff> spline, SplineSegmentIndex idx, Length? cachedSegmentLength = null) {
            var cubicPolynomial = SplineSegmentUtils.CubicPolynomialFor(spline, idx);

            return cubicPolynomial is { } val
                ? new SplineSegment<TPos, TDiff>(
                    val,
                    cachedSegmentLength
                )
                : null;
        }

        public SplineSegment(CubicPolynomial<TPos, TDiff> polynomial, Length? cachedLength) {
            Polynomial = polynomial;
            CachedLength = cachedLength;
        }
        
        public SplineSample<TPos, TDiff> SampleAt(float percentage) => new(this, percentage);
    }

    public static class SplineSegmentUtils {
        public const int DefaultLengthSamples = 10;
        
        [Pure]
        public static Length Length<TPos, TDiff>(
            this SplineSegment<TPos, TDiff> segment,
            int samples = DefaultLengthSamples
        ) 
        where TPos : struct 
        where TDiff : struct {
            if (segment.CachedLength is { } cachedResult) return cachedResult;
            else return LengthOfSegment(segment.Polynomial, samples);
        }

        [Pure]
        public static CubicPolynomial<TPos, TDiff>? CubicPolynomialFor<TPos, TDiff>(Spline<TPos, TDiff> spline, SplineSegmentIndex idx) 
        where TPos : struct 
        where TDiff : struct {
            var catmullRomSegment = CatmullRomSegment.CatmullRomSegmentAt(spline, NormalizedSplineLocation.From(idx));

            if (catmullRomSegment.HasValue)
                return CubicPolynomial.FromCatmullRomSegment(catmullRomSegment.Value.Segment, spline.Type.ToAlpha());
            else return null;
        }
        
        [Pure]
        public static Length LengthOfSegment<TPos, TDiff>(CubicPolynomial<TPos, TDiff> polynomial, int samples = DefaultLengthSamples) 
        where TPos : struct 
        where TDiff : struct {
            var length = UnitSystem.Length.Zero;
            var current = polynomial.Evaluate(t: 0);
            var increment = 1f / samples;

            var ops = polynomial.Ops;

            for (var f = increment; f < 1f; f += increment) {
                var next = polynomial.Evaluate(f);
                length += ops.Distance(current, next);
                current = next;
            }

            length += ops.Distance(current, polynomial.Evaluate(t: 1));

            return length;
        }
        
        [Pure]
        public static NormalizedSplineLocation ClosestPointInSegmentTo<TPos, TDiff>(
            this SplineSample<TPos, TDiff> sample, TPos queriedPosition, int samples
        ) 
        where TPos : struct 
        where TDiff : struct => NormalizedSplineLocation.From(sample.Segment.Polynomial.EvaluateClosestPointTo(queriedPosition, samples));
    }
}