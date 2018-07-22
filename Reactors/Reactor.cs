using System;
using System.Collections.Generic;
using System.Text;

namespace Reactors
{
    public abstract class Reactor<TState> : ReactorBase
        where TState : class, new()
    {
        protected Reactor()
            : base(new TState())
        {
        }

        public new TState State
        {
            get
            {
                return (TState)base.State;
            }
        }
    }

    public abstract class ReactorWithReplicatedState<TState, TReplicatedState> : Reactor<TState>
        where TState : class, new()
        where TReplicatedState : class, new()
    {
        public abstract TReplicatedState ReplicateState();
    }
}
