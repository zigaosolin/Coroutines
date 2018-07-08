using Coroutines;
using System;
using System.Collections.Generic;
using System.Text;

namespace Reactors
{
    public interface IReactorEvent : IEvent
    {
        IReactorReference Source { get; }
    }

    public interface IReactorEventWithReply<TReply> : IReactorEvent 
        where TReply : IReactorEvent
    {

    }
}
