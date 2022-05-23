using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace BII.WasaBii.Core {
    
    public static class EnumerableUtils {

        [Pure] public static IEnumerable<double> Range01(
            int count, bool includeFrom, bool includeTo
        ) {
            var normalizationFactor = 1 / (includeFrom, includeTo) switch {
                (false, false) => count + 1d,
                (true, false) or (false, true) => count,
                (true, true) => count - 1d
            };
            return Enumerable.Range(includeFrom ? 0 : 1, count)
                .Select(i => i * normalizationFactor);
        }

        [Pure] public static IEnumerable<T> Range<T>(
            Func<double, T> interpolate, int count, bool includeFrom, bool includeTo
        ) where T : struct => Range01(count, includeFrom, includeTo).Select(interpolate);

        [Pure] public static IEnumerable<double> Range(
            double from,
            double to,
            int count,
            bool includeFrom,
            bool includeTo
        ) => Range(t => from.Lerp(to, t), count, includeFrom, includeTo);

    }
    
}