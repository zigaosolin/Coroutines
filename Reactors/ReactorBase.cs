using Coroutines;
using Reactors.Implementation;
using System;
using System.Collections.Generic;

namespace Reactors
{
    public abstract class ReactorBase : IReactorReference
    {
        EventQueue eventQueue;
        EventCoroutineScheduler scheduler;
        FullReactorEvent currentEvent;
        IReactorListener listener;
        string uniqueName;
        long sendEventID = 1;
        SortedDictionary<long, RPCWait> pendingRPCWaits = new SortedDictionary<long, RPCWait>();
        bool currentInCriticalSection = false;
        Exception criticalException = null;

        public object State { get; }

        internal ReactorBase(string uniqueName, object reactorState, IReactorListener listener)
        {
            this.uniqueName = uniqueName;
            State = reactorState;
            eventQueue = new EventQueue();
            scheduler = new EventCoroutineScheduler(eventQueue);
            this.listener = listener;
        }

        public void Enqueue(IReactorReference source, IReactorEvent ev, long eventID = 0, long replyID = 0)
        {
            eventQueue.Enqueue(new FullReactorEvent(ev, source, eventID, replyID));
        }

        public IReactorReference Reference
        {
            get
            {
                return this;
            }
        }

        public int Update(float deltaTime, int maxEvents = int.MaxValue, bool startNewEvents = true)
        {
            if (criticalException != null)
                throw criticalException;

            int eventsProcessed = 0;
            eventQueue.NewFrame();
            scheduler.NewFrame(deltaTime);

            try
            {
                for (int i = 0; i < maxEvents; i++)
                {
                    if (!eventQueue.TryDequeue(out IEvent ev))
                        break;

                    switch (ev)
                    {
                        case ICoroutineEvent cev:
                            {
                                eventsProcessed++;
                                Exception ex = scheduler.Update(cev);
                                if(ex != null)
                                {
                                    throw ex;
                                }
                            }
                            break;
                        case FullReactorEvent rev:
                            {
                                if (rev.ReplyID != 0)
                                {
                                    eventsProcessed++;

                                    if (pendingRPCWaits.TryGetValue(rev.ReplyID, out RPCWait value))
                                    {
                                        pendingRPCWaits.Remove(rev.ReplyID);
                                        value.Trigger(rev);
                                        continue;
                                    }

                                    listener?.OnMissedReplyEvent(rev.Source, rev.Event, rev.ReplyID);
                                    continue;
                                }

                                if (startNewEvents)
                                {
                                    currentEvent = rev;
                                    currentInCriticalSection = false;
                                    try
                                    {
                                        OnEvent(rev.Event);
                                    } catch(Exception ex)
                                    {
                                        if(currentInCriticalSection)
                                        {
                                            throw ex;
                                        } else
                                        {
                                            listener?.OnValidationException(ex);
                                        }
                                    }
                                    currentInCriticalSection = true;
                                    currentEvent = null;
                                }
                                else
                                {
                                    eventQueue.EnqueueNextFrame(rev);
                                }
                            }
                            break;
                        default:
                            throw new ReactorException("Invalid event type");
                    }
                }
            } catch(Exception ex)
            {
                listener?.OnCriticalException(ex);
                criticalException = ex;
                throw ex;
            }

            return eventsProcessed;
        }

        protected abstract void OnEvent(IReactorEvent ev);

        protected void EnterCriticalSection()
        {
            currentInCriticalSection = true;
        }

        protected void Execute(Coroutine coroutine)
        {
            scheduler.Execute(coroutine);
        }

        protected void Execute(ReactorCoroutine coroutine)
        {
            coroutine.Reactor = this;
            coroutine.State = State;
            coroutine.ReplyID = ReplyID;
            coroutine.Event = currentEvent.Event;
            coroutine.Source = EventSource;

            scheduler.Execute(coroutine);
        }

        protected IReactorReference EventSource
        {
            get
            {
                return currentEvent.Source;
            }
        }

        protected long ReplyID
        {
            get
            {
                return currentEvent.EventID;
            }
        }

        protected void Reply(IReactorEvent ev)
        {
            EventSource.Send(this, ev, GetNextEventID(), ReplyID);
        }

        protected void Reply(IReactorEvent ev, long replyID)
        {
            EventSource.Send(this, ev, GetNextEventID(), replyID);
        }

        internal long GetNextEventID()
        {
            return sendEventID++;
        }

        protected internal RPCWait ReplyRPC(IReactorEvent ev)
        {
            var data = new FullReactorEvent(ev, this, GetNextEventID(), ReplyID);
            var wait = new RPCWait(data, EventSource);
            pendingRPCWaits.Add(data.EventID, wait);

            EventSource.Send(data.Source, data.Event, data.EventID, ReplyID);

            return wait;
        }

        protected internal RPCWait ReplyRPC(IReactorEvent ev, long replyID)
        {
            var data = new FullReactorEvent(ev, this, GetNextEventID(), replyID);
            var wait = new RPCWait(data, EventSource);
            pendingRPCWaits.Add(data.EventID, wait);

            EventSource.Send(data.Source, data.Event, data.EventID, replyID);
            return wait;
        }

        protected internal RPCWait RPC(IReactorReference dest, IReactorEvent ev)
        {
            var data = new FullReactorEvent(ev, this, GetNextEventID(), 0);
            var wait = new RPCWait(data, dest);
            pendingRPCWaits.Add(data.EventID, wait);

            dest.Send(data.Source, data.Event, data.EventID, data.ReplyID);

            return wait;
        }

        void IReactorReference.Send(IReactorReference source, IReactorEvent ev, long eventID, long replyID)
        {
            Enqueue(source, ev, eventID, replyID);
        }

        string IReactorReference.UniqueName => uniqueName;

        public object ReactorState => State;
    }
}
