using System;
using System.Diagnostics.Contracts;
using System.Linq;
using BII.WasaBii.Core;
using BII.WasaBii.Splines.Maths;
using BII.WasaBii.UnitSystem;

namespace BII.WasaBii.Splines {
    public readonly struct SplineSegment<TPos, TDiff, TTime, TVel>
        where TPos : unmanaged 
        where TDiff : unmanaged 
        where TTime : unmanaged, IComparable<TTime>
        where TVel : unmanaged
    {
        internal readonly Polynomial<TPos, TDiff, TTime, TVel> Polynomial;
        private readonly Lazy<Length> cachedLength;
        public Length Length => cachedLength.Value;

        public readonly TTime Duration;

        internal SplineSegment(Polynomial<TPos, TDiff, TTime, TVel> polynomial, Lazy<Length>? cachedLength = null) {
            Polynomial = polynomial;
            Duration = polynomial.Duration;
            this.cachedLength = cachedLength ?? new Lazy<Length>(() => SplineSegmentUtils.SimpsonsLengthOf(polynomial));
        }
        
        public SplineSample<TPos, TDiff, TTime, TVel> SampleAt(TTime globalT, TTime segmentOffset) => new(this, globalT, segmentOffset);
    }

    public static class SplineSegmentUtils {
        
        /// <summary>
        /// Approximates the length of the <see cref="polynomial"/> curve by
        /// applying the trapezoidal rule with <see cref="samples"/> sections.
        /// </summary>
        [Pure]
        internal static Length TrapezoidalLengthOf<TPos, TDiff, TTime, TVel>(
            Polynomial<TPos, TDiff, TTime, TVel> polynomial, 
            double? start = 0.0, double? end = 1.0,
            int samples = 10
        ) where TPos : unmanaged where TDiff : unmanaged where TTime : unmanaged where TVel : unmanaged {
            var range = SampleRange.From(start ?? 0.0, inclusive: true).To(end ?? 1.0, inclusive: true);
            var ops = polynomial.Ops;
            return range.Sample(samples + 1, (a, b, p) => MathD.Lerp(a, b, p))
                .Select(polynomial.EvaluateNormalized)
                .PairwiseSliding()
                .Sum(sample => ops.Distance(sample.Item1, sample.Item2));
        }

        /// <summary>
        /// Approximates the length of the <see cref="polynomial"/> curve by applying
        /// Simpson's 1/3 rule with <see cref="sections"/> sections / double that in subsections.
        /// </summary>
        [Pure]
        internal static Length SimpsonsLengthOf<TPos, TDiff, TTime, TVel>(
            Polynomial<TPos, TDiff, TTime, TVel> polynomial, 
            double? start = 0.0, double? end = 1.0, 
            int sections = 4
        ) where TPos : unmanaged where TDiff : unmanaged where TTime : unmanaged where TVel : unmanaged {
            
            // The function whose integral in the (0..1) range gives the polynomial curve's length.
            // This is what we want to approximate.
            double LengthDeriv(double t) {
                var v = polynomial.EvaluateDerivativeNormalized(t);
                var ret = Math.Sqrt(polynomial.Ops.Dot(v, v));
                return ret;
            }

            var ret = IntegralApproximation.SimpsonsRule(LengthDeriv, start ?? 0.0, end ?? 1.0, sections).Meters();
            return ret;
        }

        /// <summary>
        /// Approximates the polynomial curve length between 0 and <see cref="t"/>.
        /// </summary>
        [Pure]
        internal static Length ProgressToLength<TPos, TDiff, TTime, TVel>(
            this Polynomial<TPos, TDiff, TTime, TVel> polynomial, double t,
            int approximationSampleSectionCount = 4
        ) where TPos : unmanaged where TDiff : unmanaged where TTime : unmanaged where TVel : unmanaged => 
            SimpsonsLengthOf(polynomial, end: t, sections: approximationSampleSectionCount);

