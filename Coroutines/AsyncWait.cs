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
        }

        void InternalOnCompleted()
        {
            if(task.Status == TaskStatus.Canceled)
            {
                SignalCancelled();
            } else if(task.Status == TaskStatus.RanToCompletion)
            {
                SignalComplete(task.Result);
            } else if(task.Status == TaskStatus.Faulted)
            {
                SignalException(task.Exception);
            }

            throw new CoroutineException("Should never be reachable");
        }

        protected internal override IEnumerator<IWaitObject> Execute()
        {
            return null;
        }
    }
}
