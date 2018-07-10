using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Coroutines.Tests
{
    public class WaitForSecondsTest
    {

        class WaitCoroutine : Coroutine
        {
            float time;
            public int Iteration { get; private set; } = 0;

            public WaitCoroutine(float time)
            {
                this.time = time;
            }

            protected override IEnumerator<IWaitObject> Execute()
            {
                Iteration = 0;
                yield return Coroutine.WaitForSeconds(time);
                Iteration = 1;
            }
        }

        [Fact]
        public void WaitForTime()
        {
            var coroutine = new WaitCoroutine(1.0f);

            var scheduler = new InterleavedCoroutineScheduler();
            scheduler.Execute(coroutine);

            scheduler.Update(0.1f);
            Assert.Equal(CoroutineStatus.Running, coroutine.Status);
            scheduler.Update(0.5f);
            scheduler.Update(0.2f);
            Assert.Equal(0, coroutine.Iteration);
            scheduler.Update(0.31f);
            Assert.Equal(1, coroutine.Iteration); //< Wait is trigerred
            scheduler.Update(0.01f);
            Assert.Equal(1, coroutine.Iteration);
            Assert.Equal(CoroutineStatus.CompletedNormal, coroutine.Status);
        }

        [Fact]
        public void ManyWaitForSecondsAtTheSameTime()
        {
            var scheduler = new InterleavedCoroutineScheduler();

            Random random = new Random(31321);
            List<WaitCoroutine> coroutines = new List<WaitCoroutine>();
            for(int i = 0; i < 100; i++)
            {
                coroutines.Add(new WaitCoroutine((float)random.NextDouble() * 3.0f));
                scheduler.Execute(coroutines[i]);
            }
            
            for(int i = 0; i < 100; i++)
            {
                scheduler.Update(0.031f);
            }

            for (int i = 0; i < 100; i++)
            {
                Assert.Equal(CoroutineStatus.CompletedNormal, coroutines[i].Status);
            }

        }
    }
}
