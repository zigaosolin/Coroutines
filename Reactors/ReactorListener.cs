using System;
using System.Collections.Generic;
using System.Text;

namespace Reactors
{
    public interface IReactorListener
    {
        void OnMissedReplyEvent(IReactorReference reference, IReactorEvent ev, long replyID);
        void OnReplicationRequestedOnActorWithoutReplication(IReactorReference reference);
    }
}
