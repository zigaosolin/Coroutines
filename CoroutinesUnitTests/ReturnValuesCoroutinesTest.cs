using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Coroutines.Tests
{
    public class ReturnValuesCoroutinesTest
    {
        class ReturnValueCoroutine : Coroutine<int>
        {
            int max;

            public ReturnValueCoroutine(int max)
            {
                this.max = max;
            }

            protected override IEnumerator<IWaitObject> Execute()
            {
                for(int i = 0; i < max; i++)
                {
                    if(i == 2)
                    {
                        yield return CompleteWithResult(i);
                    }
                    yield return null;
                }

                // Result is not set if we go over may
            }
        }

        [Fact]
        public void CoroutineReturnValue()
        {
            var scheduler = new InterleavedCoroutineScheduler();
            var coroutine = new ReturnValueCoroutine(3);

            scheduler.Execute(coroutine);
            scheduler.Update(0);
            Assert.Equal(CoroutineStatus.Running, coroutine.Status);
            scheduler.Update(0);
            Assert.Equal(CoroutineStatus.Running, coroutine.Status);
            scheduler.Update(0);
            Assert.Equal(CoroutineStatus.CompletedNormal, coroutine.Status);
            Assert.Equal(2, coroutine.Result);
        }

        [Fact]
        public void CoroutineReturnValue_ForgotToSetResult()
        {
            var scheduler = new InterleavedCoroutineScheduler();
            var coroutine = new ReturnValueCoroutine(1);

            scheduler.Execute(coroutine);
            scheduler.Update(0);
            Assert.Equal(CoroutineStatus.Running, coroutine.Status);
            scheduler.Update(0);
            Assert.Equal(CoroutineStatus.CompletedNormal, coroutine.Status);
            Assert.Throws<CoroutineException>(() => coroutine.Result);
        }


    }
}
