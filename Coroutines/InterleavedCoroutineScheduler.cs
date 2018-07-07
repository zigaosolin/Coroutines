using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Coroutines
{
    internal class InterleavedExecutionState : IExecutionState
    {
        public float DeltaTime { get; private set; }
        public long FrameIndex { get; private set; }

        internal void Update(float deltaTime, long frameIndex)
        {
            DeltaTime = deltaTime;
            FrameIndex = frameIndex;
        }
    }

    public class InterleavedCoroutineScheduler : ICoroutineScheduler
    {
        class CoroutineState
        {
            public Coroutine Coroutine;
            public IEnumerator<IWaitObject> Enumerator;
            public IWaitObject WaitForObject;
        }

        LinkedList<CoroutineState> executingCoroutines = new LinkedList<CoroutineState>();
        ConcurrentQueue<CoroutineState> trigerredCoroutines = new ConcurrentQueue<CoroutineState>();
        ConcurrentQueue<CoroutineState> enqueuedCoroutines = new ConcurrentQueue<CoroutineState>();
        InterleavedExecutionState executionState = new InterleavedExecutionState();
        volatile int updateThreadID = -1;

        public void Execute(Coroutine coroutine)
        {
            var state = CreateCoroutineState(coroutine);
            state.Coroutine.SignalStarted(this, executionState, null);
            enqueuedCoroutines.Enqueue(state);
        }

        public void ExecuteImmediately(Coroutine coroutine)
        {
            if(updateThreadID == -1)
            {
                throw new CoroutineException("ExecuteImmediatelly not called from coroutine");
            }

            if(updateThreadID != Thread.CurrentThread.ManagedThreadId)
            {
                throw new CoroutineException("ExecuteImmediatelly called from different scheduler than current coroutine");
            }

            StartSubCoroutine(coroutine, null);
        }

        public void Update(float deltaTime)
        {
            // Dequeue all enqueued coroutines and schedule them after all other coroutines
            while(true)
            {
                if(!enqueuedCoroutines.TryDequeue(out CoroutineState result))
                {
                    break;
                }

                executingCoroutines.AddLast(result);
            }

            if(updateThreadID != -1)
            {
                throw new CoroutineException("Invalid operation; are you calling Update from more than one thread");
            }
            updateThreadID = Thread.CurrentThread.ManagedThreadId;

            executionState.Update(deltaTime, executionState.FrameIndex + 1);

            // Also dequeue all trigerred coroutines
            while (true)
            {
                if (!trigerredCoroutines.TryDequeue(out CoroutineState result))
                    break;

                executingCoroutines.AddLast(result);
            }

            for (var executingCoroutineNode = executingCoroutines.First; executingCoroutineNode != null; )
            {
                CoroutineState executingCoroutine = executingCoroutineNode.Value;
                var waitObject = executingCoroutine.WaitForObject;

                // If we need to poll wait object, this is done here (no notifies)
                if (waitObject != null)
                {
                    if (!waitObject.IsComplete)
                    {
                        executingCoroutineNode = executingCoroutineNode.Next;
                        continue;
                    }

                    // We check if wait object ended in bad state
                    if(waitObject.Exception != null)
                    {
                        executingCoroutine.Coroutine.SignalException(
                            new AggregateException("Wait for object threw an exception", waitObject.Exception));

                        var tempNode = executingCoroutineNode;
                        executingCoroutineNode = executingCoroutineNode.Next;
                        tempNode.List.Remove(tempNode);
                        continue;
                    }

                    executingCoroutine.WaitForObject = null;
                }

                var advanceAction = AdvanceCoroutine(executingCoroutine);

                var nextNode = executingCoroutineNode.Next;
                switch (advanceAction)
                {
                    case AdvanceAction.Keep:
                        break;
                    case AdvanceAction.MoveToWaitForTrigger:
                        executingCoroutineNode.List.Remove(executingCoroutineNode);
                        break;
                    case AdvanceAction.Complete:
                        executingCoroutineNode.List.Remove(executingCoroutineNode);
                        break;
                }

                while (true)
                {
                    if (!trigerredCoroutines.TryDequeue(out CoroutineState result))
                        break;

                    executingCoroutines.AddLast(result);

                    // Special case, if we are at end, we removed the node, the next node
                    // points to null when in fact it should point to trigerred coroutine
                    if (advanceAction == AdvanceAction.MoveToWaitForTrigger ||
                        advanceAction == AdvanceAction.Complete)
                    {                
                        if (nextNode == null)
                        {
                            nextNode = executingCoroutines.First;
                        }
                    }
                }

                executingCoroutineNode = nextNode;
            }

            updateThreadID = -1;
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

                // We need to lock to ensure cancellation from source does not interfere with frame
                lock (coroutine.SyncRoot)
                {
                    // Cancellation can come from outside, as well as completion
                    if(coroutine.IsComplete)
                    {
                        return AdvanceAction.Complete;
                    }

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
                        coroutine.SignalComplete(false, null);
                        return AdvanceAction.Complete;
                    }
                }

                IWaitObject newWait = executingCoroutine.Enumerator.Current;
                executingCoroutine.WaitForObject = newWait;

                // Special case null means wait to next frame
                if (newWait == null)
                {
                    return AdvanceAction.Keep;
                }
                else if (newWait is ReturnValue retVal)
                {
                    coroutine.SignalComplete(true, retVal.Result);
                    return AdvanceAction.Complete;
                }

                if (newWait is Coroutine newWaitCoroutine)
                {
                    // If we yield an unstarted coroutine, we add it to this scheduler!
                    if (newWaitCoroutine.Status == CoroutineStatus.WaitingForStart)
                    {
                        StartSubCoroutine(newWaitCoroutine, coroutine);

                        switch (newWaitCoroutine.Status)
                        {
                            case CoroutineStatus.CompletedWithException:
                                coroutine.SignalException(newWaitCoroutine.Exception);
                                return AdvanceAction.Complete;
                            case CoroutineStatus.Cancelled:
                                coroutine.SignalException(new OperationCanceledException("Internal coroutine was cancelled"));
                                return AdvanceAction.Complete;
                        }
                    }
                }

                if (newWait.IsComplete)
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

        private void StartSubCoroutine(Coroutine newWaitCoroutine, Coroutine spawner)
        {
            var internalState = CreateCoroutineState(newWaitCoroutine);
            newWaitCoroutine.SignalStarted(this, executionState, spawner);

            // We want to do evaluation to the first yield immediatelly
            var advanceAction = AdvanceCoroutine(internalState);

            switch (advanceAction)
            {
                case AdvanceAction.Keep:
                    executingCoroutines.AddFirst(internalState);
                    break;
                case AdvanceAction.MoveToWaitForTrigger:
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