        /// <summary>
        /// Iteratively approximates the progress parameter t where the length of the
        /// polynomial curve in segment (0..t) is equal to <see cref="length"/> by
        /// guessing and repeatedly correcting <see cref="iterations"/> times. Stops
        /// early when the current value is "good enough", meaning that the segment
        /// length at this point is at most <see cref="thresholdFactor"/> times the
        /// queried length and vice versa.
        /// </summary>
        /// <param name="polynomial">The polynomial in question</param>
        /// <param name="length">The length of the curve segment whose end we seek</param>
        /// <param name="iterations">How many times the guess should be corrected towards the true value</param>
        /// <param name="oversteppingFactor">When correcting towards the true value, we can expect the polynomial to
        /// stay locally similar, which means that we will probably stay on the wrong side of the true value. To avoid
        /// this, we step <see cref="oversteppingFactor"/> times further. Must be more than zero and less than two
        /// to prevent never finding the value. Should be at least one</param>
        /// <param name="thresholdFactor">Triggers an early return when the current curve segment is within a factor
        /// of <see cref="thresholdFactor"/> from the queried <see cref="length"/></param>
        /// <param name="cachedPolynomialLength">The total polynomial arc length if already known</param>
        /// <param name="approximationSampleSectionCount">How many sections to sample when calculating the <see cref="SimpsonsLengthOf{TPos,TDiff,TTime,TVel}"/></param>
        [Pure]
        internal static double LengthToProgress<TPos, TDiff, TTime, TVel>(
            this Polynomial<TPos, TDiff, TTime, TVel> polynomial,
            Length length,
            int iterations = 2,
            double oversteppingFactor = 1.1,
            double thresholdFactor = 1.01,
            Length? cachedPolynomialLength = null,
            int approximationSampleSectionCount = 2
        ) where TPos : unmanaged where TDiff : unmanaged where TTime : unmanaged where TVel : unmanaged {
            if (thresholdFactor < 1)
                throw new ArgumentException($"{nameof(thresholdFactor)} must be at least 1, was {thresholdFactor}");
            
            var upperThreshold = thresholdFactor;
            var lowerThreshold = 1 / thresholdFactor;

            var totalLength = cachedPolynomialLength ?? SimpsonsLengthOf(polynomial, sections: approximationSampleSectionCount);
            
            var lowerBound = (t: 0.0, length: Length.Zero);
            var upperBound = (t: 1.0, length: totalLength);

            // "Guessing" the starting point. This is the accurate result iff
            // the derivative's magnitude is constant across the whole polynomial.
            var t = length / totalLength;
            
            for (var i = 0; i < iterations; i++) {
                var actualLength = lowerBound.length + SimpsonsLengthOf(polynomial, start: lowerBound.t, end: t, approximationSampleSectionCount);
                var error = actualLength / length;
                if (error <= upperThreshold && error >= lowerThreshold) break;
                
                // Depending on whether the the actual value was smaller or greater than the queried one,
                // we know that that is a lower or upper bound and we need to step in the other direction.

                double tInBounds;
                if (length > actualLength) {
                    lowerBound = (t, actualLength);
                    tInBounds = Units.InverseLerp(actualLength, upperBound.length, length) * oversteppingFactor;
                } else {
                    upperBound = (t, actualLength);
                    tInBounds = Units.InverseLerp(lowerBound.length, actualLength, length) / oversteppingFactor;
                }
                
                t = MathD.Lerp(
                    lowerBound.t,
                    upperBound.t,
                    tInBounds,
                    shouldClamp: true
                );
            }

            return t;
        }

        [Pure]
        public static NormalizedSplineLocation ClosestPointInSegmentTo<TPos, TDiff, TTime, TVel>(
            this SplineSample<TPos, TDiff, TTime, TVel> sample, TPos queriedPosition, int samples
        ) where TPos : unmanaged where TDiff : unmanaged where TTime : unmanaged, IComparable<TTime> where TVel : unmanaged => 
            NormalizedSplineLocation.From(sample.TNormalized + sample.Segment.Polynomial.EvaluateClosestPointTo(queriedPosition, samples));

    }
}