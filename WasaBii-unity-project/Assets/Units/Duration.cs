using System;
using System.Collections.Generic;
using System.Linq;
using BII.WasaBii.Core;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace BII.WasaBii.Units {
    
    [JsonObject(IsReference = false)] // Treat as value type for serialization
    [MustBeSerializable] 
    public sealed class TimeUnit : Unit {
        [JsonConstructor]
        private TimeUnit(string displayName, double factor) : base(displayName, factor) { }

        public static readonly TimeUnit Milliseconds = new TimeUnit("ms", 0.001);
        public static readonly TimeUnit Seconds = new TimeUnit("s", 1);
        public static readonly TimeUnit Minutes = new TimeUnit("min", 60);
        public static readonly TimeUnit Hours = new TimeUnit("h", 60 * 60);
        public static readonly TimeUnit Days = new TimeUnit("d", 60 * 60 * 24);
        public static readonly TimeUnit Weeks = new TimeUnit("w", 60 * 60 * 24 * 7);
        
        /// <summary>
        /// Returns the next larger TimeUnit, or the unit itself if it is already the largest.
        /// </summary>
        public TimeUnit ToHigherUnit() {
            foreach (var timeUnit in All.OrderBy(u => u.Factor))
                if (timeUnit.Factor > Factor) return timeUnit;
            return this;
        }

        /// <summary>
        /// Returns the next smaller TimeUnit, or the unit itself if it is already the smallest.
        /// </summary>
        public TimeUnit ToLowerUnit() {
            foreach (var timeUnit in All.OrderByDescending(u => u.Factor))
                if (timeUnit.Factor < Factor) return timeUnit;
            return this;
        }
        
        public static IReadOnlyList<TimeUnit> All = new[] {
            Milliseconds, Seconds, Minutes, Hours, Days, Weeks
        };
    }

    [Serializable]
    [MustBeSerializable]
    public readonly struct Duration : ValueWithUnit<Duration, TimeUnit> {
        
        public IReadOnlyList<TimeUnit> AllUnits => TimeUnit.All;
        public TimeUnit DisplayUnit => TimeUnit.Seconds;
        public TimeUnit SIUnit => TimeUnit.Seconds;

        public static readonly Duration Zero = new(0, TimeUnit.Seconds);

        public static Duration Max(Duration lhs, Duration rhs) => Math.Max(lhs.seconds, rhs.seconds).Seconds();
        public static Duration Min(Duration lhs, Duration rhs) => Math.Min(lhs.seconds, rhs.seconds).Seconds();

        private readonly double seconds;

        public double SIValue => seconds;

        public Duration(double time, TimeUnit unit) => seconds = time * unit.Factor;

        public Duration CopyWithDifferentSIValue(double newSIValue) => newSIValue.Seconds();
        CopyableValueWithUnit CopyableValueWithUnit.CopyWithDifferentSIValue(double newSIValue) => 
            CopyWithDifferentSIValue(newSIValue);

        public static Duration operator +(Duration d) => d;
        public static Duration operator -(Duration d) => (-d.SIValue).Seconds();
        public static Duration operator +(Duration a, Duration b) => (a.SIValue + b.SIValue).Seconds();
        public static Duration operator -(Duration a, Duration b) => (a.SIValue - b.SIValue).Seconds();
        public static Duration operator *(Duration a, double s) => (a.SIValue * s).Seconds();
        public static Duration operator *(double s, Duration a) => (a.SIValue * s).Seconds();
        public static Duration operator /(Duration a, double s) => (a.SIValue / s).Seconds();
        public static Number operator /(Duration a, Duration b) => (a.SIValue / b.SIValue).Number();
        public static bool operator <(Duration a, Duration b) => a.SIValue < b.SIValue;
        public static bool operator >(Duration a, Duration b) => a.SIValue > b.SIValue;
        public static bool operator <=(Duration a, Duration b) => a.SIValue <= b.SIValue;
        public static bool operator >=(Duration a, Duration b) => a.SIValue >= b.SIValue;
        public static bool operator ==(Duration a, Duration b) => a.SIValue == b.SIValue;
        public static bool operator !=(Duration a, Duration b) => a.SIValue != b.SIValue;
        public static Duration operator %(Duration a, Duration b) => (a.SIValue % b.SIValue).Seconds();
    

        public override string ToString() => $"{this.AsSeconds()} Seconds";

        /// <summary>
        /// Formats the time to be displayed in with the precision of two fitting units.
        /// </summary>
        /// <example>
        /// If the underlying time is 350 seconds, the output is ~5min 5s
        /// If the underlying time is 3.5 seconds, the output is ~3s
        /// If the underlying time is 3.5 seconds, but the lowest unit is TimeUnit.Milliseconds,
        /// the output will be ~3s 5ms.
        /// </example>
        [Pure] public string FormatTime([CanBeNull] TimeUnit lowestUnit = null, bool withTilde = true) {
            TimeUnit getLowestUnit() => lowestUnit ?? TimeUnit.Seconds;
            
            var seconds = this.seconds;
            int timeInUnit(TimeUnit unit, double subtract = 0) => Mathd.FloorToInt((seconds - subtract) / unit.Factor);

            var unitPairs = TimeUnit.All
                .OrderByDescending(u => u.Factor)
                .TakeWhile(u => u.Factor >= (lowestUnit ?? TimeUnit.Seconds).Factor)
                .PairwiseSliding();

            foreach (var (higherUnit, lowerUnit) in unitPairs){
                var timeInHigherUnit = timeInUnit(higherUnit);
                if (timeInHigherUnit > 0) {
                    var timeInLowerUnit = timeInUnit(lowerUnit, subtract: timeInHigherUnit * higherUnit.Factor);
                    return $"{(withTilde ? "~" : "")}{timeInHigherUnit}{higherUnit.DisplayName} {timeInLowerUnit}{lowerUnit.DisplayName}";
                }
            }

            return $"{(withTilde ? "~" : "")}{timeInUnit(getLowestUnit())}{getLowestUnit().DisplayName}";
        }

        /// <summary>
        /// Formats the time to be displayed with all values from upperUnit to lowerUnit
        /// </summary>
        /// <example>
        /// If the underlying time is 350 seconds and upperUnit is >= Minutes, the output is 5min 5s
        /// If the underlying time is 350 seconds, but upperUnit is Seconds, the output is 350s
        /// If the underlying time is 675 seconds and upperUnit is >= Hours, the output is 1h 1min 15s
        /// If the underlying time is 675 seconds, upperUnit is >= Hours and lowerUnit is Minutes,
        /// the output is 1h 1min
        /// </example>
        public string FormatTimeFromTo(TimeUnit upperUnit, [CanBeNull] TimeUnit lowerUnit = null, bool withSeparator = false) {
            TimeUnit getLowestUnit() => lowerUnit ?? TimeUnit.Seconds;
            var seconds = this.seconds;
            int timeInUnit(TimeUnit unit, double subtract = 0) => Mathd.FloorToInt((seconds - subtract) / unit.Factor);

            var timeInCurrentUnit = timeInUnit(upperUnit);
            if (upperUnit.Factor >= getLowestUnit().Factor) {
                var timeLeft = seconds - timeInCurrentUnit * upperUnit.Factor;
                // Recursion
                return
                    $"{(withSeparator ? " " : "")}{timeInCurrentUnit}{upperUnit.DisplayName}" +
                    $"{timeLeft.Seconds().FormatTimeFromTo(upperUnit.ToLowerUnit(), getLowestUnit(), true)}";
            } 
            return "";
        }

        /// <summary>
        /// Formats the time to be displayed similar to FormatTimeFromTo, but "cuts" the Units higher than "start" out
        /// </summary>
        /// <example>
        /// If the underlying time is 350 seconds and start is >= Minutes, the output is 5min 5s
        /// If the underlying time is 350 seconds, start is >= Minutes and end == Minutes, the output is 5min
        /// If the underlying time is 350 seconds, but start is Seconds, the output is 5s
        /// </example>
        public string FormatTimeInFrame(TimeUnit start, TimeUnit end) {
            if (start == end) return FormatTimeFromTo(start, end);
            var timeLeft = seconds.Seconds() % new Duration(1, start);
            return timeLeft.FormatTimeFromTo(start, end);
        }
        
        public bool Equals(Duration other) => this == other;
        public override bool Equals(object obj) => obj is Duration duration && this == duration;
        public override int GetHashCode() => SIValue.GetHashCode();

        // This needs to be as small as possible,
        // since it is used by some nodes that
        // should not have a noticeable duration.
        public static readonly Duration Epsilon = 1.Millis();

        /// <summary>
        /// Returns the minimal positive Duration if `this` is not positive.
        /// </summary>
        public Duration AtLeastEpsilon() => this < Epsilon ? Epsilon : this;
    }

    public static class TimeExtensions {
        
        public static Duration Millis(this Number millis) => new Duration(millis, TimeUnit.Milliseconds);
        public static Duration Seconds(this Number seconds) => new Duration(seconds, TimeUnit.Seconds);
        public static Duration Minutes(this Number minutes) => new Duration(minutes, TimeUnit.Minutes);
        public static Duration Hours(this Number hours) => new Duration(hours, TimeUnit.Hours);
        public static Duration Days(this Number days) => new Duration(days, TimeUnit.Days);
        public static Duration Weeks(this Number weeks) => new Duration(weeks, TimeUnit.Weeks);
        
        public static Duration Millis(this float millis) => new Duration(millis, TimeUnit.Milliseconds);
        public static Duration Seconds(this float seconds) => new Duration(seconds, TimeUnit.Seconds);
        public static Duration Minutes(this float minutes) => new Duration(minutes, TimeUnit.Minutes);
        public static Duration Hours(this float hours) => new Duration(hours, TimeUnit.Hours);
        public static Duration Days(this float days) => new Duration(days, TimeUnit.Days);
        public static Duration Weeks(this float weeks) => new Duration(weeks, TimeUnit.Weeks);
        
        public static Duration Millis(this double millis) => new Duration(millis, TimeUnit.Milliseconds);
        public static Duration Seconds(this double seconds) => new Duration(seconds, TimeUnit.Seconds);
        public static Duration Minutes(this double minutes) => new Duration(minutes, TimeUnit.Minutes);
        public static Duration Hours(this double hours) => new Duration(hours, TimeUnit.Hours);
        public static Duration Days(this double days) => new Duration(days, TimeUnit.Days);
        public static Duration Weeks(this double weeks) => new Duration(weeks, TimeUnit.Weeks);

        public static Duration Millis(this int millis) => new Duration(millis, TimeUnit.Milliseconds);
        public static Duration Seconds(this int seconds) => new Duration(seconds, TimeUnit.Seconds);
        public static Duration Minutes(this int minutes) => new Duration(minutes, TimeUnit.Minutes);
        public static Duration Hours(this int hours) => new Duration(hours, TimeUnit.Hours);
        public static Duration Days(this int days) => new Duration(days, TimeUnit.Days);
        public static Duration Weeks(this int weeks) => new Duration(weeks, TimeUnit.Weeks);
        
        public static Number AsMillis(this Duration duration) => duration.As(TimeUnit.Milliseconds);
        public static Number AsSeconds(this Duration duration) => duration.As(TimeUnit.Seconds);
        public static Number AsMinutes(this Duration duration) => duration.As(TimeUnit.Minutes);
        public static Number AsHours(this Duration duration) => duration.As(TimeUnit.Hours);
        public static Number AsDays(this Duration duration) => duration.As(TimeUnit.Days);
        public static Number AsWeeks(this Duration duration) => duration.As(TimeUnit.Weeks);
    }
}