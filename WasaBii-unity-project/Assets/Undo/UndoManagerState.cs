using System;
using System.Collections.Generic;
using BII.WasaBii.Undos;
using JetBrains.Annotations;

namespace BII.WasaBii.Undo {

    /// Contains the undo buffer based state for the UndoManager and encapsulates it
    internal sealed class UndoManagerState {

        // Naming conventions are private-based, as they are private to all but the 
        //  UndoManager and the respective names reflect the mutability of the fields

        private sealed class BackingUndoBuffer : DefaultUndoBuffer {
            public BackingUndoBuffer(int maxStackSize) : base(maxStackSize) { }

            // Does nothing interesting, since these are never actually called.
            // There is only one instance of this class, and it always exists as a fallback.
            public override void OnBeforeAttach() { }
            public override void OnAfterDetach() { }
        }

        public readonly UndoBuffer DefaultUndoBuffer;
        private readonly Stack<UndoBuffer> _undoBufferStack = new();

        internal UndoManagerState(int maxStackSize) {
            DefaultUndoBuffer = new BackingUndoBuffer(maxStackSize);
            _undoBufferStack.Push(DefaultUndoBuffer);
        }

        internal UndoBuffer currentUndoBuffer => _undoBufferStack.Peek();

        internal void pushUndoBuffer([NotNull] UndoBuffer customBuffer) {
            if (customBuffer == null) throw new ArgumentNullException(nameof(customBuffer));
            customBuffer.OnBeforeAttach();
            _undoBufferStack.Push(customBuffer);
        }

        internal void popUndoBuffer() {
            if (_undoBufferStack.Count == 1) 
                throw new InvalidOperationException("No custom undo buffers on stack");
            var popped = _undoBufferStack.Pop();
            popped.OnAfterDetach();
        }

        internal class BufferRecordingData {
            [CanBeNull] public string _currentActionName = null;
            public bool _wasAborted = false;

            public readonly LinkedList<SymmetricOperation> _recordedOperations = new();
            public readonly HashSet<LinkedListNode<SymmetricOperation>> _validUndoPlaceholder = new();
        }

        private BufferRecordingData currentRecordingData => currentUndoBuffer._recordingData;

        internal IEnumerable<SymmetricOperation> recordedOperations => currentRecordingData._recordedOperations;

        internal void appendRecordedOperation(SymmetricOperation toAppend) =>
            currentRecordingData._recordedOperations.AddLast(toAppend);

        internal UndoManager.UndoPlaceholder appendPlaceholder() {
            var node = new LinkedListNode<SymmetricOperation>(SymmetricOperation.Empty);
            currentRecordingData._recordedOperations.AddLast(node);
            currentRecordingData._validUndoPlaceholder.Add(node);
            return new UndoManager.UndoPlaceholder(node);
        }

        internal void replacePlaceholder(UndoManager.UndoPlaceholder placeholder, SymmetricOperation op) {
            var removed = currentRecordingData._validUndoPlaceholder.Remove(placeholder.placeholderNode);
            if (!removed) throw new ArgumentException(
                "Passed UndoPlaceholder is not valid anymore. Placeholders can only be used once.");
            placeholder.placeholderNode.Value = op;
        }

        internal void finalizeRecordingOperations() {
            currentRecordingData._recordedOperations.Clear();
            currentRecordingData._validUndoPlaceholder.Clear();
            currentRecordingData._currentActionName = null;
        }

        [CanBeNull] internal string _currentActionName {
            get => currentRecordingData._currentActionName;
            set => currentRecordingData._currentActionName = value 
                ?? throw new ArgumentException("Cannot set the current undo action name to null from outside.");
        }

        internal bool _wasAborted {
            get => currentRecordingData._wasAborted;
            set => currentRecordingData._wasAborted = value;
        }
    }
}