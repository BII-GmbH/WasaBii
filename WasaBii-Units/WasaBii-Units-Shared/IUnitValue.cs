namespace BII.WasaBii.Units;

public interface IUnitValue {
    double SiValue { init; get; }
    Type UnitType { get; }
}

public interface IUnitValue<out T> : IUnitValue where T : IUnit { }

public interface IUnitValue<TSelf, out TUnit> 
    : IComparable<TSelf>, IEquatable<TSelf>, IUnitValue<TUnit> 
    where TSelf : struct where TUnit : IUnit { }
