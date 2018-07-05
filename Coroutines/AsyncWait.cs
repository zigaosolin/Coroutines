using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Coroutines
{
    public class AsyncWait<T> : Coroutine<T>
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
                Result = task.Result;
                SignalComplete();
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
