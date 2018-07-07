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

            while(true)
            {
                // We have a lock from scheduler here
                if (AreCriteriaMet())
                {
                    if (cancelUncompleted)
                    {
                        cancelUncompleted = false;
                        CancelUncompleted();

                    }
                    yield break;
                }

                yield return null;
            }
        }
        
        private bool AreCriteriaMet()
        {
            int completed = 0;
            for (int i = 0; i < waitObjects.Length; i++)
            {
                completed += waitObjects[i].IsComplete ? 1 : 0;
            }

            if (completed >= mustCompleteCount)
                return true;

            return false;
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
