using System;
using System.Collections.Generic;
using System.Text;

namespace Reactors.Implementation
{
    internal class FullReactorEvent : IReactorEvent
    {
        public IReactorEvent Event { get; }
        public IReactorReference Source { get; }
        public long EventID { get; }
        public long ReplyID { get; }

        public FullReactorEvent(IReactorEvent ev, IReactorReference source, long eventID, long replyID)
        {
            Event = ev;
            Source = source;
            EventID = eventID;
            ReplyID = replyID;
        }
    }
}
