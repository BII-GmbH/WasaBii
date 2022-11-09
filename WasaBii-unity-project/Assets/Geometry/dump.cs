using System.Collections;
using System.Collections.Generic;
using BII.WasaBii.UnitSystem;
using UnityEngine;
using WasaBii.Geometry.Shared;

namespace BII.WasaBii.Geometry
{
    
    [GeometryHelper(areFieldsIndependent: true, fieldType: (int)FieldType.Length, hasMagnitude: true, hasDirection: true)]
    public readonly partial struct GlobalOffset3 {
        public readonly Length X, Y, Z;
        public Length test() => Magnitude;
    }

}
