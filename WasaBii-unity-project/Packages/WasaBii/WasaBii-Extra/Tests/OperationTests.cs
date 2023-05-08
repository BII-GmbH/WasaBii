using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using System.Threading.Tasks;

namespace BII.WasaBii.Extra.Tests
{
    [TestFixture]
    public class OperationTests
    {
        private OperationContext _runContext;

        [SetUp]
        public void SetUp() {
            _runContext = new RunContext(new(), new(), new(), new());
        }

        private record RunContext
            : OperationContext
        {
            public RunContext(
                List<string> stepsStarted,
                List<int> stepsCompleted,
                List<int> stepCounts,
                List<double> reportedProgress
            ) : base(
                stepsStarted.Add,
                stepsCompleted.Add,
                stepCounts.Add,
                reportedProgress.Add,
                new CancellationToken()
            ) {
                this.StepsStarted = stepsStarted;
                this.StepsCompleted = stepsCompleted;
                this.StepCounts = stepCounts;
                this.ReportedProgress = reportedProgress;
            }

            public readonly List<string> StepsStarted;
            public readonly List<int> StepsCompleted;
            public readonly List<int> StepCounts;
            public readonly List<double> ReportedProgress;
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