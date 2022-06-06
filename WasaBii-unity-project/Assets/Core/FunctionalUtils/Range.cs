using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace BII.WasaBii.Core {

    public static class Range {
        
        /// <returns><see cref="count"/> equidistant values between 0 and 1 in ascending order.</returns>
        /// <param name="includeZero">Whether the result starts with 0 or the next larger number</param>
        /// <param name="includeOne">Whether the result ends with 1 or the next smaller number</param>
        [Pure] public static IEnumerable<double> Sample01(
            int count, bool includeZero, bool includeOne
        ) {
            var normalizationFactor = 1 / (includeZero, includeOne) switch {
                (false, false) => count + 1d,
                (true, false) or (false, true) => count,
                (true, true) => count - 1d
            };
            return Enumerable.Range(includeZero ? 0 : 1, count)
                .Select(i => i * normalizationFactor);
        }
        
        /// <returns><see cref="count"/> equidistant samples of the interpolation in ascending order</returns>.
        /// <param name="includeFrom">Whether the result starts with interpolate(0) or the next sample</param>
        /// <param name="includeTo">Whether the result ends with interpolate(1) or the previous sample</param>
        [Pure] public static IEnumerable<T> Sample<T>(Func<double, T> interpolate, int count, bool includeFrom, bool includeTo) => 
            Sample01(count, includeFrom, includeTo).Select(interpolate);


        [Pure] public static Range<T>.Builder From<T>(T from, bool inclusive) => new(from, inclusive);

    }
    
    public readonly struct Range<T> {
        
        public readonly T From;
        public readonly bool IncludeFrom;
        
        public readonly T To;
        public readonly bool IncludeTo;
        
        public Range(T from, T to, bool includeFrom, bool includeTo) {
            this.From = from;
            this.To = to;
            IncludeFrom = includeFrom;
            IncludeTo = includeTo;
        }

        [Pure] public IEnumerable<T> Sample(
            int count, Func<T, T, double, T> interpolate
        ) {
            var (from, to) = (From, To);
            T CalcVal(double t) => interpolate(from, to, t);
            return Range.Sample(CalcVal, count, IncludeFrom, IncludeTo);
        }

        public sealed class Builder {
            
            public readonly T From;
            public readonly bool IncludeFrom;

            public Builder(T from, bool includeFrom) {
                From = from;
                IncludeFrom = includeFrom;
            }

            public Range<T> To(T to, bool inclusive) => new(From, to, IncludeFrom, inclusive);

        }

    }
    
}