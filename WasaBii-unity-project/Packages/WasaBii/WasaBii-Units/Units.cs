using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using BII.WasaBii.Core;

#nullable enable

namespace BII.WasaBii.UnitSystem {

    public static class Units {
        
        // Construction and metadata

        public static TValue From<TValue>(double value, IUnit<TValue> unit)
        where TValue: struct, IUnitValue<TValue> => 
            FromSiValue<TValue>(value * unit.SiFactor);
        
        public static TValue FromSiValue<TValue>(double value) where TValue : IUnitValue, new() =>
            new TValue {SiValue = value};

        public static double As<TValue>(this TValue value, IUnit<TValue> unit)
            where TValue : struct, IUnitValue<TValue> => value.SiValue * unit.InverseSiFactor;

        public static double As(this IUnitValue value, IUnit unit) {
            if (!value.UnitType.IsInstanceOfType(unit))
                throw new ArgumentException(
                    $"The type of the target unit does not match the type of the value's unit type. " +
                    $"Expected {value.UnitType} but was {unit}");
            return value.SiValue * unit.InverseSiFactor;
        }

        private static IUnitDescription<TUnit> unitDescriptionOf<TUnit>() where TUnit : IUnit =>
            unitDescriptionOfDynamic<TUnit>(typeof(TUnit));

        private static IUnitDescription<TUnit> unitDescriptionOfDynamic<TUnit>(Type unitType) where TUnit : IUnit =>
            Activator.CreateInstance(
                unitType.GetCustomAttribute<UnitMetadataAttribute>()?.UnitDescriptionType
                ?? throw new ArgumentException($"{unitType} needs an attribute of type {nameof(UnitMetadataAttribute)}!")
            ) as IUnitDescription<TUnit>
            ?? throw new ArgumentException($"The {nameof(UnitMetadataAttribute.UnitDescriptionType)} of the {nameof(UnitMetadataAttribute)} " +
                                           $"must be of type {typeof(IUnitDescription<TUnit>)}.");

        public static TUnit SiUnitOf<TUnit>() where TUnit : IUnit => unitDescriptionOf<TUnit>().SiUnit;
        
        public static IReadOnlyList<TUnit> AllUnitsOf<TUnit>() where TUnit : IUnit => unitDescriptionOf<TUnit>().AllUnits;
        
        public static IReadOnlyList<IUnit<TValue>> AllUnitsFor<TValue>() where TValue : struct, IUnitValue<TValue> => 
            unitDescriptionOfDynamic<IUnit<TValue>>(default(TValue).UnitType).AllUnits;
        
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
        
        /// <summary>
        /// Returns the unit which leads to the smallest possible value not less than 1 when applied.
        /// If no unit yields a value >= 1, the unit with the greatest value is returned.
        /// Only <see cref="allowedUnits"/> will be considered if it is not null.
        /// These units will be sorted by their factor. In a performance critical context,
        /// you may want to pass an already sorted list. In this case, pass `true` for <see cref="areUnitsSorted"/>.
        /// </summary>
        public static IUnit<TValue> MostFittingDisplayUnitFor<TValue>(
            TValue value, 
            IEnumerable<IUnit<TValue>>? allowedUnits = null,
            bool areUnitsSorted = false
        ) where TValue : struct, IUnitValue<TValue> {
            var allowed = allowedUnits?.If(!areUnitsSorted, 
                units => (IEnumerable<IUnit<TValue>>) units.OrderBy(u => u.SiFactor)
            ).AsReadOnlyList() ?? AllUnitsFor<TValue>();

            if (!allowed.Any()) throw new ArgumentException($"No allowed units given for {typeof(TValue)}.");
            
            var displayUnit = allowed[0];
            foreach (var unit in allowed.Skip(1)) {
                var displayValue = value.As(unit);
                if (displayValue < 1) break;
                else displayUnit = unit;
            }

            return displayUnit;
        }

