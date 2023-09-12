using System;
using System.Collections.Generic;
using System.Linq;
using BII.WasaBii.Core;
using NUnit.Framework;

namespace BII.WasaBii.Undo.Tests {
    
    public class SymmetricOperationTest {
        
#region boilerplate
        
        private enum Op { Do, Undo, DoDispose, UndoDispose }

        private readonly Dictionary<Op, int> opCount = new();

        private readonly IReadOnlyDictionary<Op, List<int>> opActions =
            Enum.GetValues(typeof(Op)).Cast<Op>().ToDictionary(op => op, _ => new List<int>());

        private Action make(Op op) {
            var index = ++opCount[op];
            return () => opActions[op].Add(index);
        }

        private SymmetricOperation makeOp() => new(
            make(Op.Do),
            make(Op.Undo),
            make(Op.DoDispose),
            make(Op.UndoDispose)
        );

        private SymmetricOperation<string> makeOp(string result) {
            var doOp = make(Op.Do);
            return new(
                () => { doOp(); return result; },
                make(Op.Undo),
                make(Op.DoDispose),
                make(Op.UndoDispose)
            );
        }

        private void assert(Op op, params int[] actions) => CollectionAssert.AreEqual(opActions[op], actions, $"{op} actions");

        private void assertEmpty(params Op[] actions) => actions.ForEach(
            op => Assert.That(opActions[op], Is.Empty, $"Expected no actions of type {op}")
        );

        [SetUp]
        public void SetUp() {
            foreach (var op in Enum.GetValues(typeof(Op)).Cast<Op>()) {
                opCount[op] = 0;
                opActions[op].Clear();
            }
        }
        
#endregion
        
#region untyped
        
#region basic functionality

        [Test]
        public void WhenConstructed_ThenDoWorks() {
            var op = makeOp();
            assertEmpty(Op.Do, Op.Undo, Op.DoDispose, Op.UndoDispose);
            op.Do();
            assertEmpty(Op.Undo, Op.DoDispose, Op.UndoDispose);
            assert(Op.Do, 1);
        }
        
        [Test]
        public void WhenConstructed_ThenUndoWorks() {
            var op = makeOp();
            assertEmpty(Op.Do, Op.Undo, Op.DoDispose, Op.UndoDispose);
            op.Undo();
            assertEmpty(Op.Do, Op.DoDispose, Op.UndoDispose);
            assert(Op.Undo, 1);
        }
        
        [Test]
        public void WhenConstructed_ThenDisposeAfterDoWorks() {
            var op = makeOp();
            assertEmpty(Op.Do, Op.Undo, Op.DoDispose, Op.UndoDispose);
            op.DisposeAfterDo();
            assertEmpty(Op.Do, Op.Undo, Op.UndoDispose);
            assert(Op.DoDispose, 1);
        }
        
        [Test]
        public void WhenConstructed_ThenDisposeAfterUndoWorks() {
            var op = makeOp();
            assertEmpty(Op.Do, Op.Undo, Op.DoDispose, Op.UndoDispose);
            op.DisposeAfterUndo();
            assertEmpty(Op.Do, Op.Undo, Op.DoDispose);
            assert(Op.UndoDispose, 1);
        }

        [Test]
        public void Empty_DoesNothing() {
            var empty = SymmetricOperation.Empty;
            Assert.That(() => {
                empty.Do();
                empty.Undo();
                empty.DisposeAfterDo();
                empty.DisposeAfterUndo();
            }, Throws.Nothing);
        }
        
#endregion basic functionality
        
#region composition

        [Test]
        public void AndThen_Simple_CallsInOrder() {
            var composed = makeOp().AndThen(makeOp());
            assertEmpty(Op.Do, Op.Undo, Op.DoDispose, Op.UndoDispose);
            
            composed.Do();
            assert(Op.Do, 1, 2);
            
            composed.Undo();
            assert(Op.Undo, 2, 1);
            
            composed.DisposeAfterDo();
            assert(Op.DoDispose, 2, 1);
            
            composed.DisposeAfterUndo();
            assert(Op.UndoDispose, 2, 1);
            
            // Ensure nothing else happened
            assert(Op.Do, 1, 2);
            assert(Op.Undo, 2, 1);
            assert(Op.DoDispose, 2, 1);
        }
        
