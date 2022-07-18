using System;

namespace BII.WasaBii.Splines {
    
    public class InsufficientNodePositionsException : ArgumentException {
        public InsufficientNodePositionsException(int actual, int required) :
            base($"You provided {actual} node positions when constructing the spline " +
                 $"but at least {required} node positions were needed. This is the minimum" +
                 $" number of handles required to construct a catmull-rom spline segment " +
                 $"{(required < 4 ? "without" : "with")} margin handles") { }
    }
    
    public class InvalidSplineException<TPos, TDiff> : Exception 
        where TPos : struct where TDiff : struct {
        public InvalidSplineException(Spline<TPos, TDiff> spline, string reason)
            : base($"The Spline {spline} is not valid because of {reason}") { }
        public InvalidSplineException(string context, Spline<TPos, TDiff> spline, string reason)
            : base($"{context}: The Spline {spline} is not valid because of {reason}") { }
    }

}