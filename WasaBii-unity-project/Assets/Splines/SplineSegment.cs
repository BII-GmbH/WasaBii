using System;
using System.Diagnostics.Contracts;
using System.Linq;
using BII.WasaBii.Core;
using BII.WasaBii.Splines.Maths;
using BII.WasaBii.UnitSystem;
using Range = BII.WasaBii.Core.Range;

namespace BII.WasaBii.Splines {
    public readonly struct SplineSegment<TPos, TDiff>
        where TPos : struct 
        where TDiff : struct {
        internal readonly Polynomial<TPos, TDiff> Polynomial;
        private readonly Lazy<Length> cachedLength;
        public Length Length => cachedLength.Value;

        internal SplineSegment(Polynomial<TPos, TDiff> polynomial, Lazy<Length>? cachedLength = null) {
            Polynomial = polynomial;
            this.cachedLength = cachedLength ?? new Lazy<Length>(() => SplineSegmentUtils.SimpsonsLengthOf(polynomial));
        }
        
        public SplineSample<TPos, TDiff> SampleAt(double percentage) => new(this, percentage);
    }

    public static class SplineSegmentUtils {
        
        /// <summary>
        /// Approximates the length of the <see cref="polynomial"/> curve by
        /// applying the trapezoidal rule with <see cref="samples"/> sections.
        /// </summary>
        [Pure]
        internal static Length TrapezoidalLengthOf<TPos, TDiff>(Polynomial<TPos, TDiff> polynomial, Range<double>? inRange = null, int samples = 10) 
            where TPos : struct 
            where TDiff : struct {
            var range = inRange ?? Range.From(0.0, inclusive: true).To(1.0, inclusive: true);
            var ops = polynomial.Ops;
            return range.Sample(samples + 1, (a, b, p) => Mathd.Lerp(a, b, p))
                .Select(polynomial.Evaluate)
                .PairwiseSliding()
                .Sum(sample => ops.Distance(sample.Item1, sample.Item2));
        }

        /// <summary>
        /// Approximates the length of the <see cref="polynomial"/> curve by applying
        /// Simpson's 1/3 rule with <see cref="sections"/> sections / double that in subsections.
        /// </summary>
        [Pure]
        internal static Length SimpsonsLengthOf<TPos, TDiff>(Polynomial<TPos, TDiff> polynomial, Range<double>? inRange = null, int sections = 4) 
        where TPos : struct 
        where TDiff : struct {
            
            // The function whose integral in the (0..1) range gives the polynomial's length.
            // This is what we want to approximate.
            double LengthDeriv(double t) {
                var v = polynomial.EvaluateDerivative(t);
                return Math.Sqrt(polynomial.Ops.Dot(v, v));
            }

            return IntegralApproximation.SimpsonsRule(LengthDeriv, inRange?.From ?? 0, inRange?.To ?? 1, sections).Meters();
        }

        /// <summary>
        /// Approximates the polynomial curve length between 0 and <see cref="t"/>.
        /// </summary>
        [Pure]
        internal static Length ProgressToLength<TPos, TDiff>(
            this Polynomial<TPos, TDiff> polynomial, double t,
            int approximationSampleSectionCount = 4
        ) where TPos : struct where TDiff : struct 
            => SimpsonsLengthOf(polynomial, inRange: Range.From(0.0, inclusive: true).To(t, inclusive: true), approximationSampleSectionCount);

        /// <summary>
        /// Iteratively approximates the progress parameter t where the length of the
        /// polynomial curve in segment (0..t) is equal to <see cref="length"/> by
        /// guessing and repeatedly correcting <see cref="iterations"/> times. Stops
        /// early when the current value is "good enough", meaning that the segment
        /// length at this point is at most <see cref="threshold"/> times the
        /// queried length and vice versa.
        /// </summary>
        /// <param name="polynomial">The polynomial in question</param>
        /// <param name="length">The length of the curve segment whose end we seek</param>
        /// <param name="iterations">How many times the guess should be corrected towards the true value</param>
        /// <param name="oversteppingFactor">When correcting towards the true value, we can expect the polynomial to
        /// stay locally similar, which means that we will probably stay on the wrong side of the true value. To avoid
        /// this, we step <see cref="oversteppingFactor"/> times further. Must be more than one and less than two
        /// to prevent never finding the value.</param>
        /// <param name="thresholdFactor">Triggers an early return when the current curve segment is within a factor
        /// of <see cref="thresholdFactor"/> from the queried <see cref="length"/></param>
        /// <param name="cachedPolynomialLength">The total polynomial arc length if already known</param>
        /// <param name="approximationSampleSectionCount">How many sections to sample when calculating the <see cref="SimpsonsLengthOf{TPos,TDiff}"/></param>
        [Pure]
        internal static double LengthToProgress<TPos, TDiff>(
            this Polynomial<TPos, TDiff> polynomial,
            Length length,
            int iterations = 4,
            double oversteppingFactor = 1.2,
            double thresholdFactor = 1.01,
            Length? cachedPolynomialLength = null,
            int approximationSampleSectionCount = 4
        ) where TPos : struct where TDiff : struct {
            if (!oversteppingFactor.IsInsideInterval(1, 2, threshold: 0.01))
                throw new ArgumentException(
                    $"{nameof(oversteppingFactor)} must be between 1 and 2, was {oversteppingFactor}"
                );
            if (thresholdFactor < 1)
                throw new ArgumentException($"{nameof(thresholdFactor)} must be at least 1, was {thresholdFactor}");
            
            var fromStart = Range.From(0.0, inclusive: true);
            var upperThreshold = thresholdFactor;
            var lowerThreshold = 1 / thresholdFactor;
            
            var totalLength = cachedPolynomialLength ?? SimpsonsLengthOf(polynomial, inRange: fromStart.To(1, inclusive: true), sections: approximationSampleSectionCount);
            
            // "Guessing" the starting point. This is the accurate result iff
            // the derivative's magnitude is constant across the whole polynomial.
            var t = length / totalLength;
            for (var i = 0; i < iterations; i++) {
                var actualLength = SimpsonsLengthOf(polynomial, inRange: fromStart.To(t, inclusive: true), sections: approximationSampleSectionCount);
                var error = actualLength / length;
                if (error <= upperThreshold && error >= lowerThreshold) break;
                var lengthDiff = length - actualLength;
                
                // Depending on whether the diff was positive or negative,
                // the actual value for t must be higher or lower, so we
                // step in that direction.
                t += lengthDiff / totalLength * oversteppingFactor;
            }

            return t;
        }

        [Pure]
        public static NormalizedSplineLocation ClosestPointInSegmentTo<TPos, TDiff>(
            this SplineSample<TPos, TDiff> sample, TPos queriedPosition, int samples
        ) 
        where TPos : struct 
        where TDiff : struct => NormalizedSplineLocation.From(sample.T + sample.Segment.Polynomial.EvaluateClosestPointTo(queriedPosition, samples));
    }
}