﻿using Coroutines;
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

        class RPCReactorState
        {
            public bool ResponseGained { get; set; }
        }

        class RPCReactor : Reactor<RPCReactorState>
        {
            IReactorReference dest;
            
            IEnumerator<IWaitObject> SendRPC()
            {
                // We "abuse" RPC's reactor RPC here, otherwise we would need to
                // derive from ReactorCoroutine
                var rpc = RPC(dest, new SendEvent());
                yield return rpc;
                Assert.IsType<ResponseEvent>(rpc.Response);
                State.ResponseGained = true;
            }

            public RPCReactor(IReactorReference dest)
                : base("RPCReactor")
            {
                this.dest = dest;

                Execute(Coroutine.FromEnumerator(SendRPC()));
            }

            protected override void OnEvent(IReactorEvent ev)
            {
            }
        }

        class ResponseReactor : Reactor<object>
        {
            public ResponseReactor()
                : base("Response")
            {
            }

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

            Assert.True(requestReactor.State.ResponseGained);

        }

        [Fact]
        public void CoroutineRPC()
        {

        }

        [Fact]
        public void TimeoutTriggerRPC()
        {

        }
    }
}
