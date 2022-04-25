using System;
using BII.WasaBii.Core;
using NUnit.Framework;

namespace Core.Tests {
    
    public class PairwiseEnumerableExtensionsTests {
        
        [Test]
        public void Grouped_WhenSourceEmpty_ThenEmptyResultAndDoesNotThrow() {
            var source = Array.Empty<int>();
            
            Assert.That(source.Grouped(), Is.Empty);
        }
        
        [Test]
        public void Grouped_WhenGroupSizeInvalid_ThenThrows() {
            Assert.That(() => new[]{1}.Grouped(groupSize: 1), Throws.TypeOf<ArgumentException>());
        }
        
        [Test]
        public void Grouped_WhenGroupSize2_ThenGroupsCorrectly() {
            var source = new[] {1, 2, 3, 4, 5, 6};

            var actual = source.Grouped(groupSize: 2);
            var expected = new [,]{{1, 2}, {3, 4}, {5, 6}};
            Assert.That(actual, Is.EquivalentTo(unpackArray(expected)));
        }

        [Test]
        public void Grouped_WhenGroupSizeNotMultiple_ThenGroupsCorrectly() {
            var source = new[] {1, 2, 3, 4, 5, 6, 7};

            var actual = source.Grouped(groupSize: 2);
            var expected = new[] { new [] {1, 2}, new [] {3, 4}, new [] {5, 6}, new []{7}};
            Assert.That(actual, Is.EquivalentTo(expected));
        }
        
        [Test]
        public void GroupedWithoutPartial_WhenGroupSizeNotMultiple_ThenGroupsCorrectly() {
            var source = new[] {1, 2, 3, 4, 5, 6, 7};

            var actual = source.Grouped(groupSize: 2, withPartial: false);
            var expected = new[] { new [] {1, 2}, new [] {3, 4}, new [] {5, 6}};
            Assert.That(actual, Is.EquivalentTo(expected));
        }
        
        [Test]
        public void PairwiseGrouped_GroupsCorrectly() {
            var source = new[] {1, 2, 3, 4, 5, 6};

            var actual = source.PairwiseGrouped();
            var expected = new []{(1, 2), (3, 4), (5, 6)};
            Assert.That(actual, Is.EquivalentTo(expected));
        }
        
        [Test]
        public void Grouped_WhenGroupSize3_ThenGroupsCorrectly() {
            var source = new[] {1, 2, 3, 4, 5, 6};

            var actual = source.Grouped(groupSize: 3);
            var expected = new [,]{{1, 2, 3}, {4, 5, 6}};
            Assert.That(actual, Is.EquivalentTo(unpackArray(expected)));
        }
        
        [Test]
        public void Sliding_WhenSourceEmpty_ThenEmptyResultAndDoesNotThrow() {
            var source = new int[0];
            
            Assert.That(source.Sliding(), Is.Empty);
        }
        
        [Test]
        public void Sliding_WhenSlideSizeInvalid_ThenThrows() {
            Assert.That(() => new[]{1}.Sliding(slideSize: 1), Throws.TypeOf<ArgumentException>());
        }
        
        [Test]
        public void Sliding_WhenSlideSize2_ThenSlidesCorrectly() {
            var source = new[] {1, 2, 3, 4};

            var actual = source.Sliding(slideSize: 2);
            var expected = new [,]{{1, 2}, {2, 3}, {3, 4}};
            Assert.That(actual, Is.EquivalentTo(unpackArray(expected)));
        }
        
        [Test]
        public void PairwiseSliding_SlidesCorrectly() {
            var source = new[] {1, 2, 3, 4};

            var actual = source.PairwiseSliding();
            var expected = new []{(1, 2), (2, 3), (3, 4)};
            Assert.That(actual, Is.EquivalentTo(expected));
        }
        
        [Test]
        public void Sliding_WhenSlideSize3_ThenSlidesCorrectly() {
            var source = new[] {1, 2, 3, 4, 5};

            var actual = source.Sliding(slideSize: 3);
            var expected = new [,]{{1, 2, 3}, {2, 3, 4}, {3, 4, 5}};
            Assert.That(actual, Is.EquivalentTo(unpackArray(expected)));
        }
        
        // Needed for assertions, since EquivalentTo(...) does not work on 2d-arrays
        private int[][] unpackArray(int[,] source) {
            var result = new int[source.GetLength(dimension: 0)][];
            for (var i = 0; i < source.GetLength(dimension: 0); i++) {
                result[i] = new int[source.GetLength(dimension: 1)];
                for (var j = 0; j < source.GetLength(dimension: 1); j++) {
                    result[i][j] = source[i, j];
                }
            }
            return result;
        }
    }
}