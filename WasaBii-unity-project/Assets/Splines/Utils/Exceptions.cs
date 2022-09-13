using System;

namespace BII.WasaBii.Splines {
    
    public class InsufficientNodePositionsException : ArgumentException {
        public InsufficientNodePositionsException(int actual, int required) :
            base($"You provided {actual} node positions when constructing the spline " +
                 $"but at least {required} node positions were needed. This is the minimum" +
                 $" number of handles required to construct a catmull-rom spline segment " +
                 $"{(required < 4 ? "without" : "with")} margin handles") { }
    }
    
    public class InvalidSplineException : Exception  {
        public InvalidSplineException(Spline spline, string reason)
            : base($"The Spline {spline} is not valid because of {reason}") { }
        public InvalidSplineException(string context, Spline spline, string reason)
            : base($"{context}: The Spline {spline} is not valid because of {reason}") { }
    }

}