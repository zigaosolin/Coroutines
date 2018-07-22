using System;
using System.Collections.Generic;
using System.Text;

namespace Reactors
{
    public interface IReactorReference
    {
        string Reference { get; }
        void Send(IReactorReference source, IReactorEvent ev, long eventID = 0, long replyID = 0);
    }
}
