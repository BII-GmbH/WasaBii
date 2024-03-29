﻿#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using BII.WasaBii.Core;
using NUnit.Framework;

namespace BII.WasaBii.Undo.Tests {

    public class TrivialUndoBuffer : DefaultUndoBuffer<string> {
        public TrivialUndoBuffer() : base(100) { }
        public override void OnBeforeAttach() { }
        public override void OnAfterDetach() { }
    }

    public class MockUndoBuffer : UndoBuffer<string> {
        private readonly UndoBuffer<string> backing = new TrivialUndoBuffer();

        public int RegisterUndoCalled { get; private set; }
        public override void RegisterUndo(UndoAction<string> res) {
            RegisterUndoCalled += 1;
            backing.RegisterUndo(res);
        }

        public int UndoCalled { get; private set; }
        public override int Undo(int n) {
            UndoCalled += 1;
            return backing.Undo(n);
        }

        public int RedoCalled { get; private set; }
        public override int Redo(int n) {
            RedoCalled += 1;
            return backing.Redo(n);
        }

        public int ClearUndoStackCalled { get; private set; }
        public override void ClearUndoStack() {
            ClearUndoStackCalled += 1;
            backing.ClearUndoStack();
        }

        public int ClearRedoStackCalled { get; private set; }
        public override void ClearRedoStack() {
            ClearRedoStackCalled += 1;
            backing.ClearRedoStack();
        }

        public int OnBeforeAttachCalled { get; private set; }
        public override void OnBeforeAttach() {
            OnBeforeAttachCalled += 1;
            backing.OnBeforeAttach();
        }

        public int OnAfterDetachCalled { get; private set; }
        public override void OnAfterDetach() {
            OnAfterDetachCalled += 1;
            backing.OnAfterDetach();
        }

        public override IEnumerable<UndoAction<string>> UndoStack => backing.UndoStack;
        public override IEnumerable<RedoAction<string>> RedoStack => backing.RedoStack;
    }

    public class UndoManagerTest {
        private UndoManager<string> undoManager = null!; // always assigned in `Setup`
        private readonly List<string> warnings = new();

        private static void fail() => Assert.Fail();

        private void registerUndos(int n, Action? Do = null, Action? Undo = null) {
            static void DoNothing() { }
            for (var i = 0; i < n; ++i) {
                undoManager.StartRecordingAction("test #" + i);
                undoManager.RegisterAndExecute(Do ?? DoNothing, Undo ?? DoNothing);
                undoManager.StopRecordingAction();
            }
        }
        
        private void assertNoWarnings() => 
            Assert.That(warnings, Is.Empty, "There were undo manager warnings after the test.");

        [SetUp]
        public void Setup() {
            warnings.Clear();
            undoManager = new UndoManager<string>(100, warning => warnings.Add(warning));
        }

        #region Basic Usage
        
        [Test]
        public void WhenRegistering_ThenExecuted() {
            var called = false;
            undoManager.RegisterAndExecute(() => called = true, fail);
            Assert.That(called, Is.True);
            Assert.That(warnings.Count, Is.EqualTo(1), "Expected 1 warning");
        }

        [Test]
        public void WhenUndoingNothing_ThenNothingHappens() {
            Assert.That(() => undoManager.Undo(), Throws.Nothing);
            assertNoWarnings();
        }

        [Test]
        public void WhenRedoingNothing_ThenNothingHappens() {
            Assert.That(() => undoManager.Redo(), Throws.Nothing);
            assertNoWarnings();
        }

        [Test]
        public void WhenUndoing_ThenActuallyUndone() {
            bool? done = null;
            undoManager.StartRecordingAction("test");
            undoManager.RegisterAndExecute(() => done = true, () => done = false);
            undoManager.StopRecordingAction();

            Assert.That(done, Is.True);

            undoManager.Undo();

            Assert.That(done, Is.False);
            
            assertNoWarnings();
        }

