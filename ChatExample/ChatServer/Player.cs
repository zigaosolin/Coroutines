using Reactors;
using System;
using System.Collections.Generic;
using System.Text;

namespace Chat.Server
{
    class PlayerState
    {
        public string Name { get; set; }
    }

    class Player : Reactor<PlayerState>
    {
        protected override void OnEvent(IReactorEvent ev)
        {
            throw new NotImplementedException();
        }
    }
}
