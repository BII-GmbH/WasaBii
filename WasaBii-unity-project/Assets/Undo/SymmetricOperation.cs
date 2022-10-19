#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using BII.WasaBii.Core;

namespace BII.WasaBii.Undos {

    // ReSharper disable all InvalidXmlDocComment // for the caller arguments which are explicitly not documented

#if !WASABII_SYMOP_NODEBUGINFO
    /// Contains debug data that specifies where a <see cref="SymmetricOperation"/> has been constructed.
    public readonly struct SymmetricOperationDebugInfo {
        public readonly string CallerMemberName;
        public readonly string SourceFilePath;
        public readonly int SourceLineNumber;

        public SymmetricOperationDebugInfo(
            string callerMemberName, 
            string sourceFilePath, 
            int sourceLineNumber
        ) {
            CallerMemberName = callerMemberName;
            SourceFilePath = sourceFilePath;
            SourceLineNumber = sourceLineNumber;
        }
    }
#endif

    /// <summary>
    /// An operation that can be undone. Consists of two actions
    ///  - <see cref="Do"/> and <see cref="Undo"/> - with optional disposal logic.
    /// Essentially a symmetric <see cref="Action"/>.
    /// Can be properly composed via <see cref="SymmetricOperations.AndThen"/>.
    /// </summary>
    /// <remarks>
    /// Also includes <see cref="DebugInfo"/>, which contains some information 
    ///  about where the operation and its parts have been constructed.
    /// For less overhead, the compiler symbol <c>WASABII_SYMOP_NODEBUGINFO</c> can be defined to disable this.
    /// </remarks>
    /// <seealso cref="SymmetricOperation{T}"/>
    [CannotBeSerialized("Based on function references which cannot be serialized.")]
    public sealed class SymmetricOperation {
        internal readonly Action lastDo;
        internal readonly Action lastUndo;

        // Note CR: Composition can lead to thousands of symmetric operations that are composed before being used.
        //   Simply using function composition will lead to huge recursive call stacks, which can cause stack overflows.
        // Instead, once a SymOp is built using composition, we use immutable lists to keep track of the additional operations,
        //   and then iteratively run all of the operations in order. Since we don't compose most of the time,
        //   we leave the lists as null in order to avoid singleton list construction overhead.

        internal readonly ImmutableList<Action>? doInOrder;
        internal readonly ImmutableList<Action>? undoInOrder;

        internal readonly ImmutableList<Action>? disposeAfterDoInOrder;
        internal readonly ImmutableList<Action>? disposeAfterUndoInOrder;

#if !WASABII_SYMOP_NODEBUGINFO
        public readonly ImmutableList<SymmetricOperationDebugInfo> DebugInfo;
#endif
        public static readonly Action DoNothingOperation = () => { };

        private static readonly SymmetricOperation empty = new(DoNothingOperation, DoNothingOperation);
        public static ref readonly SymmetricOperation Empty => ref empty;

        public void Do() {
            doInOrder?.ForEach(fn => fn.Invoke());
            lastDo();
        }

        public void Undo() {
            lastUndo();
            undoInOrder?.ForEach(fn => fn.Invoke());
        }

        public void DisposeAfterDo() => disposeAfterDoInOrder?.ForEach(fn => fn.Invoke());
        public void DisposeAfterUndo() => disposeAfterUndoInOrder?.ForEach(fn => fn.Invoke());

        public static implicit operator SymmetricOperation<Nothing>(SymmetricOperation src) => 
            src.WithResult(default(Nothing));

        /// <summary>
        /// Describes how to both do something and undo it again. The action
        /// and its symmetric undo action should be able to be called multiple
        /// times in succession, as long as they are called one after the other.
        /// </summary>
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
        public SymmetricOperation(
            Action action, 
            Action undo, 
            Action? disposeAfterDo = null,
            Action? disposeAfterUndo = null
#if !WASABII_SYMOP_NODEBUGINFO
            , [CallerMemberName] string __callerMemberName = "",
            [CallerFilePath] string __sourceFilePath = "",
            [CallerLineNumber] int __sourceLineNumber = 0
#endif
        ) {
            this.lastDo = action;
            this.lastUndo = undo;

            // these values are only set when using composition
            // through the internal constructor
            doInOrder = null;
            undoInOrder = null;

            // optionally overwritten afterwards
            disposeAfterDoInOrder = null;
            disposeAfterUndoInOrder = null;
            if (disposeAfterDo != null) this.disposeAfterDoInOrder = ImmutableList.Create(disposeAfterDo);
            if (disposeAfterUndo != null) this.disposeAfterUndoInOrder = ImmutableList.Create(disposeAfterUndo);
#if !WASABII_SYMOP_NODEBUGINFO
            this.DebugInfo = ImmutableList.Create(
                new SymmetricOperationDebugInfo(__callerMemberName, __sourceFilePath, __sourceLineNumber)
            );
#endif
        }

