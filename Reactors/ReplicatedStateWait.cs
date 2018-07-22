using Reactors.Implementation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Reactors
{
    public sealed class ReplicatedStateWait<TReplicatedState> : RPCWait
        where TReplicatedState : class, new()
    {
        internal ReplicatedStateWait(FullReactorEvent data, IReactorReference dest)
            : base(data, dest)
        {
        }

        public new TReplicatedState Response
        {
            get
            {
                var response = base.Response;
                var responnseReplicatedState = (ReactorStateReplicated)response;
                return (TReplicatedState)responnseReplicatedState.ReplicatedState;
            }
        }

    }
}
