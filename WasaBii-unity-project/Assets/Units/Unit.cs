using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using BII.WasaBii.Core;

namespace BII.WasaBii.Units {
    
    public abstract class Unit : IEquatable<Unit> {
        public readonly string DisplayName;
        public readonly double Factor;

        protected Unit(string displayName, double factor) {
            DisplayName = displayName;
            Factor = factor;
        }

        public bool Equals(Unit other) {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            // All Unit subtypes use this Equals method so it returns false if the unit is of another subtype than this.
            if (this.GetType() != other.GetType()) return false;
            return DisplayName == other.DisplayName && Factor.Equals(other.Factor);
        }

        public override bool Equals(object obj) => obj is Unit other && Equals(other);

        public override int GetHashCode() {
            unchecked {
                return ((DisplayName != null ? DisplayName.GetHashCode() : 0) * 397) ^ Factor.GetHashCode();
            }
        }

        public static bool operator ==(Unit left, Unit right) => Equals(left, right);
        public static bool operator !=(Unit left, Unit right) => !Equals(left, right);
    }
    
    [MustBeSerializable] public interface ValueWithUnit {
        double SIValue { get; } 
        
        // It's not really this type's responsibility to know these units.
        // They are here to enforce that they exist for every ValueWithUnit.
        // TODO for WasaBii: Implement properly with attributes and stuff.
        IReadOnlyList<Unit> AllUnits { get; }
        Unit DisplayUnit { get; }
        Unit SIUnit { get; }
    }

    [MustBeSerializable] public interface ValueWithUnit<out T> : ValueWithUnit where T : Unit {
        // Used for fine-grained pattern matching with static metadata in UnitUtils.
        new IReadOnlyList<T> AllUnits { get; }
        new T DisplayUnit { get; }
        new T SIUnit { get; }

        // It's not really this type's responsibility to know these units.
        // They are here to enforce that they exist for every ValueWithUnit.
        // TODO for WasaBii: Implement properly with attributes and stuff.
        IReadOnlyList<Unit> ValueWithUnit.AllUnits => AllUnits;
        Unit ValueWithUnit.DisplayUnit => DisplayUnit;
        Unit ValueWithUnit.SIUnit => SIUnit;
    }

    [MustBeSerializable] public interface CopyableValueWithUnit : ValueWithUnit {
        [Pure] CopyableValueWithUnit CopyWithDifferentSIValue(double newSIValue);
    }
    
    // Used for utilities that do not require or cannot infer the unit type.
    [MustBeSerializable] 
    public interface CopyableValueWithUnit<out TSelf> : CopyableValueWithUnit where TSelf : struct, CopyableValueWithUnit<TSelf> {
        [Pure] new TSelf CopyWithDifferentSIValue(double newSIValue);
    }

    [MustBeSerializable]
    public interface ValueWithUnit<TSelf, TUnit> : CopyableValueWithUnit<TSelf>, ValueWithUnit<TUnit>, IEquatable<TSelf>, IComparable<TSelf>
    where TUnit : Unit
    where TSelf : struct, ValueWithUnit<TSelf, TUnit> {
    }

    public static class UnitUtils {

        [Pure] public static Number As(this ValueWithUnit value, Unit unit) => (value.SIValue / unit.Factor).Number();
        
        [Pure] public static Number As<TValue, TUnit>(this TValue value, TUnit unit) 
        where TValue : struct, ValueWithUnit<TValue, TUnit>
        where TUnit : Unit
            => (value.SIValue / unit.Factor).Number();
        
        [Pure] public static TSelf FromSI<TSelf>(double siValue)
        where TSelf: struct, CopyableValueWithUnit<TSelf>
            => default(TSelf).CopyWithDifferentSIValue(siValue);
        
        [Pure] public static TSelf From<TSelf, TUnit>(double value, TUnit unit)
        where TUnit : Unit
        where TSelf: struct, ValueWithUnit<TSelf, TUnit>
            => FromSI<TSelf>(value * unit.Factor);