        internal SymmetricOperation(
            Action lastAction, 
            Action lastUndo, 
            ImmutableList<Action>? doInOrder,
            ImmutableList<Action>? undoInOrder,
            ImmutableList<Action>? disposeAfterDoInOrder,
            ImmutableList<Action>? disposeAfterUndoInOrder
#if !WASABII_SYMOP_NODEBUGINFO
            , ImmutableList<SymmetricOperationDebugInfo> debugInfo
#endif
        ) {
            this.lastDo = lastAction;
            this.lastUndo = lastUndo;
            this.doInOrder = doInOrder;
            this.undoInOrder = undoInOrder;

            this.disposeAfterDoInOrder = disposeAfterDoInOrder;
            this.disposeAfterUndoInOrder = disposeAfterUndoInOrder;
#if !WASABII_SYMOP_NODEBUGINFO
            this.DebugInfo = debugInfo;
#endif
        }
    }
    
    /// <summary>
    /// An operation that returns a value of type <typeparamref name="T"/>
    ///  which causes side-effects. These side-effects can be undone.
    /// Consists of a func <see cref="Do"/> and an action <see cref="Undo"/> with optional disposal logic.
    /// This is a monad, and thus provides <see cref="SymmetricOperations.Map"/>
    ///  and <see cref="SymmetricOperations.FlatMap"/>.
    /// Can be constructed from a <see cref="SymmetricOperation"/> in numerous ways via extensions,
    ///  and can be composed via <see cref="SymmetricOperations.AndThen{T}"/>.
    /// </summary>
    /// <remarks>
    /// Also includes <see cref="DebugInfo"/>, which contains some information 
    ///  about where the operation and its parts have been constructed.
    /// For less overhead, the compiler symbol <c>WASABII_SYMOP_NODEBUGINFO</c> can be defined to disable this.
    /// </remarks>
    [CannotBeSerialized("Based on function references which cannot be serialized.")]
    public sealed class SymmetricOperation<T> {

        // Note CR: we still use function composition for this type, as there is usually
        //   only one final typed SymOp after a chain of untyped SymOps.
        //  => Composition of typed SymOps is unlikely, so recursive calls are fine.
        //   In particular, one would
        //     either need to call `.WithoutResult` and then compose untyped which shouldn't happen a lot,
        //     or use `.Map` which would require recursive calls either way. 

        public readonly Func<T> Do;
        public readonly Action Undo;
        public readonly Action DisposeAfterDo;  
        public readonly Action DisposeAfterUndo;
        
#if !WASABII_SYMOP_NODEBUGINFO
        public readonly ImmutableList<SymmetricOperationDebugInfo> DebugInfo;
#endif

        /// <summary>
        /// Describes how to both do something and undo it again. The action
        /// and its symmetric undo action should be able to be called multiple
        /// times in succession, as long as they are called one after the other.
        /// </summary>
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
        public SymmetricOperation(
            Func<T> action, 
            Action undo, 
            Action? disposeAfterDo = null,
            Action? disposeAfterUndo = null
#if !WASABII_SYMOP_NODEBUGINFO
            , [CallerMemberName] string __callerMemberName = "",
            [CallerFilePath] string __sourceFilePath = "",
            [CallerLineNumber] int __sourceLineNumber = 0
#endif
        ) {
            this.Do = action;
            this.Undo = undo;
            this.DisposeAfterDo = disposeAfterDo ?? SymmetricOperation.DoNothingOperation;
            this.DisposeAfterUndo = disposeAfterUndo ?? SymmetricOperation.DoNothingOperation;
#if !WASABII_SYMOP_NODEBUGINFO
            this.DebugInfo = ImmutableList.Create(
                new SymmetricOperationDebugInfo(__callerMemberName, __sourceFilePath, __sourceLineNumber)
            );
#endif
        }

