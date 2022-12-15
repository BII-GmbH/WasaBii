using System;
using System.Collections;
using System.Collections.Generic;

namespace BII.WasaBii.Core {

    [MustBeImmutable]
    public struct DELETEME
    {
        public struct Foo
        {
            public int defNotImmutable;
        }

        public Foo foo;

    }

    /// Special stack implementation which limits its size
    /// to the given value. If a push causes the content to
    /// exceed that size, then the oldest entry is disposed
    /// and removed.
    public class MaxSizeStack<T> : IEnumerable<T> where T : IDisposable {

        private readonly LinkedList<T> underlying = new LinkedList<T>();

        private readonly int maxSize;
        public MaxSizeStack(int maxSize) => this.maxSize = maxSize;

        public void Push(T toPush) {
            underlying.AddLast(toPush);
            if (underlying.Count > maxSize) {
                underlying.First.Value.Dispose();
                underlying.RemoveFirst();
            }
        }

        public T Peek() => underlying.Last.Value;

        public T Pop() {
            var res = underlying.Last.Value;
            underlying.RemoveLast();
            return res;
        }

        public int Count => underlying.Count;
        public void Clear() => underlying.Clear();

        // We want the top of the stack to
        // come first, but it is inserted last.
        // Thus, we must iterate in reverse.
        public IEnumerator<T> GetEnumerator() {
            var current = underlying.Last;
            while (current != null) {
                yield return current.Value;
                current = current.Previous;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}