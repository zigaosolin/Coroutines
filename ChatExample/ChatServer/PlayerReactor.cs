using Coroutines;
using Reactors;
using System;
using System.Collections.Generic;
using System.Text;

namespace Chat.Server
{
    class PlayerState
    {
        public IReactorReference Identifier { get; set; } = null;
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Password { get; set; } = "";
        public string NickName { get; set; } = "";
        public string PrimaryLanguage { get; set; } = "en";
        public List<IReactorReference> ChatRooms { get; set; } = new List<IReactorReference>();
    }

    class PlayerInternalInvite : IReactorEvent
    {
    }

    class PlayerReactor : Reactor<PlayerState>
    {
        public PlayerReactor(string uniqueIdentifier)
            : base(uniqueIdentifier)
        {
        }

        protected override void OnEvent(IReactorEvent ev)
        {
            switch(ev)
            {
                case RenamePlayer rev:
                    OnRenamePlayer(rev);
                    break;
                case InvitePlayerToRoom iev:
                    Execute(new InviteCoroutine());
                    break;
                case PlayerInternalInvite iiv:
                    Reply(new OK());
                    break;
                default:
                    throw new Exception("Invalid event");
            }
        }

        private void OnRenamePlayer(RenamePlayer ev)
        {
            if(string.IsNullOrEmpty(ev.NewFirstName))
            {
                Reply(new Error("Invalid first name"));
                return;
            }

            if (string.IsNullOrEmpty(ev.NewLastName))
            {
                Reply(new Error("Invalid last name"));
                return;
            }

            if (string.IsNullOrEmpty(ev.NewNickName))
            {
                Reply(new Error("Invalid nick name"));
                return;
            }

            EnterCriticalSection();

            State.FirstName = ev.NewFirstName;
            State.LastName = ev.NewLastName;
            State.NickName = ev.NewNickName;

            Reply(new OK());
        }

        class InviteCoroutine : ReactorCoroutine<InvitePlayerToRoom, PlayerState>
        { 
            protected override IEnumerator<IWaitObject> Execute()
            {
                var wait = RPC(Event.OtherPlayer, new PlayerInternalInvite());
                yield return wait;
                if(wait.Response is OK)
                {
                    Reply(new OK());
                } else
                {
                    Reply(new Error("Failed to invite player"));
                }
            }
        }
    }
}
