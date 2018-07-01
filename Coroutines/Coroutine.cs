using System;
using System.Collections.Generic;

namespace Coroutines
{
    public enum CoroutineStatus
    {
        WaitingForStart,
        Running,
        Cancelled,
        Paused,
        CompletedNormal,
        CompletedWithException
    }

    public abstract class Coroutine : IWaitObject
    {
        public CoroutineStatus Status { get; internal set; }
        public Coroutine Spawner { get; internal set; }
        public ICoroutineScheduler Scheduler { get; internal set; }

        public bool IsComplete => Status == CoroutineStatus.CompletedNormal || Status == CoroutineStatus.CompletedWithException || Status == CoroutineStatus.Cancelled;

        public void OnCompleted(Action continuation)
        {           
        }

        internal protected abstract IEnumerator<IWaitObject> Execute();
        
    }
}
