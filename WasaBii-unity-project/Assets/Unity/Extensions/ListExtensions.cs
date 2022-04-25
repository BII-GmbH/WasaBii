using System.Collections.Generic;

namespace BII.WasaBii.Unity {
    public static class ListExtensions {

        public static void Add<T>(this IList<T> source, params T[] items) {
            // IList doesn't have an `AddRange`, hence the loop.
            foreach(var item in items) source.Add(item);
        }
        
    }
}