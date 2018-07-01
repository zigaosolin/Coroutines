using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Coroutines
{
    public class AsyncWait<T> : IWaitObject
    {
        Task<T> task;

        public AsyncWait(Task<T> task)
        {
            this.task = task;
        }

        public bool IsComplete => task.IsCompleted;

        public void OnCompleted(Action continuation)
        {
            task.GetAwaiter().OnCompleted(continuation);
        }

        public T Result
        {
            get
            {
                return task.Result;
            }
        }
    }
}
