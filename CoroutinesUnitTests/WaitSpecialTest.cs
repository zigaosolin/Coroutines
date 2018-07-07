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

            var threadWait = Coroutines.WaitForThread(
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
                var asyncWait = Coroutines.WaitForAsync(TaskAsync());
                yield return asyncWait;
                yield return CompleteWithResult(asyncWait.Result);
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

        class WaitForAnyTestCoroutine : Coroutine
        {
            protected override IEnumerator<IWaitObject> Execute()
            {
                yield return Coroutines.WaitForAny(
                    Coroutines.WaitForAsync(Task.Delay(300)),
                    Coroutines.WaitForAsync(Task.Delay(350)),
                    Coroutines.WaitForAsync(Task.Delay(100))
                    );
            }
        }

        [Fact]
        public async Task WaitForAny()
        {
            var scheduler = new InterleavedCoroutineScheduler();

            var coroutine = new WaitForAnyTestCoroutine();
            scheduler.Execute(coroutine);

            scheduler.Update(0);
            await Task.Delay(50); // All async waiters still executing
            scheduler.Update(0.05f);
            Assert.Equal(CoroutineStatus.Running, coroutine.Status);
            await Task.Delay(150); // One should be done here
            scheduler.Update(0.150f);
            Assert.Equal(CoroutineStatus.CompletedNormal, coroutine.Status);
        }

        class WaitForAllTestCoroutine : Coroutine
        {
            protected override IEnumerator<IWaitObject> Execute()
            {
                yield return Coroutines.WaitForAll(
                    Coroutines.WaitForAsync(Task.Delay(300)),
                    Coroutines.WaitForAsync(Task.Delay(350)),
                    Coroutines.WaitForAsync(Task.Delay(100))
                    );
            }
        }

        [Fact]
        public async Task WaitForAll()
        {
            var scheduler = new InterleavedCoroutineScheduler();

            var coroutine = new WaitForAllTestCoroutine();
            scheduler.Execute(coroutine);

            scheduler.Update(0);
            await Task.Delay(50); // All async waiters still executing
            scheduler.Update(0.05f);
            Assert.Equal(CoroutineStatus.Running, coroutine.Status);
            await Task.Delay(150); // One should be done here
            scheduler.Update(0.150f);
            Assert.Equal(CoroutineStatus.Running, coroutine.Status);
            await Task.Delay(250); // All should be done here
            scheduler.Update(0.250f);
            Assert.Equal(CoroutineStatus.CompletedNormal, coroutine.Status);
        }

        class WaitForAnyWithCancelTestCoroutine : Coroutine
        {
            int loops;
            Coroutine[] childrenToStart;

            public WaitForAnyWithCancelTestCoroutine(params Coroutine[] children)
            {
                childrenToStart = children;
            }

            public WaitForAnyWithCancelTestCoroutine(int loops)
            {
                this.loops = loops;
            }

            protected override IEnumerator<IWaitObject> Execute()
            {
                if (childrenToStart != null)
                {
                    yield return Coroutines.WaitForAnyCancelOthers(
                        childrenToStart
                        );
                }
                else
                {
                    for(int i = 0; i < loops; i++)
                    {
                        yield return null;
                    }
                }
            }
        }

        [Fact]
        public void WaitForAnyWithCancel()
        {
            var scheduler = new InterleavedCoroutineScheduler();

            var sub1 = new WaitForAnyWithCancelTestCoroutine(1);
            var sub2 = new WaitForAnyWithCancelTestCoroutine(2);
            var sub3 = new WaitForAnyWithCancelTestCoroutine(3);

            var coroutine = new WaitForAnyWithCancelTestCoroutine(sub1, sub2, sub3);
            scheduler.Execute(coroutine);

            Assert.Equal(CoroutineStatus.WaitingForStart, sub1.Status);
            scheduler.Update(0);
            Assert.Equal(CoroutineStatus.Running, sub1.Status);
            Assert.Equal(CoroutineStatus.Running, sub2.Status);
            Assert.Equal(CoroutineStatus.Running, sub3.Status);
            scheduler.Update(0);
            Assert.Equal(CoroutineStatus.CompletedNormal, sub1.Status);
            
            // Not immediatelly trigerred because WaitFor uses polling
            scheduler.Update(0);
            Assert.Equal(CoroutineStatus.Cancelled, sub2.Status);
            Assert.Equal(CoroutineStatus.Cancelled, sub3.Status);
            Assert.Equal(CoroutineStatus.CompletedNormal, coroutine.Status);
        }

        class WaitForExceptionPropagationCoroutine : Coroutine
        {
            protected override IEnumerator<IWaitObject> Execute()
            {
                yield return Coroutines.WaitForAll(
                    Coroutines.WaitForAsync(Task.Run(() => throw new Exception("Internal exception"))),
                    Coroutines.WaitForAsync(Task.Delay(350))
                    );
            }
        }

        [Fact]
        public async Task WaitForExceptionPropagation()
        {
            var scheduler = new InterleavedCoroutineScheduler();

            var coroutine = new WaitForExceptionPropagationCoroutine();
            scheduler.Execute(coroutine);

            scheduler.Update(0);
            Assert.Equal(CoroutineStatus.Running, coroutine.Status);
            await Task.Delay(50);
            scheduler.Update(0);
            Assert.Equal(CoroutineStatus.CompletedWithException, coroutine.Status);
            Assert.IsType<AggregateException>(coroutine.Exception);
        }
    }
}
