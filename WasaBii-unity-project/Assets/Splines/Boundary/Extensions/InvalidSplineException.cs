using System;

namespace BII.WasaBii.CatmullRomSplines {

    public class InvalidSplineException : Exception {
        public InvalidSplineException(UntypedSpline spline, string reason)
            : base($"The Spline {spline} is not valid because of {reason}") { }
        public InvalidSplineException(string context, UntypedSpline spline, string reason)
            : base($"{context}: The Spline {spline} is not valid because of {reason}") { }
    }

}