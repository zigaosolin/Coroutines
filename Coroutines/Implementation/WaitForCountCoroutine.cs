using System;
using System.Collections.Generic;
using System.Text;

namespace Coroutines.Implementation
{
    public class WaitForCountCoroutine : Coroutine
    {
        int mustCompleteCount = 0;
        IWaitObject[] waitObjects;
        volatile bool cancelUncompleted = false;

        public WaitForCountCoroutine(List<IWaitObject> waitObjects, int mustCompleteCount, bool cancelUncompleted)
        {
            this.waitObjects = waitObjects.ToArray();
            this.mustCompleteCount = mustCompleteCount;
            this.cancelUncompleted = cancelUncompleted;
        }

        protected internal override IEnumerator<IWaitObject> Execute()
        {
            StartWaitCoroutines();

            // TODO: we could subscribe to all notifies here. We could
            // even make execute return null if all waits have notifies
            // and avoid polling all together

            while (true)
            {            
                int completed = 0;
                for (int i = 0; i < waitObjects.Length; i++)
                {
                    if (waitObjects[i].IsComplete)
                    {
                        completed += 1;
                        if (waitObjects[i].Exception != null)
                        {
                            throw new AggregateException("WaitFor* internal coroutine threw and exception", waitObjects[i].Exception);
                        }
                    }         
                }

                if (completed >= mustCompleteCount)
                    break;

                yield return null;
            }

            if(cancelUncompleted)
            {
                CancelUncompleted();
            }
        }
        

        private void StartWaitCoroutines()
        {
            for (int i = 0; i < waitObjects.Length; i++)
            {
                var waitObject = waitObjects[i];
                if (waitObject is Coroutine waitCoroutine)
                {
                    if (waitCoroutine.Status == CoroutineStatus.WaitingForStart)
                    {
                        Scheduler.ExecuteImmediately(waitCoroutine);
                    }
                }
            }
        }

        private void CancelUncompleted()
        {
            for (int i = 0; i < waitObjects.Length; i++)
            {
                var waitObject = waitObjects[i];
                if (waitObject is Coroutine waitCoroutine)
                {
                    if(!waitCoroutine.IsComplete)
                    {
                        waitCoroutine.Cancel();
                    }
                }
            }
        }
    }
}
