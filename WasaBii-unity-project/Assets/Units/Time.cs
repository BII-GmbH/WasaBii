using System;
using JetBrains.Annotations;
using static BII.Units.TimeInterval;

namespace BII.Units {
    
    [Serializable]
    public readonly struct TimeInterval {
        public enum Comparison {
            Inside,
            Before,
            After,
            Overlap
        }

        public static bool HasAnyOverlapWith(Comparison comp) => comp == Comparison.Inside || comp == Comparison.Overlap; 
        
        public readonly Duration Start;
        public readonly Duration End;
        public Duration Total => End - Start;
        
        public TimeInterval(Duration start, Duration end) {
            Start = start; 
            End = end;
        }
    }

    public static class TimeIntervalExtensions {
        public static bool IsNearly(this TimeInterval self, TimeInterval other) =>
            self.Start.IsNearly(other.Start) && self.End.IsNearly(other.End);
        public static TimeInterval WithStart(this TimeInterval interval, Duration newStart) 
            => new TimeInterval(newStart, interval.End);
        public static TimeInterval WithEnd(this TimeInterval interval, Duration newEnd) 
            => new TimeInterval(interval.Start, newEnd);
        
        // TODO DG for maintainer: Once we have C#9 and relational pattern matching, we can improve this code
        public static Comparison CompareToInterval(this Duration time, TimeInterval interval) =>
            (time.CompareTo(interval.Start), time.CompareTo(interval.End)) switch {
                (-1, _) => Comparison.Before,
                (_, 1) => Comparison.After,
                _ => Comparison.Inside
            };

        public static Comparison CompareToInterval(this TimeInterval interval, TimeInterval other) {
            var start = interval.Start.CompareToInterval(other);
            var end = interval.End.CompareToInterval(other);
            return (start, end) switch {
                (Comparison.Before, Comparison.After) => Comparison.Inside,
                (Comparison.Before, Comparison.Inside) => Comparison.Overlap,
                (Comparison.Inside, Comparison.Inside) => Comparison.Overlap,
                (Comparison.Inside, Comparison.After) => Comparison.Overlap,
                (Comparison.After, _) => Comparison.Before,
                (_, Comparison.Before) => Comparison.After,
                _ => throw new Exception("No matching comparision found. This should not happen, duh")
            };
        }
        
        // /// If the value is inside the interval, returns the percentage
        // /// of how far it is between the start and end. (InverseLerp) 
        // /// Returns null if the value is outside of the interval.
        // public static double? TryFindPercentageInInterval(this TimeInterval interval, Duration value) => 
        //     value >= interval.Start && value <= interval.End 
        //         ? Mathd.InverseLerp(interval.Start.SIValue, interval.End.SIValue, value.SIValue) 
        //         : default(double?);
        
        [Pure]
        public static TimeInterval Encapsulate(this TimeInterval interval, TimeInterval other) => new TimeInterval(
            interval.Start.Min(other.Start),
            interval.End.Max(other.End)
        );
    }
}