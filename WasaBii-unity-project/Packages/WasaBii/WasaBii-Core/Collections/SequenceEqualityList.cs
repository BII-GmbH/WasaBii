using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace BII.WasaBii.Core {
    
    /// <summary>
    /// Implements `.Equals()` and `.GetHashCode()`
    /// so that they take the elements into account.
    /// Creates a copy of the wrapped list so that even
    /// if the original gets mutated, the contents of
    /// `this` remains unchanged.
    /// </summary>
    [Serializable]
    public sealed class SequenceEqualityList<T> : IReadOnlyList<T>, IEquatable<IEnumerable<T>>, IEquatable<SequenceEqualityList<T>> {
        
        private readonly ImmutableArray<T> wrapped;
        private readonly Lazy<int> hashCode;

        // Note DS: Json.Net leads, I follow.
        // Json does not lead good though, this took a lot of time to figure out.
        // Fyi: It seems like when a json serialized type implements IReadOnlyList
        // (or maybe even IEnumerable?), json expects a constructor that takes
        // an IEnumerable. Thus, I got invalid cast exceptions when only providing
        // an IReadOnlyList constructor.
        // Since I couldn't find any documentation on this (which may or may not
        // in part be due to the fact that I'm too lazy to search for more than
        // 5 minutes), I won't trust Json.Net not to call this with null though, thus
        // the null check. Since therefore, the Json constructor and the actual one
        // have the exactly same signature, I cannot separate them.
        public SequenceEqualityList(IEnumerable<T> wrapped) {
            this.wrapped = (wrapped ?? ImmutableArray<T>.Empty).AsImmutableArray();
            hashCode = new Lazy<int>(() => this.wrapped.Aggregate(0, (hash, now) => 
                (hash << 7) ^ (hash >> 5) ^ (now != null ? now.GetHashCode() : 0)));
        }

        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)wrapped).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)wrapped).GetEnumerator();
        public int Count => wrapped.Length;

        public T this[int index] => wrapped[index];

        public override bool Equals(object obj) => 
            obj is IEnumerable<T> other && Equals(other);

        public bool Equals(IEnumerable<T> other) => other != null &&
            this.SequenceEqual(other);

        public bool Equals(SequenceEqualityList<T> other) => other != null && this.SequenceEqual(other);

        public override int GetHashCode() => hashCode.Value;

    }

    public static class SequenceEqualityListExtensions {
        public static SequenceEqualityList<T> ToSequenceEqualityList<T>(this IEnumerable<T> enumerable) => new(enumerable);
    }

}