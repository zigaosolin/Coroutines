using System;
using System.Collections.Generic;
using System.Text;

namespace Reactors
{
    public interface IReactorReference
    {
        string GUID { get; }
        void Send(IReactorEvent ev);
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
