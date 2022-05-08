using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BII.WasaBii.Core;
using BII.WasaBii.Undo.Logic;
using BII.WasaBii.Undos;
using JetBrains.Annotations;
using UnityEngine;

namespace BII.WasaBii.Undo {
    
    // TODO CR: not just a `string name`, but any generic object with undo operation metadata.
    // The user might want images or other associations in there.
    // Note: also adjust docs!

    [CannotBeSerialized("Undos are closure-based. Do not serialize closures.")]
    public class UndoManager {

        public event Action OnAfterActionRecorded;
        public event Action<int> OnAfterUndo;
        public event Action<int> OnAfterRedo;
        public event Action OnUndoBufferPushed;
        public event Action OnUndoBufferPopped;

        private bool _currentlyRegistering = false;
        private readonly UndoManagerState state;
        
        /// <param name="maxUndoStackSize">
        /// The default <see cref="UndoBuffer"/> will only store up to this many undo or redo operations.
        /// When the number of operations would exceed that number, the oldest operation is removed and freed.
        /// </param>
        public UndoManager(int maxUndoStackSize) => state = new UndoManagerState(maxUndoStackSize);

        public void PushUndoBuffer([NotNull] UndoBuffer customBuffer) {
            state.pushUndoBuffer(customBuffer);
            OnUndoBufferPushed?.Invoke();
        }

        public void PopUndoBuffer() {
            state.popUndoBuffer();
            OnUndoBufferPopped?.Invoke();
        }

        public IEnumerable<string> UndoLabels => state.currentUndoBuffer.UndoStack.Select(a => a.Name);
        public IEnumerable<string> RedoLabels => state.currentUndoBuffer.RedoStack.Select(a => a.Name);

        public bool IsRecording => state._currentActionName != null;
        
        [CanBeNull]
        public string CurrentActionName {
            get => state._currentActionName; set {
                if (state._currentActionName == null) throw new InvalidOperationException(
                    "Cannot set an undo action name when there is no action running.");
                state._currentActionName = value;
            }
        }

        /// Can be used to inject undo logic into a specific point in the undo stack.
        /// Get one by calling <see cref="UndoManager.RegisterUndoPlaceholder"/>.
        /// Call `UndoManager.RegisterAndExecute` and pass the placeholder to execute
        /// the undo at the point when the placeholder has been inserted rather than
        /// later. This invalidates the placeholder.
        public sealed class UndoPlaceholder {
            internal readonly LinkedListNode<SymmetricOperation> placeholderNode;
            internal UndoPlaceholder(LinkedListNode<SymmetricOperation> placeholderNode) =>
                this.placeholderNode = placeholderNode;
        }

        /// Appends a placeholder to the undo stack. This placeholder can later be used
        /// to inject only the undo-part of a symmetric operation into that place, even
        /// after other symmetric operations have been registered and executed since then.
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
            [NotNull] Action action,
            [NotNull] Action undo,
            [CanBeNull] Action disposeAfterDo = null,
            [CanBeNull] Action disposeAfterUndo = null,
            [CanBeNull] UndoPlaceholder saveUndoAt = null,
            bool warningOnNotRecording = true,
            [CallerMemberName] string __callerMemberName = "",
            [CallerFilePath] string __sourceFilePath = "",
            [CallerLineNumber] int __sourceLineNumber = 0
        ) => registerAndExecuteInternal(
            new SymmetricOperation<object>(
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
            [CanBeNull] UndoPlaceholder saveUndoAt = null,
            bool warningOnNotRecording = true
        ) => registerAndExecuteInternal(operation.WithResult<object>(null), saveUndoAt, warningOnNotRecording);

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
            [CanBeNull] UndoPlaceholder saveUndoAt = null,
            bool warningOnNotRecording = true
        ) => registerAndExecuteInternal(operation, saveUndoAt, warningOnNotRecording);

        private T registerAndExecuteInternal<T>(
            SymmetricOperation<T> operation,
            [CanBeNull] UndoPlaceholder saveUndoAt,
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
                    Debug.LogWarning("Registering an undo action while not recording. Is this intentional?");
                operation.DisposeAfterDo?.Invoke(); // not saved, so we dispose instantly if necessary
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
        public void StartRecordingAction(string initialName) {
            if (IsRecording) {
                Debug.LogWarning($"Started recording undo action {initialName} while the action " +
                                 $"{state._currentActionName} was still recording. Stopping running recording...");
                StopRecordingAction();
            }

            state._wasAborted = false;
            state._currentActionName = initialName 
                ?? throw new ArgumentNullException(nameof(initialName));
        }

        /// <summary>
        /// Stops the action being recorded and saves it to the undo stack.
        /// When no action is being recorded, an InvalidOperationException is thrown.
        /// Logs a warning when stopped without any symmetric operations being registered.
        /// Returns null when the action was aborted before calling this.
        /// </summary>
        [CanBeNull]
        public UndoAction StopRecordingAction(string finalName = null) {
            if (!IsRecording) throw new InvalidOperationException(
                "Tried to stop recording an undo action without starting one.");

            if (state._wasAborted) {
                state._wasAborted = false;
                return null;
            }

            if (finalName != null) state._currentActionName = finalName;

            if (state.recordedOperations.IsEmpty()) Debug.LogWarning(
                $"Operation {state._currentActionName} saved without anything to undo.");

            var res = new UndoAction(state._currentActionName, new Stack<SymmetricOperation>(state.recordedOperations));

            state.currentUndoBuffer.RegisterUndo(res);

            state.finalizeRecordingOperations();
            state.currentUndoBuffer.ClearRedoStack();

            OnAfterActionRecorded?.Invoke();
            
            return res;
        }

        /// <summary>
        /// Executes the passed action between starting and stopping the recording of the action. 
        /// </summary>
        public async Task RecordCompleteAction(string actionName, Func<Task> action) {
            StartRecordingAction(actionName);
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
        public void RecordCompleteAction(string actionName, Action action) {
            StartRecordingAction(actionName);
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
        public T RecordCompleteAction<T>(string actionName, Func<T> func) {
            StartRecordingAction(actionName);
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

            if (state.recordedOperations.IsNotEmpty()) Debug.LogWarning(
                $"Operation {state._currentActionName} aborted with registered undos. These undos are discarded");

            var undoAction = new UndoAction("abort", new Stack<SymmetricOperation>(state.recordedOperations));
            var redoAction = undoAction.ExecuteUndo();
            redoAction.Dispose();

            state.finalizeRecordingOperations();
            state._wasAborted = true;
        }
        
        /// Undos at most n actions. Returns the number of actions actually undone.
        public int Undo(int n = 1) {
            if (IsRecording)
                throw new InvalidOperationException(
                    $"Cannot undo: currently recording {state._currentActionName}.");
            var undoCount = state.currentUndoBuffer.Undo(n);
            OnAfterUndo?.Invoke(undoCount);
            return undoCount;
        }
        
        /// Redos at most n actions. Returns the number of actions actually redone.
        public int Redo(int n = 1) {
            if (IsRecording) throw new InvalidOperationException(
                $"Cannot redo: currently recording {state._currentActionName}.");
            var redoCount =  state.currentUndoBuffer.Redo(n);
            OnAfterRedo?.Invoke(redoCount);
            return redoCount;
        }
    }
}