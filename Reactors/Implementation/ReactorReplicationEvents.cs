using System;
using System.Collections.Generic;
using System.Text;

namespace Reactors.Implementation
{
    internal sealed class ReactorGetReplicatedStateEvent : IReactorEvent
    {
    }

    internal sealed class ReactorStateReplicated : IReactorEvent
    {
        public object ReplicatedState { get; set; }
    }
}
