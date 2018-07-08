using Coroutines;
using System;
using System.Collections.Generic;
using System.Text;

namespace Reactors
{
    public class RPCWait : IWaitObjectWithNotifyCompletion
    {
        object syncRoot = new object();
        Implementation.FullReactorEvent data;
        IReactorReference dest;

        bool isCompleted = false;
        Implementation.FullReactorEvent replyData = null;
        Action onCompleted = null;

        internal RPCWait(Implementation.FullReactorEvent data, IReactorReference dest)
        {
            this.data = data;
            this.dest = dest;
        }

        internal void Timeout()
        {
            lock(syncRoot)
            {
                if (isCompleted)
                    return;

                isCompleted = true;

                onCompleted?.Invoke();
            }
        }

        internal void Trigger(Implementation.FullReactorEvent replyData)
        {
            lock(syncRoot)
            {
                if (isCompleted)
                    return;

                this.replyData = replyData;
                isCompleted = true;

                onCompleted?.Invoke();
            }
        }

        public bool IsComplete { get; }

        public Exception Exception
        {
            get
            {
                return replyData == null ? new ReactorException("RPC has timed out") : null;
            }
        }

        public IReactorEvent Response
        {
            get
            {
                if (!isCompleted)
                    throw new ReactorException("You must yield return RPC wait to get the response");

                return replyData.Event;
            }
        }

        void IWaitObjectWithNotifyCompletion.RegisterCompleteSignal(Action onCompleted)
        {
            lock(syncRoot)
            {
                if(IsComplete)
                {
                    onCompleted();
                } else
                {
                    this.onCompleted += onCompleted;
                }
            }
        }
    }
}
