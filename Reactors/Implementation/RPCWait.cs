using Coroutines;
using System;
using System.Collections.Generic;
using System.Text;

namespace Reactors.Implementation
{
    internal class RPCWait<TReply> : IWaitObjectWithNotifyCompletion
        where TReply : IReactorEvent
    {
        Reactor source;
        Reactor destination;
        IReactorEventWithReply<TReply> sentEvent;

        public RPCWait(Reactor source, Reactor destination, IReactorEventWithReply<TReply> ev)
        {
            this.source = source;
            this.destination = destination;
            this.sentEvent = ev;
        }

        public bool IsComplete => throw new NotImplementedException();

        public Exception Exception => throw new NotImplementedException();

        public void RegisterCompleteSignal(Action onCompleted)
        {
            throw new NotImplementedException();
        }
    }
}
