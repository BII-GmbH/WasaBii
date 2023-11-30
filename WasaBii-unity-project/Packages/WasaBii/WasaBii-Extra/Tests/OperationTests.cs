using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using System.Threading.Tasks;
using BII.WasaBii.Unity;

namespace BII.WasaBii.Extra.Tests
{
    [TestFixture]
    public class OperationTests
    {
        private const int initialValue = 0;
        
        // Non-nullable field is initialized in `SetUp`, not the constructor
        #pragma warning disable 8618
        private RunContext _runContext;
        #pragma warning restore

        [SetUp]
        public void SetUp() {
            _runContext = new RunContext(new(), new(), new(), new());
        }

        private record RunContext
            : OperationContext<int>
        {
            public RunContext(
                List<string> stepsStarted,
                List<int> stepsCompleted,
                List<int> stepCountDiffs,
                List<double> reportedProgress
            ) : base(
                initialValue,
                stepsStarted.Add,
                s => stepsCompleted.Add(s),
                stepCountDiffs.Add,
                reportedProgress.Add,
                new CancellationToken()
            ) {
                this.StepsStarted = stepsStarted;
                this.StepsCompleted = stepsCompleted;
                this.StepCountDiffs = stepCountDiffs;
                this.ReportedProgress = reportedProgress;
            }

            public readonly List<string> StepsStarted;
            public readonly List<int> StepsCompleted;
            public readonly List<int> StepCountDiffs;
            public readonly List<double> ReportedProgress;
        }

#region Error Handling
        
        private class TestException : Exception { }
        
        // Note: There is `Assert.ThrowsAsync`, but I do not trust that to do what I want here.
        //       I explicitly want to test different low-level, low-magic async cases here.
        
        [Test]
        public async Task Run_Linear_WhenException_ThenReported() {
            var op = Operation.Empty().Step("throw", ctx => throw new TestException());
            
            var thrown = false;
            try {
                await op.Run(_runContext);
            } catch (TestException) {
                thrown = true;
            }
            Assert.That(thrown, Is.True);
        }
        
        [Test]
        public async Task Run_Suspended_WhenException_ThenReported() {
            var op = Operation.Empty().Step("throw", async ctx => {
                await AsyncWait.ForFrames(1);
                throw new TestException();
            });
            
            var thrown = false;
            try {
                await op.Run(_runContext);
            } catch (TestException) {
                thrown = true;
            }
            Assert.That(thrown, Is.True);
        }
        
        [Test]
        public async Task Run_OnOtherThread_WhenException_ThenReported() {
            var op = Operation.Empty().Step("throw", ctx => Task.Run(() => throw new TestException()));
            
            var thrown = false;
            try {
                await op.Run(_runContext);
            } catch (TestException) {
                thrown = true;
            }
            Assert.That(thrown, Is.True);
        }
        
#endregion

#region Events

        private void assertStepsStarted(params string[] expected) =>
            Assert.That(_runContext.StepsStarted, Is.EqualTo(expected));
        
        private void assertStepsCompleted(params int[] expected) =>
            Assert.That(_runContext.StepsCompleted, Is.EqualTo(expected));
        
        private void assertStepCountChanged(params int[] expected) =>
            Assert.That(_runContext.StepCountDiffs, Is.EqualTo(expected));
        
        private void assertReportedProgress(params double[] expected) =>
            Assert.That(_runContext.ReportedProgress, Is.EqualTo(expected));

