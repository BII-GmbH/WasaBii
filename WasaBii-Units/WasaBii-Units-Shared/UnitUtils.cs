using System.Diagnostics.Contracts;
using System.Reflection;

namespace BII.WasaBii.Units;

public static class Units {
    public static TValue FromSiValue<TValue>(double value) where TValue : IUnitValue, new() => new TValue {SiValue = value};

    public static double As<TValue, TUnit>(this TValue value, TUnit unit)
        where TValue : IUnitValue<TUnit> where TUnit : IUnit => value.SiValue * unit.SiFactor;

    public static double As(this IUnitValue value, IUnit unit) {
        Contract.Assert(value.UnitType.IsInstanceOfType(unit));
        return value.SiValue * unit.SiFactor;
    }

    public static TUnit SiUnitOf<TUnit>() where TUnit : IUnit =>
        (Activator.CreateInstance(
            typeof(TUnit).GetCustomAttribute<UnitMetadataAttribute>()?.UnitDescriptionType 
                ?? throw new ArgumentException($"Cannot call .{nameof(SiUnitOf)}: " +
                    $"{typeof(TUnit)} needs an attribute of type {nameof(UnitMetadataAttribute)}.")
        ) as IUnitDescription<TUnit> 
            ?? throw new ArgumentException($"Cannot call .{nameof(SiUnitOf)}: " +
                $"The {nameof(UnitMetadataAttribute.UnitDescriptionType)} of the {nameof(UnitMetadataAttribute)} " +
                $"must be of type {typeof(IUnitDescription<TUnit>)}.")
        ).SiUnit;

    // TODO: the other unit utilities from dProB...
}