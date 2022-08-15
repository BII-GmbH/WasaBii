# Undos Module

## Overview

An instance of the `UndoManager` class represents a complete and customizable undo system.

Your code can easily work with Undos by using so called `SymmetricOperation`s.
A `SymmetricOperation` consists of two action lambdas: `Do` and `Undo`, as well
as optional custom actions to dispose resources after doing or undoing the action.

Every symmetric operation executed during the recording of an action automagically
becomes part of the recorded undo action (by using `UndoManager.RegisterAndExecute(op)`).
Calling `UndoManager.Undo()` undoes the last completely recorded action, which is the sum
of all symmetric operations in reverse order.

## Quickstart

Symmetric operations do not need to be constructed directly. Instead, they are constructed,
stored and immediately ran by calling the following code with two to four `Action`s:

```cs
UndoManager.RegisterAndExecute(do, undo[, disposeAfterDo, disposeAfterUndo]);
```

Registering an action does not make a full undo. Instead, you have to do the following:

```cs
UndoManager.StartRecordingAction("My action name");
// some code that calls RegisterAndExecute at least once ...
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

## Registering a Placeholder

Sometimes, you cannot know in advance how a certain undo step will look like.

For example, your operation modifies some state, and after everything has happened,
you need to set imagine having some `flag` which represents an important part of state.
Your operation will include a lot of code which might read and set the flag, and you do
not know in which ways yet. But at the end, you always want to set the flag to `done`.

Now, when the operation is undone, the `flag` might get set to all kinds of values again.
But after the *undo*, you want the flag to be the original value for sure.

The regular undo mechanism doesn't support this easily. If you just do a single
`.RegisterAndExecute` for setting the flag in the end, then that `undo` will be called
*first*, before the operation undo will modify the flag again all over the place.

In order to support this use-case, you can use `UndoManager.RegisterUndoPlaceholder()`.
This method returns an `UndoPlaceholder` instance and saves an *empty slot* in the undo
stack, which can later be replaced with a concrete undo action once you know the details.

You can then pass this `UndoPlaceholder` instance to a call of `.RegisterAndExecute`, which
will cause the undo (and *only* the undo) to be executed when the placeholder was originally
registered.

For the case above, this means: you register a placeholder, then do the complex operation.
Finally, you register a symmetric operation which sets the flag to `done` in do, and to
the original value in `undo`, but you also pass your placeholder. That way, the undo
will be executed last, and not first.

## Intentions and How It All Works

The UndoManager is designed with an expected "usage model" in mind:

The topmost code - directly at the level of user interactions - manages the *recording*.
At the beginning of the user input handling code, you call `.StartRecordingAction`.
At the end of the user input handling code, you call `.StopRecordingAction`.
The recorded action is now a *single action* that makes sense to the user, with a distinct name.
This action can be displayed in a list, and a single `.Undo()` undoes it. This is the "user" level.

The bottommost code - *the lowest point in the call stack where state is mutated* - manages the *symmetric operations*.
Pure functions do not need to be undone, as only new values are calculated from existing ones. What really needs to be
undone are *state mutations*, e.g. setting fields of classes, modifying components, etc.
And at every point where you mutate state, you can also save a copy of the original state (or do inverse calculations)
and thereby provide the code to *undo* that mutation.

The `undo` should always **reset the system to the exact state it had before the `do`**.

So, whenever you have a block of "state mutating code", instead of just writing regular code, you call `.RegisterAndExecute`
with at least two lambdas: the `do` lambda (the code which you would have written anyway), as well as the `undo` lambda which
resets the state to the original. A redo is simply another call of the `do` function that you passed.

An action is made up of at least one, but possibly many calls to `.RegisterAndExecute`. Once you call `.StopRecordingAction`,
all registered symmetric operations are collected as a stack called an `UndoAction` and pushed to the undo stack.
Undo pops that stack, and then pops all symmetric operations from the action's stack and runs the undo logic in reverse order.
Then, a new `RedoAction` is created with the same symmetric operations and pushed to the redo stack. And so forth.

The whole undo manager therefore mostly consists of two nested stacks:
the undo stack and redo stack are stacks of `Undo/RedoOperation`s,
which in turn contain stacks of `SymmetricOperation`s in them.
The undo manager is responsible of properly moving onto and between these stacks.

## SymmetricOperation Magic

In most cases, and for all simple problems, you will never need to manually create a `SymmetricOperation`.

But the system outlined above has a fatal flaw: What happens when the `do` code in a `RegisterAndExecute` call
does call `RegisterAndExecute` itself? How will we keep track of undo and redo?

In short: this is a mess, and when this happens, an exception is thrown.
**You cannot recursively call `RegisterAndExecute`**.

When does this happen? In our experience, there are a few cases to avoid:

- the `do` case calls an *event*, which might in turn cause another symmetric operation to be registered in an event handler
- a class that you are reusing already secretly calls `.RegisterAndExecute` in some method, and you call that method in a `do`

Basically, whenever the system becomes too dynamic and pluggable, you can run into problems with the default model of the undo manager.
But fear not! For we are prepared for this case.

Functional Programming - Haskell in particular - favors the use of *Monads*.
These are essentially *immutable, recursively composable objects which can hold values*.
There are a million tutorials out there, so I will not go into detail about this topic here, but will instead provide some usage examples.

Essentially, when you are writing code that may be used in unexpected ways, then instead of hoping that you get no `RegisterAndExecute`
conflicts, you can just *manually compose symmetric operations*. The idea is this:

When you have a class with methods that would call `.RegisterAndExecute`, then you just return the `SymmetricOperation` instead.
This lets the caller of the methods decide when to execute the operation, and whether to compose it further or to just register it.
Or just call the `.Do()` and be done with it. In this case, you can compose multiple symmetric operations by chaining them with `.AndThen`,
or `.AndThenDo`. These work exactly as if you would register two operations in order, except that you get a new `SymmetricOperation`
that *has not executed yet*.

But what if your method also returned some value of type `T`? That's not a problem either! Instead, you can just return a `SymmetricOperation<T>`
The generic variant returns the value of type `T` from the `Do()` call. When you pass a `SymmetricOperation<T>` to `.RegisterAndExecute`, then
you get the resulting object of type `T` as a return value!

But now you have a `SymmetricOperation<T>`, and it hasn't run yet, but you need the return value to continue doing stuff. And you do not want to call
`.RegisterAndExecute` yet, because that might happen in a higher level. For these cases, you can use the monadic `.Map` and `.FlatMap` operations.

(Some languages call these `fmap` and `bind`, or `<$>` and `>>=` (Haskell), or `Select` and `SelectMany` (LINQ))

Essentially. when you have a `SymmetricOperation<A>`, and you need the `A` to calculate a new value of type `B`, then you call
`symOp.Map(a => calculate(a))`, and this returns a `SymmetricOperation<B>`! But the operation has not run yet. So how does the
function you pass get the `a` value? Easy: The function you pass is also delayed until you either call `.Do()` or pass the
resulting symmetric operation to `.RegisterAndExecute`. At that point, the original operation will run and produce the value that
your function needs.

**A `SymmetricOperation<T>` *promises* a value of type `T` once you run it,
and also holds the `Undo()` logic to undo all state modifications done while calculating that value.**

But what if your `.Map` calls code which *again* returns a `SymmetricOperation<B>`?
Then you would end up with a `SymmetricOperation<SymmetricOperation<B>>`! That's horrible, especially since we just said that
you cannot recursively call `.RegisterAndExecute`. But you only get the inner operation once you run the outer one!

Fear not: In this case, you just call `.FlatMap` instead, which *flattens* the nested symmetric operations into a single one.

```csharp
SymmetricOperation<A> symOp = ...
SymmetricOperation<B> opB = 
    symOp.FlatMap(a => 
        new SymmetricOperation<B>(
            () => calculate(a), 
            () => undoCalculate()
        )
    );
```

This also solves the issue with *events* mentioned at the beginning of this section. Instead of using the `event` keyword,
you can track a list of listeners (see GOF listener pattern). These listeners may be delegates that return a `SymmetricOperation`,
e.g. `Func<EventArgs, SymmetricOperation>`. This is a strong signal to the listeners that they should *delay* any code that
mutates state, and return it in a `SymmetricOperation` instead of running it immediately. Then you can write a loop
that calls all these listeners and composes all the `SymmetricOperation`s into a single large one. Finally, you can either
return that `SymmetricOperation` from the method that invoked the "event", or you can call `.RegisterAndExecute` at that point.