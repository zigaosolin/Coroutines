using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Coroutines.Tests
{
    public class WaitSpecialTest
    {
        IEnumerator<IWaitObject> CoroutineThread()
        {
            yield return null;

            ThreadWait threadWait = new ThreadWait(
                new Thread(
                    () => Thread.Sleep(100)
                )
            );
            yield return threadWait;
        }

        [Fact]
        public async Task WaitForThread()
        {
            // This test may fail sometimes if threads are busy

            var scheduler = new InterleavedCoroutineScheduler();

            var coroutine = Coroutines.FromEnumerator(CoroutineThread());
            scheduler.Execute(coroutine);

            Assert.Equal(CoroutineStatus.Running, coroutine.Status);
            scheduler.Update(0); // Null is yielded
            Assert.Equal(CoroutineStatus.Running, coroutine.Status);
            scheduler.Update(0.0f); // Thread is yielded, 300 ms before it end
            await Task.Delay(75);
            scheduler.Update(0.075f); // Thread should not be over yet
            Assert.Equal(CoroutineStatus.Running, coroutine.Status);
            await Task.Delay(75);
            scheduler.Update(0.075f);
            Assert.Equal(CoroutineStatus.CompletedNormal, coroutine.Status);
        }

        class AwaitCoroutine : Coroutine<string>
        {
            async Task<string> TaskAsync()
            {
                await Task.Delay(10);
                return "TEST-DONE";
            }

            protected override IEnumerator<IWaitObject> Execute()
            {
                var waitObject = new AsyncWait<string>(TaskAsync());
                yield return waitObject;
                Result = waitObject.Result;
            }

        }
        [Fact]
        public void WaitForAsync()
        {
            var scheduler = new InterleavedCoroutineScheduler();

            var coroutine = new AwaitCoroutine();
            scheduler.Execute(coroutine);

            while(!coroutine.IsComplete)
            {
                scheduler.Update(0.0f);
            }

            Assert.Equal("TEST-DONE", coroutine.Result);
        }
    }
}
