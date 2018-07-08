using Coroutines;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Reactors.Tests
{
    public class ReactorRPCTest
    {
        class SendEvent : IReactorEvent
        {
        }

        class ResponseEvent : IReactorEvent
        {
        }

        class RPCReactor : Reactor
        {
            IReactorReference dest;
            public bool ResponseGained { get; private set; }

            IEnumerator<IWaitObject> SendRPC()
            {
                // We "abuse" RPC's reactor RPC here, otherwise we would need to
                // derive from ReactorCoroutine
                var rpc = RPC(dest, new SendEvent());
                yield return rpc;
                Assert.IsType<ResponseEvent>(rpc.Response);
                ResponseGained = true;
            }

            public RPCReactor(IReactorReference dest)
            {
                this.dest = dest;

                Execute(Coroutines.Coroutines.FromEnumerator(SendRPC()));
            }

            protected override void OnEvent(IReactorEvent ev)
            {
            }
        }

        class ResponseReactor : Reactor
        {
            protected override void OnEvent(IReactorEvent ev)
            {
                if(ev is SendEvent)
                {
                    Reply(new ResponseEvent());
                }
            }
        }

        [Fact]
        public void RPC()
        {
            var responseReactor = new ResponseReactor();
            var requestReactor = new RPCReactor(responseReactor);

            requestReactor.Update(0);
            responseReactor.Update(0);
            requestReactor.Update(0);

            Assert.True(requestReactor.ResponseGained);

        }

        [Fact]
        public void CoroutineRPC()
        {

        }

    }
}
