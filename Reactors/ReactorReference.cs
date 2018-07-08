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

    public interface IReplicatedStateConnector<TReplicatedState>
    {
        DateTime LastUpdate { get; }
        TReplicatedState State { get; }
    }

    public interface IReactorReference<TReplicatedState> : IReactorReference
    {
        IReplicatedStateConnector<TReplicatedState> Subscribe(float desiredFrequencyOfUpdate);
        void Unsubscribe(IReplicatedStateConnector<TReplicatedState> connector);
    }
}
