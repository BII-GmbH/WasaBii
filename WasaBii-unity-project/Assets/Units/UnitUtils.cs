﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;

using BII.WasaBii.Core;

namespace BII.WasaBii.Units {

    public static class UnitUtils {
        
        // Construction and metadata

        public static TValue From<TValue, TUnit>(double value, TUnit unit)
        where TUnit : IUnit where TValue: struct, IUnitValue<TValue, TUnit> => 
            FromSiValue<TValue>(value * unit.SiFactor);
        
        public static TValue FromSiValue<TValue>(double value) where TValue : IUnitValue, new() =>
            new TValue {SiValue = value};

        public static double As<TValue, TUnit>(this TValue value, TUnit unit)
            where TValue : struct, IUnitValue<TValue, TUnit> where TUnit : IUnit => value.SiValue * unit.SiFactor;

        public static double As(this IUnitValue value, IUnit unit) {
            Contract.Assert(value.UnitType.IsInstanceOfType(unit));
            return value.SiValue * unit.SiFactor;
        }

        private static IUnitDescription<TUnit> unitDescriptionOf<TUnit>() where TUnit : IUnit =>
            Activator.CreateInstance(
                typeof(TUnit).GetCustomAttribute<UnitMetadataAttribute>()?.UnitDescriptionType
                ?? throw new ArgumentException($"Cannot call .{nameof(SiUnitOf)}: " +
                                               $"{typeof(TUnit)} needs an attribute of type {nameof(UnitMetadataAttribute)}.")
            ) as IUnitDescription<TUnit>
            ?? throw new ArgumentException($"Cannot call .{nameof(SiUnitOf)}: " +
                                           $"The {nameof(UnitMetadataAttribute.UnitDescriptionType)} of the {nameof(UnitMetadataAttribute)} " +
                                           $"must be of type {typeof(IUnitDescription<TUnit>)}.");

        public static TUnit SiUnitOf<TUnit>() where TUnit : IUnit => unitDescriptionOf<TUnit>().SiUnit;
        
        public static TUnit[] AllUnitsOf<TUnit>() where TUnit : IUnit => unitDescriptionOf<TUnit>().AllUnits;
        
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
            FromSiValue<TSelf>(Mathd.Min(values.Prepend(first).Select(v => v.SiValue).ToArray()));

        public static TSelf Max<TSelf>(
            TSelf first, params TSelf[] values
        ) where TSelf : struct, IUnitValue<TSelf> =>
            FromSiValue<TSelf>(Mathd.Max(values.Prepend(first).Select(v => v.SiValue).ToArray()));
        
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
            FromSiValue<TSelf>(
                shouldClamp 
                    ? Mathd.Lerp(from.SiValue, to.SiValue, progress)
                    : Mathd.LerpUnclamped(from.SiValue, to.SiValue, progress));

        public static double InverseLerp<TSelf>(
            TSelf from,
            TSelf to,
            TSelf value
        ) where TSelf : struct, IUnitValue<TSelf> =>
            Mathd.InverseLerp(from.SiValue, to.SiValue, value.SiValue);
        
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
        
        // Formatting
        
        /// Returns the unit which leads to the smallest possible value not less than 1 when applied.
        /// If no unit yields a value >= 1, the unit with the greatest value is returned.
        /// Only <see cref="allowedUnits"/> will be considered if it is not null.
        /// These units will be sorted by their factor. In a performance critical context,
        /// you may want to pass an already sorted list. In this case, pass `true` for <see cref="areUnitsSorted"/>.
        public static TUnit MostFittingDisplayUnitFor<TUnit>(
            IUnitValueOf<TUnit> value, 
            IEnumerable<TUnit> allowedUnits = null, 
            bool areUnitsSorted = false
        ) where TUnit : IUnit {
            Contract.Assert(!(areUnitsSorted && allowedUnits == null));
            
            var allowed = allowedUnits?.If(!areUnitsSorted, 
                units => (IEnumerable<TUnit>) units.SortedBy(u => u.SiFactor)
            ).AsReadOnlyList() ?? AllUnitsOf<TUnit>();
            
            Contract.Assert(allowed.Any());
            
            var displayUnit = allowed[0];
            foreach (var unit in allowed.Skip(1)) {
                var displayValue = value.As(unit);
                if (displayValue < 1) break;
                else displayUnit = unit;
            }

            return displayUnit;
        }

        public static string Format<TUnit>(
            this IUnitValueOf<TUnit> value, 
            TUnit unit, 
            int digits, 
            RoundingMode roundingMode, 
            double zeroThreshold = 1E-5f
        ) where TUnit : IUnit {
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