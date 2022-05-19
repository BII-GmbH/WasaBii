using System.ComponentModel;
using BII.WasaBii.Core;

namespace BII.WasaBii.Splines {
    
    /// Describes how closely a catmull-rom spline interpolates its handles.
    /// Centripetal is the recommended default, as Uniform may cause loops
    /// and Chordal may cause sharp curves.
    [MustBeSerializable] 
    public enum SplineType {
        Uniform,
        Centripetal,
        Chordal
    }

    namespace Logic {
        public static class SplineTypeUtils {
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
}

