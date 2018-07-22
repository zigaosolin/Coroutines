using System;
using System.Collections.Generic;
using System.Text;
using Coroutines;
using Xunit;

namespace Reactors.Tests
{
    public class ReactorReplicationTest
    {
        class ReplicationReactorReplState
        {
            public string Data { get; set; }
        }

        // Full state is usually a superset of replicated state
        class ReplicationReactorState : ReplicationReactorReplState
        {

        }

        class ChangeStateEvent : IReactorEvent
        {
            public string NewData { get; set; }
        }

        class ReplicationReactor : ReactorWithReplicatedState<ReplicationReactorState, ReplicationReactorReplState>
        {
            public ReplicationReactor() : base("ReplicationActor")
            {
                State.Data = "NEW-DATA";
            }

            protected override void OnEvent(IReactorEvent ev)
            {
                switch(ev)
                {
                    case ChangeStateEvent csev:
                        State.Data = csev.NewData;
                        break;
                    default:
                        throw new Exception("Invalid event");
                }
            }

            protected override void Replicate(ReplicationReactorReplState replState)
            {
                replState.Data = State.Data;
            }
        }

        class RequestState : IReactorEvent
        {
        }


        class RequestStateCoroutine : ReactorCoroutine<RequestReplicatedStateReactorState>
        {
            protected override IEnumerator<IWaitObject> Execute()
            {
                var replStateWait = GetReplicatedState<ReplicationReactorReplState>(State.DstReference);
                yield return replStateWait;

                State.Data = replStateWait.Response.Data;            
            }
        }

        class RequestReplicatedStateReactorState
        {
            public string Data { get; set; }
            public IReactorReference DstReference { get; set; }
        }

        class RequestReplicatedStateReactor : Reactor<RequestReplicatedStateReactorState>
        {
            

            public RequestReplicatedStateReactor(IReactorReference dstReference)
                : base("RequestState")
            {
                State.DstReference = dstReference;
            }

            protected override void OnEvent(IReactorEvent ev)
            {
                switch(ev)
                {
                    case RequestState rs:
                        Execute(new RequestStateCoroutine());
                        break;
                    default:
                        throw new Exception("Invalid event");
                }
                    
            }
        }

        [Fact]
        public void Replicate_Simple()
        {
            var responseReactor = new ReplicationReactor();
            var requestReactor = new RequestReplicatedStateReactor(responseReactor);
            requestReactor.Enqueue(null, new RequestState());

            requestReactor.Update(0);
            responseReactor.Update(0);
            requestReactor.Update(0);

            Assert.Equal("NEW-DATA", requestReactor.State.Data);
        }

        [Fact]
        public void Replicate_ChangeState()
        {
            var responseReactor = new ReplicationReactor();
            var requestReactor = new RequestReplicatedStateReactor(responseReactor);
            requestReactor.Enqueue(null, new RequestState());

            requestReactor.Update(0);
            responseReactor.Update(0);
            requestReactor.Update(0);

            Assert.Equal("NEW-DATA", requestReactor.State.Data);

            responseReactor.Enqueue(null, new ChangeStateEvent() { NewData = "1" });
            requestReactor.Enqueue(null, new RequestState());

            requestReactor.Update(0); //< Send event
            responseReactor.Update(0); //< Process change state
            responseReactor.Update(0); //< Process repl request
            Assert.Equal("NEW-DATA", requestReactor.State.Data);
            requestReactor.Update(0); //<
            Assert.Equal("1", requestReactor.State.Data);
        }

    }
}