        [Test]
        public void AndThen_ThreeInOrder_CallsInOrder() {
            var composed = makeOp().AndThen(makeOp()).AndThen(makeOp());
            assertEmpty(Op.Do, Op.Undo, Op.DoDispose, Op.UndoDispose);
            
            composed.Do();
            assert(Op.Do, 1, 2, 3);
            
            composed.Undo();
            assert(Op.Undo, 3, 2, 1);
            
            composed.DisposeAfterDo();
            assert(Op.DoDispose, 3, 2, 1);
            
            composed.DisposeAfterUndo();
            assert(Op.UndoDispose, 3, 2, 1);
            
            // Ensure nothing else happened
            assert(Op.Do, 1, 2, 3);
            assert(Op.Undo, 3, 2, 1);
            assert(Op.DoDispose, 3, 2, 1);
        }

        [Test]
        public void AndThen_TwoTwo_CallsInOrder() {
            var composed = (makeOp().AndThen(makeOp())).AndThen(makeOp().AndThen(makeOp()));
            assertEmpty(Op.Do, Op.Undo, Op.DoDispose, Op.UndoDispose);
            
            composed.Do();
            assert(Op.Do, 1, 2, 3, 4);
            
            composed.Undo();
            assert(Op.Undo, 4, 3, 2, 1);
            
            composed.DisposeAfterDo();
            assert(Op.DoDispose, 4, 3, 2, 1);
            
            composed.DisposeAfterUndo();
            assert(Op.UndoDispose, 4, 3, 2, 1);
            
            // Ensure nothing else happened
            assert(Op.Do, 1, 2, 3, 4);
            assert(Op.Undo, 4, 3, 2, 1);
            assert(Op.DoDispose, 4, 3, 2, 1);
        }

        [Test]
        public void AndThen_ThreeThree_CallsInOrder() {
            var first = makeOp().AndThen(makeOp()).AndThen(makeOp());
            var second = makeOp().AndThen(makeOp()).AndThen(makeOp());
            
            var expectedDo = new[] {1, 2, 3, 4, 5, 6};
            var expectedUndo = new[] {6, 5, 4, 3, 2, 1};

            var composed = first.AndThen(second);
            
            assertEmpty(Op.Do, Op.Undo, Op.DoDispose, Op.UndoDispose);
            
            composed.Do();
            assert(Op.Do, expectedDo);
            
            composed.Undo();
            assert(Op.Undo, expectedUndo);
            
            composed.DisposeAfterDo();
            assert(Op.DoDispose, expectedUndo); // resource disposal is backwards
            
            composed.DisposeAfterUndo();
            assert(Op.UndoDispose, expectedUndo);
            
            // Ensure nothing else happened
            assert(Op.Do, expectedDo);
            assert(Op.Undo, expectedUndo);
            assert(Op.DoDispose, expectedUndo);
        }
        
        [Test]
        public void AndThenDo_ThreeOne_CallsInOrder() {
            var first = makeOp().AndThen(makeOp()).AndThen(makeOp());
            var composed = first.AndThenDo(make(Op.Do), make(Op.Undo), make(Op.DoDispose), make(Op.UndoDispose));
            
            var expectedDo = new[] {1, 2, 3, 4};
            var expectedUndo = new[] {4, 3, 2, 1};
            
            assertEmpty(Op.Do, Op.Undo, Op.DoDispose, Op.UndoDispose);
            
            composed.Do();
            assert(Op.Do, expectedDo);
            
            composed.Undo();
            assert(Op.Undo, expectedUndo);
            
            composed.DisposeAfterDo();
            assert(Op.DoDispose, expectedUndo); // resource disposal is backwards
            
            composed.DisposeAfterUndo();
            assert(Op.UndoDispose, expectedUndo);
            
            // Ensure nothing else happened
            assert(Op.Do, expectedDo);
            assert(Op.Undo, expectedUndo);
            assert(Op.DoDispose, expectedUndo);
        }

        [Test]
        public void ComposeInOrder_WhenEmpty_ReturnsEmptySymOp() {
            var empty = Array.Empty<SymmetricOperation>().CombineInOrder();
            Assert.That(() => {
                empty.Do();
                empty.Undo();
                empty.DisposeAfterDo();
                empty.DisposeAfterUndo();
            }, Throws.Nothing);
        }

