using System;
using System.Collections.Generic;

namespace BII.WasaBii.Core {

    public static class Compare {
        
        /// <summary>
        /// Returns a comparer that maps both compared values using the specified mapper and compares the results.
        /// </summary>
        public static IComparer<T> By<T, TRes>(Func<T, TRes> compareMapper) where TRes : IComparable<TRes> => 
            new CompareBy<T,TRes>(compareMapper);
        
        /// <summary>
        /// A generic, non-serializable IComparer for use in collections.
        /// Compares by mapping both compared values to a new type and comparing that type instead.
        /// </summary>
        private sealed class CompareBy<T, TRes> : IComparer<T> where TRes : IComparable<TRes> {
            private readonly Func<T, TRes> compareBy;
            public CompareBy(Func<T, TRes> compareBy) => this.compareBy = compareBy;
            public int Compare(T x, T y) => compareBy(x).CompareTo(compareBy(y));
        }
    }
    
    
}