        /// Returns the unit which leads to the smallest possible value not less than 1 when applied.
        /// If no unit yields a value >= 1, the unit with the greatest value is returned.
        /// Only <see cref="allowedUnits"/> will be considered if it is not null.
        /// These units will be sorted by their factor. In a performance critical context,
        /// you may want to pass an already sorted list. In this case, pass `true` for <see cref="areUnitsSorted"/>.
        [Pure] public static TUnit MostFittingDisplayUnitFor<TUnit>(ValueWithUnit<TUnit> value, IReadOnlyList<TUnit> allowedUnits = null, bool areUnitsSorted = false) 
        where TUnit : Unit {
            Contract.Assert(!(areUnitsSorted && allowedUnits == null));
            allowedUnits = allowedUnits?.If(!areUnitsSorted, 
                units => units.SortedBy(u => u.Factor).AsReadOnlyList()
            ) ?? value.AllUnits;
            Contract.Assert(allowedUnits.Any());
            var displayUnit = allowedUnits[0];
            foreach (var unit in allowedUnits.Skip(1)) {
                var displayValue = value.As(unit);
                if (displayValue < 1) break;
                else displayUnit = unit;
            }

            return displayUnit;
        }
        
        public class UnsupportedHasUnitException : Exception {
            public UnsupportedHasUnitException(Type hasUnitType)
                : base($"The HasUnit {hasUnitType.Name} is not supported by the UnitUtils") { }
        }

        [Pure] public static TSelf Min<TSelf>(
            TSelf first, params TSelf[] values
        )
        where TSelf : struct, CopyableValueWithUnit<TSelf> =>
            FromSI<TSelf>(Mathd.Min(values.Prepend(first).Select(v => v.SIValue).ToArray()));

        [Pure] public static TSelf Max<TSelf>(
            TSelf first, params TSelf[] values
        ) 
        where TSelf : struct, CopyableValueWithUnit<TSelf> =>
            FromSI<TSelf>(Mathd.Max(values.Prepend(first).Select(v => v.SIValue).ToArray()));

        [Pure] public static TSelf Clamp<TSelf>(TSelf value, TSelf min, TSelf max)
        where TSelf : struct, CopyableValueWithUnit<TSelf> =>
            Max(Min(value, max), min);

        [Pure] public static TSelf Lerp<TSelf>(
            TSelf from,
            TSelf to,
            double progress,
            bool shouldClamp = true
        )
        where TSelf : struct, CopyableValueWithUnit<TSelf> =>
            FromSI<TSelf>(
                shouldClamp 
                    ? Mathd.Lerp(from.SIValue, to.SIValue, progress)
                    : Mathd.LerpUnclamped(from.SIValue, to.SIValue, progress));

        [Pure] public static double InverseLerp<TSelf>(
            TSelf from,
            TSelf to,
            TSelf value,
            bool shouldClamp = true
        )
        where TSelf : struct, CopyableValueWithUnit<TSelf> =>
            Mathd.InverseLerp(from.SIValue, to.SIValue, value.SIValue, shouldClamp);

        public static TSelf RandomRange<TSelf>(
            TSelf min,
            TSelf max
        )
        where TSelf : struct, CopyableValueWithUnit<TSelf> =>
            FromSI<TSelf>(DRandom.Range(min.SIValue, max.SIValue));

        /// <param name="progressExponent">Defines how the samples are distributed.
        /// A value between 0 and 1 will shift the samples towards <see cref="to"/>,
        /// values grater than 1 will shift them closer to <see cref="from"/>.
        /// 1 or `null` means uniform distribution.
        /// Should never be 0 or less unless you want crazy extrapolation.</param>
        [Pure] public static IEnumerable<TSelf> SampleLinearInterpolation<TSelf>(
            TSelf from,
            TSelf to,
            int sampleCount,
            double? progressExponent = null
        )
        where TSelf : struct, CopyableValueWithUnit<TSelf> {
            Contract.Assert(sampleCount >= 2, $"At least 2 sample points are needed. {sampleCount} were requested.");
            for (double i = 0; i < sampleCount; i++) {
                yield return Lerp(
                    from, 
                    to, 
                    (i / (sampleCount - 1)).If(
                        progressExponent != null, 
                        p => Math.Pow(p, progressExponent.Value)
                    )
                );
            }
        }
        