        // TODO: provide default format with most fitting unit & implement analyzer to prefer this over .ToString()
        public static string Format<TValue>(
            this TValue value, 
            IUnit<TValue> unit, 
            int digits, 
            RoundingMode roundingMode, 
            double zeroThreshold = 1E-5f
        ) where TValue : struct, IUnitValue<TValue> {
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

        public static string Format<TValue>(
            this TValue value, int digits, RoundingMode roundingMode, double zeroThreshold = 1E-5f
        ) where TValue : struct, IUnitValue<TValue, IUnit<TValue>> => value.Format(
            MostFittingDisplayUnitFor(value),
            digits,
            roundingMode,
            zeroThreshold
        );

        public static TSelf Round<TSelf>(this TSelf value, IUnit<TSelf> unit)
        where TSelf : struct, IUnitValue<TSelf> =>
            From(Math.Round(value.As(unit)), unit);

        public static TSelf RoundToWholeMultipleOf<TSelf>(this TSelf value, TSelf factor)
        where TSelf : struct, IUnitValue<TSelf> =>
            new TSelf { SiValue = Math.Round(value.SiValue / factor.SiValue) * factor.SiValue };

        
        public static TSelf CeilToWholeMultipleOf<TSelf>(this TSelf value, TSelf factor)
        where TSelf : struct, IUnitValue<TSelf> =>
            new TSelf { SiValue = Math.Ceiling(value.SiValue / factor.SiValue) * factor.SiValue };
        
        public static TSelf FloorToWholeMultipleOf<TSelf>(this TSelf value, TSelf factor)
        where TSelf : struct, IUnitValue<TSelf> =>
        new TSelf { SiValue = Math.Floor(value.SiValue / factor.SiValue) * factor.SiValue };

        /// <summary>
        /// Tries to parse the given text as a value of <typeparamref name="TValue"/>.
        /// Uses the unit given by the string postfix, if present. Unit strings are detected
        /// by their <see cref="IUnit.LongName"/> and <see cref="IUnit.ShortName"/>,
        /// which correspond to "name" and "short" as given in the ".units.json" file.
        /// Whitespaces and case are ignored for unit detection.
        /// </summary>
        /// <param name="fallbackUnit">The unit to use if none is given in the <paramref name="text"/>.</param>
        /// <returns>The value if it could be parsed successfully. <see cref="Option.None"/> if the string is not
        /// a valid numeral or does not end with the unit while no <paramref name="fallbackUnit"/> is specified.</returns>
        /// <remarks>Assumes the decimal separator given by the <paramref name="numberFormatInfo"/>. A text with the
        /// wrong decimal separator might result in a faulty value.
        /// 
        /// As the units are detected using their names as specified in the JSON file, note that strings like
        /// "kph" might not be recognized if the specified name is "km/h".</remarks>
        public static Option<TValue> TryParse<TValue>(
            string text,
            IUnit<TValue>? fallbackUnit = default,
            NumberStyles numberStyles = NumberStyles.Float, 
            NumberFormatInfo? numberFormatInfo = null
        ) where TValue : struct, IUnitValue<TValue> {
            var formatInfo = numberFormatInfo ?? NumberFormatInfo.InvariantInfo;

            var chars = text.ToCharArray();
#region select number span
            var numberStartIndex = 0;
            while (numberStartIndex < chars.Length && char.IsWhiteSpace(chars[numberStartIndex])) numberStartIndex++;
            var numberEndIndex = numberStartIndex;
            while (numberEndIndex < chars.Length) {
                if (char.IsDigit(chars[numberEndIndex])) numberEndIndex++;
                else if (formatInfo.NumberDecimalSeparator.Length == 1
                    ? chars[numberEndIndex] == formatInfo.NumberDecimalSeparator[0]
                    : new ArraySegment<char>(chars, numberEndIndex, formatInfo.NumberDecimalSeparator.Length)
                        .SequenceEqual(formatInfo.NumberDecimalSeparator))
                    numberEndIndex += formatInfo.NumberDecimalSeparator.Length;
                else if (formatInfo.NumberGroupSeparator.Length == 1
                    ? chars[numberEndIndex] == formatInfo.NumberGroupSeparator[0]
                    : new ArraySegment<char>(chars, numberEndIndex, formatInfo.NumberGroupSeparator.Length)
                        .SequenceEqual(formatInfo.NumberGroupSeparator))
                    numberEndIndex += formatInfo.NumberGroupSeparator.Length;
                else {
                    break;
                }
            }
#endregion
            if(numberEndIndex == numberStartIndex || !double.TryParse(
                new ReadOnlySpan<char>(chars, numberStartIndex, numberEndIndex - numberStartIndex), 
                numberStyles, formatInfo, out var val)
            ) return Option.None;

            var unitStartIndex = numberEndIndex;
            while (unitStartIndex < chars.Length && char.IsWhiteSpace(chars[unitStartIndex])) unitStartIndex++;
            if (unitStartIndex == chars.Length) 
                return fallbackUnit == null ? Option.None : From(val, fallbackUnit);

            foreach (var unit in AllUnitsFor<TValue>()) {
                foreach (var unitName in new [] { unit.ShortName, unit.LongName }) {
                    var textI = unitStartIndex;
                    var unitI = 0;
                    while (unitI < unitName.Length && char.IsWhiteSpace(unitName[unitI])) unitI++;
                    while (textI < chars.Length && unitI < unitName.Length) {
                        if (char.ToLower(chars[textI]) != char.ToLower(unitName[unitI])) break;
                        do unitI++; while (unitI < unitName.Length && char.IsWhiteSpace(unitName[unitI]));
                        do textI++; while (textI < chars.Length && char.IsWhiteSpace(chars[textI]));
                    }

                    if (textI == chars.Length && unitI == unitName.Length)
                        return Option.Some(From(val, unit));
                }
            }
            
            return Option.None;
        }

    }

}