        [Test]
        public void WhenRedoing_ThenActuallyRedone() {
            bool? done = null;
            undoManager.StartRecordingAction("test");
            undoManager.RegisterAndExecute(() => done = true, () => done = false);
            undoManager.StopRecordingAction();

            undoManager.Undo();
            Assert.That(done, Is.False);

            undoManager.Redo();
            Assert.That(done, Is.True);
            
            assertNoWarnings();
        }

        [Test]
        public void WhenUndoingAndRedoing_ThenInProperOrder() {
            // Test with non-associative operations
            var sum = 0;

            undoManager.StartRecordingAction("test");
            undoManager.RegisterAndExecute(() => sum += 2, () => sum -= 2);
            undoManager.RegisterAndExecute(() => sum /= 2, () => sum *= 2);
            undoManager.StopRecordingAction();

            Assert.That(sum, Is.EqualTo(1));

            // Just in case something breaks after multiple applications
            for (var i = 0; i < 10; ++i) {
                undoManager.Undo();
                Assert.That(sum, Is.Zero);

                undoManager.Redo();
                Assert.That(sum, Is.EqualTo(1));
            }
            
            assertNoWarnings();
        }

        [Test]
        public void WhenCompletingRecording_ThenAllRecorded() {
            bool? c1 = null;
            bool? c2 = null;

            undoManager.StartRecordingAction("test");
            undoManager.RegisterAndExecute(() => c1 = true, () => c1 = false);
            undoManager.RegisterAndExecute(() => c2 = true, () => c2 = false);
            undoManager.StopRecordingAction();

            Assert.That(c1, Is.True);
            Assert.That(c2, Is.True);

            undoManager.Undo();

            Assert.That(c1, Is.False);
            Assert.That(c2, Is.False);

            undoManager.Redo();

            Assert.That(c1, Is.True);
            Assert.That(c2, Is.True);
            
            assertNoWarnings();
        }

        [Test]
        public void WhenNotRecording_ThenNothingSaved() {
            bool? c1 = null;

            undoManager.RegisterAndExecute(() => c1 = true, fail);
            undoManager.Undo();

            Assert.That(c1, Is.True);
            Assert.That(warnings.Count, Is.EqualTo(1), "Expected 1 warning");
        }

        [Test]
        public void WhenUndoingTooMuch_ThenEarlyStop() {
            registerUndos(5);

            var undone = undoManager.Undo(11);

            Assert.That(undone, Is.EqualTo(5));
            
            assertNoWarnings();
        }

        [Test]
        public void WhenRedoingTooMuch_ThenEarlyStop() {
            registerUndos(5);
            undoManager.Undo(5);

            var redone = undoManager.Redo(11);

            Assert.That(redone, Is.EqualTo(5));
            
            assertNoWarnings();
        }

        [Test]
        public void WhenRestartingRecording_ThenCurrentRecordingSaved() {
            var counter = 0;

            undoManager.StartRecordingAction("test");
            undoManager.RegisterAndExecute(() => counter++, () => counter--);
            undoManager.RegisterAndExecute(() => counter++, () => counter--);

            undoManager.StartRecordingAction("another test");
            Assert.That(warnings.Count, Is.EqualTo(1), "Expected 1 warning");
            
            undoManager.StopRecordingAction();
            Assert.That(warnings.Count, Is.EqualTo(2), "Expected 2 warnings");

            Assert.That(counter, Is.EqualTo(2));

            undoManager.Undo(2);

            Assert.That(counter, Is.Zero);
            
            
        }

        [Test]
        public void WhenStoppingNoRecording_ThenInvalidOperationException() {
            Assert.That(() => undoManager.StopRecordingAction(), Throws.InvalidOperationException);

            undoManager.StartRecordingAction("test");
            undoManager.StopRecordingAction();

            Assert.That(() => undoManager.StopRecordingAction(), Throws.InvalidOperationException);
            
            Assert.That(warnings.Count, Is.EqualTo(1), "Expected 1 warning");
        }

        [Test]
        public void WhenUndoingWhileRecording_ThenInvalidOperationException() {
            undoManager.StartRecordingAction("test");
            Assert.That(() => undoManager.Undo(), Throws.InvalidOperationException);
            assertNoWarnings();
        }

