using Reactors;
using System;
using System.Collections.Generic;
using System.Text;

namespace Chat.Server
{
    class PlayerDiscoveryState
    {
        public Dictionary<string, Tuple<string, IReactorReference>> UsernameToPlayer 
            = new Dictionary<string, Tuple<string, IReactorReference>>();
    }

    sealed class PlayerDiscovery : Reactor<PlayerDiscoveryState>
    {
        public PlayerDiscovery() 
            : base(typeof(PlayerDiscovery).Name)
        {
        }

        protected override void OnEvent(IReactorEvent ev)
        {
            switch(ev)
            {
                case PlayerLogin loginEv:
                    OnPlayerLogin(loginEv);
                    break;
                default:
                    throw new ReactorException("Invalid event");
            }
        }

        private void OnPlayerLogin(PlayerLogin ev)
        {
            
        }
    }
}
