using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace BII.WasaBii.Unity.Geometry {
    
    /// Supertype for all transform utils.
    public interface TransformHelper<TSelf>
        where TSelf : TransformHelper<TSelf> {

        [Pure] TSelf LerpTo(TSelf target, double progress, bool shouldClamp = true);
        [Pure] TSelf SlerpTo(TSelf target, double progress, bool shouldClamp = true);
    }

    public static class TransformHelperExtensions {

        [Pure]
        public static T Average<T>(this IEnumerable<T> enumerable) where T : struct, TransformHelper<T>
            => enumerable.Select(e => (val: e, amount: 1))
                .Aggregate((l, r) => (
                    val: l.val.LerpTo(r.val, l.amount / (double)(l.amount + r.amount)), 
                    amount: l.amount + r.amount
                )).val;

    }

}