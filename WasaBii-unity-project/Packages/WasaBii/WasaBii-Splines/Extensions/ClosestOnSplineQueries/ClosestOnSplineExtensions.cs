using System;
using BII.WasaBii.Core;

namespace BII.WasaBii.Splines {
    public static class ClosestOnSplineExtensions {
        
        public const int DefaultInitialSamplingCount = 10;
        public const int DefaultIterations = 10;
        public const double DefaultMinStepSize = 0.005;

        /// <summary>
        /// Tries to find the location on the spline where its position has the minimum Euclidean distance to the given <see cref="position"/>.
        /// Works by first sampling the spline at <paramref name="initialSamples"/> points and using the best one as a starting point.
        /// Then, it iteratively refines the result by using the Newton method, with a maximum of <paramref name="iterations"/> iterations.
        /// Returns early when the individual step size is smaller than <paramref name="minStepSize"/>.
        /// <br/>
        /// More initial samples and more iterations lead to higher accuracy, but also higher computational cost.
        /// Increasing the number of iterations increases the chance of finding a minimum.
        /// Increasing the number of initial samples means that fewer iterations are needed and reduces the chance of getting stuck in a local minimum.
        /// Complex splines with a lot of turns require more initial samples than simple straight-ish ones.
        /// </summary>
        public static ClosestOnSplineQueryResult<TPos, TDiff, TTime, TVel> QueryClosestPositionOnSplineTo<TPos, TDiff, TTime, TVel>(
            this Spline<TPos, TDiff, TTime, TVel> spline,
            TPos position,
            int initialSamples = DefaultInitialSamplingCount,
            int iterations = DefaultIterations,
            double minStepSize = DefaultMinStepSize
        ) where TPos : unmanaged where TDiff : unmanaged where TTime : unmanaged, IComparable<TTime> where TVel : unmanaged {
            var ops = spline.Ops;
            
            if(initialSamples < 1) throw new ArgumentException("The number of initial samples must be at least 1.");
            
            // Take a number of samples along the spline and take the best one -> attempt to find the area of the global minimum
            var closest = spline.SampleSpline(initialSamples)
                .MinBy(sample => ops.Distance(sample.Position, position))
                .GetOrThrow(); // should not throw if initialSamples > 0

            // we need to scale the derivatives later, depending on what temporal proportion of the spline a segment has
            var durationScaling = ops.Div(spline.TotalDuration, closest.Segment.Duration);
            var currentSegment = closest.SegmentIndex;
            
            // Iteratively refine the result using the newton method
            for (var i = 0; i < iterations; i++) {
                // We want to minimize f(t) = ||spline[t].pos - position||^2
                // where t is the normalized location
                // Refining with newton's method means updating the location like this:
                // t = t - f'(t) / f''(t)
                // where
                // f'(t) = 2 * dot(spline[t].pos - position, spline[t].deriv)
                // f''(t) = 2 * (dot(spline[t].deriv, spline[t].deriv) + dot(spline[t].pos - position, spline[t].deriv2))
                
                var p = closest.Position;
                var pDeriv = ops.Mul(closest.DerivativeInSegment, durationScaling);
                var pDeriv2 = ops.Mul(closest.SecondDerivativeInSegment, durationScaling);

                // we skip the "2 * " part as it cancels out
                var diff = ops.Sub(p, position);
                var fDeriv = ops.Dot(diff, pDeriv);
                var fDeriv2 = ops.Dot(pDeriv, pDeriv) + ops.Dot(diff, pDeriv2);
                
                var dDiff = fDeriv / fDeriv2;
                
                // Apply the new t (and clamp)
                var oldT = closest.NormalizedLocation;
                var newT = oldT - dDiff;
                if(newT.Value < 0) { newT = new NormalizedSplineLocation(0); }
                if(newT.Value > spline.SegmentCount) newT = new NormalizedSplineLocation(spline.SegmentCount);
                closest = spline[newT];

                if (Math.Abs(oldT.Value - newT.Value) < minStepSize) break;
                
                if(currentSegment != closest.SegmentIndex)
                    durationScaling = ops.Div(spline.TotalDuration, closest.Segment.Duration);
            }

            return new ClosestOnSplineQueryResult<TPos, TDiff, TTime, TVel>(position, spline, closest);
        }
    }
}