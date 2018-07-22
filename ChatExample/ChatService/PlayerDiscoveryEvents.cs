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
            Password = password;
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
        public string Username { get; }
        public string FirstName { get; }
        public string LastName { get; }
        public string Password { get; }

        public CreatePlayer(string username, string firstName, string lastName, string password)
        {
            Username = username;
            FirstName = firstName;
            LastName = lastName;
            Password = password;
        }
    }

    public sealed class CreatePlayerResponse : IReactorEvent
    {
        public bool Succesfull { get; } = true;
        public IReactorReference PlayerReactor { get; }
        public string ErrorMessage { get; }

        public CreatePlayerResponse(IReactorReference playerReactor)
        {
            PlayerReactor = playerReactor;
        }

        public CreatePlayerResponse(string errorMessage)
        {
            Succesfull = false;
            ErrorMessage = errorMessage;
        }
    }

    
}
