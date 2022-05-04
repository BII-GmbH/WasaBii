using System;

namespace BII.WasaBii.Splines {

    public class InvalidSplineException<TPos, TDiff> : Exception 
        where TPos : struct where TDiff : struct {
        public InvalidSplineException(Spline<TPos, TDiff> spline, string reason)
            : base($"The Spline {spline} is not valid because of {reason}") { }
        public InvalidSplineException(string context, Spline<TPos, TDiff> spline, string reason)
            : base($"{context}: The Spline {spline} is not valid because of {reason}") { }
    }

}