using System;
using System.Collections.Generic;
using System.Text;

namespace Reactors
{
    public interface IReactorListener
    {
        void OnMissedReplyEvent(IReactorReference reference, IReactorEvent ev, long replyID);
        void OnCriticalException(Exception ex);
        void OnValidationException(Exception ex);
    }
}
