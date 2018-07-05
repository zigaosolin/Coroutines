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
        internal object SyncRoot { get; } = new object();
        List<Action> onCompletedNotifies = new List<Action>();
        object result = null;
        volatile CoroutineStatus status = CoroutineStatus.WaitingForStart;

        public CoroutineStatus Status
        {
            get
            {
                return status;
            }
            private set
            {
                status = value;
            }
        }

        public Coroutine Spawner { get; private set; }
        public ICoroutineScheduler Scheduler { get; private set; }
        public Exception Exception { get; private set; }
        public IExecutionState ExecutionState { get; private set; }    

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

        public bool Cancel()
        {
            lock (SyncRoot)
            {
                if (Status != CoroutineStatus.Running)
                    return false;

                SignalCancelled();
                return true;
            }         
        }

        bool IWaitObject.IsComplete => IsComplete;

        // Return null if coroutine is already started/scheduled manually.
        // This is useful when scheduled as async or with any other system
        internal protected abstract IEnumerator<IWaitObject> Execute();

        void IWaitObjectWithNotifyCompletion.RegisterCompleteSignal(Action onCompleted)
        {
            lock (SyncRoot)
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
            lock(SyncRoot)
            {
                if(Status != CoroutineStatus.Running)
                    throw new CoroutineException("Cancelling coroutine that is not running");
                
                Exception = new OperationCanceledException();
                Status = CoroutineStatus.Cancelled;               
                NotifyCompleted();
            }
        }

        internal void SignalException(Exception ex)
        {
            lock (SyncRoot)
            {
                if (Status != CoroutineStatus.Running)
                    throw new CoroutineException("Signalling end state to running coroutine");

                Exception = ex;
                Status = CoroutineStatus.CompletedWithException;
                NotifyCompleted();
            }
        }

        internal void SignalComplete()
        {
            lock(SyncRoot)
            {
                if (Status != CoroutineStatus.Running)
                    throw new CoroutineException("Signalling end state to running coroutine");

                Status = CoroutineStatus.CompletedNormal;
                NotifyCompleted();
            }
        }

        internal void SignalStarted(ICoroutineScheduler scheduler, IExecutionState executionState, Coroutine spawner)
        {
            lock(SyncRoot)
            {
                if (Status != CoroutineStatus.WaitingForStart)
                    throw new CoroutineException("Signalling start to non-stated coroutine");

                Scheduler = scheduler;
                ExecutionState = executionState;
                Spawner = spawner;
                Status = CoroutineStatus.Running;
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
