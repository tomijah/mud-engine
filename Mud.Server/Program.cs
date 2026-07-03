namespace Mud.Server
{
    using System;

    using Mud.Communication.Tcp;
    using Mud.Core;
    using Mud.Core.Commands;
    using Mud.Core.Game;
    using Mud.Core.Session;

    internal class Program
    {
        private const int Port = 4000;

        private static void Main(string[] args)
        {
            var world = DefaultWorld.Create();
            var commands = CommandDispatcher.CreateDefault();
            var context = new GameContext(world, commands);

            var connections = new TcpConnectionManager(Port);
            using (var sessionManager = new SessionManager(connections, context))
            {
                sessionManager.Start();
                Console.WriteLine($"MUD server listening on port {Port}. Type 'stop' to shut down; anything else is broadcast.");

                while (true)
                {
                    var message = Console.ReadLine();
                    if (message == null || message == "stop")
                    {
                        break;
                    }

                    if (message.Length > 0)
                    {
                        sessionManager.Broadcast($"&Y[Server] {message}");
                    }
                }
            }
        }
    }
}
