using System;
using System.Collections.Generic;
using System.Linq;

namespace BII.WasaBii.Core {
    
    public static class ZipExtensions {
        
        public static IEnumerable<(A, B)> Zip<A, B>(this IEnumerable<A> first, IEnumerable<B> second) =>
            first.Zip(second, (a, b) => (a, b));

        public static IEnumerable<(A, B, C)> Zip<A, B, C>(
            this IEnumerable<A> first, IEnumerable<B> second, IEnumerable<C> third
        ) =>
            first.Zip<A, (B, C), (A, B, C)>(second.Zip(third), (a, tuple) => (a, tuple.Item1, tuple.Item2));
        
        public static IEnumerable<(A, B, C, D)> Zip<A, B, C, D>(
            this IEnumerable<A> first, IEnumerable<B> second, IEnumerable<C> third, IEnumerable<D> fourth
        ) =>
            first.Zip(second).Zip(third.Zip(fourth), 
                (aAndB, cAndD) => (aAndB.Item1, aAndB.Item2, cAndD.Item1, cAndD.Item2));

        public static IEnumerable<(A, B, C, D, E)> Zip<A, B, C, D, E>(
            this IEnumerable<A> first, IEnumerable<B> second, IEnumerable<C> third, IEnumerable<D> fourth, IEnumerable<E> fifth
        ) =>
            first.Zip(second).Zip(third.Zip(fourth, fifth), 
                (aAndB, cAndDAndE) => (aAndB.Item1, aAndB.Item2, cAndDAndE.Item1, cAndDAndE.Item2, cAndDAndE.Item3));

        public static IEnumerable<Out> Zip<A, B, C, Out>(
            this IEnumerable<A> first, IEnumerable<B> second, IEnumerable<C> third, Func<A, B, C, Out> mapping
        ) =>
            first.Zip(second.Zip(third), (a, tuple) => mapping(a, tuple.Item1, tuple.Item2));

        public static IEnumerable<Out> Zip<A, B, C, D, Out>(
            this IEnumerable<A> first, IEnumerable<B> second, IEnumerable<C> third, IEnumerable<D> fourth, Func<A, B, C, D, Out> mapping
        ) =>
            first.Zip(second).Zip(third.Zip(fourth), 
                (aAndB, cAndD) => mapping(aAndB.Item1, aAndB.Item2, cAndD.Item1, cAndD.Item2));

        public static IEnumerable<Out> Zip<A, B, C, D, E, Out>(
            this IEnumerable<A> first, IEnumerable<B> second, IEnumerable<C> third, IEnumerable<D> fourth, IEnumerable<E> fifth, Func<A, B, C, D, E, Out> mapping
        ) =>
            first.Zip(second).Zip(third.Zip(fourth, fifth), 
                (aAndB, cAndDAndE) => mapping(aAndB.Item1, aAndB.Item2, cAndDAndE.Item1, cAndDAndE.Item2, cAndDAndE.Item3));
        
        public static (List<T1>, List<T2>) Unzip<T1, T2>(this IEnumerable<(T1, T2)> enumerable) {
            var t1List = new List<T1>();
            var t2List = new List<T2>();
            
            foreach (var (t1, t2) in enumerable) {
                t1List.Add(t1);
                t2List.Add(t2);
            }

            return (t1List, t2List);
        }
        
        public static IEnumerable<(T item, int index)> ZipWithIndices<T>(this IEnumerable<T> source) {
            var index = 0;
            foreach (var item in source) {
                yield return (item, index++);
            }
        }
        
    }
}