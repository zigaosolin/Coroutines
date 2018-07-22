using Reactors;
using System;
using System.Collections.Generic;
using System.Text;

namespace Chat
{
    public sealed class PlayerLogin : IReactorEvent
    {
        public string Username { get; }
        public string Password { get; }

        public PlayerLogin(string username, string password)
        {
            Username = username;
            Password = Password;
        }
    }

    public sealed class PlayerLoginResponse : IReactorEvent
    {
        public bool Succesfull { get; } = true;
        public IReactorReference PlayerReactor { get; }
        public string ErrorMessage { get; }

        public PlayerLoginResponse(IReactorReference playerReactor)
        {
            PlayerReactor = playerReactor;
        }

        public PlayerLoginResponse(string errorMessage)
        {
            Succesfull = false;
            ErrorMessage = errorMessage;
        }
    }

    public sealed class CreatePlayer : IReactorEvent
    {
        public string UserName { get; }
        public string FirstName { get; }
        public string LastName { get; }
    }
}