        [Test]
        public void ComposeInOrder_WithManyComplex_CallsInOrder() {
            var operation = new[] {
                makeOp().AndThen(makeOp()), 
                makeOp(), 
                makeOp(), 
                makeOp().AndThen(makeOp()),
                makeOp().AndThen(makeOp()).AndThen(makeOp())
            }.CombineInOrder();
            
            var expectedDo = new[] {1, 2, 3, 4, 5, 6, 7, 8, 9};
            var expectedUndo = new[] {9, 8, 7, 6, 5, 4, 3, 2, 1};
            
            assertEmpty(Op.Do, Op.Undo, Op.DoDispose, Op.UndoDispose);
            
            operation.Do();
            assert(Op.Do, expectedDo);
            assertEmpty(Op.Undo, Op.DoDispose, Op.UndoDispose);
            
            operation.Undo();
            assert(Op.Undo, expectedUndo);
            assertEmpty(Op.DoDispose, Op.UndoDispose);
            
            operation.DisposeAfterDo();
            assert(Op.DoDispose, expectedUndo); // resource disposal is backwards
            assertEmpty(Op.UndoDispose);
            
            operation.DisposeAfterUndo();
            assert(Op.UndoDispose, expectedUndo);
            
            // Ensure nothing else happened
            assert(Op.Do, expectedDo);
            assert(Op.Undo, expectedUndo);
            assert(Op.DoDispose, expectedUndo);
        }
        
#endregion composition
        
#endregion untyped
     
        
#region typed

#region basic functionality
        
        private const string helloWorld = "hello world!";
        
        [Test]
        public void WhenTypedConstructed_ThenDoWorks() {
            var op = makeOp(helloWorld);
            assertEmpty(Op.Do, Op.Undo, Op.DoDispose, Op.UndoDispose);
            var res = op.Do();
            Assert.That(res, Is.EqualTo(helloWorld));
            assertEmpty(Op.Undo, Op.DoDispose, Op.UndoDispose);
            assert(Op.Do, 1);
        }
        
        [Test]
        public void WhenTypedConstructed_ThenUndoWorks() {
            var op = makeOp();
            assertEmpty(Op.Do, Op.Undo, Op.DoDispose, Op.UndoDispose);
            op.Undo();
            assertEmpty(Op.Do, Op.DoDispose, Op.UndoDispose);
            assert(Op.Undo, 1);
        }
        
        [Test]
        public void WhenTypedConstructed_ThenDisposeAfterDoWorks() {
            var op = makeOp();
            assertEmpty(Op.Do, Op.Undo, Op.DoDispose, Op.UndoDispose);
            op.DisposeAfterDo();
            assertEmpty(Op.Do, Op.Undo, Op.UndoDispose);
            assert(Op.DoDispose, 1);
        }
        
        [Test]
        public void WhenTypedConstructed_ThenDisposeAfterUndoWorks() {
            var op = makeOp();
            assertEmpty(Op.Do, Op.Undo, Op.DoDispose, Op.UndoDispose);
            op.DisposeAfterUndo();
            assertEmpty(Op.Do, Op.Undo, Op.DoDispose);
            assert(Op.UndoDispose, 1);
        }
        
        // typed symmetric operation has no empty version; theres always a result

        [Test] public void WithLazyResult_WhenCalledMultipleTimes_ThenResultGetterCalledOnce() {
            var resultGetterCounter = 0;
            var lazy = makeOp().WithLazyResult(() => resultGetterCounter++);
            assertEmpty(Op.Do, Op.Undo, Op.DoDispose, Op.UndoDispose);
            
            var res1 = lazy.Do();
            Assert.That(res1, Is.EqualTo(0));
            assert(Op.Do, 1);

            lazy.Undo();
            assert(Op.Undo, 1);

            var res2 = lazy.Do();
            assert(Op.Do, 1, 1);
            
            Assert.That(res2, Is.EqualTo(0));
            Assert.That(resultGetterCounter, Is.EqualTo(1));
            
            assertEmpty(Op.DoDispose, Op.UndoDispose);
        }

#endregion basic functionality

#region composition
        
        [Test]
        public void AndThenReturn_ThreeOne_CallsInOrder() {
            var first = makeOp().AndThen(makeOp()).AndThen(makeOp());
            var doOp = make(Op.Do);
            
            var composed = first.AndThenReturn(() => {
                doOp();
                return helloWorld;
            }, make(Op.Undo), make(Op.DoDispose), make(Op.UndoDispose));
            
            var expectedDo = new[] {1, 2, 3, 4};
            var expectedUndo = new[] {4, 3, 2, 1};
            
            assertEmpty(Op.Do, Op.Undo, Op.DoDispose, Op.UndoDispose);
            
            var res = composed.Do();
            assert(Op.Do, expectedDo);
            
            Assert.That(res, Is.EqualTo(helloWorld));
            
            composed.Undo();
            assert(Op.Undo, expectedUndo);
            
            composed.DisposeAfterDo();
            assert(Op.DoDispose, expectedUndo); // resource disposal is backwards
            
            composed.DisposeAfterUndo();
            assert(Op.UndoDispose, expectedUndo);
            
            // Ensure nothing else happened
            assert(Op.Do, expectedDo);
            assert(Op.Undo, expectedUndo);
            assert(Op.DoDispose, expectedUndo);
        }
        
