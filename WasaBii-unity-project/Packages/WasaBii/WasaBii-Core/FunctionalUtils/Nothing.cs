using System;

namespace BII.WasaBii.Core {
    /// <summary>
    /// Struct that does nothing special and has no meaning whatsoever.
    /// Can be used as a generic type to signal that there is nothing special in that case.
    /// </summary>
    /// <remarks>
    /// Corresponds to "Unit" in other languages such as Rust and Haskell. This is not a bottom type.
    /// </remarks>
    [Serializable] public readonly struct Nothing {
        public override bool Equals(object obj) => obj is Nothing;
        public override int GetHashCode() => 0;
    }
}