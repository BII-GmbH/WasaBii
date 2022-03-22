using System;
using BII.Utilities.Independent;

namespace BII.CatmullRomSplines {
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
            public static float ToAlpha(this SplineType type) {
                switch (type) {
                    case SplineType.Uniform:
                        return 0;
                    case SplineType.Centripetal:
                        return 0.5f;
                    case SplineType.Chordal:
                        return 1;
                    default:
                        throw new InvalidOperationException($"SplineTypeExtensions.ToAlpha does not support the value {type} yet");
                }
            }
        }
    }
}

