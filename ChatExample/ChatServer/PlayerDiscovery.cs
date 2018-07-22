using Reactors;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

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
                case CreatePlayer createPlayer:
                    OnCreatePlayer(createPlayer);
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

        static Regex usernameRegex = new Regex("[a-zA-Z]*");
        static Regex passwordRegex = new Regex("[a-zA-Z0-9]*");

        private void OnCreatePlayer(CreatePlayer ev)
        {
            if (string.IsNullOrEmpty(ev.Username) || ev.Username.Length < 5 || !usernameRegex.IsMatch(ev.Username))
                Reply(new CreatePlayerResponse("Username must be at least 5 characters and consist only of US characters"));

            if (string.IsNullOrEmpty(ev.Password) || ev.Password.Length < 5 || !passwordRegex.IsMatch(ev.Password))
                Reply(new CreatePlayerResponse("Username must be at least 5 characters and consist only of US characters and numbers"));

            // May throw, as we are not in critical section it is ok
            if (State.UsernameToPlayer.TryGetValue(ev.Username, out Tuple<string, IReactorReference> result))
            {
                Reply(new CreatePlayerResponse("Username already exists");
                return;
            }

            EnterCriticalSection();

            var player = new Player(typeof(Player).Name + ":" + ev.Username /*TODO: init state*/);
            // Need to add it somewhere


        }
    }
}
