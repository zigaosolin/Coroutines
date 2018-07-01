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
                throw new Exception();
            }
        }

        [Fact]
        public void RunCoroutine_ExectureThrowsException()
        {

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
