using System;

namespace BII.WasaBii.Core {
    
    public class UnsupportedEnumValueException : Exception {
        public UnsupportedEnumValueException(Enum value, string context)
            : base($"The {value.GetType().Name} value {value} is not supported in {context}") { }
    }
    
}