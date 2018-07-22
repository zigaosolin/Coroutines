using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Reactors
{
    public sealed class ReactorEventSink : ReactorBase
    {
        ConcurrentQueue<IReactorEvent> replyEvents = new ConcurrentQueue<IReactorEvent>();

        public ReactorEventSink(string uniqueName) 
            : base(uniqueName, new object(), null)
        {
        }

        protected override void OnEvent(IReactorEvent ev)
        {
            replyEvents.Enqueue(ev);
        }

        public IReactorEvent Dequeue()
        {
            if (replyEvents.TryDequeue(out IReactorEvent result))
                return result;
            return null;
        }
    }
}