        [Pure] public static string Format<TUnit>(this ValueWithUnit<TUnit> value, TUnit unit, int digits, RoundingMode roundingMode, double zeroThreshold = 1E-5f)
        where TUnit : Unit {
            var doubleValue = (double)value.As(unit);
            var fractalDigits = 0;
            switch (roundingMode) {
                case RoundingMode.DecimalPlaces:
                    fractalDigits = digits;
                    break;
                case RoundingMode.SignificantDigits:
                    if(!doubleValue.IsNearly(0, zeroThreshold))
                        fractalDigits = Math.Max(0, digits - doubleValue.PositionOfFirstSignificantDigit() - 1);
                    break;
                default: throw new UnsupportedEnumValueException(roundingMode, "ValueWithUnit formatting");
            }
            // e.g. 0.000 for 3 fractal digits
            var formatSpecifier = string.Concat(Enumerable.Repeat("0", fractalDigits).Prepend("0."));
            
            return $"{doubleValue.Round(digits, roundingMode).ToString(formatSpecifier)} {unit.DisplayName}";
        }

        [Pure] public static string Format<TUnit>(
            this ValueWithUnit<TUnit> value, int digits, RoundingMode roundingMode, double zeroThreshold = 1E-5f
        )
        where TUnit : Unit => value.Format(
            MostFittingDisplayUnitFor(value),
            digits,
            roundingMode,
            zeroThreshold
        );

        [Pure] public static TSelf Abs<TSelf>(this TSelf val)
        where TSelf : struct, CopyableValueWithUnit<TSelf> 
            => FromSI<TSelf>(Math.Abs(val.SIValue));

        [Pure] public static TSelf NegateIf<TSelf>(this TSelf self, bool shouldNegate)
        where TSelf : struct, CopyableValueWithUnit<TSelf> 
            => FromSI<TSelf>(self.SIValue.NegateIf(shouldNegate));

        [Pure] public static TSelf Sum<TSelf>(this IEnumerable<TSelf> enumerable)
        where TSelf : struct, CopyableValueWithUnit<TSelf>
            => FromSI<TSelf>(enumerable.Aggregate(0.0, (sum, e) => sum + e.SIValue));

        [Pure] public static TValue Sum<TSelf, TValue>(this IEnumerable<TSelf> enumerable, Func<TSelf, TValue> func)
        where TValue : struct, CopyableValueWithUnit<TValue> => enumerable.Select(func).Sum();

        
        [Pure] public static ValueWithUnit CreateWithValue(this Unit unit, double value) {
            return unit switch {
                AmountUnit au => new Amount(value, au),
                AngleUnit au => new Angle(value, au),
                AnglePerDurationUnit apdu => new AnglePerDuration(value, apdu),
                AreaUnit au => new Area(value, au),
                TimeUnit tu => new Duration(value, tu),
                ForceUnit fu => new Force(value, fu),
                LengthUnit lu => new Length(value, lu),
                MassUnit mu => new Mass(value, mu),
                MassPerLengthUnit mplu => new MassPerLength(value, mplu),
                NumberUnit nu => new Number(value, nu),
                VelocityUnit vlu => new Velocity(value, vlu),
                VolumeUnit vu => new Volume(value, vu),
                VolumePerDurationUnit vpdu => new VolumePerDuration(value, vpdu),
                _ => throw new UnsupportedHasUnitException(unit.GetType())
            };
        }

        [Pure] public static bool IsNearly<TSelf>(this TSelf value, TSelf other, TSelf? threshold = null)
        where TSelf : struct, CopyableValueWithUnit<TSelf>
            => value.SIValue.IsNearly(other.SIValue, threshold?.SIValue ?? double.Epsilon);
        
        [Pure] public static TSelf RoundToWholeMultipleOf<TSelf>(this TSelf value, TSelf factor)
        where TSelf : struct, CopyableValueWithUnit<TSelf>
            => value.CopyWithDifferentSIValue(Math.Round(value.SIValue / factor.SIValue) * factor.SIValue);
        
        [Pure]
        public static T Average<T>(this IEnumerable<T> enumerable) where T : struct, CopyableValueWithUnit<T> {
            var (head, tail) = enumerable;
            return head.CopyWithDifferentSIValue(head.PrependTo(tail).Select(t => t.SIValue).Average());
        }
        
    }
}