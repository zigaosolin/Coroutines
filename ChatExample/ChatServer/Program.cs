using Reactors;
using System;
using System.Threading;

namespace Chat.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            // Createa a global reactor repository
            ReactorRepository.CreateGlobal(2, useTasks: true);
            ReactorRepository.Global.Add(new PlayerDiscoveryReactor());

            // Reactor event sink is useful for handling events outside reactor (test cases)
            var source = new ReactorEventSink("");
            ReactorRepository.AddToGlobal(source);

            var discoveryReference = ReactorRepository.Global.Resolve<PlayerDiscoveryReactor>("");
            var sourceReference = ReactorRepository.Global.Resolve<ReactorEventSink>("");
            discoveryReference.Send(sourceReference,
                new CreatePlayer(
                    username: "nihum",
                    firstName: "Ziga",
                    lastName: "Osolin",
                    password: "mypassword")
            );

            Thread.Sleep(100);

            IReactorEvent ev = source.Dequeue();
            switch (ev)
            {
                case CreatePlayerResponse resp:
                    Console.WriteLine("Created player " + resp.PlayerReactor.Reference);
                    break;
                default:
                    Console.WriteLine("Error");
                    return;
            }

            // We do a login
            discoveryReference.Send(sourceReference,
                new PlayerLogin("nihum", "mypassword")
            );

            Thread.Sleep(100);

            ev = source.Dequeue();
            IReactorReference playerReference;
            switch (ev)
            {
                case PlayerLoginResponse resp:
                    Console.WriteLine("Login player " + resp.PlayerReactor.Reference);
                    playerReference = resp.PlayerReactor;
                    break;
                default:
                    Console.WriteLine("Error");
                    return;
            }

            playerReference.Send(sourceReference,
                new RenamePlayer("Ziga2", "Osolin", "Nihum")
            );

            Thread.Sleep(100);

            ev = source.Dequeue();
            switch (ev)
            {
                case OK resp:
                    Console.WriteLine("Renamed player");
                    break;
                default:
                    Console.WriteLine("Error");
                    return;
            }

            // We create one more player
            discoveryReference.Send(sourceReference,
                new CreatePlayer(
                    username: "otherplayer",
                    firstName: "Ziga2",
                    lastName: "Osolin",
                    password: "mypassword")
            );

            Thread.Sleep(100);

            ev = source.Dequeue();
            IReactorReference otherPlayerReference;
            switch (ev)
            {
                case CreatePlayerResponse resp:
                    Console.WriteLine("Created player " + resp.PlayerReactor.Reference);
                    otherPlayerReference = resp.PlayerReactor;
                    break;
                default:
                    Console.WriteLine("Error");
                    return;
            }

            playerReference.Send(sourceReference,
                new InvitePlayerToRoom(
                    otherPlayerReference,
                    null)
            );

            Thread.Sleep(100);

            ev = source.Dequeue();
            switch (ev)
            {
                case OK resp:
                    Console.WriteLine("Invited player");
                    break;
                default:
                    Console.WriteLine("Error");
                    return;
            }
        }
    }
}

