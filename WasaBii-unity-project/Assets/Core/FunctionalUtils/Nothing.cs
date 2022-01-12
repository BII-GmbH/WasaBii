namespace BII.WasaBii.Core {
    /// Struct that does nothing special and has no meaning whatsoever.
    /// Can be used as a generic type to signal that there is nothing special in that case.
    [MustBeSerializable] public readonly struct Nothing {
        public override bool Equals(object obj) => obj is Nothing;
        public override int GetHashCode() => 0;
    }
}