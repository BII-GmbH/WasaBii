using System;
using System.Collections.Generic;
using BII.WasaBii.Core;

namespace BII.WasaBii.Undo {

    /// <summary>
    /// Class that handles the current behavior of the UndoManager.
    /// It manages completed undo actions and the actual undo and redo logic.
    /// </summary>
    public abstract class UndoBuffer<TLabel> {

        public abstract void RegisterUndo(UndoAction<TLabel> res);

        /// <summary> Undos at most n actions. Returns the number of actions actually undone. </summary>
        public abstract int Undo(int n);

        /// <summary> Redos at most n actions. Returns the number of actions actually redone. </summary>
        public abstract int Redo(int n);

        public abstract void ClearUndoStack();
        public abstract void ClearRedoStack();

        /// <summary> Called before this buffer is pushed to the undo buffer stack. </summary>
        public abstract void OnBeforeAttach();

        /// <summary> Called after this buffer is popped from the undo buffer stack. </summary>
        public abstract void OnAfterDetach();

        public abstract IEnumerable<UndoAction<TLabel>> UndoStack { get; }
        public abstract IEnumerable<RedoAction<TLabel>> RedoStack { get; }

        internal readonly UndoManagerState<TLabel>.BufferRecordingData _recordingData = new();
    }


    /// <summary>
    /// Default implementation for the UndoBuffer interface
    /// with callbacks to be implemented by concrete implementations.
    /// These callbacks manage what may relative to the current state
    /// of the UndoManager.
    /// </summary>
    public abstract class DefaultUndoBuffer<TLabel> : UndoBuffer<TLabel> {

        /// <summary>
        /// Every undo or redo after this causes the oldest undo or redo
        /// to be forgotten and its resources to be freed.
        /// </summary>
        public readonly int MaxStackSize;

        private readonly MaxSizeStack<UndoAction<TLabel>> _undoStack;
        private readonly MaxSizeStack<RedoAction<TLabel>> _redoStack;

        /// <param name="maxStackSize">
        /// This buffer will only store up to this many undo or redo operations.
        /// When the number of operations would exceed that number, the oldest operation is removed and freed.
        /// </param>
        protected DefaultUndoBuffer(int maxStackSize) {
            MaxStackSize = maxStackSize;
            _undoStack = new MaxSizeStack<UndoAction<TLabel>>(maxStackSize);
            _redoStack = new MaxSizeStack<RedoAction<TLabel>>(maxStackSize);
        }

        public sealed override IEnumerable<UndoAction<TLabel>> UndoStack => _undoStack;
        public sealed override IEnumerable<RedoAction<TLabel>> RedoStack => _redoStack;

        public sealed override void RegisterUndo(UndoAction<TLabel> res) => _undoStack.Push(res);

        public sealed override int Undo(int n) {
            for (var i = 0; i < n; ++i) {
                if (_undoStack.Count == 0) return i;
                var undo = _undoStack.Pop();
                try {
                    _redoStack.Push(undo.ExecuteUndo());
                } catch (Exception) {
                    _undoStack.Push(undo);
                    throw;
                }
            }
            return n;
        }

        public sealed override int Redo(int n) {
            for (var i = 0; i < n; ++i) {
                if (_redoStack.Count == 0) return i;
                var redo = _redoStack.Pop();
                try {
                    _undoStack.Push(redo.ExecuteRedo());
                } catch (Exception) {
                    _redoStack.Push(redo);
                    throw;
                }
            }
            return n;
        }

        public sealed override void ClearUndoStack() {
            _undoStack.ForEach(u => u.Dispose());
            _undoStack.Clear();
        }

        public sealed override void ClearRedoStack() {
            _redoStack.ForEach(r => r.Dispose());
            _redoStack.Clear();
        }
    }

}