        [Test]
        public void WhenRedoingWhileRecording_ThenInvalidOperationException() {
            undoManager.StartRecordingAction("test");
            Assert.That(() => undoManager.Redo(), Throws.InvalidOperationException);
            assertNoWarnings();
        }

        [Test]
        public void WhenRegisteringDuringRegistration_WhenRecording_ThenInvalidOperationException() {
            undoManager.StartRecordingAction("test");
            Assert.That(() => undoManager.RegisterAndExecute(
                () => undoManager.RegisterAndExecute(() => { }, fail),
                fail
            ), Throws.InvalidOperationException);
            undoManager.StopRecordingAction();
            Assert.That(warnings.Count, Is.EqualTo(1), "Expected 1 warning");
        }

        [Test]
        public void WhenRegisteringDuringRegistration_WhenRecordingInInnerButNotOuter_ThenWorks() {
            bool? done = null;
            
            Assert.That(() => undoManager.RegisterAndExecute(
                () => {
                    undoManager.StartRecordingAction("test");
                    undoManager.RegisterAndExecute(() => done = true, () => done = false);
                    undoManager.StopRecordingAction();
                },
                fail
            ), Throws.Nothing);

            Assert.That(done, Is.True);

            Assert.That(undoManager.Undo(), Is.EqualTo(1));

            Assert.That(done, Is.False);
            
            Assert.That(warnings.Count, Is.EqualTo(1), "Expected 1 warning");
        }
        
        #endregion

        #region Exception Handling

        public class TestException : Exception { }

        [Test]
        public void WhenExceptionDuringUndo_ThenRollbackAndRethrow() {
            bool? firstOperationDone = null;
            bool? lastOperationDone = null;
            bool thrown = false;

            undoManager.StartRecordingAction("test");
            undoManager.RegisterAndExecute(
                () => firstOperationDone = true,
                () => firstOperationDone = false);
            undoManager.RegisterAndExecute(
                () => { },
                () => {
                    if (!thrown) {
                        thrown = true;
                        throw new TestException();
                    }
                });
            undoManager.RegisterAndExecute(
                () => lastOperationDone = true,
                () => lastOperationDone = false);
            undoManager.StopRecordingAction();

            Assert.That(() => undoManager.Undo(), Throws.InstanceOf<UndoException>());
            Assert.That(firstOperationDone, Is.True);
            Assert.That(lastOperationDone, Is.True);
            Assert.That(thrown, Is.True);

            Assert.That(() => undoManager.Undo(), Throws.Nothing);
            Assert.That(firstOperationDone, Is.False);
            Assert.That(lastOperationDone, Is.False);
            Assert.That(thrown, Is.True);
            
            assertNoWarnings();
        }

        [Test]
        public void WhenExceptionDuringRedo_ThenRollbackAndRethrow() {
            bool? firstOperationDone = null;
            bool? lastOperationDone = null;
            bool doThrow = false;

            undoManager.StartRecordingAction("test");
            undoManager.RegisterAndExecute(
                () => firstOperationDone = true,
                () => firstOperationDone = false);
            undoManager.RegisterAndExecute(
                () => {
                    if (!doThrow) {
                        doThrow = true;
                    } else {
                        doThrow = false;
                        throw new TestException();
                    }
                },
                () => { }
            );
            undoManager.RegisterAndExecute(
                () => lastOperationDone = true,
                () => lastOperationDone = false);
            undoManager.StopRecordingAction();

            Assert.That(() => undoManager.Undo(), Throws.Nothing);
            Assert.That(() => undoManager.Redo(), Throws.InstanceOf<UndoException>());
            Assert.That(firstOperationDone, Is.False);
            Assert.That(lastOperationDone, Is.False);
            Assert.That(doThrow, Is.False);

            Assert.That(() => undoManager.Redo(), Throws.Nothing);
            Assert.That(firstOperationDone, Is.True);
            Assert.That(lastOperationDone, Is.True);
            Assert.That(doThrow, Is.True);
            
            assertNoWarnings();
        }

