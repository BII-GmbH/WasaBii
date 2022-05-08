using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BII.WasaBii.Core;
using BII.WasaBii.Undo.Logic;

// Note CR: So that we can mock the `_recordingData` in unit tests
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace BII.WasaBii.Undo {

    /// Class that handles the current behavior of the UndoManager.
    /// It manages completed undo actions and the actual undo and redo logic.
    public interface UndoBuffer {
        
        void RegisterUndo(UndoAction res);

        /// Undos at most n actions. Returns the number of actions actually undone.
        int Undo(int n);
        
        /// Redos at most n actions. Returns the number of actions actually redone.
        int Redo(int n);

        void ClearUndoStack();
        void ClearRedoStack();
        
        /// Called before this buffer is pushed to the undo buffer stack.
        void OnBeforeAttach();
        
        /// Called after this buffer is popped from the undo buffer stack.
        void OnAfterDetach();
        
        IEnumerable<UndoAction> UndoStack { get; }
        IEnumerable<RedoAction> RedoStack { get; }
        
        internal UndoManagerState.BufferRecordingData _recordingData { get; }
    }
    
    
    /// Default implementation for the UndoBuffer interface
    /// with callbacks to be implemented by concrete implementations.
    /// These callbacks manage what may relative to the current state
    /// of the UndoManager.
    public abstract class DefaultUndoBuffer : UndoBuffer {
        
        /// Every undo or redo after this causes the oldest undo or redo
        /// to be forgotten and its resources to be freed.
        public readonly int MaxStackSize;
            
        private readonly MaxSizeStack<UndoAction> _undoStack;
        private readonly MaxSizeStack<RedoAction> _redoStack;

        private readonly UndoManagerState.BufferRecordingData _recordingData;
        
        /// <param name="maxStackSize">
        /// This buffer will only store up to this many undo or redo operations.
        /// When the number of operations would exceed that number, the oldest operation is removed and freed.
        /// </param>
        protected DefaultUndoBuffer(int maxStackSize) {
            MaxStackSize = maxStackSize;
            _undoStack = new MaxSizeStack<UndoAction>(maxStackSize);
            _redoStack = new MaxSizeStack<RedoAction>(maxStackSize);
            _recordingData = new UndoManagerState.BufferRecordingData();
        }

        public IEnumerable<UndoAction> UndoStack => _undoStack;
        public IEnumerable<RedoAction> RedoStack => _redoStack;

        public abstract void OnBeforeAttach();
        public abstract void OnAfterDetach();

        public void RegisterUndo(UndoAction res) => _undoStack.Push(res);

        public int Undo(int n) {
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

        public int Redo(int n) {
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

        public void ClearUndoStack() {
            _undoStack.ForEach(u => u.Dispose());
            _undoStack.Clear();
        }

        public void ClearRedoStack() {
            _redoStack.ForEach(r => r.Dispose());
            _redoStack.Clear();
        }
        
        UndoManagerState.BufferRecordingData UndoBuffer._recordingData => _recordingData;
    }
    
}