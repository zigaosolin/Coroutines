using System;
using System.Collections.Generic;
using System.Text;

namespace Coroutines.Implementation
{
    public class WaitForCountCoroutine : Coroutine
    {
        int mustCompleteCount = 0;
        IWaitObject[] waitObjects;
        bool cancelUncompleted = false;

        public WaitForCountCoroutine(List<IWaitObject> waitObjects, int mustCompleteCount, bool cancelUncompleted)
        {
            this.waitObjects = waitObjects.ToArray();
            this.mustCompleteCount = mustCompleteCount;
            this.cancelUncompleted = cancelUncompleted;
        }

        protected internal override IEnumerator<IWaitObject> Execute()
        {
            StartWaitCoroutines();

            while(true)
            {
                int completed = 0;
                for(int i = 0; i < waitObjects.Length; i++)
                {
                    completed += waitObjects[i].IsComplete ? 1 : 0;
                }

                if (completed > mustCompleteCount)
                    break;

                yield return null;
            }

            if(cancelUncompleted)
            {
                // TODO:
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
    }
}
