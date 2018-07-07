using System;
using System.Collections.Generic;
using System.Text;

namespace Reactors
{
    public abstract class ReactorWithState<TReplicatedState> : Reactor
    {
        public abstract TReplicatedState ReplicateState();
    }
}
