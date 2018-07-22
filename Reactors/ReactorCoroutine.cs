using System;
using System.Collections.Generic;
using System.Text;
using Coroutines;

namespace Reactors
{
    public abstract class ReactorCoroutine : Coroutine
    {
        public ReactorBase Reactor { get; internal set; }
        public object State { get; internal set; }
        public IReactorReference Source { get; internal set; }
        public IReactorEvent Event { get; internal set; }
        public long ReplyID { get; internal set; }

        protected void Reply(IReactorEvent ev)
        {
            Source.Send(Reactor, ev, Reactor.GetNextEventID(),  ReplyID);
        }

        protected void Reply(IReactorEvent ev, long replyID)
        {
            Source.Send(Reactor, ev, Reactor.GetNextEventID(), replyID);
        }

        protected RPCWait ReplyRPC(IReactorEvent ev)
        {
            return Reactor.ReplyRPC(ev, ReplyID);
        }

        protected RPCWait ReplyRPC(IReactorEvent ev, long replyID)
        {
            return Reactor.ReplyRPC(ev, replyID);
        }

        protected RPCWait RPC(IReactorReference dest, IReactorEvent ev)
        {
            return Reactor.RPC(dest, ev);
        }

    }

    public abstract class ReactorCoroutine<TReactorState> : ReactorCoroutine
        where TReactorState : class, new()
    {
        public new TReactorState State
        {
            get
            {
                return (TReactorState)base.State;
            }
        }
    }
}
