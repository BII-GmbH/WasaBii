using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using BII.Utilities.Independent;
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
    
    public interface ValueWithUnit {
        double SIValue { get; } 
    }

    public interface ValueWithUnit<T> : ValueWithUnit where T : Unit {
        // Used for fine-grained pattern matching with static metadata in UnitUtils.
    }

    public interface CopyableValueWithUnit : ValueWithUnit {
        CopyableValueWithUnit CopyWithDifferentSIValue(double newSIValue);
    }
    
    // Used for utilities that do not require or cannot infer the unit type.
    public interface CopyableValueWithUnit<out TSelf> : CopyableValueWithUnit where TSelf : struct, CopyableValueWithUnit<TSelf> {
        new TSelf CopyWithDifferentSIValue(double newSIValue);
    }

    public interface ValueWithUnit<TSelf, TUnit> : CopyableValueWithUnit<TSelf>, ValueWithUnit<TUnit>, IEquatable<TSelf>, IComparable<TSelf>
    where TUnit : Unit
    where TSelf : struct, ValueWithUnit<TSelf, TUnit> {}

    public static class UnitUtils {

        public static Number As(this ValueWithUnit value, Unit unit) => (value.SIValue / unit.Factor).Number();
        
        public static TSelf FromSI<TSelf>(double siValue)
        where TSelf: struct, CopyableValueWithUnit<TSelf>
            => default(TSelf).CopyWithDifferentSIValue(siValue);
        
        public static TSelf From<TSelf, TUnit>(double value, TUnit unit)
        where TUnit : Unit
        where TSelf: struct, ValueWithUnit<TSelf, TUnit>
            => FromSI<TSelf>(value * unit.Factor);

        public static IReadOnlyList<Unit> AllUnitsFor(Type t) {
            if (typeof(ValueWithUnit<TimeUnit>).IsAssignableFrom(t))
                return TimeUnit.All;
            if (typeof(ValueWithUnit<AngleUnit>).IsAssignableFrom(t))
                return AngleUnit.All;
            if (typeof(ValueWithUnit<LengthUnit>).IsAssignableFrom(t))
                return LengthUnit.All;
            if (typeof(ValueWithUnit<MassUnit>).IsAssignableFrom(t))
                return MassUnit.All;
            if (typeof(ValueWithUnit<VelocityUnit>).IsAssignableFrom(t))
                return VelocityUnit.All;
            if (typeof(ValueWithUnit<VolumeUnit>).IsAssignableFrom(t))
                return VolumeUnit.All;

            throw new UnsupportedHasUnitException(t);
        }
        public static IReadOnlyList<U> AllUnits<U>() where  U : Unit=> (IReadOnlyList<U>) AllUnitsFor(typeof(ValueWithUnit<U>));

        public static IReadOnlyList<Unit> AllUnits(this ValueWithUnit value) => AllUnitsFor(value.GetType());

        public static Unit DisplayUnitFor(Type t) {
            if (typeof(ValueWithUnit<TimeUnit>).IsAssignableFrom(t))
                return TimeUnit.Seconds;
            if (typeof(ValueWithUnit<AngleUnit>).IsAssignableFrom(t))
                return AngleUnit.Degrees;
            if (typeof(ValueWithUnit<LengthUnit>).IsAssignableFrom(t))
                return LengthUnit.Meter;
            if (typeof(ValueWithUnit<MassUnit>).IsAssignableFrom(t))
                return MassUnit.Kilograms;
            if (typeof(ValueWithUnit<VelocityUnit>).IsAssignableFrom(t))
                return VelocityUnit.KilometersPerHour;
            if (typeof(ValueWithUnit<VolumeUnit>).IsAssignableFrom(t))
                return VolumeUnit.CubicMeter;
            if (typeof(ValueWithUnit<AmountUnit>).IsAssignableFrom(t))
                return AmountUnit.Amount;
            if (typeof(ValueWithUnit<VolumePerDurationUnit>).IsAssignableFrom(t))
                return VolumePerDurationUnit.CubicMetersPerSecond;

            throw new UnsupportedHasUnitException(t);
        }

        public static U DisplayUnit<U>() where U : Unit => (U) DisplayUnitFor(typeof(ValueWithUnit<U>));
        
        public static Unit DisplayUnit(this ValueWithUnit value) => DisplayUnitFor(value.GetType());

        public static Unit SIUnitFor(Type t) {
            if (typeof(ValueWithUnit<TimeUnit>).IsAssignableFrom(t))
                return TimeUnit.Seconds;
            if (typeof(ValueWithUnit<AngleUnit>).IsAssignableFrom(t))
                return AngleUnit.Radians;
            if (typeof(ValueWithUnit<LengthUnit>).IsAssignableFrom(t))
                return LengthUnit.Meter;
            if (typeof(ValueWithUnit<MassUnit>).IsAssignableFrom(t))
                return MassUnit.Kilograms;
            if (typeof(ValueWithUnit<VelocityUnit>).IsAssignableFrom(t))
                return VelocityUnit.MetersPerSecond;
            if (typeof(ValueWithUnit<VolumeUnit>).IsAssignableFrom(t))
                return VolumeUnit.CubicMeter;
            if (typeof(ValueWithUnit<VolumePerDurationUnit>).IsAssignableFrom(t))
                return VolumePerDurationUnit.CubicMetersPerSecond;

            throw new UnsupportedHasUnitException(t);
        }
        
        public static U SIUnit<U>() where U : Unit => (U) SIUnitFor(typeof(ValueWithUnit<U>));

        /// Returns the unit which leads to the smallest possible value not less than 1 when applied.
        /// If no unit yields a value >= 1, the unit with the greatest value is returned.
        /// Only <see cref="allowedUnits"/> will be considered if it is not null.
        /// These units will be sorted by their factor. In a performance critical context,
        /// you may want to pass an already sorted list. In this case, pass `true` for <see cref="areUnitsSorted"/>.
        public static TUnit MostFittingDisplayUnitFor<TUnit>(ValueWithUnit<TUnit> value, IReadOnlyList<TUnit> allowedUnits = null, bool areUnitsSorted = false) 
        where TUnit : Unit {
            Contract.Assert(!(areUnitsSorted && allowedUnits == null));
            allowedUnits = allowedUnits?.If(!areUnitsSorted, 
                units => units.SortedBy(u => u.Factor).AsReadOnlyList()
            ) ?? AllUnits<TUnit>();
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

        public static TSelf Min<TSelf>(
            TSelf first, params TSelf[] values
        )
        where TSelf : struct, CopyableValueWithUnit<TSelf> =>
            FromSI<TSelf>(Mathd.Min(values.Prepend(first).Select(v => v.SIValue).ToArray()));

        public static TSelf Max<TSelf>(
            TSelf first, params TSelf[] values
        ) 
        where TSelf : struct, CopyableValueWithUnit<TSelf> =>
            FromSI<TSelf>(Mathd.Max(values.Prepend(first).Select(v => v.SIValue).ToArray()));

        public static TSelf Lerp<TSelf>(
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

        public static double InverseLerp<TSelf>(
            TSelf from,
            TSelf to,
            TSelf value
        )
        where TSelf : struct, CopyableValueWithUnit<TSelf> =>
            Mathd.InverseLerp(from.SIValue, to.SIValue, value.SIValue);

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
        public static IEnumerable<TSelf> SampleLinearInterpolation<TSelf>(
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
        
        public static string Format<TUnit>(this ValueWithUnit<TUnit> value, TUnit unit, int digits, RoundingMode roundingMode, double zeroThreshold = 1E-5f)
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

        public static string Format<TUnit>(
            this ValueWithUnit<TUnit> value, int digits, RoundingMode roundingMode, double zeroThreshold = 1E-5f
        )
        where TUnit : Unit => value.Format(
            MostFittingDisplayUnitFor(value),
            digits,
            roundingMode,
            zeroThreshold
        );

        public static TSelf Abs<TSelf>(this TSelf val)
        where TSelf : struct, CopyableValueWithUnit<TSelf> 
            => FromSI<TSelf>(Math.Abs(val.SIValue));

        public static TSelf NegateIf<TSelf>(this TSelf self, bool shouldNegate)
        where TSelf : struct, CopyableValueWithUnit<TSelf> 
            => FromSI<TSelf>(self.SIValue.NegateIf(shouldNegate));

        public static TSelf Sum<TSelf>(this IEnumerable<TSelf> enumerable)
        where TSelf : struct, CopyableValueWithUnit<TSelf>
            => FromSI<TSelf>(enumerable.Aggregate(0.0, (sum, e) => sum + e.SIValue));
        
        public static ValueWithUnit CreateWithValue(this Unit unit, double value) {
            switch (unit) {
                case TimeUnit tu:      return new Duration(value, tu);
                case AngleUnit au:     return new Angle(value, au);
                case LengthUnit lu:    return new Length(value, lu);
                case MassUnit mu:      return new Mass(value, mu);
                case VelocityUnit vlu: return new Velocity(value, vlu);
                case VolumeUnit vu:    return new Volume(value, vu);
                case AmountUnit qu:    return new Amount(value, qu);
                case VolumePerDurationUnit vu: return new VolumePerDuration(value, vu);
                default:               throw new UnsupportedHasUnitException(unit.GetType());
            }
        }

        public static bool IsNearly<TSelf>(this TSelf value, TSelf other, TSelf? threshold = null)
        where TSelf : struct, CopyableValueWithUnit<TSelf>
            => value.SIValue.IsNearly(other.SIValue, threshold?.SIValue ?? double.Epsilon);
        
    }
}