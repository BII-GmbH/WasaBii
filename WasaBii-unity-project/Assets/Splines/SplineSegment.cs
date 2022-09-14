using System;
using System.Diagnostics.Contracts;
using BII.WasaBii.Core;
using BII.WasaBii.Splines.Maths;
using BII.WasaBii.UnitSystem;

namespace BII.WasaBii.Splines {
    public readonly struct SplineSegment<TPos, TDiff>
        where TPos : struct 
        where TDiff : struct {
        internal readonly Polynomial<TPos, TDiff> Polynomial;
        private readonly Lazy<Length> cachedLength;
        public Length Length => cachedLength.Value;

        internal SplineSegment(Polynomial<TPos, TDiff> polynomial, Length? cachedLength = null) {
            Polynomial = polynomial;
            this.cachedLength = new Lazy<Length>(() => cachedLength ?? SplineSegmentUtils.SimpsonsLengthOf(polynomial));
        }
        
        public SplineSample<TPos, TDiff> SampleAt(double percentage) => new(this, percentage);
    }

    public static class SplineSegmentUtils {
        
        /// <summary>
        /// Approximates the length of the <see cref="polynomial"/> by
        /// applying the trapezoidal rule with <see cref="samples"/> sections.
        /// </summary>
        [Pure]
        internal static Length TrapezoidalLengthOf<TPos, TDiff>(Polynomial<TPos, TDiff> polynomial, int samples = 10) 
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

        /// <summary>
        /// Approximates the length of the <see cref="polynomial"/> by applying
        /// Simpson's 1/3 rule with <see cref="sections"/> sections / double that in subsections.
        /// </summary>
        [Pure]
        internal static Length SimpsonsLengthOf<TPos, TDiff>(Polynomial<TPos, TDiff> polynomial, int sections = 4) 
        where TPos : struct 
        where TDiff : struct {
            
            // The function whose integral in the (0..1) range gives the polynomial's length.
            // This is what we want to approximate.
            double LengthDeriv(double t) {
                var v = polynomial.EvaluateDerivative(t);
                return Math.Sqrt(polynomial.Ops.Dot(v, v));
            }

            return IntegralApproximation.SimpsonsRule(LengthDeriv, from: 0, to: 1, sections).Meters();
        }
        
        [Pure]
        public static NormalizedSplineLocation ClosestPointInSegmentTo<TPos, TDiff>(
            this SplineSample<TPos, TDiff> sample, TPos queriedPosition, int samples
        ) 
        where TPos : struct 
        where TDiff : struct => NormalizedSplineLocation.From(sample.T + sample.Segment.Polynomial.EvaluateClosestPointTo(queriedPosition, samples));
    }
}