        internal SymmetricOperation(
            Func<T> action, 
            Action undo, 
            Action? disposeAfterDo,
            Action? disposeAfterUndo
#if !WASABII_SYMOP_NODEBUGINFO
            , ImmutableList<SymmetricOperationDebugInfo> debugInfo
#endif
        ) {
            this.Do = action;
            this.Undo = undo;
            this.DisposeAfterDo = disposeAfterDo ?? SymmetricOperation.DoNothingOperation;
            this.DisposeAfterUndo = disposeAfterUndo ?? SymmetricOperation.DoNothingOperation;
#if !WASABII_SYMOP_NODEBUGINFO
            this.DebugInfo = debugInfo;
#endif
        }
    }

    public static class SymmetricOperations {

        // TODO CR: further optimize using a collection that allows efficient immutable *merging* of two instances
        //          We currently do `immutableList.AddRange(otherList)`, but there must be a way to just merge both efficiently

        /// Discards the result of a <see cref="SymmetricOperation{T}"/>,
        public static SymmetricOperation WithoutResult<T>(this SymmetricOperation<T> src) => 
            new SymmetricOperation(() => src.Do(), src.Undo, src.DisposeAfterDo, src.DisposeAfterUndo);

        /// Result will be immediately captured by value.
        public static SymmetricOperation<T> WithResult<T>(this SymmetricOperation src, T result) => 
            new SymmetricOperation<T>(
                () => {
                    src.Do();
                    return result;
                }, 
                src.Undo, 
                src.DisposeAfterDo, 
                src.DisposeAfterUndo
#if !WASABII_SYMOP_NODEBUGINFO
                , src.DebugInfo
#endif
            );

        /// Result will be calculated the first time the symmetric operation is executed.
        /// Use this instead of <see cref="WithResult{T}"/>
        ///   when the result depends on state modified
        ///   through side-effects in the symmetric operation.
        public static SymmetricOperation<T> WithLazyResult<T>(this SymmetricOperation src, Func<T> result) {
            var cachedResult = Option<T>.None;
            return new SymmetricOperation<T>(
                () => {
                    src.Do();
                    return cachedResult.Match(
                        v => v,
                        () => {
                            var res = result();
                            cachedResult = res;
                            return res;
                        }
                    );
                }, 
                src.Undo, 
                src.DisposeAfterDo, 
                src.DisposeAfterUndo
#if !WASABII_SYMOP_NODEBUGINFO
                , src.DebugInfo
#endif
            );
        }

        public static SymmetricOperation<TRes> Map<T, TRes>(
            this SymmetricOperation<T> src, 
            Func<T, TRes> mapping
#if !WASABII_SYMOP_NODEBUGINFO
            , [CallerMemberName] string __callerMemberName = "",
            [CallerFilePath] string __sourceFilePath = "",
            [CallerLineNumber] int __sourceLineNumber = 0
#endif
        ) => new SymmetricOperation<TRes>(
            () => mapping(src.Do()), 
            src.Undo, 
            src.DisposeAfterDo, 
            src.DisposeAfterUndo
#if !WASABII_SYMOP_NODEBUGINFO
            , src.DebugInfo.Add(new SymmetricOperationDebugInfo(
                __callerMemberName,
                __sourceFilePath,
                __sourceLineNumber
            ))
#endif
        );

        public static SymmetricOperation<TRes> FlatMap<T, TRes>(
            this SymmetricOperation<T> src,
            Func<T, SymmetricOperation<TRes>> mapping
#if !WASABII_SYMOP_NODEBUGINFO
            , [CallerMemberName] string __callerMemberName = "",
            [CallerFilePath] string __sourceFilePath = "",
            [CallerLineNumber] int __sourceLineNumber = 0
#endif
        ) {
            SymmetricOperation<TRes> mappingRes = default!;
            return new SymmetricOperation<TRes>(
                () => (mappingRes = mapping(src.Do())).Do(), 
                () => {
                    mappingRes.Undo!(); src.Undo();
                }, () => {
                    mappingRes.DisposeAfterDo!();
                    src.DisposeAfterDo();
                }, () => {
                    mappingRes.DisposeAfterUndo!();
                    src.DisposeAfterUndo();
                }
#if !WASABII_SYMOP_NODEBUGINFO
                , src.DebugInfo.Add(new SymmetricOperationDebugInfo(
                    __callerMemberName,
                    __sourceFilePath,
                    __sourceLineNumber
                ))
#endif
            );
        }

