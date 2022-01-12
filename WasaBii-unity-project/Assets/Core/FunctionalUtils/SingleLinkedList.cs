#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace BII.WasaBii.Core {

    /// Simple functional implementation of an immutable single-linked list, Haskell style.
    /// Only allows efficient prepends.
    /// Random access, getting the last element and getting the count are have O(n) linear complexity.
    public sealed class SingleLinkedList<T> : IEnumerable<T> {

        private sealed class ListLink {
            public readonly T? Item;
            public readonly ListLink? Next;
            public ListLink(T? item, ListLink? next) { this.Item = item; this.Next = next; }
        }

        private static readonly ListLink Nil = new(default, null);

        private readonly ListLink head;

        public T? Head => head.Item;

        public SingleLinkedList<T> Tail =>
            head.Next == null
                ? new SingleLinkedList<T>()
                : new SingleLinkedList<T>(head.Next);

        public SingleLinkedList() => this.head = Nil;

        private SingleLinkedList(ListLink head) => this.head = head;

        public SingleLinkedList(IEnumerable<T> source) : this(source.GetEnumerator()) { }
        public SingleLinkedList(IEnumerator<T> source) => 
            this.head = source.MoveNext() 
                ? new ListLink(source.Current, new SingleLinkedList<T>(source).head) : Nil;

        [Pure] public SingleLinkedList<T> Prepend(T value) => new(new ListLink(value, head));

        public IEnumerator<T> GetEnumerator() {
            var curr = head;
            while (curr is {Item: { }}) {
                yield return curr.Item;
                curr = curr.Next;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}