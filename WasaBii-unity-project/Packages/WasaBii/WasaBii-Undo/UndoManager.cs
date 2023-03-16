#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BII.WasaBii.Core;

namespace BII.WasaBii.Undo {
    
    /// <summary>
    /// Can be used to inject undo logic into a specific point in the undo stack.
    /// Get one by calling <see cref="UndoManager{TLabel}.RegisterUndoPlaceholder"/>.
    /// Call `UndoManager.RegisterAndExecute` and pass the placeholder to execute
    ///  the undo at the point when the placeholder has been inserted rather than later.
    /// This invalidates the placeholder.
    /// </summary>
    public sealed class UndoPlaceholder {
        internal readonly LinkedListNode<SymmetricOperation> placeholderNode;
        internal UndoPlaceholder(LinkedListNode<SymmetricOperation> placeholderNode) =>
            this.placeholderNode = placeholderNode;
    }
    
    public class UndoManager<TLabel> {

        public event Action? OnAfterActionRecorded;
        public event Action<int>? OnAfterUndo;
        public event Action<int>? OnAfterRedo;
        public event Action? OnUndoBufferPushed;
        public event Action? OnUndoBufferPopped;

        private bool _currentlyRegistering = false;
        private readonly UndoManagerState<TLabel> state;
        private readonly Action<string> handleWarning;

        /// <param name="maxUndoStackSize">
        /// The default <see cref="UndoBuffer{TLabel}"/> will only store up to this many undo or redo operations.
        /// When the number of operations would exceed that number, the oldest operation is removed and freed.
        /// </param>
        public UndoManager(int maxUndoStackSize, Action<string> handleWarning) {
            state = new UndoManagerState<TLabel>(maxUndoStackSize);
            this.handleWarning = handleWarning;
        }

        public void PushUndoBuffer(UndoBuffer<TLabel> customBuffer) {
            state.pushUndoBuffer(customBuffer);
            OnUndoBufferPushed?.Invoke();
        }

        public void PopUndoBuffer() {
            state.popUndoBuffer();
            OnUndoBufferPopped?.Invoke();
        }

        public IEnumerable<TLabel> UndoLabels => state.currentUndoBuffer.UndoStack.Select(a => a.Label);
        public IEnumerable<TLabel> RedoLabels => state.currentUndoBuffer.RedoStack.Select(a => a.Label);

        public bool IsRecording => state._currentActionLabel != null;
        
        public TLabel? CurrentActionLabel {
            get => state._currentActionLabel; set {
                if (state._currentActionLabel == null) throw new InvalidOperationException(
                    "Cannot set an undo action name when there is no action running.");
                state._currentActionLabel = value;
            }
        }
        
        /// <summary>
        /// Appends a placeholder to the undo stack. This placeholder can later be used
        /// to inject only the undo-part of a symmetric operation into that place, even
        /// after other symmetric operations have been registered and executed since then.
        /// </summary>
        /// <seealso cref="UndoPlaceholder"/>
        public UndoPlaceholder RegisterUndoPlaceholder() {
            if (!IsRecording)
                throw new InvalidOperationException("Cannot register an undo placeholder when not recording.");
            return state.appendPlaceholder();
        }

        /// <summary>
        /// Registers the passed action together with an undo-action 
        /// to the currently running recording and executes the action. 
        /// When nothing is being recorded, a warning is logged.
        /// </summary>
        /// <param name="action">Code that is executed immediately and on every redo</param>
        /// <param name="undo">Code executed on every undo</param>
        /// <param name="disposeAfterDo">
        /// This is called when an operation has been on the undo stack
        /// for too long and the stack exceeds the max undo stack size.
        /// This enables resources to be freed, for example actually
        /// destroying an object instead of just disabling it in case
        /// the operation is undone.
        /// </param>
        /// <param name="disposeAfterUndo">
        /// This is called when the operation is undone and either
        /// the redo stack is cleared or the recording is aborted.
        /// Some operations (like instantiation) introduce dependencies with following
        /// operations, which might break consecutive redos, and thus need custom
        /// dispose logic to clear resources when the redo stack is discarded.
        /// </param>
        /// <param name="warningOnNotRecording">
        /// When true, emits a warning when not recording. When false, the undo operation is discarded silently.
        /// </param>
        /// <param name="saveUndoAt">
        /// When present, then the undo operation is executed at the point where the placeholder was executed.
        /// This means that all undos registered between the placeholder registration and this call will be 
        /// executed first, in reverse order, before the passed undo operation will finally be executed.
        /// Note that only <paramref name="undo"/> is inserted at the placeholder.
        /// <paramref name="action"/> is still executed after all previously registered actions.
        /// </param>
        public void RegisterAndExecute(
            Action action,
            Action undo,
            Action? disposeAfterDo = null,
            Action? disposeAfterUndo = null,
            UndoPlaceholder? saveUndoAt = null,
            bool warningOnNotRecording = true,
            // ReSharper disable thrice InvalidXmlDocComment
            [CallerMemberName] string __callerMemberName = "",
            [CallerFilePath] string __sourceFilePath = "",
            [CallerLineNumber] int __sourceLineNumber = 0
        ) => registerAndExecuteInternal(
            new SymmetricOperation<object?>(
                () => { action(); return null; }, undo, disposeAfterDo, disposeAfterUndo, 
                // ReSharper disable thrice ExplicitCallerInfoArgument // intentional
                __callerMemberName,
                __sourceFilePath,
                __sourceLineNumber
            ),
            saveUndoAt, 
            warningOnNotRecording
        );

