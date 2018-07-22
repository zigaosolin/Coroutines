using Reactors;
using System;
using System.Collections.Generic;
using System.Text;

namespace Chat
{
    public class RenamePlayer : IReactorEvent
    {
        public string NewFirstName { get; }
        public string NewLastName { get; }
        public string NewNickName { get; }

        public RenamePlayer(string firstName, string lastName, string nickName)
        {
            NewFirstName = firstName;
            NewLastName = lastName;
            NewNickName = nickName;
        }
    }

    public class InvitePlayerToRoom : IReactorEvent
    {
        public IReactorReference OtherPlayer { get; }
        public IReactorReference Room { get; }

        public InvitePlayerToRoom(IReactorReference otherPlayer, IReactorReference room)
        {
            OtherPlayer = otherPlayer;
            Room = room;
        }
    }
}
