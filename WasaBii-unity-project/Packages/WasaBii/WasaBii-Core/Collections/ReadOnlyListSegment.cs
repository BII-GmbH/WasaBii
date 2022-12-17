using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace BII.WasaBii.Core {
    
    /// A readonly <see cref="ArraySegment{T}"/> but for <see cref="IReadOnlyList{T}"/>s.
    /// This is a view on a list, mutating the original will mutate this too.
    public readonly struct ReadOnlyListSegment<T> : IReadOnlyList<T> {

        private readonly IReadOnlyList<T> _wrapped;

        private readonly int _offset;
        public int Count { get; }

        public ReadOnlyListSegment(IReadOnlyList<T> wrapped, int offset, int count) {
            Debug.Assert(wrapped != null);
            Debug.Assert(offset >= 0);
            Debug.Assert(count >= 0);
            Debug.Assert(wrapped.Count >= offset + count);
            _wrapped = wrapped;
            this._offset = offset;
            this.Count = count;
        }

        public T this[int i] {
            get {
                if (i >= Count) throw new IndexOutOfRangeException();
                return _wrapped[i + _offset];
            }
        }

        public IEnumerator<T> GetEnumerator() {
            for (var i = _offset; i < _offset + Count; i++) yield return _wrapped[i];
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    
}