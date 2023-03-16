#nullable enable

using System;
using System.Collections.Generic;

namespace BII.WasaBii.Undo {

    /// Contains the undo buffer based state for the UndoManager and encapsulates it
    internal sealed class UndoManagerState<TLabel> {

        // Naming conventions are private-based, as they are private to all but the 
        //  UndoManager and the respective names reflect the mutability of the fields

        private sealed class BackingUndoBuffer : DefaultUndoBuffer<TLabel> {
            public BackingUndoBuffer(int maxStackSize) : base(maxStackSize) { }

            // Does nothing interesting, since these are never actually called.
            // There is only one instance of this class, and it always exists as a fallback.
            public override void OnBeforeAttach() { }
            public override void OnAfterDetach() { }
        }

        public readonly UndoBuffer<TLabel> DefaultUndoBuffer;
        private readonly Stack<UndoBuffer<TLabel>> _undoBufferStack = new();

        internal UndoManagerState(int maxStackSize) {
            DefaultUndoBuffer = new BackingUndoBuffer(maxStackSize);
            _undoBufferStack.Push(DefaultUndoBuffer);
        }

        internal UndoBuffer<TLabel> currentUndoBuffer => _undoBufferStack.Peek();

        internal void pushUndoBuffer(UndoBuffer<TLabel> customBuffer) {
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
        
        // ReSharper disable four times InconsistentNaming
        internal class BufferRecordingData {
            public TLabel? _currentActionLabel = default;
            public bool _wasAborted = false;

            public readonly LinkedList<SymmetricOperation> _recordedOperations = new();
            public readonly HashSet<LinkedListNode<SymmetricOperation>> _validUndoPlaceholder = new();
        }

        private BufferRecordingData currentRecordingData => currentUndoBuffer._recordingData;

        internal IEnumerable<SymmetricOperation> recordedOperations => currentRecordingData._recordedOperations;

        internal void appendRecordedOperation(SymmetricOperation toAppend) =>
            currentRecordingData._recordedOperations.AddLast(toAppend);

        internal UndoPlaceholder appendPlaceholder() {
            var node = new LinkedListNode<SymmetricOperation>(SymmetricOperation.Empty);
            currentRecordingData._recordedOperations.AddLast(node);
            currentRecordingData._validUndoPlaceholder.Add(node);
            return new UndoPlaceholder(node);
        }

        internal void replacePlaceholder(UndoPlaceholder placeholder, SymmetricOperation op) {
            var removed = currentRecordingData._validUndoPlaceholder.Remove(placeholder.placeholderNode);
            if (!removed) throw new ArgumentException(
                "Passed UndoPlaceholder is not valid anymore. Placeholders can only be used once.");
            placeholder.placeholderNode.Value = op;
        }

        internal void finalizeRecordingOperations() {
            currentRecordingData._recordedOperations.Clear();
            currentRecordingData._validUndoPlaceholder.Clear();
            currentRecordingData._currentActionLabel = default;
        }

        internal TLabel? _currentActionLabel {
            get => currentRecordingData._currentActionLabel;
            set => currentRecordingData._currentActionLabel = value 
                ?? throw new ArgumentException("Cannot set the current undo action name to null from outside.");
        }

        internal bool _wasAborted {
            get => currentRecordingData._wasAborted;
            set => currentRecordingData._wasAborted = value;
        }
    }
}