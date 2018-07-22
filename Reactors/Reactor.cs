using System;
using System.Collections.Generic;
using System.Text;

namespace Reactors
{
    public abstract class Reactor<TState> : ReactorBase
        where TState : class, new()
    {
        protected Reactor(string uniqueName, IReactorListener listener = null)
            : base(uniqueName, new TState(), listener)
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
        protected ReactorWithReplicatedState(string uniqueName, IReactorListener listener = null)
            : base(uniqueName, listener)
        {
        }

        protected sealed override object ReplicateState()
        {
            TReplicatedState newState = new TReplicatedState();
            Replicate(newState);
            return newState;
        }

        protected abstract void Replicate(TReplicatedState replState);
    }
}
