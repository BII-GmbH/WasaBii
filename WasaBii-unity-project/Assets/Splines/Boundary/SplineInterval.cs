using System;
using BII.Utilities.Independent.Maths;
using UnityEngine;

namespace BII.CatmullRomSplines {
    
    [Serializable]
    public struct SplineInterval {

        // Should be readonly but isn't because that breaks unity serialization. Do not mutate except in constructor.
        [SerializeField] private SplineLocation __start;
        public SplineLocation Start => __start;

        // Should be readonly but isn't because that breaks unity serialization. Do not mutate except in constructor.        
        [SerializeField] private SplineLocation __end;
        public SplineLocation End => __end;
        public SplineLocation Center => new SplineLocation((Start.Value + End.Value) / 2);

        public SplineInterval(SplineLocation start, SplineLocation end) {
            this.__start = MathExtensions.Min(start,end);
            this.__end = MathExtensions.Max(start, end);
        }

        public static implicit operator SplineInterval((SplineLocation start, SplineLocation end) tuple) =>
            new SplineInterval(tuple.start, tuple.end);

        public void Deconstruct(out SplineLocation start, out SplineLocation end) {
            start = this.Start;
            end = this.End;
        }
    }
}