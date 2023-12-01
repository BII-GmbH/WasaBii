using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

#nullable enable

namespace BII.WasaBii.Core {
    
    /// <summary>
    /// A readonly <see cref="ArraySegment{T}"/> but for <see cref="IReadOnlyList{T}"/>s.
    /// This is a view on a list, mutating the original will mutate this too.
    /// </summary>
    public readonly struct ReadOnlyListSegment<T> : IReadOnlyList<T> {

        private readonly IReadOnlyList<T> _wrapped;

        private readonly int _offset;
        public int Count { get; }

        public ReadOnlyListSegment(IReadOnlyList<T> wrapped, int offset, int count) {
            if(offset < 0) 
                throw new ArgumentOutOfRangeException($"{nameof(offset)} must be non-negative (was {offset})");
            if(count < 0) 
                throw new ArgumentOutOfRangeException($"{nameof(count)} must be non-negative (was {count})");
            if(wrapped.Count < offset + count) 
                throw new ArgumentOutOfRangeException($"Segment exceeds wrapped list length (segment end: {offset + count}, wrapped length: {wrapped.Count})");
            
            _wrapped = wrapped;
            this._offset = offset;
            this.Count = count;
        }

        public T this[int i] {
            get {
                if (i >= Count || i < 0) throw new IndexOutOfRangeException($"Index {i} is out of range for "
                    + $"{nameof(ReadOnlyListSegment<T>)} with count {Count} (offset: {_offset}, original count: {_wrapped.Count})");
                return _wrapped[i + _offset];
            }
        }

        public IEnumerator<T> GetEnumerator() {
            for (var i = _offset; i < _offset + Count; i++) yield return _wrapped[i];
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    
}