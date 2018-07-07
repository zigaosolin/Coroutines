using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Coroutines.Implementation
{
    internal class AsyncWait<T> : Coroutine<T>
    {
        Task<T> task;

        public AsyncWait(Task<T> task)
        {
            this.task = task;
            this.task.GetAwaiter().OnCompleted(InternalOnCompleted);

            SignalStarted(null, null, null);
        }

        void InternalOnCompleted()
        {
            if (task.Status == TaskStatus.Canceled)
            {
                SignalCancelled();
            }
            else if (task.Status == TaskStatus.RanToCompletion)
            {
                SignalComplete(true, task.Result);
            }
            else if (task.Status == TaskStatus.Faulted)
            {
                SignalException(task.Exception);
            }
            else
            {
                throw new CoroutineException("Should never be reachable");
            }
        }

        protected internal override IEnumerator<IWaitObject> Execute()
        {
            return null;
        }
    }

    internal class AsyncWait : Coroutine
    {
        Task task;

        public AsyncWait(Task task)
        {
            this.task = task;
            this.task.GetAwaiter().OnCompleted(InternalOnCompleted);

            SignalStarted(null, null, null);
        }

        void InternalOnCompleted()
        {
            if (task.Status == TaskStatus.Canceled)
            {
                SignalCancelled();
            }
            else if (task.Status == TaskStatus.RanToCompletion)
            {
                SignalComplete(false, null);
            }
            else if (task.Status == TaskStatus.Faulted)
            {
                SignalException(task.Exception);
            }
            else
            {
                throw new CoroutineException("Should never be reachable");
            }
        }

        protected internal override IEnumerator<IWaitObject> Execute()
        {
            return null;
        }
    }
}
