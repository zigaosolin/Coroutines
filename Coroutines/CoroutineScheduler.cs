using System;
using System.Collections.Generic;
using System.Text;

namespace Coroutines
{
    public interface ICoroutineScheduler
    {
    }

    public class CoroutineScheduler : ICoroutineScheduler
    {
        class CoroutineState
        {
            public Coroutine Coroutine;
            public IEnumerator<IWaitObject> Enumerator;
            public IWaitObject WaitForObject;
        }

        LinkedList<CoroutineState> executingCoroutines = new LinkedList<CoroutineState>();
        LinkedList<CoroutineState> waitingForTriggerCoroutines = new LinkedList<CoroutineState>();

        
        public void Update(float deltaTime)
        {
            for(var executingCoroutineNode = executingCoroutines.First; executingCoroutineNode != null; executingCoroutineNode = executingCoroutineNode.Next)
            {
                CoroutineState executingCoroutine = executingCoroutineNode.Value;
                var waitObject = executingCoroutine.WaitForObject;

                // If we need to poll wait object, this is done here.
                if(executingCoroutine.WaitForObject != null)
                {
                    if (!executingCoroutine.WaitForObject.IsComplete)
                        continue;

                    executingCoroutine.WaitForObject = null;
                }

                // Execute the coroutine's next frame
                bool isCompleted;
                try
                {
                    isCompleted = executingCoroutine.Enumerator.MoveNext();
                } catch(Exception ex)
                {
                    executingCoroutine.Coroutine.SignalException(ex);
                }
            }
        }


    }
}