        [Test]
        public async Task WhenStepping_ThenEventsProperlyCalled() {
            var initial = Operation.WithInput<int>();
            var first = initial.Step(
                "1",
                ctx => {
                    Assert.That(ctx.PreviousResult, Is.EqualTo(0));
                    assertStepsStarted("1");
                    assertStepsCompleted();
                    assertStepCountChanged();
                    assertReportedProgress();
                    ctx.ReportProgressInStep(0.5);
                    assertReportedProgress(0.5);
                    return Task.FromResult(ctx.PreviousResult + 1);
                }
            );
            var second = first.Step(
                "2",
                ctx => {
                    Assert.That(ctx.PreviousResult, Is.EqualTo(1));
                    assertStepsStarted("1", "2");
                    assertStepsCompleted(1);
                    assertStepCountChanged();
                    assertReportedProgress(0.5);
                    ctx.ReportProgressInStep(0.4);
                    assertReportedProgress(0.5, 0.4);
                    return Task.FromResult(ctx.PreviousResult + 1);
                }
            );
            var third = second.Step(
                "3",
                ctx => {
                    Assert.That(ctx.PreviousResult, Is.EqualTo(2));
                    assertStepsStarted("1", "2", "3");
                    assertStepsCompleted(1, 2);
                    assertStepCountChanged();
                    assertReportedProgress(0.5, 0.4);
                    ctx.ReportProgressInStep(0.3);
                    assertReportedProgress(0.5, 0.4, 0.3);
                    ctx.ReportProgressInStep(0.2);
                    assertReportedProgress(0.5, 0.4, 0.3, 0.2);
                    return Task.FromResult(ctx.PreviousResult + 1);
                }
            );
            Assert.That(third.EstimatedStepCount, Is.EqualTo(3));
            var res = await third.Run(_runContext);
            Assert.That(res, Is.EqualTo(3));
            assertStepsStarted("1", "2", "3");
            assertStepsCompleted(1, 2, 3);
            assertStepCountChanged();
            assertReportedProgress(0.5, 0.4, 0.3, 0.2);
        }
        
        [Test]
        public async Task WhenChaining_ThenEventsProperlyCalled() {
            var initial = Operation.WithInput<int>();
            var first = initial.Step(
                "1",
                ctx => {
                    Assert.That(ctx.PreviousResult, Is.EqualTo(0));
                    assertStepsStarted("1");
                    assertStepsCompleted();
                    assertStepCountChanged();
                    assertReportedProgress();
                    ctx.ReportProgressInStep(0.5);
                    assertReportedProgress(0.5);
                    return Task.FromResult(ctx.PreviousResult + 1);
                }
            );
            var second = first.Chain(Operation.WithInput<int>().Step(
                "2",
                ctx => {
                    Assert.That(ctx.PreviousResult, Is.EqualTo(1));
                    assertStepsStarted("1", "2");
                    assertStepsCompleted(1);
                    assertStepCountChanged();
                    assertReportedProgress(0.5);
                    ctx.ReportProgressInStep(0.4);
                    assertReportedProgress(0.5, 0.4);
                    return Task.FromResult(ctx.PreviousResult + 1);
                }
            ));
            var third = second.Chain(Operation.WithInput<int>().Step(
                "3",
                ctx => {
                    Assert.That(ctx.PreviousResult, Is.EqualTo(2));
                    assertStepsStarted("1", "2", "3");
                    assertStepsCompleted(1, 2);
                    assertStepCountChanged();
                    assertReportedProgress(0.5, 0.4);
                    ctx.ReportProgressInStep(0.3);
                    assertReportedProgress(0.5, 0.4, 0.3);
                    ctx.ReportProgressInStep(0.2);
                    assertReportedProgress(0.5, 0.4, 0.3, 0.2);
                    return Task.FromResult(ctx.PreviousResult + 1);
                }
            ));
            Assert.That(third.EstimatedStepCount, Is.EqualTo(3));
            var res = await third.Run(_runContext);
            Assert.That(res, Is.EqualTo(3));
            assertStepsStarted("1", "2", "3");
            assertStepsCompleted(1, 2, 3);
            assertStepCountChanged();
            assertReportedProgress(0.5, 0.4, 0.3, 0.2);
        }
        
