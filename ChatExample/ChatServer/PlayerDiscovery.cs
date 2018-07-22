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
                    throw new ChatException("Invalid event");
            }
        }

        private void OnPlayerLogin(PlayerLogin ev)
        {
            if(string.IsNullOrEmpty(ev.Username))
                throw new ChatException("Username is empty");
            
            if (string.IsNullOrEmpty(ev.Password))
                throw new ChatException("Password is empty");

            // May throw, as we are not in critical section it is ok
            if(!State.UsernameToPlayer.TryGetValue(ev.Username, out Tuple<string, IReactorReference> result))
            {
                Reply(new PlayerLoginResponse("Invalid username"));
                return;
            }

            if (result.Item1 != ev.Password)
            {
                Reply(new PlayerLoginResponse("Invalid password"));
                return;
            }

            EnterCriticalSection();

            Reply(new PlayerLoginResponse(result.Item2));
        }
    }
}
