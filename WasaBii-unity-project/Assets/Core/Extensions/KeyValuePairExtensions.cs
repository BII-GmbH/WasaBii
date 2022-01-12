using System.Collections.Generic;

namespace BII.WasaBii.Core {

    public static class KeyValuePairExtensions {
        /// <summary>
        /// Allows key value pairs to be deconstructed into a tuple implicitly, 
        /// making foreach loops over dictionaries more readable
        /// </summary>
        public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> kvp, out TKey key, out TValue value) {
            key = kvp.Key;
            value = kvp.Value;
        }
    }

}
