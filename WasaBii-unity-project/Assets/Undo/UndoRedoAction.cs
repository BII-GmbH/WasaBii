using System;
using System.Collections.Generic;
using System.Linq;
using BII.WasaBii.Core;
using BII.WasaBii.Undos;

namespace BII.WasaBii.Undo {

    public class UndoAction : IDisposable {
        public string Name { get; }
        private readonly Stack<SymmetricOperation> undos;

        public UndoAction(string name, Stack<SymmetricOperation> undos) {
            this.Name = name;
            this.undos = undos;
        }

        /// <summary>
        /// Used when the undo stack is cleared in order to
        /// free resources of operations which could not have
        /// been freed while the operation was still undoable.
        /// (For example the removal of an object in the scene).
        /// This operation invalidates this object.
        /// </summary>
        public void Dispose() {
            var exceptions = new List<Exception>();
            while (undos.Count > 0)
                try {
                    undos.Pop().DisposeAfterDo();
                } catch (Exception e) { exceptions.Add(e); }
            if (exceptions.IsNotEmpty())
                throw new SummaryException(exceptions);
        }

        /// <summary>
        /// Executes this undo action. This method invalidates
        /// this undo action and returns an appropriate redo action.
        /// When an exception is thrown, all changes are redone and
        /// the exception is rethrown.
        /// Calling this after invalidation has no effect.
        /// </summary>
        public RedoAction ExecuteUndo() {
            var redoStack = new Stack<SymmetricOperation>();
            while (undos.Count > 0) {
                var undo = undos.Pop();
                try {
                    undo.Undo();
                } catch (Exception e) {
                    // Roll back as much as we can before we "rethrow" with additional data,
                    //  in order to get back into a consistent state.
                    // Note that this still fails if the current `.Undo()` caused effects before throwing.
                    undos.Push(undo);
                    while (redoStack.Count > 0) {
                        var redo = redoStack.Pop();
                        redo.Do();
                        undos.Push(redo);
                    }
                    throw new UndoException(e, UndoException.UndoInvocationType.Undo, undo.DebugInfo);
                }
                redoStack.Push(undo);
            }
            return new RedoAction(Name, redoStack);
        }
    }

    public class RedoAction : IDisposable {
        public string Name { get; }
        private readonly Stack<SymmetricOperation> redos;

        public RedoAction(string name, Stack<SymmetricOperation> redos) {
            this.Name = name;
            this.redos = redos;
        }

        /// <summary>
        /// Used when the redo stack is cleared in order to
        /// free resources of operations which have been undone.
        /// This operation invalidates this object.
        /// </summary>
        public void Dispose() {
            var exceptions = new List<Exception>();
            while (redos.Count > 0)
                try {
                    redos.Pop().DisposeAfterUndo();
                } catch (Exception e) { exceptions.Add(e); }
            if (exceptions.IsNotEmpty())
                throw new SummaryException(exceptions);
        }

        /// <summary>
        /// Executes this redo action. This method invalidates
        /// this redo action and returns an appropriate undo action.
        /// When an exception is thrown, all changes are undone and
        /// the exception is rethrown.
        /// Calling this after invalidation has no effect.
        /// </summary>
        public UndoAction ExecuteRedo() {
            var undoStack = new Stack<SymmetricOperation>();
            while (redos.Count > 0) {
                var redo = redos.Pop();
                try {
                    redo.Do();
                } catch (Exception e) {
                    // Roll back as much as we can before we "rethrow" with additional data,
                    //  in order to get back into a consistent state.
                    // Note that this still fails if the current `.Redo()` caused effects before throwing.
                    redos.Push(redo);
                    while (undoStack.Count > 0) {
                        var undo = undoStack.Pop();
                        undo.Undo();
                        redos.Push(undo);
                    }
                    throw new UndoException(e, UndoException.UndoInvocationType.Redo, redo.DebugInfo);
                }
                undoStack.Push(redo);
            }
            return new UndoAction(Name, undoStack);
        }
    }
    public class SummaryException : Exception {
        public IReadOnlyCollection<Exception> Exceptions;
        public SummaryException(IReadOnlyCollection<Exception> wrapped)
        : base(string.Join("\n", wrapped.Select(e => e.Message)), wrapped.First()) {
            Exceptions = wrapped;
        }
    }

}