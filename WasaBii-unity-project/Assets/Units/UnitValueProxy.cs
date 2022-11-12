// Cannot be in a Unity assembly because the generated editors in WasaBii-Units need to access this.
#if UNITY_5_3_OR_NEWER

using System;
using BII.WasaBii.UnitSystem;
using UnityEngine;

namespace BII.WasaBii.Unity {

    /// <summary>
    /// Unity is not capable of serializing readonly structs. Since our units are readonly, this means that
    /// they cannot be used in unity serialized fields which also prevents them from appearing in inspector
    /// drawers. This wrapper enables this functionality by not being readonly and can be converted to and
    /// from a normal unit value. The mutability of this type also reflects the fact that unity-serialized
    /// fields can be mutated at runtime through the inspector in an editor instance.
    /// </summary>
    [Serializable]
    public struct UnitValueProxy<TValue>
    where TValue : struct, IUnitValue<TValue> {
        
        [SerializeField] private double _siValue;
        
        public double SiValue => _siValue;

        public UnitValueProxy(double siValue) => _siValue = siValue;
        public UnitValueProxy(TValue val) => _siValue = val.SiValue;
        
        public TValue Value => new() {SiValue = _siValue};
    }

}

#endif