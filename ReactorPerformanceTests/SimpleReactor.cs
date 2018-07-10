namespace Reactors.Performance
{
    public class SimpleEvent : IReactorEvent
    {
    }

    public class SimpleReactor : Reactor
    {
        public long EventsProcessed { get; private set; } = 0;

        protected override void OnEvent(IReactorEvent ev)
        {
            switch(ev)
            {
                case SimpleEvent sev:
                    EventsProcessed++;
                    break;

            }
        }
    }
}
