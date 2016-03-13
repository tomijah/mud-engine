namespace Mud.Server
{
    using System;

    using Communication.Tcp;
    using Core.Session;

    internal class Program
    {
        private static void Main(string[] args)
        {
            var connections = new TcpConnectionManager(4000);
            using (var sessionManager = new SessionManager(connections))
            {
                while (true)
                {
                    var message = Console.ReadLine();
                    if (message == "stop")
                    {
                        break;
                    }

                    sessionManager.Broadcast(message);
                }
            }
        }
    }
}