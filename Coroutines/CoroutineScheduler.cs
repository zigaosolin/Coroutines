using System;
using System.Collections.Generic;
using System.Text;

namespace Coroutines
{
    public interface ICoroutineScheduler
    {
        void Execute(Coroutine coroutine);

        // Can be only called from coroutine with the same scheduler
        void ExecuteImmediately(Coroutine coroutine);
    }
}
