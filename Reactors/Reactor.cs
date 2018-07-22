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
}
