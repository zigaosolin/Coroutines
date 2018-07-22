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
        public string PrimaryLanguage { get; set; } = "en";
        public List<IReactorReference> ChatRooms { get; set; } = new List<IReactorReference>();
    }

    class Player : Reactor<PlayerState>
    {
        public Player(string uniqueIdentifier)
            : base(typeof(Player).FullName + ":" + uniqueIdentifier)
        {
        }

        protected override void OnEvent(IReactorEvent ev)
        {
            throw new NotImplementedException();
        }
    }
}
