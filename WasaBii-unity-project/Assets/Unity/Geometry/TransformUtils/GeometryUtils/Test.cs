using System.Numerics;
using BII.WasaBii.UnitSystem;
using BII.WasaBii.Geometry.Shared;

namespace BII.WasaBii.Unity.Geometry {

    [GeometryHelper(areFieldsIndependent: false, fieldType: FieldType.Other, hasMagnitude: false, hasDirection: false)]
    public readonly partial struct GlobalRotation2 {
        public readonly Quaternion AsQuaternion;
    }
    
    [GeometryHelper(areFieldsIndependent: false, fieldType: FieldType.Double, hasMagnitude: false, hasDirection: true)]
    public readonly partial struct GlobalDirection2 {
        public readonly double X, Y, Z;
    }
}