        #endregion

        #region Buffer

        [Test]
        public void WhenPushingCustomBuffer_ThenProperlyForwarded() {
            var uut = new MockUndoBuffer();

            var didOrig = false;
            var undidOrig = false;
            undoManager.RecordCompleteAction("Test action", () => 
                undoManager.RegisterAndExecute(() => didOrig = true, () => undidOrig = true));

            Assert.That(didOrig, Is.True);
            Assert.That(undidOrig, Is.False);

            undoManager.PushUndoBuffer(uut);

            Assert.That(uut.OnBeforeAttachCalled, Is.EqualTo(1));

            Assert.That(undoManager.Undo(3), Is.Zero);
            Assert.That(undidOrig, Is.False);

            Assert.That(uut.UndoCalled, Is.EqualTo(1));

            // ensure that recording is forwarded to custom buffer

            var customUndoneReceivedCheck = false;
            undoManager.RecordCompleteAction("Custom action", () => 
                undoManager.RegisterAndExecute(() => { }, () => customUndoneReceivedCheck = true));

            Assert.That(uut.RegisterUndoCalled, Is.EqualTo(1));
            Assert.That(uut.UndoStack.Count(), Is.EqualTo(1));

            uut.UndoStack.Single().ExecuteUndo();
            Assert.That(customUndoneReceivedCheck, Is.True);

            // ensure that no random undos happen during push or pop
            undoManager.RecordCompleteAction("Fail action", () => 
                undoManager.RegisterAndExecute(() => { }, () => Assert.Fail("Invalid undo called.")));

            var above = new MockUndoBuffer();

            undoManager.PushUndoBuffer(above);
            Assert.That(above.OnBeforeAttachCalled, Is.EqualTo(1));

            Assert.That(undoManager.Undo(3), Is.EqualTo(0));

            undoManager.PopUndoBuffer();
            Assert.That(above.OnAfterDetachCalled, Is.EqualTo(1));

            undoManager.PopUndoBuffer();
            Assert.That(uut.OnAfterDetachCalled, Is.EqualTo(1));

            Assert.That(undoManager.Undo(2), Is.EqualTo(1));
            Assert.That(undidOrig, Is.True);
            
            assertNoWarnings();
        }

        [Test]
        public void WhenPushingDuringRecording_ThenStateSavedUntilPopped() {
            var topLevelActionName = "topLevelActionName";
            undoManager.StartRecordingAction(topLevelActionName);

            Assert.That(undoManager.IsRecording, Is.True);
            Assert.That(undoManager.CurrentActionLabel, Is.EqualTo(topLevelActionName));

            var topLevelUndone = false;
            undoManager.RegisterAndExecute(() => {}, () => topLevelUndone = true);

            var uut = new MockUndoBuffer();
            Assert.That(() => undoManager.PushUndoBuffer(uut), Throws.Nothing);

            Assert.That(undoManager.IsRecording, Is.False);
            Assert.That(undoManager.CurrentActionLabel, Is.Null);
            Assert.That(() => undoManager.StopRecordingAction(), Throws.Exception);

            var uutActionName = "uutActionName";
            undoManager.StartRecordingAction(uutActionName);

            Assert.That(undoManager.IsRecording, Is.True);
            Assert.That(undoManager.CurrentActionLabel, Is.EqualTo(uutActionName));

            undoManager.RegisterAndExecute(() => {}, () => Assert.Fail("Invalid undo called."));
            undoManager.StopRecordingAction();

            Assert.That(undoManager.IsRecording, Is.False);
            Assert.That(undoManager.CurrentActionLabel, Is.Null);

            undoManager.PopUndoBuffer();

            Assert.That(undoManager.IsRecording, Is.True);
            Assert.That(undoManager.CurrentActionLabel, Is.EqualTo(topLevelActionName));

            var cancelUut = new MockUndoBuffer();
            undoManager.PushUndoBuffer(cancelUut);

            undoManager.StartRecordingAction("to be cancelled by popping");
            undoManager.RegisterAndExecute(() => {}, () => Assert.Fail("Invalid undo called."));

            // we expect abort with a warning
            Assert.That(() => undoManager.PopUndoBuffer(), Throws.Nothing);
            Assert.That(cancelUut.Undo(1), Is.EqualTo(0));

            undoManager.StopRecordingAction();

            Assert.That(undoManager.Undo(2), Is.EqualTo(1));
            Assert.That(topLevelUndone, Is.True);
            
            assertNoWarnings();
        }