        /// <summary>
        /// Registers the passed symmetric operation to the currently running recording and
        /// executes the operation's do-action. When nothing is being recorded, a warning is logged.
        /// </summary>
        /// <param name="warningOnNotRecording">
        /// When true, emits a warning when not recording. When false, the undo operation is discarded silently.
        /// </param>
        /// <param name="saveUndoAt">
        /// When present, then the undo operation is executed at the point where the placeholder was executed.
        /// This means that all undos registered between the placeholder registration and this call will be 
        /// executed first, in reverse order, before the passed undo operation will finally be executed.
        /// Note that only operation.Undo is inserted at the placeholder.
        /// `operation.Do` is still executed after all previously registered actions.
        /// </param>
        public void RegisterAndExecute(
            in SymmetricOperation operation, 
            UndoPlaceholder? saveUndoAt = null,
            bool warningOnNotRecording = true
        ) => registerAndExecuteInternal(operation.WithResult<object?>(null), saveUndoAt, warningOnNotRecording);

        /// <summary>
        /// Registers the passed symmetric operation to the currently running recording and
        /// executes the operation's do-action. When nothing is being recorded, a warning is logged.
        /// </summary>
        /// <param name="warningOnNotRecording">
        /// When true, emits a warning when not recording. When false, the undo operation is discarded silently.
        /// </param>
        /// <param name="saveUndoAt">
        /// When present, then the undo operation is executed at the point where the placeholder was executed.
        /// This means that all undos registered between the placeholder registration and this call will be 
        /// executed first, in reverse order, before the passed undo operation will finally be executed.
        /// Note that only operation.Undo is inserted at the placeholder.
        /// `operation.Do` is still executed after all previously registered actions.
        /// </param>
        public T RegisterAndExecute<T>(
            in SymmetricOperation<T> operation,
            UndoPlaceholder? saveUndoAt = null,
            bool warningOnNotRecording = true
        ) => registerAndExecuteInternal(operation, saveUndoAt, warningOnNotRecording);

        private T registerAndExecuteInternal<T>(
            SymmetricOperation<T> operation,
            UndoPlaceholder? saveUndoAt,
            bool warningOnNotRecording
        ) {
            // this extra variable is important, since recording can be started in `action()`
            // but the stop can happen long afterwards. This is unlikely, but an edge case.
            var startedRegistering = false;
            if (IsRecording) {
                if (_currentlyRegistering) throw new InvalidOperationException(
                    "Cannot register a new action while another action is executed, " +
                    "since nested symmetrical actions lead to error-prone undos.");
                _currentlyRegistering = startedRegistering = true;
            }

            T result;
            try {
                result = operation.Do();
            } finally {
                if (startedRegistering)
                    _currentlyRegistering = false;
            }

            // register only if no exception has been thrown
            if (!IsRecording) {
                if (warningOnNotRecording)
                    handleWarning("Registering an undo action while not recording. Is this intentional?");
                operation.DisposeAfterDo.Invoke(); // not saved, so we dispose instantly if necessary
            } else {
                if (saveUndoAt != null) {
                    state.replacePlaceholder(saveUndoAt, 
                        new SymmetricOperation(
                            action: () => { }, 
                            operation.Undo, 
                            disposeAfterDo: () => { }, 
                            operation.DisposeAfterUndo
                        )
                    );
                    state.appendRecordedOperation(new SymmetricOperation(
                        () => operation.Do(), 
                        undo: () => { },
                        operation.DisposeAfterDo, 
                        disposeAfterUndo: () => { }
                    ));
                } else state.appendRecordedOperation(operation.WithoutResult());
            }

            return result;
        }

