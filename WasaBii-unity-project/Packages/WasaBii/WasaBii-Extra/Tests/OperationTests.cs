using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace BII.WasaBii.Extra.Tests
{
    [TestFixture]
    public class OperationTests
    {
        private const int initialValue = 0;
        private RunContext _runContext;

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
                stepsCompleted.Add,
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

        private void assertStepsStarted(params string[] expected) =>
            Assert.That(_runContext.StepsStarted, Is.EqualTo(expected));
        
        private void assertStepsCompleted(params int[] expected) =>
            Assert.That(_runContext.StepsCompleted, Is.EqualTo(expected));
        
        private void assertStepCountChanged(params int[] expected) =>
            Assert.That(_runContext.StepCountDiffs, Is.EqualTo(expected));
        
        private void assertReportedProgress(params double[] expected) =>
            Assert.That(_runContext.ReportedProgress, Is.EqualTo(expected));

        [Test]
        [CanBeNull]
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
        public async Task Step_With_Task_Returns_Operation_With_Expected_Result() {
            // Arrange
            int input = 10;
            int expected = input + 5;

            // Act
            var operation = Operation.From(input)
                .Step("AddFive", async () => {
                    int result = input + 5;
                    return await Task.FromResult(result);
                });
            var result = await operation.Run(_runContext);

            // Assert
            Assert.AreEqual(expected, result);
        }

        [Test]
        public async Task Step_With_Context_Returns_Operation_With_Expected_Result() {
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
            int expected = 6; // result of first discarded

            var first = Operation.From(input)
                .Step("AddFive", async () => {
                    int result = input + 5;
                    return await Task.FromResult(result);
                });

            var second = Operation.From(3);

            // Act
            var operation = first.Chain(second)
                .Step("AddThree", async context => {
                    int result = context.PreviousResult + 3;
                    return await Task.FromResult(result);
                });
            var result = await operation.Run(_runContext);

            // Assert
            Assert.AreEqual(expected, result);
        }

        [Test]
        public async Task ChainWithStart_Returns_Operation_With_Expected_Result() {
            // Arrange
            int input = 10;
            int expected = input + 5 + 3;

            var first = Operation.From(input)
                .Step("AddFive", async () => {
                    int result = input + 5;
                    return await Task.FromResult(result);
                });

            var second = Operation.WithInput<int>().Map(v => v + 3);

            // Act
            var operation = first.Chain(second);
            var result = await operation.Run(_runContext);

            // Assert
            Assert.AreEqual(expected, result);
        }
        
        [Test]
        public async Task FlatMapWithStart_Returns_Operation_With_Expected_Result() {
            // Arrange
            int input = 10;
            int expected = input + 5 + 3;

            var first = Operation.From(input)
                .Step("AddFive", async () => {
                    int result = input + 5;
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