        /// Chains a <see cref="SymmetricOperation"/> after another <see cref="SymmetricOperation"/>.
        public static SymmetricOperation AndThen(this SymmetricOperation src, in SymmetricOperation then) {
            var newDo = ImmutableList.CreateBuilder<Action>();
            if (src.doInOrder != null) newDo.AddRange(src.doInOrder);
            newDo.Add(src.lastDo);
            if (then.doInOrder != null) newDo.AddRange(then.doInOrder);

            var newUndo = ImmutableList.CreateBuilder<Action>();
            if (then.undoInOrder != null) newUndo.AddRange(then.undoInOrder);
            newUndo.Add(src.lastUndo);
            if (src.undoInOrder != null) newUndo.AddRange(src.undoInOrder);

            // then-dispose can depend on resources of src-dispose

            var newDispose = (then.disposeAfterDoInOrder, src.disposeAfterDoInOrder) switch {
                (null, var sourceD) => sourceD,
                ({} thenD, null) => thenD,
                ({} thenD, {} srcD) => thenD.AddRange(srcD)
            };

            var newUndoDispose = (then.disposeAfterUndoInOrder, src.disposeAfterUndoInOrder) switch {
                (null, var sourceD) => sourceD,
                ({} thenD, null) => thenD,
                ({} thenD, {} sourceD) => thenD.AddRange(sourceD)
            };

            return new SymmetricOperation(
                then.lastDo, 
                then.lastUndo, 
                newDo.ToImmutable(), 
                newUndo.ToImmutable(), 
                newDispose, 
                newUndoDispose
#if !WASABII_SYMOP_NODEBUGINFO
                , src.DebugInfo.AddRange(then.DebugInfo)
#endif
            );
        }

        /// Chains a <see cref="SymmetricOperation{T}"/> after a <see cref="SymmetricOperation"/>.
        /// The source operation will precede the symmetric operation with a result.
        /// Use <see cref="FlatMap{T,TRes}"/> when the source already has a result.
        public static SymmetricOperation<TRes> AndThen<TRes>(
            this SymmetricOperation src, SymmetricOperation<TRes> then
        ) => new SymmetricOperation<TRes>(() => {
                src.Do();
                return then.Do();
            }, () => {
                then.Undo();
                src.Undo();
            }, () => {
                // then-dispose can depend on resources of src-dispose
                then.DisposeAfterDo();
                src.DisposeAfterDo();
            }, () => {
                // then-dispose can depend on resources of src-dispose
                then.DisposeAfterUndo();
                src.DisposeAfterUndo();
            }
#if !WASABII_SYMOP_NODEBUGINFO
            , src.DebugInfo.AddRange(then.DebugInfo)
#endif
        );

        /// Adds do and undo logic to an existing <see cref="SymmetricOperation"/>.
        /// <paramref name="doThen"/> will be executed after the source Do.
        /// <paramref name="undoThen"/> will be executed before the source Undo.
        public static SymmetricOperation AndThenDo(
            this SymmetricOperation src,
            Action doThen,
            Action undoThen,
            Action? disposeAfterDoThen = null,
            Action? disposeAfterUndoThen = null
#if !WASABII_SYMOP_NODEBUGINFO
            , [CallerMemberName] string __callerMemberName = "",
            [CallerFilePath] string __sourceFilePath = "",
            [CallerLineNumber] int __sourceLineNumber = 0
#endif
        ) {
            var newDo = src.doInOrder != null 
                ? src.doInOrder.Add(src.lastDo) 
                : ImmutableList.Create(src.lastDo);

            var newUndo = src.undoInOrder != null
                ? src.undoInOrder.Insert(0, undoThen)
                : ImmutableList.Create(undoThen);

            // then-dispose can depend on resources of src-dispose

            var newDispose = (disposeAfterDoThen, src.disposeAfterDoInOrder) switch {
                (null, var sourceD) => sourceD,
                ({} thenD, null) => ImmutableList.Create(thenD),
                ({} thenD, {} sourceD) => sourceD.Insert(0, thenD)
            };

            var newUndoDispose = (disposeAfterUndoThen, src.disposeAfterUndoInOrder) switch {
                (null, var sourceD) => sourceD,
                ({} thenD, null) => ImmutableList.Create(thenD),
                ({} thenD, {} sourceD) => sourceD.Insert(0, thenD)
            };

            return new SymmetricOperation(
                doThen, 
                src.lastUndo, 
                newDo, 
                newUndo,
                newDispose, 
                newUndoDispose
#if !WASABII_SYMOP_NODEBUGINFO
                , src.DebugInfo.Add(new SymmetricOperationDebugInfo(
                    __callerMemberName,
                    __sourceFilePath,
                    __sourceLineNumber
                ))
#endif
            );
        }