        [Test]
        public async Task WhenFlatMapping_ThenEventsProperlyCalled() {
            var initial = Operation.WithInput<int>();
            var first = initial.Step(
                "1",
                ctx => {
                    Assert.That(ctx.PreviousResult, Is.EqualTo(0));
                    assertStepsStarted("1");
                    assertStepsCompleted();
                    assertStepCountChanged();
                    assertReportedProgress();
                    ctx.ReportProgressInStep(0.5);
                    assertReportedProgress(0.5);
                    return Task.FromResult(ctx.PreviousResult + 1);
                }
            );
            var second = first.FlatMap(2, r => Operation.From(r).Step(
                "2",
                ctx => {
                    Assert.That(ctx.PreviousResult, Is.EqualTo(1));
                    assertStepsStarted("1", "2");
                    assertStepsCompleted(1);
                    assertStepCountChanged(-1); // 1 less step than expected
                    assertReportedProgress(0.5);
                    ctx.ReportProgressInStep(0.4);
                    assertReportedProgress(0.5, 0.4);
                    return Task.FromResult(ctx.PreviousResult + 1);
                }
            ));
            var third = second.FlatMap(1, r => Operation.From(r).Step(
                "3",
                ctx => {
                    Assert.That(ctx.PreviousResult, Is.EqualTo(2));
                    assertStepsStarted("1", "2", "3");
                    assertStepsCompleted(1, 2);
                    assertStepCountChanged(-1); // not called bc step count matches expectation
                    assertReportedProgress(0.5, 0.4);
                    ctx.ReportProgressInStep(0.3);
                    assertReportedProgress(0.5, 0.4, 0.3);
                    ctx.ReportProgressInStep(0.2);
                    assertReportedProgress(0.5, 0.4, 0.3, 0.2);
                    return Task.FromResult(ctx.PreviousResult + 1);
                }
            ));
            Assert.That(third.EstimatedStepCount, Is.EqualTo(4));
            var res = await third.Run(_runContext);
            Assert.That(res, Is.EqualTo(3));
            assertStepsStarted("1", "2", "3");
            assertStepsCompleted(1, 2, 3);
            assertStepCountChanged(-1);
            assertReportedProgress(0.5, 0.4, 0.3, 0.2);
        }
        
#endregion
        
#region ChatGPT generated (basic functionality)

        [Test]
        public async Task From_Returns_Operation_With_Expected_Result() {
            // Arrange
            int expected = 42;

            // Act
            var operation = Operation.From(expected);
            var result = await operation.Run(_runContext);

            // Assert
            Assert.AreEqual(expected, result);
        }

        [Test]
        public async Task Step_Returns_Operation_With_Expected_Result() {
            // Arrange
            int input = 10;
            int expected = input + 5;

            // Act
            var operation = Operation.From(input)
                .Step("AddFive", async context => {
                    int result = context.PreviousResult + 5;
                    return await Task.FromResult(result);
                });
            var result = await operation.Run(_runContext);

            // Assert
            Assert.AreEqual(expected, result);
        }
        
        [Test]
        public async Task Chain_Returns_Operation_With_Expected_Result() {
            // Arrange
            int input = 10;
            int expected = input + 5 + 3;

            var first = Operation.From(input)
                .Step("AddFive", async ctx => {
                    int result = ctx.PreviousResult + 5;
                    return await Task.FromResult(result);
                });

            var second = Operation.WithInput<int>().Step("AddThree", async context => {
                int result = context.PreviousResult + 3;
                return await Task.FromResult(result);
            });

            // Act
            var operation = first.Chain(second);
            var result = await operation.Run(_runContext);

            // Assert
            Assert.AreEqual(expected, result);
        }
        
        [Test]
        public async Task FlatMap_Returns_Operation_With_Expected_Result() {
            // Arrange
            int input = 10;
            int expected = input + 5 + 3;

            var first = Operation.From(input)
                .Step("AddFive", async ctx => {
                    int result = ctx.PreviousResult + 5;
                    return await Task.FromResult(result);
                });

            // Act
            var operation = first.FlatMap(2, x => 
                Operation.From(x).Step("AddThree", async context => {
                    int result = context.PreviousResult + 3;
                    return await Task.FromResult(result);
                }));
            var result = await operation.Run(_runContext);

            // Assert
            Assert.AreEqual(expected, result);
        }
        
#endregion
    }
}