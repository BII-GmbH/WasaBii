using BII.WasaBii.UnitSystem;
using UnityEngine;
using WasaBii.Geometry.Shared;
using static WasaBii.Geometry.Shared.FieldType;

namespace BII.WasaBii.Unity.Geometry {
    [GeometryHelper(areFieldsIndependent: false, fieldType: Other, hasMagnitude: false, hasDirection: false)]
    public readonly partial struct GlobalRotation2 {
        public readonly Quaternion AsQuaternion;
    }

    [GeometryHelper(areFieldsIndependent: true, fieldType: FieldType.Length, hasMagnitude: false, hasDirection: false)]
    public readonly partial struct GlobalPosition2 {
        public readonly Length X, Y, Z;
    }

    [GeometryHelper(areFieldsIndependent: true, fieldType: FieldType.Length, hasMagnitude: true, hasDirection: true)]
    public readonly partial struct GlobalOffset2 {
        public readonly Length X, Y, Z;
        public Length test() => Magnitude;
    }

    [GeometryHelper(areFieldsIndependent: false, FieldType.Double, hasMagnitude: false, hasDirection: true)]
    public readonly partial struct GlobalDirection2 {
        public readonly double X, Y, Z;
    }
}