using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using BII.WasaBii.Core;

namespace BII.WasaBii.UnitSystem {

    public static class Units {
        
        // Construction and metadata

        public static TValue From<TValue, TUnit>(double value, TUnit unit)
        where TUnit : IUnit where TValue: struct, IUnitValue<TValue, TUnit> => 
            FromSiValue<TValue>(value * unit.SiFactor);
        
        public static TValue FromSiValue<TValue>(double value) where TValue : IUnitValue, new() =>
            new TValue {SiValue = value};

        public static double As<TValue, TUnit>(this TValue value, TUnit unit)
            where TValue : struct, IUnitValue<TValue, TUnit> where TUnit : IUnit => value.SiValue * unit.InverseSiFactor;

        public static double As(this IUnitValue value, IUnit unit) {
            if (!value.UnitType.IsInstanceOfType(unit))
                throw new ArgumentException(
                    $"The type of the target unit does not match the type of the value's unit type. " +
                    $"Expected {value.UnitType} but was {unit}");
            return value.SiValue * unit.InverseSiFactor;
        }

        private static IUnitDescription<TUnit> unitDescriptionOf<TUnit>() where TUnit : IUnit =>
            Activator.CreateInstance(
                typeof(TUnit).GetCustomAttribute<UnitMetadataAttribute>()?.UnitDescriptionType
                ?? throw new ArgumentException($"{typeof(TUnit)} needs an attribute of type {nameof(UnitMetadataAttribute)}!")
            ) as IUnitDescription<TUnit>
            ?? throw new ArgumentException($"The {nameof(UnitMetadataAttribute.UnitDescriptionType)} of the {nameof(UnitMetadataAttribute)} " +
                                           $"must be of type {typeof(IUnitDescription<TUnit>)}.");

        public static TUnit SiUnitOf<TUnit>() where TUnit : IUnit => unitDescriptionOf<TUnit>().SiUnit;
        
        public static IReadOnlyList<TUnit> AllUnitsOf<TUnit>() where TUnit : IUnit => unitDescriptionOf<TUnit>().AllUnits;
        
        // Basic maths
        
        public static TSelf Abs<TSelf>(this TSelf val)
            where TSelf : struct, IUnitValue<TSelf> 
            => FromSiValue<TSelf>(Math.Abs(val.SiValue));

        public static TSelf NegateIf<TSelf>(this TSelf self, bool shouldNegate)
            where TSelf : struct, IUnitValue<TSelf> 
            => FromSiValue<TSelf>(self.SiValue.NegateIf(shouldNegate));

        public static TSelf Sum<TSelf>(this IEnumerable<TSelf> enumerable)
            where TSelf : struct, IUnitValue<TSelf>
            => FromSiValue<TSelf>(enumerable.Aggregate(0.0, (sum, e) => sum + e.SiValue));

        public static TValue Sum<TSelf, TValue>(this IEnumerable<TSelf> enumerable, Func<TSelf, TValue> func)
            where TValue : struct, IUnitValue<TValue> => enumerable.Select(func).Sum();
        
        public static bool IsNearly<TSelf>(this TSelf value, TSelf other, TSelf? threshold = null)
            where TSelf : struct, IUnitValue<TSelf>
            => value.SiValue.IsNearly(other.SiValue, threshold?.SiValue ?? double.Epsilon);

        public static TSelf Average<TSelf>(this IEnumerable<TSelf> values)
            where TSelf : struct, IUnitValue<TSelf>
            => new TSelf {SiValue = values.Average(v => v.SiValue)};
        
        public static TSelf Min<TSelf>(
            TSelf first, params TSelf[] values
        ) where TSelf : struct, IUnitValue<TSelf> =>
            FromSiValue<TSelf>(MathD.Min(values.Prepend(first).Select(v => v.SiValue).ToArray()));

        public static TSelf Max<TSelf>(
            TSelf first, params TSelf[] values
        ) where TSelf : struct, IUnitValue<TSelf> =>
            FromSiValue<TSelf>(MathD.Max(values.Prepend(first).Select(v => v.SiValue).ToArray()));
        
        // Interpolation
        
        public static TSelf Clamp<TSelf>(TSelf value, TSelf min, TSelf max)
            where TSelf : struct, IUnitValue<TSelf> =>
            Max(Min(value, max), min);

