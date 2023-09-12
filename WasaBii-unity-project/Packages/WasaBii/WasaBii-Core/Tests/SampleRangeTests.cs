using System;
using System.Linq;
using NUnit.Framework;

namespace BII.WasaBii.Core.Tests {
    
    public class SampleRangeTests {

        [Test]
        public void NegativeSampleCountThrows() {
            // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
            Assert.Throws<ArgumentException>(() => SampleRange.Sample01(-1, includeZero: true, includeOne: true));
        }

        [Test]
        public void ZeroSampleCountThrows() {
            // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
            Assert.Throws<ArgumentException>(() => SampleRange.Sample01(0, includeZero: true, includeOne: true));
        }

        [Test]
        public void OneSampleCountThrows() {
            // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
            Assert.Throws<ArgumentException>(() => SampleRange.Sample01(1, includeZero: true, includeOne: true));
        }

        [Test]
        public void WhenBothInclusive_ThenSamplesIncludeBoth() {
            var samples = SampleRange.Sample01(2, includeZero: true, includeOne: true).ToArray();
            Assert.AreEqual(samples[0], 0);
            Assert.AreEqual(samples[1], 1);
        }
        
        [Test]
        public void WhenBothExclusive_ThenSamplesExcludeBoth() {
            var samples = SampleRange.Sample01(2, includeZero: false, includeOne: false).ToArray();
            Assert.AreNotEqual(samples[0], 0);
            Assert.AreNotEqual(samples[1], 1);
        }
        
        [Test]
        public void WhenFromInclusive_ThenSamplesIncludeFrom() {
            var samples = SampleRange.Sample01(2, includeZero: true, includeOne: false).ToArray();
            Assert.AreEqual(samples[0], 0);
            Assert.AreNotEqual(samples[1], 1);
        }
        
        [Test]
        public void WhenToInclusive_ThenSamplesIncludeTo() {
            var samples = SampleRange.Sample01(2, includeZero: false, includeOne: true).ToArray();
            Assert.AreNotEqual(samples[0], 0);
            Assert.AreEqual(samples[1], 1);
        }

        [Test]
        public void SamplesAreAscending() {
            var samples = SampleRange.Sample01(count: 100, includeZero: true, includeOne: true).ToArray();
            Assert.IsTrue(samples.PairwiseSliding().All(t => t.Item1 < t.Item2));
        }

    }
    
}