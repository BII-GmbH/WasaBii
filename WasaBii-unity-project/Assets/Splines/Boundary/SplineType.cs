using System.ComponentModel;
using BII.WasaBii.Core;

namespace BII.WasaBii.Splines {
    
    /// Describes how closely a catmull-rom spline interpolates its handles.
    /// Centripetal is the recommended default, as Uniform may cause loops
    /// and Chordal may cause sharp curves.
    /// See <a href="https://upload.wikimedia.org/wikipedia/commons/thumb/2/2f/Catmull-Rom_examples_with_parameters..png/220px-Catmull-Rom_examples_with_parameters..png">
    ///   this graph
    /// </a>
    /// for a visual demonstration.
    [MustBeSerializable] 
    public enum SplineType {
        Uniform,
        Centripetal,
        Chordal
    }

    internal static class SplineTypeUtils {
        
        /// A mathematical value used by the catmull-rom segment interpolation calculations to determine how the interpolation is influenced by the respective handles.
        public static float ToAlpha(this SplineType type) =>
            type switch {
                SplineType.Uniform => 0,
                SplineType.Centripetal => 0.5f,
                SplineType.Chordal => 1,
                _ => throw new InvalidEnumArgumentException(
                    $"{nameof(SplineTypeUtils)}.{nameof(ToAlpha)} does not support the value {type}")
            };
    }
}

