using Coroutines.Implementation;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Coroutines
{
    internal class EventExecutionState : IExecutionState
    {
        public float DeltaTime { get; private set; }
        public long FrameIndex { get; private set; }

        internal void Update(float deltaTime)
        {
            DeltaTime = deltaTime;
            FrameIndex++;
        }
    }

    public interface ICoroutineEvent : IEvent
    {
    }


    internal class ContinueCoroutineEvent : ICoroutineEvent
    {
        public long CoroutineID { get; }

        public ContinueCoroutineEvent(long coroutineID)
        {
            CoroutineID = coroutineID;
        }
    }

    internal class StartCoroutineEvent : ICoroutineEvent
    {
        public Coroutine Coroutine { get; }

        public StartCoroutineEvent(Coroutine coroutine)
        {
            Coroutine = coroutine;
        }
    }

    internal class EventCoroutineState
    {
        public long CoroutineID { get; set; }
        public Coroutine Coroutine { get; set; }
        public IWaitObject WaitForObject { get; set; }
        public IEnumerator<IWaitObject> Iterator { get; set; }
    }

    // This implementation will queue all continuations into one
    // event poll. This event poll can be used for other events
    // as well. You need to call update with the next event that
    // is dequeued from event poll. ICoroutineEvent as meant for
    // this scheduler
    public class EventCoroutineScheduler : ICoroutineScheduler
    {
        IEventPusher eventQueue;
        EventExecutionState executionState = new EventExecutionState();
        TimerTrigger<ContinueCoroutineEvent> timerTrigger = new TimerTrigger<ContinueCoroutineEvent>();
        int updateThreadID = -1;

        long currentCoroutineID = 0;
        Dictionary<long, EventCoroutineState> runningCoroutines = new Dictionary<long, EventCoroutineState>();

        public EventCoroutineScheduler(IEventPusher eventQueue)
        {
            this.eventQueue = eventQueue;
        }

        public void NewFrame(float deltaTime)
        {
            executionState.Update(deltaTime);
            timerTrigger.Update(deltaTime,
                (ContinueCoroutineEvent ev) => eventQueue.Enqueue(ev)
            );
        }

        public void Execute(Coroutine coroutine)
        {
            coroutine.SignalStarted(this, executionState, null);          
            var ev = new StartCoroutineEvent(coroutine);
            eventQueue.Enqueue(ev);
        }

        public void ExecuteImmediately(Coroutine coroutine)
        {
            if (updateThreadID == -1)
            {
                throw new CoroutineException("ExecuteImmediatelly not called from coroutine");
            }

            if (updateThreadID != Thread.CurrentThread.ManagedThreadId)
            {
                throw new CoroutineException("ExecuteImmediatelly called from different scheduler than current coroutine");
            }

            coroutine.SignalStarted(this, executionState, null);
            StartAndMakeFirstIteration(coroutine);
        }

        public Exception Update(ICoroutineEvent nextEvent)
        {
            if(updateThreadID != -1)
            {
                throw new CoroutineException("Update called from more than one thread");
            }
            updateThreadID = Thread.CurrentThread.ManagedThreadId;

            Exception result = null;
            switch (nextEvent)
            {
                case StartCoroutineEvent sce:
                    result = StartCoroutine(sce);
                    break;
                case ContinueCoroutineEvent cce:
                    result = ContinueCoroutine(cce);
                    break;
            }

            updateThreadID = -1;

            return result;
        }

        private Exception StartCoroutine(StartCoroutineEvent sce)
        {
            return StartAndMakeFirstIteration(sce.Coroutine);
        }

        private Exception ContinueCoroutine(ContinueCoroutineEvent cce)
        {
            var state = runningCoroutines[cce.CoroutineID];

            var coroutine = state.Coroutine;
            var waitForObject = state.WaitForObject;
            var iterator = state.Iterator;

            // If we need to poll wait object, this is done here (no notifies)
            if (waitForObject != null)
            {
                if (!waitForObject.IsComplete)
                {
                    // We are not finished, poll next frame
                    eventQueue.EnqueueNextFrame(cce);
                    return null;
                }

                // We check if wait object ended in bad state
                if (waitForObject.Exception != null)
                {
                    coroutine.SignalException(
                        new AggregateException("Wait for object threw an exception", waitForObject.Exception));

                    return coroutine.Exception;
                }

                waitForObject = null;
            }

            if(AdvanceCoroutine(state.CoroutineID, coroutine, iterator))
            {
                // We remove it if completed
                runningCoroutines.Remove(state.CoroutineID);
            }

            return coroutine.Exception;
        }


        private bool AdvanceCoroutine(long coroutineID, Coroutine coroutine, IEnumerator<IWaitObject> iterator)
        {
            while (true)
            {
                // Execute the coroutine's next frame
                bool isCompleted;

                // We need to lock to ensure cancellation from source does not interfere with frame
                lock (coroutine.SyncRoot)
                {
                    // Cancellation can come from outside, as well as completion
                    if (coroutine.IsComplete)
                    {
                        return true;
                    }

                    try
                    {
                        isCompleted = !iterator.MoveNext();
                    }
                    catch (Exception ex)
                    {
                        coroutine.SignalException(ex);
                        return true;
                    }

                    if (isCompleted)
                    {
                        coroutine.SignalComplete(false, null);
                        return true;
                    }
                }

                IWaitObject newWait = iterator.Current;

                // Special case null means wait to next frame
                if (newWait is WaitForSeconds waitForSeconds)
                {
                    timerTrigger.AddTrigger(waitForSeconds.WaitTime, new ContinueCoroutineEvent(coroutineID));
                    return false;
                }

                // Special case null means wait to next frame
                if (newWait == null)
                {
                    eventQueue.EnqueueNextFrame(new ContinueCoroutineEvent(coroutineID));
                    return false;
                }
                else if (newWait is ReturnValue retVal)
                {
                    coroutine.SignalComplete(true, retVal.Result);
                    return true;
                }

                if (newWait is Coroutine newWaitCoroutine)
                {
                    // If we yield an unstarted coroutine, we add it to this scheduler!
                    if (newWaitCoroutine.Status == CoroutineStatus.WaitingForStart)
                    {
                        coroutine.SignalStarted(this, executionState, coroutine);
                        StartAndMakeFirstIteration(newWaitCoroutine);

                        switch (newWaitCoroutine.Status)
                        {
                            case CoroutineStatus.CompletedWithException:
                                coroutine.SignalException(newWaitCoroutine.Exception);
                                return true;
                            case CoroutineStatus.Cancelled:
                                coroutine.SignalException(new OperationCanceledException("Internal coroutine was cancelled"));
                                return true;
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
                            eventQueue.Enqueue(new ContinueCoroutineEvent(coroutineID));
                        });
                }

                return false;
            }
        }

        private Exception StartAndMakeFirstIteration(Coroutine coroutine)
        {           
            var iterator = coroutine.Execute();

            var state = new EventCoroutineState()
            {
                CoroutineID = currentCoroutineID++,
                Coroutine = coroutine,
                Iterator = iterator
            };

            
            if(!AdvanceCoroutine(state.CoroutineID, coroutine, iterator))
            {
                // We add it to running coroutines if not completed
                runningCoroutines.Add(state.CoroutineID, state);
            }

            return coroutine.Exception;
        }
    }
}
