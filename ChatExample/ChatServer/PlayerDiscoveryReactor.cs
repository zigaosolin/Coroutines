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

        public Dictionary<string, IReactorReference> PlayersByHandle
            = new Dictionary<string, IReactorReference>();
    }

    sealed class PlayerDiscoveryReactor : Reactor<PlayerDiscoveryState>
    {
        public PlayerDiscoveryReactor() 
            : base("")
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
                Reply(new CreatePlayerResponse("Username already exists"));
                return;
            }

            string handle = Guid.NewGuid().ToString();

            if(State.PlayersByHandle.TryGetValue(handle, out IReactorReference result2))
            {
                Reply(new CreatePlayerResponse("Player's handle already exists"));
                return;
            }

            EnterCriticalSection();

            // Need create player's reactor and add it to repo
            var playerReactor = new PlayerReactor(handle);
            Repository.Add(playerReactor);

            var reference = Repository.Resolve<PlayerReactor>(handle);

            State.UsernameToPlayer.Add(ev.Username, new Tuple<string, IReactorReference>(ev.Password, reference));
            State.PlayersByHandle.Add(handle, reference);

            Reply(new CreatePlayerResponse(reference));

        }
    }
}
