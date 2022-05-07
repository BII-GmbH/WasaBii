# Undos Module

## Overview

Undos in this project are used through the `UndoManager` singleton. 

Your code can easily work with Undos by using so called `SymmetricOperation`s.
A `SymmetricOperation` consists of two action lambdas: `Do` and `Undo`, as well
as optional custom actions to dispose resources after doing or undoing the action. 

Every symmetric operation executed during the recording of an action automagically
becomes part of the recorded undo action. Calling `UndoManager.Undo()` undoes the
last completely recorded action, which is the sum of all symmetric operations in
reverse order.

## Quickstart

Symmetric operations are not constructed directly. Instead, they are constructed,
stored and immediately ran by calling the following code with two to four `Action`s:

```cs
UndoManager.RegisterAndExecute(do, undo[, disposeAfterDo, disposeAfterUndo]);
```

Registering an action does not make a full undo. Instead, you have to do the following:

```cs
UndoManager.StartRecordingAction("My action name");
// some code that executes RegisterAndExecute somewhere...
UndoManager.StopRecordingAction();
```

Every `SymmetricOperation` registered during recording will be undone in reverse 
order, and redone in the same order as executed during recording.

As a shortcut for short undo actions, you may execute:

```cs
UndoManager.RecordCompleteAction("action name", () => { ... });
```

where the passed function may optionally be `async`.

You can set `UndoManager.CurrentActionName` at any time *during a recording*.
You can also pass a final name to `StopRecordingAction`, in case more specific
information became available while recording.

When an error happens, you may want to abort. `UndoManager.AbortRecordingAction()`
aborts the current recording and undoes every symmetric action recorded since the 
beginning of the recording. In general, when an `UndoManager.Undo()` or `UndoManager.Redo()`
fails, the undoing is redone and the original exception is rethrown properly. 

## Overriding Undo Behavior Temporarily

Sometimes, you want a custom undo mechanism 
(for example when creating a user-guided workflow with small steps).
In this case, it makes no sense to completely design your custom undo mechanisms. 
Instead, you may create a custom class that implements the `UndoBuffer` interface to 
override how completely recorded actions are handled and the undo mechanism works 
relative to them. 

The `UndoManager` singleton always has an underlying `UndoBuffer` as default, and 
may accept custom buffers at any time by calling `UndoManager.PushUndoBuffer(buffer)`.
Calling `UndoManager.PopUndoBuffer()` restores the last pushed buffer with its
state before attaching the custom undo buffer.

In case you want the default behavior in a scope only, you may extend the abstract
`DefaultUndoBuffer` class instead. This class only requires you to implement two
callback methods: `OnBeforeAttach` and `OnAfterDetach`. The former allows you to 
execute code based on the previous UndoBuffer (which might be the default buffer),
e.g. to copy the underlying UndoStack and transform it, or redirect to it if required.
The latter allows you to do cleanup code like moving the current `UndoAction`s into the
default buffer.

You may use a RAII-like pattern with the `using` statement by having the custom undo buffer
implement `IDisposable`, attach itself during its constructor, and detach itself during the 
`Dispose` method. 

