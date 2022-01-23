using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace BII.WasaBii.Core {

    [Serializable] [MustBeSerializable]
    public sealed class ReverseList<T> : IReadOnlyList<T> {

        private readonly IReadOnlyList<T> wrapped;
        public ReverseList(IReadOnlyList<T> wrapped) => this.wrapped = wrapped;
        
        [JsonConstructor] private ReverseList(IEnumerable<T> wrapped) 
            : this(wrapped as IReadOnlyList<T> ?? wrapped.ToList()) { }

        public IEnumerator<T> GetEnumerator() {
            for (var i = wrapped.Count - 1; i >= 0; i--)
                yield return wrapped[i];
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public int Count => wrapped.Count;

        public T this[int i] => wrapped[^i];
        
        public override bool Equals(object obj) => obj is ReverseList<T> other && Equals(wrapped, other.wrapped);

        public override int GetHashCode() => wrapped != null ? -wrapped.GetHashCode() : 0;
    }

    public static class ReverseListExtension {
        
        public static IReadOnlyList<T> ReverseList<T>(this IReadOnlyList<T> list) => new ReverseList<T>(list);
        
    }

}