        [Test]
        public void TypedAndThen_ThreeThree_CallsInOrder() {
            var first = makeOp().AndThen(makeOp()).AndThen(makeOp());
            var second = makeOp().AndThen(makeOp()).AndThen(makeOp(helloWorld));
            
            var expectedDo = new[] {1, 2, 3, 4, 5, 6};
            var expectedUndo = new[] {6, 5, 4, 3, 2, 1};

            var composed = first.AndThen(second);
            
            assertEmpty(Op.Do, Op.Undo, Op.DoDispose, Op.UndoDispose);
            
            var res = composed.Do();
            assert(Op.Do, expectedDo);
            Assert.That(res, Is.EqualTo(helloWorld));
            
            composed.Undo();
            assert(Op.Undo, expectedUndo);
            
            composed.DisposeAfterDo();
            assert(Op.DoDispose, expectedUndo); // resource disposal is backwards
            
            composed.DisposeAfterUndo();
            assert(Op.UndoDispose, expectedUndo);
            
            // Ensure nothing else happened
            assert(Op.Do, expectedDo);
            assert(Op.Undo, expectedUndo);
            assert(Op.DoDispose, expectedUndo);
        }

        [Test]
        public void TypedOperation_Map_IsLazy() {
            var start = makeOp("hello");
            var mapFuncCalled = 0;
            var mapped = start.Map(s => { 
                mapFuncCalled++;
                return s + " world!";
            });
            Assert.That(mapFuncCalled, Is.Zero);
            
            var res1 = mapped.Do();
            assert(Op.Do, 1);
            
            Assert.That(res1, Is.EqualTo(helloWorld));
            Assert.That(mapFuncCalled, Is.EqualTo(1));
            
            mapped.Undo();
            assert(Op.Undo, 1);

            var res2 = mapped.Do();
            assert(Op.Do, 1, 1);

            Assert.That(res2, Is.EqualTo(helloWorld));
            Assert.That(mapFuncCalled, Is.EqualTo(2)); // result may change based on what happened since undo
            
            assertEmpty(Op.DoDispose, Op.UndoDispose);
        }

        [Test]
        public void TypedOperation_FlatMap_IsLazyAndInOrder() {
            var start = makeOp("hello");
            var mapFuncCalled = 0;
            var mapped = start.FlatMap(s => {
                mapFuncCalled++;
                return makeOp().AndThen(makeOp(s + " world!" + mapFuncCalled));
            });
            Assert.That(mapFuncCalled, Is.Zero);
            
            var res1 = mapped.Do();
            assert(Op.Do, 1, 2, 3);
            
            Assert.That(res1, Is.EqualTo(helloWorld + '1'));
            Assert.That(mapFuncCalled, Is.EqualTo(1));
            
            // ensure composition is in order
            
            mapped.Undo();
            assert(Op.Undo, 3, 2, 1);

            mapped.DisposeAfterDo();
            assert(Op.DoDispose, 3, 2, 1);

            mapped.DisposeAfterUndo();
            assert(Op.UndoDispose, 3, 2, 1);
            
            // ensure that calling again calls the mapping func again
            
            var res2 = mapped.Do();
            assert(Op.Do, 1, 2, 3, 1, 4, 5); // 1, 2, 3 from before
            
            Assert.That(res2, Is.EqualTo(helloWorld + '2'));
            Assert.That(mapFuncCalled, Is.EqualTo(2));
            
            // ensure composition is in order
            
            mapped.Undo();
            assert(Op.Undo, 3, 2, 1, 5, 4, 1);

            mapped.DisposeAfterDo();
            assert(Op.DoDispose, 3, 2, 1, 5, 4, 1);

            mapped.DisposeAfterUndo();
            assert(Op.UndoDispose, 3, 2, 1, 5, 4, 1);
        }

#endregion composition

#endregion
    }
}