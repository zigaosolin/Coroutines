using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Xunit;

namespace Coroutines.Tests
{
    public class WaitThreadTest
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
        public void WaitForThread()
        {
            Coroutines.FromEnumerator(CoroutineThread());
        }
    }
}
