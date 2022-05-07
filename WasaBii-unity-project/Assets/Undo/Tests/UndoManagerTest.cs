using System;
using System.Linq;
using BII.WasaBii.Core;
using BII.WasaBii.Undos;
using NSubstitute;
using NUnit.Framework;

namespace BII.WasaBii.Undo.Tests {

    public class UndoManagerTest {
        private UndoManager undoManager;
        
        private static void fail() => Assert.Fail();
        
        private void registerUndos(int n, Action Do = null, Action Undo = null) {
            static void DoNothing() { }
            for (var i = 0; i < n; ++i) {
                undoManager.StartRecordingAction("test #" + i);
                undoManager.RegisterAndExecute(Do ?? DoNothing, Undo ?? DoNothing);
                undoManager.StopRecordingAction();
            }
        }
        
        [SetUp]
        public void Setup() { undoManager = new UndoManager(); }

        [Test]
        public void WhenRegistering_ThenExecuted() {
            var called = false;
            undoManager.RegisterAndExecute(() => called = true, fail);
            Assert.That(called, Is.True);
        }

        [Test]
        public void WhenUndoingNothing_ThenNothingHappens() {
            Assert.That(() => undoManager.Undo(), Throws.Nothing);
        }

        [Test]
        public void WhenRedoingNothing_ThenNothingHappens() {
            Assert.That(() => undoManager.Redo(), Throws.Nothing);
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
        }

        [Test]
        public void WhenNotRecording_ThenNothingSaved() {
            bool? c1 = null;

            undoManager.RegisterAndExecute(() => c1 = true, fail);
            undoManager.Undo();

            Assert.That(c1, Is.True);
        }

        [Test]
        public void WhenUndoingTooMuch_ThenEarlyStop() {
            registerUndos(5);

            var undone = undoManager.Undo(11);

            Assert.That(undone, Is.EqualTo(5));
        }

        [Test]
        public void WhenRedoingTooMuch_ThenEarlyStop() {
            registerUndos(5);
            undoManager.Undo(5);

            var redone = undoManager.Redo(11);

            Assert.That(redone, Is.EqualTo(5));
        }

        [Test]
        public void WhenRestartingRecording_ThenCurrentRecordingSaved() {
            var counter = 0;

            undoManager.StartRecordingAction("test");
            undoManager.RegisterAndExecute(() => counter++, () => counter--);
            undoManager.RegisterAndExecute(() => counter++, () => counter--);

            undoManager.StartRecordingAction("another test");
            undoManager.StopRecordingAction();

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
        }

        [Test]
        public void WhenUndoingWhileRecording_ThenInvalidOperationException() {
            undoManager.StartRecordingAction("test");
            Assert.That(() => undoManager.Undo(), Throws.InvalidOperationException);
        }

        [Test]
        public void WhenRedoingWhileRecording_ThenInvalidOperationException() {
            undoManager.StartRecordingAction("test");
            Assert.That(() => undoManager.Redo(), Throws.InvalidOperationException);
        }

        [Test]
        public void WhenRegisteringDuringRegistration_WhenRecording_ThenInvalidOperationException() {
            undoManager.StartRecordingAction("test");
            Assert.That(() => undoManager.RegisterAndExecute(
                () => undoManager.RegisterAndExecute(() => { }, fail),
                fail
            ), Throws.InvalidOperationException);
            undoManager.StopRecordingAction();
        }

        [Test]
        public void WhenRegisteringDuringRegistration_WhenRecordingInInner_ThenWorks() {
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
        }
        
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
        }
        
        #endregion
        
        #region Buffer

        [Test]
        public void WhenPushingCustomBuffer_ThenProperlyForwarded() {
            var uut = Substitute.For<UndoBuffer>();
            
            var didOrig = false;
            var undidOrig = false;
            undoManager.RecordCompleteAction("Test action", () => 
                undoManager.RegisterAndExecute(() => didOrig = true, () => undidOrig = true));
            
            Assert.That(didOrig, Is.True);
            Assert.That(undidOrig, Is.False);
            
            undoManager.PushUndoBuffer(uut);
            
            uut.Received().OnBeforeAttach();

            Assert.That(undoManager.Undo(3), Is.Zero);
            Assert.That(undidOrig, Is.False);
            uut.Received().Undo(3);

            // ensure that recording is forwarded to custom buffer
            
            var customUndoneReceivedCheck = false;
            undoManager.RecordCompleteAction("Custom action", () => 
                undoManager.RegisterAndExecute(() => { }, () => customUndoneReceivedCheck = true));

            uut.Received(requiredNumberOfCalls: 1).RegisterUndo(Arg.Any<UndoAction>());
            Assert.That(uut.ReceivedCalls().Any(c => {
                if (c.GetMethodInfo() != typeof(UndoBuffer).GetMethod("RegisterUndo")) return false;
                (c.GetArguments()[0] as UndoAction)?.ExecuteUndo();
                return customUndoneReceivedCheck;
            }), Is.True);
            
            // ensure that no random undos happen during push or pop
            undoManager.RecordCompleteAction("Fail action", () => 
                undoManager.RegisterAndExecute(() => { }, () => Assert.Fail("Invalid undo called.")));
            
            var above = Substitute.For<UndoBuffer>();

            undoManager.PushUndoBuffer(above);
            above.Received().OnBeforeAttach();
            
            Assert.That(undoManager.Undo(3), Is.EqualTo(0));

            undoManager.PopUndoBuffer();
            above.Received().OnAfterDetach();
            
            undoManager.PopUndoBuffer();
            uut.Received().OnAfterDetach();
            
            Assert.That(undoManager.Undo(2), Is.EqualTo(1));
            Assert.That(undidOrig, Is.True);
        }

