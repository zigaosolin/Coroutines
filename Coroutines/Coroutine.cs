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

    public abstract class Coroutine : IWaitObject, IWaitObjectWithNotifyCompletion
    {
        object syncRoot = new object();
        List<Action> onCompletedNotifies = new List<Action>();
        object result = null;
        volatile CoroutineStatus status = CoroutineStatus.WaitingForStart;

        public CoroutineStatus Status
        {
            get
            {
                return status;
            }
            internal set
            {
                if (IsComplete)
                    throw new CoroutineException("Setting status to already completed coroutine");

                status = value;
            }
        }

        public Coroutine Spawner { get; internal set; }
        public ICoroutineScheduler Scheduler { get; internal set; }
        public Exception Exception { get; private set; }
        public ExecutionState ExecutionState { get; internal set; }    

        public object Result
        {
            get
            {
                if (!IsComplete)
                    throw new CoroutineException("Trying to access result before waiting for Coroutine to complete. You must yield return it to wait for result");

                return result;
            }
            set
            {
                result = value;
            }
        }

        public bool IsComplete
        {
            get
            {
                return Status == CoroutineStatus.CompletedNormal ||
                    Status == CoroutineStatus.CompletedWithException ||
                    Status == CoroutineStatus.Cancelled;
            }
        }

        bool IWaitObject.IsComplete => throw new NotImplementedException();

        // Return null if coroutine is already started/scheduled manually.
        // This is useful when scheduled as async or with any other system
        internal protected abstract IEnumerator<IWaitObject> Execute();

        void IWaitObjectWithNotifyCompletion.RegisterCompleteSignal(Action onCompleted)
        {
            lock (syncRoot)
            {
                if (IsComplete)
                {
                    onCompleted();
                }
                else
                {
                    onCompletedNotifies.Add(onCompleted);
                }
            }
        }

        internal void SignalCancelled()
        {
            lock(syncRoot)
            {
                Exception = new OperationCanceledException();
                Status = CoroutineStatus.Cancelled;               
                NotifyCompleted();
            }
        }

        internal void SignalException(Exception ex)
        {
            lock (syncRoot)
            {
                Exception = ex;
                Status = CoroutineStatus.CompletedWithException;
                NotifyCompleted();
            }
        }

        internal void SignalComplete()
        {
            lock(syncRoot)
            {
                Status = CoroutineStatus.CompletedNormal;
                NotifyCompleted();
            }
        }
        
        void NotifyCompleted()
        {
            foreach (var notify in onCompletedNotifies)
            {
                notify();
            }
        }
    }

    public abstract class Coroutine<T> : Coroutine
    {
        public new T Result
        {
            get
            {
                return (T)base.Result;
            }
            set
            {
                base.Result = value;
            }
        }
    }
}
