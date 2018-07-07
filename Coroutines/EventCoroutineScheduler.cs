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

    internal class ContinueCoroutineEvent : ICoroutineEvent
    {
        public Coroutine Coroutine { get; }
        public IWaitObject WaitForObject { get; }
        public IEnumerator<IWaitObject> Iterator { get; }

        public ContinueCoroutineEvent(Coroutine coroutine, IWaitObject waitForObject, IEnumerator<IWaitObject> iterator)
        {
            Coroutine = coroutine;
            WaitForObject = waitForObject;
            Iterator = iterator;
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

    // This implementation will queue all continuations into one
    // event poll. This event poll can be used for other events
    // as well. You need to call update with the next event that
    // is dequeued from event poll. ICoroutineEvent as meant for
    // this scheduler
    public class EventCoroutineScheduler : ICoroutineScheduler
    {
        IEventQueue eventQueue;
        EventExecutionState executionState = new EventExecutionState();
        int updateThreadID = -1;

        public EventCoroutineScheduler(IEventQueue eventQueue)
        {
            this.eventQueue = eventQueue;
        }

        public void NewFrame(float deltaTime)
        {
            executionState.Update(deltaTime);
        }

        public void Execute(Coroutine coroutine)
        {
            coroutine.SignalStarted(this, executionState, null);
            var ev = new StartCoroutineEvent(coroutine);
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

            StartSubCoroutine(coroutine, null);
        }

        public void Update(ICoroutineEvent nextEvent)
        {
            switch (nextEvent)
            {
                case StartCoroutineEvent sce:
                    StartCoroutine(sce);
                    break;
                case ContinueCoroutineEvent cce:
                    ContinueCoroutine(cce);
                    break;
            }
        }

        private void StartCoroutine(StartCoroutineEvent sce)
        {
            StartSubCoroutine(sce.Coroutine, null);
        }

        private void ContinueCoroutine(ContinueCoroutineEvent cce)
        {
            var coroutine = cce.Coroutine;
            var waitForObject = cce.WaitForObject;
            var iterator = cce.Iterator;

            // If we need to poll wait object, this is done here (no notifies)
            if (waitForObject != null)
            {
                if (!waitForObject.IsComplete)
                {
                    // We are not finished, poll next frame
                    eventQueue.Enqueue(cce, nextFrame: true);
                    return;
                }

                // We check if wait object ended in bad state
                if (waitForObject.Exception != null)
                {
                    coroutine.SignalException(
                        new AggregateException("Wait for object threw an exception", waitForObject.Exception));

                    return;
                }

                waitForObject = null;
            }

            AdvanceCoroutine(coroutine, iterator);
        }


        private bool AdvanceCoroutine(Coroutine coroutine, IEnumerator<IWaitObject> iterator)
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
                if (newWait == null)
                {
                    eventQueue.Enqueue(new ContinueCoroutineEvent(coroutine, null, iterator), nextFrame: true);
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
                        StartSubCoroutine(newWaitCoroutine, coroutine);

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
                            eventQueue.Enqueue(new ContinueCoroutineEvent(coroutine, null, iterator), nextFrame: false);
                        });
                }

                return false;
            }
        }

        private void StartSubCoroutine(Coroutine coroutine, Coroutine spawner)
        {           
            coroutine.SignalStarted(this, executionState, spawner);

            var iterator = coroutine.Execute();

            // We want to do evaluation to the first yield immediatelly
            AdvanceCoroutine(coroutine, iterator);        
        }
    }
}
