using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Coroutines.Tests
{
    public class EventSchedulerTest
    {
        public class NextFrameCoroutine : Coroutine
        {
            protected override IEnumerator<IWaitObject> Execute()
            {
                throw new NotImplementedException();
            }
        }

        [Fact]
        public void NextFrame()
        {

        }
    }
}
