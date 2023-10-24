using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace BII.WasaBii.Core {

    /// <summary>
    /// A list view that simply inverts the order of elements.
    /// Wraps the original list without copying it. Be aware
    /// that changes to the original list will reflect to this.
    /// </summary>
    [Serializable]
    public sealed class ReverseList<T> : IReadOnlyList<T> {

        private readonly IReadOnlyList<T> wrapped;
        public ReverseList(IReadOnlyList<T> wrapped) => this.wrapped = wrapped;
        
        public ReverseList(IEnumerable<T> wrapped) 
            : this(wrapped as IReadOnlyList<T> ?? wrapped.ToList()) { }

        public IEnumerator<T> GetEnumerator() {
            for (var i = wrapped.Count - 1; i >= 0; i--)
                yield return wrapped[i];
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public int Count => wrapped.Count;

        public T this[int i] => wrapped[^(i+1)];
        
        public override bool Equals(object obj) => obj is ReverseList<T> other && Equals(wrapped, other.wrapped);

        public override int GetHashCode() => wrapped != null ? -wrapped.GetHashCode() : 0;
    }

    public static class ReverseListExtension {
        
        public static IReadOnlyList<T> ReverseList<T>(this IReadOnlyList<T> list) => new ReverseList<T>(list);
        
    }

}