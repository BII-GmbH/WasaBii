using System.Reflection;

namespace BII.WasaBii.Units;

public interface UnitValue {
    double SiValue { init; get; }
    Type UnitType { get; }
}

public interface UnitValue<out T> : UnitValue where T : Unit { }

public interface UnitValue<TSelf, out TUnit> 
    : IComparable<TSelf>, IEquatable<TSelf>, UnitValue<TUnit> 
    where TSelf : struct where TUnit : Unit { }