        public static TSelf Lerp<TSelf>(
            TSelf from,
            TSelf to,
            double progress,
            bool shouldClamp = true
        ) where TSelf : struct, IUnitValue<TSelf> =>
            FromSiValue<TSelf>(from.SiValue.Lerp(to.SiValue, progress, shouldClamp));
        
        public static TSelf LerpTo<TSelf>(
            this TSelf from,
            TSelf to,
            double progress,
            bool shouldClamp = true
        ) where TSelf : struct, IUnitValue<TSelf> => Lerp(from, to, progress, shouldClamp);
        
        public static double InverseLerp<TSelf>(
            TSelf from,
            TSelf to,
            TSelf value,
            bool shouldClamp = true
        ) where TSelf : struct, IUnitValue<TSelf> =>
            MathD.InverseLerp(from.SiValue, to.SiValue, value.SiValue, shouldClamp);
        
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
        ) where TSelf : struct, IUnitValue<TSelf> {
            if (sampleCount < 2) 
                throw new ArgumentException($"At least 2 sample points are needed. {sampleCount} were requested.");
            
            for (double i = 0; i < sampleCount; i++) {
                yield return Lerp(
                    from, 
                    to, 
                    (i / (sampleCount - 1)).If(
                        progressExponent != null, 
                        p => Math.Pow(p, progressExponent!.Value)
                    )
                );
            }
        }
        
        // Formatting
        
        /// Returns the unit which leads to the smallest possible value not less than 1 when applied.
        /// If no unit yields a value >= 1, the unit with the greatest value is returned.
        /// Only <see cref="allowedUnits"/> will be considered if it is not null.
        /// These units will be sorted by their factor. In a performance critical context,
        /// you may want to pass an already sorted list. In this case, pass `true` for <see cref="areUnitsSorted"/>.
        public static TUnit MostFittingDisplayUnitFor<TUnit>(
            IUnitValueOf<TUnit> value, 
            IEnumerable<TUnit> allowedUnits, 
            bool areUnitsSorted = false
        ) where TUnit : IUnit {
            var allowed = allowedUnits?.If(!areUnitsSorted, 
                units => (IEnumerable<TUnit>) units.OrderBy(u => u.SiFactor)
            ).AsReadOnlyList() ?? AllUnitsOf<TUnit>();

            if (!allowed.Any()) throw new ArgumentException($"No allowed units given for {typeof(TUnit)}.");
            
            var displayUnit = allowed[0];
            foreach (var unit in allowed.Skip(1)) {
                var displayValue = value.As(unit);
                if (displayValue < 1) break;
                else displayUnit = unit;
            }

            return displayUnit;
        }

        /// Returns the unit which leads to the smallest possible value not less than 1 when applied.
        /// If no unit yields a value >= 1, the unit with the greatest value is returned.
        public static TUnit MostFittingDisplayUnitFor<TUnit>(
            IUnitValueOf<TUnit> value
        ) where TUnit : IUnit => MostFittingDisplayUnitFor(value, AllUnitsOf<TUnit>(), areUnitsSorted: false);

        // TODO CR for maintainer: provide default format with most fitting unit & implement analyzer to prefer this over .ToString()
        public static string Format<TUnit>(
            this IUnitValueOf<TUnit> value, 
            TUnit unit, 
            int digits, 
            RoundingMode roundingMode, 
            double zeroThreshold = 1E-5f
        ) where TUnit : IUnit {
            var doubleValue = value.As(unit);
            var fractalDigits = 0;
            switch (roundingMode) {
                case RoundingMode.DecimalPlaces:
                    fractalDigits = digits;
                    break;
                case RoundingMode.SignificantDigits:
                    if(!doubleValue.IsNearly(0, zeroThreshold))
                        fractalDigits = Math.Max(0, digits - doubleValue.PositionOfFirstSignificantDigit() - 1);
                    break;
                default: throw new UnsupportedEnumValueException(roundingMode);
            }
            // e.g. 0.000 for 3 fractal digits
            var formatSpecifier = string.Concat(Enumerable.Repeat("0", fractalDigits).Prepend("0."));
            
            return $"{doubleValue.Round(digits, roundingMode).ToString(formatSpecifier)} {unit.ShortName}";
        }

        public static string Format<TUnit>(
            this IUnitValueOf<TUnit> value, int digits, RoundingMode roundingMode, double zeroThreshold = 1E-5f
        ) where TUnit : IUnit => value.Format(
            MostFittingDisplayUnitFor(value),
            digits,
            roundingMode,
            zeroThreshold
        );
    }

}