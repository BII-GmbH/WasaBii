using System;
using BII.WasaBii.UnitSystem;
using UnityEngine;
using UnityEngine.Serialization;

namespace BII.WasaBii.Unity {

    /// Unity is not capable of serializing readonly structs. Since our units are readonly, this means that they
    /// cannot be used in unity serialized fields which also prevents them from appearing in inspector drawers.
    /// This wrapper enables this functionality by not being readonly and can be converted to and from a normal
    /// unit value. The mutability of this type also reflects the fact that unity-serialized fields can be mutated
    /// at runtime through the inspector in an editor instance.
    [Serializable]
    public struct UnitValueProxy<TValue>
    where TValue : struct, IUnitValue<TValue> {

        // Our units used to be serialized directly and all of them had an individually-named private field
        // to hold the SI value. Since many prefabs were created in that time, they still hold the value
        // with that name. In order to be able to deserialize these, we must collect all the old field names:
        [FormerlySerializedAs("_amount")] 
        [FormerlySerializedAs("_radians")] 
        [FormerlySerializedAs("_radiansPerSecond")] 
        [FormerlySerializedAs("_sqrMeter")] 
        [FormerlySerializedAs("_seconds")] 
        [FormerlySerializedAs("_newton")] 
        [FormerlySerializedAs("_meter")] 
        [FormerlySerializedAs("_kilograms")] 
        [FormerlySerializedAs("_kilogramsPerMeter")] 
        [FormerlySerializedAs("_number")] [FormerlySerializedAs("_Number")] 
        [FormerlySerializedAs("_metersPerSecond")] [FormerlySerializedAs("_MetersPerSecond")] 
        [FormerlySerializedAs("_cubicMeter")] 
        [FormerlySerializedAs("_cubicMetersPerSecond")]
        [SerializeField] private double _siValue;
        
        public double SIValue => _siValue;

        public UnitValueProxy(double siValue) => _siValue = siValue;
        public UnitValueProxy(TValue val) => _siValue = val.SiValue;
        
        public TValue Value => Units.FromSiValue<TValue>(_siValue);

    }

    public static class UnitValueProxyExtensions {

        public static UnitValueProxy<TValue> AsProxy<TValue>(this TValue value)
        where TValue : struct, IUnitValue<TValue> => new(value);

    }
    
}