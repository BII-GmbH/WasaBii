using System;
using JetBrains.Annotations;
using UnityEngine;

namespace BII.WasaBii.Unity {
    
    public static class ColorExtensions {
        
        /// <summary>
        /// Returns a new color with specific values changed to new values.
        /// This method is intended to be used with named parameters.
        /// </summary>
        /// <example>
        /// var foo = color.With(r: 0.3f, a: 0.1f);
        /// </example>
        /// <returns>A new vector with it's coordinates set to everything specified that is not null.</returns>
        [Pure]
        public static Color With(this Color color, float? r = null, float? g = null, float? b = null, float? a = null) {
            if (r != null) color.r = r.Value;
            if (g != null) color.g = g.Value;
            if (b != null) color.b = b.Value;
            if (a != null) color.a = a.Value;
            return color; // this is a copy since Color is a struct.
        }

        /// <returns>A new color with it's r value set to the specified value.</returns>
        [Pure]
        public static Color WithR(this Color col, float r) => 
            new Color(r, col.g, col.b, col.a);

        /// <returns>A new color with it's g value set to the specified value.</returns>
        [Pure]
        public static Color WithG(this Color col, float g) => 
            new Color(col.r, g, col.b, col.a);

        /// <returns>A new color with it's b value set to the specified value.</returns>
        [Pure]
        public static Color WithB(this Color col, float b) => 
            new Color(col.r, col.g, b, col.a);

        /// <returns>A new color with it's a value set to the specified value.</returns>
        [Pure]
        public static Color WithA(this Color col, float a) => 
            new Color(col.r, col.g, col.b, a);

        /// <returns>A new color with it's r value set to the result
        /// of applying the specified function to the original value.</returns>
        [Pure]
        public static Color WithR(this Color col, [NotNull] Func<float, float> fr) => 
            new Color(fr(col.r), col.g, col.b, col.a);

        /// <returns>A new color with it's g value set to the result
        /// of applying the specified function to the original value.</returns>
        [Pure]
        public static Color WithG(this Color col, [NotNull] Func<float, float> fg) => 
            new Color(col.r, fg(col.g), col.b, col.a);

        /// <returns>A new color with it's b value set to the result
        /// of applying the specified function to the original value.</returns>
        [Pure]
        public static Color WithB(this Color col, [NotNull] Func<float, float> fb) => 
            new Color(col.r, col.g, fb(col.b), col.a);
        
    }
}