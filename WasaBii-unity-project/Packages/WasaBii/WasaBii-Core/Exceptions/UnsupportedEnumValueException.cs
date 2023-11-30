using System;

namespace BII.WasaBii.Core {
    
    public class UnsupportedEnumValueException : Exception {
        public readonly Enum Value;

        public UnsupportedEnumValueException(Enum value) : base(
            $"The {value.GetType().Name} value {value} ({Convert.ToInt32(value)}) is not supported"
        ) => Value = value;
    }
    
}