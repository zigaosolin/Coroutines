using System;
using System.Collections.Concurrent;
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
        List<CoroutineState> scheduledCoroutines = new List<CoroutineState>();
        ConcurrentQueue<CoroutineState> trigerredCoroutines = new ConcurrentQueue<CoroutineState>();
        ConcurrentQueue<CoroutineState> enqueuedCoroutines = new ConcurrentQueue<CoroutineState>();

        public void Execute(Coroutine coroutine)
        {
            var state = CreateCoroutineState(coroutine);
            state.Coroutine.SignalStarted();
            enqueuedCoroutines.Enqueue(state);
        }

        public void Update(float deltaTime)
        {
            // Dequeue all enqueued coroutines and schedule them after all other coroutines
            while(true)
            {
                if(enqueuedCoroutines.TryDequeue(out CoroutineState result))
                {
                    executingCoroutines.AddLast(result);
                }
                break;
            }

            // Also dequeue all trigerred coroutines
            while (true)
            {
                if (!trigerredCoroutines.TryDequeue(out CoroutineState result))
                    break;

                executingCoroutines.AddLast(result);
            }

            for (var executingCoroutineNode = executingCoroutines.First; executingCoroutineNode != null; executingCoroutineNode = executingCoroutineNode.Next)
            {
                CoroutineState executingCoroutine = executingCoroutineNode.Value;
                var waitObject = executingCoroutine.WaitForObject;

                // If we need to poll wait object, this is done here (no notifies)
                if (executingCoroutine.WaitForObject != null)
                {
                    if (!executingCoroutine.WaitForObject.IsComplete)
                        continue;

                    // We check if wait object ended in bad state
                    if(waitObject.Exception != null)
                    {
                        executingCoroutine.Coroutine.SignalException(
                            new AggregateException("Wait for object threw an exception", waitObject.Exception));
                        continue;
                    }

                    executingCoroutine.WaitForObject = null;
                }

                var advanceAction = AdvanceCoroutine(executingCoroutine);
                switch (advanceAction)
                {
                    case AdvanceAction.Keep:
                        break;
                    case AdvanceAction.MoveToWaitForTrigger:
                        executingCoroutineNode.List.Remove(executingCoroutineNode);
                        break;
                    case AdvanceAction.Complete:
                        executingCoroutineNode.List.Remove(executingCoroutineNode);
                        scheduledCoroutines.Remove(executingCoroutineNode.Value);
                        break;
                }

                while (true)
                {
                    if (!trigerredCoroutines.TryDequeue(out CoroutineState result))
                        break;

                    executingCoroutines.AddLast(result);
                }
            }
        }

        enum AdvanceAction
        {
            Keep,
            MoveToWaitForTrigger,
            Complete
        }


        private AdvanceAction AdvanceCoroutine(CoroutineState executingCoroutine)
        {
            var coroutine = executingCoroutine.Coroutine;

            while (true)
            {
                // Execute the coroutine's next frame
                bool isCompleted;
                try
                {
                    isCompleted = !executingCoroutine.Enumerator.MoveNext();
                }
                catch (Exception ex)
                {
                    coroutine.SignalException(ex);
                    return AdvanceAction.Complete;
                }

                if (isCompleted)
                {
                    coroutine.SignalComplete();
                    return AdvanceAction.Complete;
                }

                IWaitObject newWait = executingCoroutine.Enumerator.Current;
                executingCoroutine.WaitForObject = newWait;

                // Special case null means wait to next frame
                if (newWait == null)
                    return AdvanceAction.Keep;

                if (newWait is Coroutine newWaitCoroutine)
                {
                    TryStartSubCoroutine(newWaitCoroutine);

                    switch(newWaitCoroutine.Status)
                    {
                        case CoroutineStatus.CompletedNormal:
                            continue;

                        case CoroutineStatus.CompletedWithException:
                            coroutine.SignalException(newWaitCoroutine.Exception);
                            return AdvanceAction.Complete;

                        case CoroutineStatus.Cancelled:
                            coroutine.SignalException(new OperationCanceledException("Internal coroutine was cancelled"));
                            return AdvanceAction.Complete;
                    }
                }           
                else if (newWait.IsComplete)
                {
                    // If the wait object is complete, we continue immediatelly (yield does not split frames)
                    continue;
                }

                // Check if we get notified for completion, otherwise polling is used
                if (newWait is IWaitObjectWithNotifyCompletion withCompletion)
                {
                    withCompletion.RegisterCompleteSignal(
                        () =>
                        {
                            trigerredCoroutines.Enqueue(executingCoroutine);
                        });
                    return AdvanceAction.MoveToWaitForTrigger;

                }
                

                return AdvanceAction.Keep;
            }
        }

        private void TryStartSubCoroutine(Coroutine newWaitCoroutine)
        {
            // If we yield an unsarted coroutine, we add it to this scheduler!
            if (newWaitCoroutine.Status != CoroutineStatus.WaitingForStart)
                return;

            var internalState = CreateCoroutineState(newWaitCoroutine);
            newWaitCoroutine.SignalStarted();

            // We want to do evaluation to the first yield immediatelly
            var advanceAction = AdvanceCoroutine(internalState);

            switch (advanceAction)
            {
                case AdvanceAction.Keep:
                    executingCoroutines.AddFirst(internalState);
                    scheduledCoroutines.Add(internalState);
                    break;
                case AdvanceAction.MoveToWaitForTrigger:
                    scheduledCoroutines.Add(internalState);
                    break;
                case AdvanceAction.Complete:
                    break;
            }

        }

        private CoroutineState CreateCoroutineState(Coroutine executingCoroutine)
        {
            CoroutineState state = new CoroutineState()
            {
                Coroutine = executingCoroutine,
                Enumerator = executingCoroutine.Execute(),
                WaitForObject = null
            };

            return state;
        }
    }
}
