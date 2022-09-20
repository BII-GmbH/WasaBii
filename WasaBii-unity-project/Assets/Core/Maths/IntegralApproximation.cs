using System;
using System.Collections.Generic;
using System.Linq;

namespace BII.WasaBii.Core {
    
    public static class IntegralApproximation {

        /// <summary>
        /// Approximates the integral of the function <see cref="f"/> by applying
        /// Simpson's 1/3 rule with 2 * <see cref="sections"/> subsections.
        /// </summary>
        public static T SimpsonsRule<T>(
            Func<double, T> f, 
            double from, double to, 
            int sections,
            Func<T, T, T> add,
            Func<T, double, T> mul
        ) {
            if(sections < 1) throw new ArgumentException($"Cannot sample less than 1 sections (tried to sample {sections})");
            var factors = new List<int> { 1, 4 };
            for (var i = 1; i < sections; i++) {
                factors.Add(2);
                factors.Add(4);
            }
            factors.Add(1);

            var subsections = sections * 2;
            var diff = to - from;

            return mul(
                factors.Zip(Range.Sample01(subsections + 1, includeZero:true, includeOne:true))
                    .Select((fac, i) => mul(f(from + i * diff), fac))
                    .Aggregate(add),
                diff / (subsections * 3)
            );
        }
     
        /// <inheritdoc cref="SimpsonsRule{T}"/>
        public static double SimpsonsRule(
            Func<double, double> f, 
            double from, double to, 
            int sections
        ) => SimpsonsRule(
            f, from, to, sections, 
            add: (a, b) => a + b, 
            mul: (a, b) => a * b
        );

        /// <summary>
        /// Calculates the trapezoidal integral approximation of the function <see cref="f"/>.
        /// </summary>
        public static T Trapezoidal<T>(
            Func<double, T> f,
            double from,
            double to,
            int samples,
            Func<T, T, T> add,
            Func<T, double, T> mul
        ) {
            if(samples < 2) throw new ArgumentException($"Cannot sample less than 2 values (tried to sample {samples})");
            var fromSample = f(from);
            var toSample = f(to);
            var intermediarySamples = samples > 2 
                ? Range.Sample(i => from + i * (to - from), samples - 2, includeFrom: false, includeTo: false)
                    .Select(f)
                : Enumerable.Empty<T>();
            return mul(
                // Every sample except the first and last need to be weighted twice
                intermediarySamples.duplicateElements()
                    .Prepend(fromSample)
                    .Append(toSample)
                    .Average(add, division: (a, d) => mul(a, 1d / d)),
                to - from
            );
        }

        private static IEnumerable<T> duplicateElements<T>(this IEnumerable<T> e) {
            foreach (var t in e) {
                yield return t;
                yield return t;
            }
        }

        /// <inheritdoc cref="Trapezoidal{T}"/>
        public static double Trapezoidal(
            Func<double, double> f, 
            double from, double to,
            int samples
        ) => Range.Sample(i => from + i * (to - from), samples, includeFrom: true, includeTo: true)
            .Select(f)
            .Average() 
            * (to - from);

    }
    
}