        /// Adds do and undo logic to an existing <see cref="SymmetricOperation"/>.
        /// The returned <see cref="SymmetricOperation{T}"/> wraps the result of <paramref name="doThen"/>.
        /// <paramref name="doThen"/> will be executed after the source Do.
        /// <paramref name="undoThen"/> will be executed before the source Undo.
        /// Use <see cref="FlatMap{T,TRes}"/> if your Do code depends on a previous result.
        public static SymmetricOperation<TRes> AndThenReturn<TRes>(
            this SymmetricOperation src, 
            Func<TRes> doThen, 
            Action undoThen, 
            Action? disposeAfterDoThen = null, 
            Action? disposeAfterUndoThen = null
#if !WASABII_SYMOP_NODEBUGINFO
            , [CallerMemberName] string __callerMemberName = "",
            [CallerFilePath] string __sourceFilePath = "",
            [CallerLineNumber] int __sourceLineNumber = 0
#endif
        ) => new SymmetricOperation<TRes>(() => {
                src.Do();
                return doThen();
            }, () => {
                undoThen();
                src.Undo();
            }, () => {
                // then-dispose can depend on resources of src-dispose
                disposeAfterDoThen?.Invoke();
                src.DisposeAfterDo();
            }, () => {
                // then-dispose can depend on resources of src-dispose
                disposeAfterUndoThen?.Invoke();
                src.DisposeAfterUndo();
            }
#if !WASABII_SYMOP_NODEBUGINFO
            , src.DebugInfo.Add(new SymmetricOperationDebugInfo(
                __callerMemberName,
                __sourceFilePath,
                __sourceLineNumber
            ))
#endif
        );

        public static SymmetricOperation CombineInOrder(
            this SymmetricOperation source,
            IEnumerable<SymmetricOperation> others
        ) => CombineInOrder(others.Prepend(source));

        public static SymmetricOperation CombineInOrder(this IEnumerable<SymmetricOperation> toCombine) {
            var toCombineList = toCombine.AsReadOnlyList();
            if (toCombineList.IsEmpty()) return SymmetricOperation.Empty;
            if (toCombineList.Count == 1) return toCombineList[0];
            if (toCombineList.Count == 2) return toCombineList[0].AndThen(toCombineList[1]);

            var newDo = new List<Action>();
#if !WASABII_SYMOP_NODEBUGINFO
            var debugInfo = new List<SymmetricOperationDebugInfo>();
#endif
            
            foreach (var symOp in toCombine) {
                if (symOp.doInOrder != null) 
                    newDo.AddRange(symOp.doInOrder);
                newDo.Add(symOp.lastDo);
#if !WASABII_SYMOP_NODEBUGINFO
                debugInfo.AddRange(symOp.DebugInfo);
#endif
            }
            
            var newUndo = new List<Action>();
            var newDispose = new List<Action>();
            var newUndoDispose = new List<Action>();

            foreach (var symOp in toCombine.Reverse()) {
                newUndo.Add(symOp.lastUndo);
                if (symOp.undoInOrder != null)
                    newUndo.AddRange(symOp.undoInOrder);
                if (symOp.disposeAfterDoInOrder != null) 
                    newDispose.AddRange(symOp.disposeAfterDoInOrder);
                if (symOp.disposeAfterUndoInOrder != null)
                    newUndoDispose.AddRange(symOp.disposeAfterUndoInOrder);
            }

            return new SymmetricOperation(
                newDo.Last(),
                newUndo.First(), 
                newDo.Take(newDo.Count - 1).ToImmutableList(), 
                newUndo.Skip(1).ToImmutableList(), 
                newDispose.ToImmutableList(), 
                newUndoDispose.ToImmutableList()
#if !WASABII_SYMOP_NODEBUGINFO
                , debugInfo.ToImmutableList()
#endif
            );   
        }
    }
}