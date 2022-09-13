using System;
using System.Collections.Generic;

namespace BII.WasaBii.Core {
    
    public static class Approximations {

        /// <summary>
        /// Approximates the integral of the function <see cref="f"/> by applying
        /// Simpson's 1/3 rule with 2 * <see cref="sections"/> subsections.
        /// </summary>
        public static double SimpsonsRule(
            Func<double, double> f, 
            double from, double to, 
            int sections
        ) {
            var factors = new List<int> { 1, 4 };
            for (var i = 1; i < sections; i++) {
                factors.Add(2);
                factors.Add(4);
            }
            factors.Add(1);

            var accum = 0d;
            var subsections = sections * 2;
            var diff = to - from;

            for (var i = 0; i <= subsections; i++) 
                accum += factors[i] * f(from + diff * i / subsections);
            return accum / (subsections * 3);
        }
        
    }
    
}