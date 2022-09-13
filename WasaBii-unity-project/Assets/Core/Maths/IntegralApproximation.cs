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
            var factors = new List<int> { 1, 4 };
            for (var i = 1; i < sections; i++) {
                factors.Add(2);
                factors.Add(4);
            }
            factors.Add(1);

            var subsections = sections * 2;
            var diff = to - from;

            return mul(
                factors
                    .Select((fac, i) => mul(f(from + diff * i / subsections), fac))
                    .Aggregate(add),
                diff / (subsections * 3)
            );
        }
     
        /// <inheritdoc cref="SimpsonsRule(System.Func{double,double},double,double,int)"/>
        public static double SimpsonsRule(
            Func<double, double> f, 
            double from, double to, 
            int sections
        ) => SimpsonsRule(
            f, from, to, sections, 
            add: (a, b) => a + b, 
            mul: (a, b) => a * b
        );

        public static T Trapezoidal<T>(
            Func<double, T> f,
            double from,
            double to,
            int samples,
            Func<T, T, T> add,
            Func<T, double, T> mul
        ) => (to - from)
            * Range.Sample(i => from + i * to, samples, includeFrom: true, includeTo: true)
                .Select(f)
                .Average(add, div: (a, d) => mul(a, 1d / d));

    }
    
}