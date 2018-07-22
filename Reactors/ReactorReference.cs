using System;
using System.Collections.Generic;
using System.Text;

namespace Reactors
{
    public interface IReactorReference
    {
        string UniqueName { get; }
        void Send(IReactorReference source, IReactorEvent ev, long eventID, long replyID);
    }
}
