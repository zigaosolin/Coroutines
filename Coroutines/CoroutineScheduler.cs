using System;
using System.Collections.Generic;
using System.Text;

namespace Coroutines
{
    public interface ICoroutineScheduler
    {
    }

    public class CoroutineScheduler : ICoroutineScheduler
    {

        class CoroutineState
        {
            public Coroutine Coroutine;
            public IWaitObject WaitForObject;
            public bool MustPollForWaitObject;
        }



    }
}