        #endregion

        #region Placeholder

        [Test]
        public void WhenRegisteringPlaceholderWithoutRecording_ThenException() {
            Assert.That(undoManager.RegisterUndoPlaceholder, Throws.InvalidOperationException);
            assertNoWarnings();
        }

        [Test]
        public void WhenRegisteringPlaceholderAndNotUsed_ThenIgnored() {
            undoManager.StartRecordingAction("test");

            _ = undoManager.RegisterUndoPlaceholder();

            bool? done = null;
            undoManager.RegisterAndExecute(() => done = true, () => done = false);

            undoManager.StopRecordingAction();

            Assert.That(done, Is.True);

            undoManager.Undo();

            Assert.That(done, Is.False);

            undoManager.Redo();

            Assert.That(done, Is.True);
            
            assertNoWarnings();
        }

        [Test]
        public void WhenRegisteringPlaceholderAndUsed_ThenProperlyUndoneAndRedone() {
            undoManager.StartRecordingAction("test");

            var placeholder = undoManager.RegisterUndoPlaceholder();

            bool? done = null;
            bool? placeholderDone = null;

            undoManager.RegisterAndExecute(() => done = true, () => done = false);
            undoManager.RegisterAndExecute(
                () => {
                    placeholderDone = false;
                    Assert.That(done, Is.True);
                },
                () => {
                    placeholderDone = true;
                    Assert.That(done, Is.False);
                },
                saveUndoAt: placeholder
            );

            undoManager.StopRecordingAction();

            Assert.That(done, Is.True);
            Assert.That(placeholderDone, Is.False);

            undoManager.Undo();

            Assert.That(done, Is.False);
            Assert.That(placeholderDone, Is.True);

            undoManager.Redo();

            Assert.That(done, Is.True);
            Assert.That(placeholderDone, Is.False);
            
            assertNoWarnings();
        }

        [Test]
        public void WhenUsingPlaceholderTwice_ThenException() {
            undoManager.StartRecordingAction("test");

            var placeholder = undoManager.RegisterUndoPlaceholder();
            undoManager.RegisterAndExecute(SymmetricOperation.Empty, saveUndoAt: placeholder);

            Assert.That(
                () => undoManager.RegisterAndExecute(SymmetricOperation.Empty, saveUndoAt: placeholder),
                Throws.ArgumentException
            );
            
            assertNoWarnings();
        }

        [Test]
        public void WhenUsingPlaceholderAfterRecordingStop_ThenException() {
            undoManager.StartRecordingAction("test");
            var placeholder = undoManager.RegisterUndoPlaceholder();
            undoManager.StopRecordingAction();

            undoManager.StartRecordingAction("test2");

            Assert.That(
                () => undoManager.RegisterAndExecute(SymmetricOperation.Empty, saveUndoAt: placeholder),
                Throws.ArgumentException
            );
            
            assertNoWarnings();
        }

        [Test]
        public void WhenUsingPlaceholderAfterRecordingAbort_ThenException() {
            undoManager.StartRecordingAction("test");
            var placeholder = undoManager.RegisterUndoPlaceholder();
            undoManager.AbortRecordingAction();
            
            Assert.That(warnings.Count, Is.EqualTo(1), "Expected 1 warning");

            undoManager.StartRecordingAction("test2");

            Assert.That(
                () => undoManager.RegisterAndExecute(SymmetricOperation.Empty, saveUndoAt: placeholder),
                Throws.ArgumentException
            );

            undoManager.StopRecordingAction();

            Assert.That(warnings.Count, Is.EqualTo(2), "Expected 2 warnings");
        }

        #endregion

        #region Labels

