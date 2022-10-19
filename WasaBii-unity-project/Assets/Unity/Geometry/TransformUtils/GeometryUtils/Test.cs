using BII.WasaBii.UnitSystem;
using WasaBii.Geometry.Shared;

namespace BII.WasaBii.Core.GeometryUtils {
    [GeometryHelper(areFieldsIndependent: false, hasMagnitude: false)]
    public readonly partial struct GlobalRotation {
        public readonly Length X, Y, Z, W;
    }

    [GeometryHelper(areFieldsIndependent: true, hasMagnitude: false)]
    public readonly partial struct GlobalPosition {
        public readonly Length X, Y, Z;
    }

    [GeometryHelper(areFieldsIndependent: true, hasMagnitude: true)]
    public readonly partial struct GlobalOffset {
        public readonly Length X, Y, Z;
        public Length test() => Magnitude;
    }
}