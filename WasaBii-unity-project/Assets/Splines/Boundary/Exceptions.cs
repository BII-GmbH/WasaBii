﻿using System;

namespace BII.WasaBii.Splines {
    public class InsufficientNodePositionsException : Exception {
        public InsufficientNodePositionsException(int actual, int required) :
            base($"You provided {actual} node positions when constructing the spline " +
                 $"but {required} node positions were needed") { }
    }
}