        [Test]
        public void WhenRecordingActions_ThenUndoLabelOrderCorrect() {
            const string firstLabel = "first";
            const string secondLabel = "second";

            undoManager.RecordCompleteAction(
                firstLabel,
                () => undoManager.RegisterAndExecute(SymmetricOperation.Empty)
            );
            undoManager.RecordCompleteAction(
                secondLabel,
                () => undoManager.RegisterAndExecute(SymmetricOperation.Empty)
            );

            var labels = undoManager.UndoLabels.AsReadOnlyList();
            Assert.That(labels.Count, Is.EqualTo(2));
            Assert.That(labels[0], Is.EqualTo(secondLabel));
            Assert.That(labels[1], Is.EqualTo(firstLabel));
            Assert.That(undoManager.RedoLabels.IsEmpty());
            
            assertNoWarnings();
        }

        [Test]
        public void WhenRecordingActionsAndUndoing_ThenRedoLabelOrderCorrect() {
            const string firstLabel = "first";
            const string secondLabel = "second";

            undoManager.RecordCompleteAction(
                firstLabel,
                () => undoManager.RegisterAndExecute(SymmetricOperation.Empty)
            );
            undoManager.RecordCompleteAction(
                secondLabel,
                () => undoManager.RegisterAndExecute(SymmetricOperation.Empty)
            );
            undoManager.Undo(2);

            var labels = undoManager.RedoLabels.AsReadOnlyList();
            Assert.That(labels.Count, Is.EqualTo(2));
            Assert.That(labels[0], Is.EqualTo(firstLabel));
            Assert.That(labels[1], Is.EqualTo(secondLabel));
            Assert.That(undoManager.UndoLabels.IsEmpty());
            
            assertNoWarnings();
        }

        #endregion

        #region Events

        [Test]
        public void WhenRecordingActions_ThenEventInvoked() {
            int invokeCount = 0;

            void onActionRecorded() => invokeCount++;

            undoManager.OnAfterActionRecorded += onActionRecorded;

            registerUndos(1);

            Assert.That(invokeCount, Is.EqualTo(1));

            registerUndos(2);

            Assert.That(invokeCount, Is.EqualTo(3));
            
            assertNoWarnings();
        }

        [Test]
        public void WhenUndoing_ThenEventInvokedWithCorrectUndoCount() {
            int invokeCount = 0;
            int totalUndoCount = 0;

            void onUndo(int undoCount) {
                invokeCount++;
                totalUndoCount += undoCount;
            }

            undoManager.OnAfterUndo += onUndo;

            registerUndos(1);
            Assert.That(invokeCount, Is.EqualTo(0));
            undoManager.Undo();

            Assert.That(invokeCount, Is.EqualTo(1));
            Assert.That(totalUndoCount, Is.EqualTo(1));
            
            assertNoWarnings();
        }

        [Test]
        public void WhenUndoingMoreThanRegistered_ThenEventInvokedWithCorrectUndoCount() {
            int invokeCount = 0;
            int totalUndoCount = 0;

            void onUndo(int undoCount) {
                invokeCount++;
                totalUndoCount += undoCount;
            }

            undoManager.OnAfterUndo += onUndo;

            registerUndos(2);
            undoManager.Undo(3);

            Assert.That(invokeCount, Is.EqualTo(1));
            Assert.That(totalUndoCount, Is.EqualTo(2));
            
            assertNoWarnings();
        }

        [Test]
        public void WhenRedoing_ThenEventInvokedWithCorrectUndoCount() {
            int invokeCount = 0;
            int totalRedoCount = 0;

            void onRedo(int undoCount) {
                invokeCount++;
                totalRedoCount += undoCount;
            }

            undoManager.OnAfterRedo += onRedo;

            registerUndos(1);
            undoManager.Undo();
            Assert.That(invokeCount, Is.EqualTo(0));
            undoManager.Redo();

            Assert.That(invokeCount, Is.EqualTo(1));
            Assert.That(totalRedoCount, Is.EqualTo(1));
            
            assertNoWarnings();
        }

