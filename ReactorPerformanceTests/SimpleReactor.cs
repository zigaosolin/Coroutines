namespace Reactors.Performance
{
    public class SimpleEvent : IReactorEvent
    {
    }

    class SimpleReactorState
    {
        public long EventsProcessed { get; set; } = 0;
    }

    class SimpleReactor : Reactor<SimpleReactorState>
    {
        protected override void OnEvent(IReactorEvent ev)
        {
            switch(ev)
            {
                case SimpleEvent sev:
                    State.EventsProcessed++;
                    break;

            }
        }
    }
}
