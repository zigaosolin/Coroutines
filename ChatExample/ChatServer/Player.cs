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
        public Player()
            : base("Player")
        {
        }

        protected override void OnEvent(IReactorEvent ev)
        {
            throw new NotImplementedException();
        }
    }
}
