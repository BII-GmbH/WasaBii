using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace BII.WasaBii.Unity.Geometry {
    
    /// Supertype for all geometry utils.
    /// Those wrap low-level mathematical structures like vectors and quaternions in
    /// expressive typed helpers that give more context (e.g. position, offset, direction).
    /// All geometry helpers have to implement linear and spherical interpolation (lerp & slerp)
    /// as this is needed for several utility extensions like <code>Average</code>.
    public interface GeometryHelper<TSelf>
        where TSelf : GeometryHelper<TSelf> {

        [Pure] TSelf LerpTo(TSelf target, double progress, bool shouldClamp = true);
        [Pure] TSelf SlerpTo(TSelf target, double progress, bool shouldClamp = true);
    }

    public static class GeometryHelperExtensions {

        [Pure]
        public static T Average<T>(this IEnumerable<T> enumerable) where T : struct, GeometryHelper<T>
            => enumerable.Select(e => (val: e, amount: 1))
                .Aggregate((l, r) => (
                    val: l.val.LerpTo(r.val, l.amount / (double)(l.amount + r.amount)), 
                    amount: l.amount + r.amount
                )).val;

    }

}