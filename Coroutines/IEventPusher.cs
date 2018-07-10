using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Coroutines
{
    [JsonObject(MemberSerialization.Fields)]
    public interface IEvent
    {
    }

    public interface IEventPusher
    {
        void Enqueue(IEvent ev);
        void EnqueueNextFrame(IEvent ev);
    }
}