        /// <summary>
        /// Starts recording an undoable action with the specified name.
        /// Logs a warning if a recording is already running and stops and saves that recording.
        /// </summary>
        public void StartRecordingAction(TLabel initialLabel) {
            if (IsRecording) {
                handleWarning($"Started recording undo action {initialLabel} while the action " +
                                 $"{state._currentActionLabel} was still recording. Stopping running recording...");
                StopRecordingAction();
            }

            state._wasAborted = false;
            state._currentActionLabel = initialLabel ?? throw new ArgumentNullException(nameof(initialLabel));
        }

        /// <summary>
        /// Stops the action being recorded and saves it to the undo stack.
        /// When no action is being recorded, an InvalidOperationException is thrown.
        /// Logs a warning when stopped without any symmetric operations being registered.
        /// Returns null when the action was aborted before calling this.
        /// </summary>
        public UndoAction<TLabel>? StopRecordingAction(TLabel? finalLabel = default) {
            if (!IsRecording) throw new InvalidOperationException(
                "Tried to stop recording an undo action without starting one.");

            if (state._wasAborted) {
                state._wasAborted = false;
                return null;
            }

            if (!Equals(finalLabel, default(TLabel))) 
                state._currentActionLabel = finalLabel;

            if (state.recordedOperations.IsEmpty()) handleWarning(
                $"Operation {state._currentActionLabel} saved without anything to undo.");

            var res = new UndoAction<TLabel>(state._currentActionLabel!, new Stack<SymmetricOperation>(state.recordedOperations));

            state.currentUndoBuffer.RegisterUndo(res);

            state.finalizeRecordingOperations();
            state.currentUndoBuffer.ClearRedoStack();

            OnAfterActionRecorded?.Invoke();

            return res;
        }

        /// <summary>
        /// Executes the passed action between starting and stopping the recording of the action. 
        /// </summary>
        public async Task RecordCompleteAction(TLabel actionLabel, Func<Task> action) {
            StartRecordingAction(actionLabel);
            try {
                await action();
            } catch (Exception) {
                AbortRecordingAction();
                throw;
            }
            // Intentionally not inside the try block, because we don't want to catch exceptions thrown by StopRecordingAction
            StopRecordingAction();
        }

        /// <summary>
        /// Executes the passed action between starting and stopping the recording of the action. 
        /// </summary>
        public void RecordCompleteAction(TLabel actionLabel, Action action) {
            StartRecordingAction(actionLabel);
            try {
                action();
            } catch (Exception) {
                AbortRecordingAction();
                throw;
            }
            // Intentionally not inside the try block, because we don't want to catch exceptions thrown by StopRecordingAction
            StopRecordingAction();
        }

        /// <summary>
        /// Executes the passed func between starting and stopping the recording of the action.
        /// The result of the func is returned when the recording is stopped. 
        /// </summary>
        public T RecordCompleteAction<T>(TLabel actionInfo, Func<T> func) {
            StartRecordingAction(actionInfo);
            T res;
            try {
                res = func();
            } catch (Exception) {
                AbortRecordingAction();
                throw;
            }
            // Intentionally not inside the try block, because we don't want to catch exceptions thrown by StopRecordingAction
            StopRecordingAction();
            return res;
        }

        /// <summary>
        /// Stops the action being recorded without saving it to the undo stack.
        /// When no action is being recorded, an InvalidOperationException is thrown.
        /// Logs a warning when stopped while any symmetric operations were registered,
        /// undoes them in order and then disposes the resulting redo stack.
        /// </summary>
        public void AbortRecordingAction() {
            if (!IsRecording) throw new InvalidOperationException(
               "Tried to abort recording an undo action without starting one.");

            if (state.recordedOperations.IsNotEmpty()) handleWarning(
                $"Operation {state._currentActionLabel} aborted with registered undos. These undos are discarded");

            var undoAction = new UndoAction<string>(
                "Abort Recording", 
                new Stack<SymmetricOperation>(state.recordedOperations)
            );
            
            var redoAction = undoAction.ExecuteUndo();
            redoAction.Dispose();

            state.finalizeRecordingOperations();
            state._wasAborted = true;
        }

        /// <summary> Undos at most n actions. Returns the number of actions actually undone. </summary>
        public int Undo(int n = 1) {
            if (IsRecording)
                throw new InvalidOperationException(
                    $"Cannot undo: currently recording {state._currentActionLabel}.");
            var undoCount = state.currentUndoBuffer.Undo(n);
            OnAfterUndo?.Invoke(undoCount);
            return undoCount;
        }

        /// <summary> Redoes at most n actions. Returns the number of actions actually redone. </summary>
        public int Redo(int n = 1) {
            if (IsRecording) throw new InvalidOperationException(
                $"Cannot redo: currently recording {state._currentActionLabel}.");
            var redoCount =  state.currentUndoBuffer.Redo(n);
            OnAfterRedo?.Invoke(redoCount);
            return redoCount;
        }
    }
}