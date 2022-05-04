namespace BII.WasaBii.Units;

public interface IUnitValue {
    double SiValue { init; get; }
    Type UnitType { get; }
}

public interface IUnitValueOf<out TUnit> : IUnitValue where TUnit : IUnit { }

public interface IUnitValue<TSelf> : IUnitValue where TSelf : struct, IUnitValue<TSelf> { }

public interface IUnitValue<TSelf, out TUnit> 
    : IComparable<TSelf>, IEquatable<TSelf>, IUnitValue<TSelf>, IUnitValueOf<TUnit>
    where TSelf : struct, IUnitValue<TSelf> where TUnit : IUnit { }

/// <summary>
/// Generic value of some unit type. Can be used in conjunction with the extensions in
///   <see cref="UnitMulDivExtensions"/> to type-safely work over any combination of units,
///   even if the concrete unit you need has not been generated yet.
/// </summary>
/// <seealso cref="IUnitValue{TSelf, TUnit}"/>
public readonly struct UnitValueOf<TUnit> : IUnitValue<UnitValueOf<TUnit>, TUnit> where TUnit : IUnit {
    public double SiValue { get; init; }
    public Type UnitType => typeof(TUnit);

    public UnitValueOf(double value, TUnit unit) => SiValue = value * unit.SiFactor;

    public int CompareTo(UnitValueOf<TUnit> other) => SiValue.CompareTo(other.SiValue);
    public bool Equals(UnitValueOf<TUnit> other) => SiValue.Equals(other.SiValue);
    
    public static bool operator ==(UnitValueOf<TUnit> left, UnitValueOf<TUnit> right) => Equals(left, right);
    public static bool operator !=(UnitValueOf<TUnit> left, UnitValueOf<TUnit> right) => !Equals(left, right);

    public override bool Equals(object obj) => obj is UnitValueOf<TUnit> other && Equals(other);

    // We include this type in case values of different units are hashed in the same collection
    public override int GetHashCode() => HashCode.Combine(this.SiValue, typeof(UnitValueOf<TUnit>));

    public static UnitValueOf<TUnit> operator +(UnitValueOf<TUnit> first, UnitValueOf<TUnit> second) => new UnitValueOf<TUnit> { SiValue = first.SiValue + second.SiValue };
    public static UnitValueOf<TUnit> operator -(UnitValueOf<TUnit> first, UnitValueOf<TUnit> second) => new UnitValueOf<TUnit> { SiValue = first.SiValue - second.SiValue };

    public static UnitValueOf<TUnit> operator*(UnitValueOf<TUnit> a, double s) => new UnitValueOf<TUnit> { SiValue = a.SiValue * s };
    public static UnitValueOf<TUnit> operator*(UnitValueOf<TUnit> a, float s) => new UnitValueOf<TUnit> { SiValue = a.SiValue * s };
    
    public static UnitValueOf<TUnit> operator*(double s, UnitValueOf<TUnit> a) => new UnitValueOf<TUnit> { SiValue = a.SiValue * s };
    public static UnitValueOf<TUnit> operator*(float s, UnitValueOf<TUnit> a) => new UnitValueOf<TUnit> { SiValue = a.SiValue * s };

    public static UnitValueOf<TUnit> operator/(UnitValueOf<TUnit> a, double s) => new UnitValueOf<TUnit> { SiValue = a.SiValue / s };
    public static UnitValueOf<TUnit> operator/(UnitValueOf<TUnit> a, float s) => new UnitValueOf<TUnit> { SiValue = a.SiValue / s };
}