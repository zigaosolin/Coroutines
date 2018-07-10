using System;
using System.Collections.Generic;
using System.Text;
using Coroutines;

namespace Reactors.Performance
{


    public class CoroutineResponseReactor : Reactor
    {
        public long EventsProcessed { get; private set; } = 0;

        class ResponseCoroutine : ReactorCoroutine<CoroutineResponseReactor>
        {
            protected override IEnumerator<IWaitObject> Execute()
            {
                yield return null;
                Reactor.EventsProcessed++;
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