        [Test]
        public void WhenPushingDuringRecording_ThenStateSavedUntilPopped() {
            var topLevelActionName = "topLevelActionName";
            undoManager.StartRecordingAction(topLevelActionName);
            
            Assert.That(undoManager.IsRecording, Is.True);
            Assert.That(undoManager.CurrentActionName, Is.EqualTo(topLevelActionName));

            var topLevelUndone = false;
            undoManager.RegisterAndExecute(() => {}, () => topLevelUndone = true);
            
            var uut = Substitute.For<UndoBuffer>();
            Assert.That(() => undoManager.PushUndoBuffer(uut), Throws.Nothing);

            Assert.That(undoManager.IsRecording, Is.False);
            Assert.That(undoManager.CurrentActionName, Is.Null);
            Assert.That(() => undoManager.StopRecordingAction(), Throws.Exception);

            var uutActionName = "uutActionName";
            undoManager.StartRecordingAction(uutActionName);
            
            Assert.That(undoManager.IsRecording, Is.True);
            Assert.That(undoManager.CurrentActionName, Is.EqualTo(uutActionName));
            
            undoManager.RegisterAndExecute(() => {}, () => Assert.Fail("Invalid undo called."));
            undoManager.StopRecordingAction();
            
            Assert.That(undoManager.IsRecording, Is.False);
            Assert.That(undoManager.CurrentActionName, Is.Null);
            
            undoManager.PopUndoBuffer();
            
            Assert.That(undoManager.IsRecording, Is.True);
            Assert.That(undoManager.CurrentActionName, Is.EqualTo(topLevelActionName));

            var cancelUut = Substitute.For<UndoBuffer>();
            undoManager.PushUndoBuffer(cancelUut);
            
            undoManager.StartRecordingAction("to be cancelled by popping");
            undoManager.RegisterAndExecute(() => {}, () => Assert.Fail("Invalid undo called."));
            
            // we expect abort with a warning
            Assert.That(() => undoManager.PopUndoBuffer(), Throws.Nothing);
            Assert.That(cancelUut.Undo(1), Is.EqualTo(0));

            undoManager.StopRecordingAction();
            
            Assert.That(undoManager.Undo(2), Is.EqualTo(1));
            Assert.That(topLevelUndone, Is.True);
        }
        
        #endregion
        
        #region Placeholder
        
        [Test]
        public void WhenRegisteringPlaceholderWithoutRecording_ThenException() {
            Assert.That(undoManager.RegisterUndoPlaceholder, Throws.InvalidOperationException);
        }

        [Test]
        public void WhenRegisteringPlaceholderAndNotUsed_ThenIgnored() {
            undoManager.StartRecordingAction("test");
            
            var unusedPlaceholder = undoManager.RegisterUndoPlaceholder();
            
            bool? done = null;
            undoManager.RegisterAndExecute(() => done = true, () => done = false);

            undoManager.StopRecordingAction();

            Assert.That(done, Is.True);
            
            undoManager.Undo();

            Assert.That(done, Is.False);

            undoManager.Redo();

            Assert.That(done, Is.True);
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
        }
        
        [Test]
        public void WhenUsingPlaceholderAfterRecordingAbort_ThenException() {
            undoManager.StartRecordingAction("test");
            var placeholder = undoManager.RegisterUndoPlaceholder();
            undoManager.AbortRecordingAction();
            
            undoManager.StartRecordingAction("test2");
            
            Assert.That(
                () => undoManager.RegisterAndExecute(SymmetricOperation.Empty, saveUndoAt: placeholder),
                Throws.ArgumentException
            );
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
        }

        [Test]
        public void WhenPushingUndoBuffer_ThenEventInvoked() {
            int invokeCount = 0;

            void onUndoBufferPushed() => invokeCount++;

            undoManager.OnUndoBufferPushed += onUndoBufferPushed;
            
            var uut = Substitute.For<UndoBuffer>();
            
            undoManager.PushUndoBuffer(uut);

            Assert.That(invokeCount, Is.EqualTo(1));
        }
        
        [Test]
        public void WhenPoppingUndoBuffer_ThenEventInvoked() {
            int invokeCount = 0;

            void onUndoBufferPopped() => invokeCount++;

            undoManager.OnUndoBufferPopped += onUndoBufferPopped;
            
            var uut = Substitute.For<UndoBuffer>();
            
            undoManager.PushUndoBuffer(uut);
            
            Assert.That(invokeCount, Is.EqualTo(0));
            
            undoManager.PopUndoBuffer();

            Assert.That(invokeCount, Is.EqualTo(1));
        }

#endregion
    }
}