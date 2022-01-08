using System.Collections.Generic;

namespace BII.Utilities.Unity {
    public static class ListExtensions {
        public static void AddRange<T>(this List<T> source, params T[] items) => source.AddRange(items);
    }
}