using Coroutines;
using System;

namespace Reactors
{
    public abstract class Reactor
    {
        EventQueue eventQueue;
        EventCoroutineScheduler scheduler;

        public Reactor()
        {
            eventQueue = new EventQueue();
            scheduler = new EventCoroutineScheduler(eventQueue);
        }

        public void Enqueue(IReactorEvent ev)
        {
            eventQueue.Enqueue(ev);
        }

        public void Update(float deltaTime)
        {
            eventQueue.NewFrame();
            scheduler.NewFrame(deltaTime);

            while(true)
            {
                if (!eventQueue.TryDequeue(out IEvent ev))
                    break;

                if(ev is ICoroutineEvent cev)
                {
                    scheduler.Update(cev);
                }
                else if(ev is IReactorEvent rev)
                {
                    OnEvent(rev);
                }
                else
                {
                    throw new ReactorException("Invalid event type");
                }
            }
        }

        protected abstract void OnEvent(IReactorEvent ev);

        protected void Execute(Coroutine coroutine)
        {
            scheduler.Execute(coroutine);
        }
    }
}
