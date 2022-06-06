using System;
using System.Linq;
using BII.WasaBii.Core;
using NUnit.Framework;
using Range = BII.WasaBii.Core.Range;

namespace Core.Tests {
    
    public class RangeTests {
        
        [Test]
        public void Range_Constructor_Test() {
            var range = new Range<int>(1, 2, includeFrom:true, includeTo:true);
            Assert.AreEqual(1, range.From);
            Assert.AreEqual(2, range.To);
        }

        [Test]
        public void NegativeSampleCountThrows() {
            // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
            Assert.Throws<ArgumentException>(() => Range.Sample01(-1, includeZero: true, includeOne: true));
        }

        [Test]
        public void ZeroSampleCountThrows() {
            // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
            Assert.Throws<ArgumentException>(() => Range.Sample01(0, includeZero: true, includeOne: true));
        }

        [Test]
        public void OneSampleCountThrows() {
            // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
            Assert.Throws<ArgumentException>(() => Range.Sample01(1, includeZero: true, includeOne: true));
        }

        [Test]
        public void WhenBothInclusive_ThenSamplesIncludeBoth() {
            var samples = Range.Sample01(2, includeZero: true, includeOne: true).ToArray();
            Assert.AreEqual(samples[0], 0);
            Assert.AreEqual(samples[1], 1);
        }
        
        [Test]
        public void WhenBothExclusive_ThenSamplesExcludeBoth() {
            var samples = Range.Sample01(2, includeZero: false, includeOne: false).ToArray();
            Assert.AreNotEqual(samples[0], 0);
            Assert.AreNotEqual(samples[1], 1);
        }
        
        [Test]
        public void WhenFromInclusive_ThenSamplesIncludeFrom() {
            var samples = Range.Sample01(2, includeZero: true, includeOne: false).ToArray();
            Assert.AreEqual(samples[0], 0);
            Assert.AreNotEqual(samples[1], 1);
        }
        
        [Test]
        public void WhenToInclusive_ThenSamplesIncludeTo() {
            var samples = Range.Sample01(2, includeZero: false, includeOne: true).ToArray();
            Assert.AreNotEqual(samples[0], 0);
            Assert.AreEqual(samples[1], 1);
        }

        [Test]
        public void SamplesAreAscending() {
            var samples = Range.Sample01(count: 100, includeZero: true, includeOne: true).ToArray();
            Assert.IsTrue(samples.PairwiseSliding().All(t => t.Item1 < t.Item2));
        }

    }
    
}