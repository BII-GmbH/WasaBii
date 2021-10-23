using System;

namespace BII.Utilities.Independent {
    
    /// <summary>
    /// DRandom is the double equivalent to Unity's Random class.
    /// It helps you to easily generate random data without instantiating a new Random object
    /// each time you want to get a random value.
    /// </summary>
    public static class DRandom {

        private static readonly Random random = new Random();

        public static double Range(double min, double max) => random.NextDouble() * (max - min) + min;
    }
}