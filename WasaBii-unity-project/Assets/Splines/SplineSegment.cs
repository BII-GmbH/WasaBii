using System;
using System.Diagnostics.Contracts;
using BII.WasaBii.Core;
using BII.WasaBii.Splines.Maths;
using BII.WasaBii.UnitSystem;

namespace BII.WasaBii.Splines {
    public readonly struct SplineSegment<TPos, TDiff>
        where TPos : struct 
        where TDiff : struct {
        internal readonly CubicPolynomial<TPos, TDiff> Polynomial;
        private readonly Lazy<Length> cachedLength;
        public Length Length => cachedLength.Value;

        public static Option<SplineSegment<TPos, TDiff>> From(Spline<TPos, TDiff> spline, SplineSegmentIndex idx, Length? cachedSegmentLength = null) =>
            SplineSegmentUtils.CubicPolynomialFor(spline, idx)
                .Map(val => new SplineSegment<TPos, TDiff>(val, cachedSegmentLength));

        internal SplineSegment(CubicPolynomial<TPos, TDiff> polynomial, Length? cachedLength = null) {
            Polynomial = polynomial;
            this.cachedLength = new Lazy<Length>(() => cachedLength ?? SplineSegmentUtils.LengthOfSegment(polynomial));
        }
        
        public SplineSample<TPos, TDiff> SampleAt(double percentage) => new(this, percentage);
    }

    public static class SplineSegmentUtils {
        public const int DefaultLengthSamples = 10;
        
        [Pure]
        internal static Option<CubicPolynomial<TPos, TDiff>> CubicPolynomialFor<TPos, TDiff>(Spline<TPos, TDiff> spline, SplineSegmentIndex idx) 
        where TPos : struct 
        where TDiff : struct {
            var catmullRomSegment = CatmullRomSegment.CatmullRomSegmentAt(spline, NormalizedSplineLocation.From(idx));

            return catmullRomSegment is { } val 
                ? CubicPolynomial.FromCatmullRomSegment(val.Segment, spline.Type.ToAlpha()) 
                : Option.None;
        }
        
        [Pure]
        internal static Length LengthOfSegment<TPos, TDiff>(CubicPolynomial<TPos, TDiff> polynomial, int samples = DefaultLengthSamples) 
            where TPos : struct 
            where TDiff : struct {
            var length = Length.Zero;
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
        where TDiff : struct => NormalizedSplineLocation.From(sample.T + sample.Segment.Polynomial.EvaluateClosestPointTo(queriedPosition, samples));
    }
}