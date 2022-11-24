using UnityEngine;

namespace BII.WasaBii.Unity.Geometry
{
    /// This struct provides implicit conversions for all supported
    /// types that can be used to obtain a global position vector.
    /// Currently, it is exclusively used by the `Offset.From().To()` pattern,
    /// but it is designed to be future proof, following the same principles as `TransformProvider`.
    public readonly struct PositionProvider {
        public readonly GlobalPosition Wrapped;
        public PositionProvider(GlobalPosition wrapped) => this.Wrapped = wrapped;
        public static implicit operator PositionProvider(System.Numerics.Vector3 position)
            => new (position.AsGlobalPosition());
        public static implicit operator PositionProvider(Vector3 position)
            => new (position.AsGlobalPosition());
        public static implicit operator PositionProvider(Component component)
            => new (component.GlobalPosition());
        public static implicit operator PositionProvider(GameObject gameObject)
            => new (gameObject.GlobalPosition());
    }
}