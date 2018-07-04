using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Coroutines.Tests
{
    public class CoroutineTest
    {
        class RunCoroutineExecuteThrowsException : Coroutine
        {
            protected override IEnumerator<IWaitObject> Execute()
            {
                // This code has no yield statements and is not
                // a enumerator (just a normal function)
                throw new Exception();
            }
        }

        [Fact]
        public void RunCoroutine_ExecuteThrowsException()
        {
            var scheduler = new CoroutineScheduler();

            var coroutine = new RunCoroutineExecuteThrowsException();

            Assert.Equal(CoroutineStatus.WaitingForStart, coroutine.Status);

            // This throws exception on Execute as there are no yield statements
            // and calling that method actually executes it (it does not prepare enumeration)
            Assert.Throws<Exception>(() => scheduler.Execute(coroutine));
        }

        class RunCoroutineExecuteThrowsExceptionInCoroutine : Coroutine
        {
            int numberOfNullReturns = 0;

            public RunCoroutineExecuteThrowsExceptionInCoroutine(int nullReturns)
            {
                numberOfNullReturns = nullReturns;
            }

            protected override IEnumerator<IWaitObject> Execute()
            {
                for (int i = 0; i < numberOfNullReturns; i++)
                {
                    yield return null;
                }
                throw new Exception();
            }
        }

        [Fact]
        public void RunCoroutine_ThrowsException()
        {
            var scheduler = new CoroutineScheduler();

            var coroutine = new RunCoroutineExecuteThrowsExceptionInCoroutine(1);

            Assert.Equal(CoroutineStatus.WaitingForStart, coroutine.Status);
            scheduler.Execute(coroutine);
            Assert.Equal(CoroutineStatus.Running, coroutine.Status);
            scheduler.Update(0);
            Assert.Equal(CoroutineStatus.Running, coroutine.Status);
            scheduler.Update(0);

            Assert.Equal(CoroutineStatus.CompletedWithException, coroutine.Status);
            Assert.IsType<Exception>(coroutine.Exception);
        }

        class RunCoroutineWithYieldNull : Coroutine
        {
            public int Iteration { get; private set; } = 0;

            protected override IEnumerator<IWaitObject> Execute()
            {
                Iteration = 1;
                yield return null;
                Iteration = 2;
                yield return Coroutines.NextFrame(); // same as null
                Iteration = 3;
                yield break;
            }
        }

        [Fact]
        public void RunCoroutine_WithYieldNull()
        {
            var scheduler = new CoroutineScheduler();

            var coroutine = new RunCoroutineWithYieldNull();
            Assert.Equal(CoroutineStatus.WaitingForStart, coroutine.Status);
            scheduler.Execute(coroutine);
            Assert.Equal(CoroutineStatus.Running, coroutine.Status);

            Assert.Equal(0, coroutine.Iteration);
            scheduler.Update(0);
            Assert.Equal(1, coroutine.Iteration);
            scheduler.Update(0);
            Assert.Equal(2, coroutine.Iteration);
            scheduler.Update(0);
            Assert.Equal(3, coroutine.Iteration);
            Assert.Equal(CoroutineStatus.CompletedNormal, coroutine.Status);
        }

    }
}