        [Test]
        public void WhenRedoingMoreThanRegistered_ThenEventInvokedWithCorrectUndoCount() {
            int invokeCount = 0;
            int totalRedoCount = 0;

            void onRedo(int undoCount) {
                invokeCount++;
                totalRedoCount += undoCount;
            }

            undoManager.OnAfterRedo += onRedo;

            registerUndos(2);
            undoManager.Undo(2);
            Assert.That(invokeCount, Is.EqualTo(0));
            undoManager.Redo(3);

            Assert.That(invokeCount, Is.EqualTo(1));
            Assert.That(totalRedoCount, Is.EqualTo(2));
            
            assertNoWarnings();
        }

        [Test]
        public void WhenPushingUndoBuffer_ThenEventInvoked() {
            int invokeCount = 0;

            void onUndoBufferPushed() => invokeCount++;

            undoManager.OnUndoBufferPushed += onUndoBufferPushed;

            var uut = new MockUndoBuffer();

            undoManager.PushUndoBuffer(uut);

            Assert.That(invokeCount, Is.EqualTo(1));
            
            assertNoWarnings();
        }

        [Test]
        public void WhenPoppingUndoBuffer_ThenEventInvoked() {
            int invokeCount = 0;

            void onUndoBufferPopped() => invokeCount++;

            undoManager.OnUndoBufferPopped += onUndoBufferPopped;

            var uut = new MockUndoBuffer();

            undoManager.PushUndoBuffer(uut);

            Assert.That(invokeCount, Is.EqualTo(0));

            undoManager.PopUndoBuffer();

            Assert.That(invokeCount, Is.EqualTo(1));
            
            assertNoWarnings();
        }

        private class SingleElementUndoBuffer : DefaultUndoBuffer<string> {
            private readonly Action onBeforeAttach;
            private readonly Action onAfterAttach;

            public SingleElementUndoBuffer(Action onBeforeAttach, Action onAfterAttach) : base(1) {
                this.onAfterAttach = onAfterAttach;
                this.onBeforeAttach = onBeforeAttach;
            }

            public override void OnBeforeAttach() => onBeforeAttach();
            public override void OnAfterDetach() => onAfterAttach();
        }

        [Test]
        public void WhenUndosExceedCap_ThenOldestRemoved() {
            #pragma warning disable CS0219 // wrong? Assigned right below in closures
            var onBeforeAttachCalled = false;
            var onAfterDetachCalled = false;
            #pragma warning restore CS0219

            var buffer = new SingleElementUndoBuffer(
                () => onBeforeAttachCalled = true, 
                () => onAfterDetachCalled = true
            );

            undoManager.PushUndoBuffer(buffer);

            var action1Done = false;
            var action2Done = false;
            var action1Undone = false;
            var action2Undone = false;

            undoManager.StartRecordingAction("Test action 1");
            undoManager.RegisterAndExecute(
                () => action1Done = true, 
                () => {
                    action1Undone = true;
                    action1Done = false;
                }
            );
            undoManager.StopRecordingAction();

            Assert.That(action1Done, Is.True);
            Assert.That(action1Undone, Is.False);

            undoManager.StartRecordingAction("Test action 2");
            undoManager.RegisterAndExecute(
                () => action2Done = true, 
                () => {
                    action2Undone = true;
                    action2Done = false;
                }
            );
            undoManager.StopRecordingAction();

            Assert.That(action2Done, Is.True);
            Assert.That(action2Undone, Is.False);

            Assert.That(undoManager.Undo(2), Is.EqualTo(1));

            // only 2 should be undone; action 1 should still be done

            Assert.That(action2Done, Is.False);
            Assert.That(action2Undone, Is.True);

            Assert.That(action1Done, Is.True);
            Assert.That(action1Undone, Is.False);

            Assert.That(undoManager.Redo(2), Is.EqualTo(1));

            Assert.That(action1Done, Is.True);
            Assert.That(action1Undone, Is.False);

            Assert.That(action2Done, Is.True);
            
            assertNoWarnings();
        }

#endregion
    }
}