using System;
using System.Collections.Generic;
using System.Text;
using Coroutines;

namespace Reactors.Performance
{
    class CoroutineResponseReactorState
    {
        public long EventsProcessed { get; set; } = 0;
    }

    class CoroutineResponseReactor : Reactor<CoroutineResponseReactorState>
    {

        class ResponseCoroutine : ReactorCoroutine<CoroutineResponseReactorState>
        {
            protected override IEnumerator<IWaitObject> Execute()
            {
                yield return null;
                State.EventsProcessed++;
            }
        }

        protected override void OnEvent(IReactorEvent ev)
        {
            switch (ev)
            {
                case SimpleEvent sev:
                    Execute(new ResponseCoroutine());
                    break;

            }
        }
    }
}
