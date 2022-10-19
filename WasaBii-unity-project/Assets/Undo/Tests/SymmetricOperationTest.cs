using System;
using System.Collections.Generic;
using System.Linq;
using BII.WasaBii.Core;
using BII.WasaBii.Undos;
using NUnit.Framework;

namespace BII.WasaBii.Undo.Tests {
    
    public class SymmetricOperationTest
    {
        
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
        public void Single_WhenConstructed_ThenDisposeAfterDoWorks() {
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
        
#endregion
        
#endregion untyped
        
        // TODO: typed symop tests, map, flatmap, etc
    }
}