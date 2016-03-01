namespace Mud.Server
{
    using System;
    using System.Data;

    using Communication.Tcp;
    using Common.Communication;

    using Mud.Core.Ascii;
    using Mud.Core.Session;

    class Program
    {
        static void Main(string[] args)
        {
            var connections = new TcpConnectionManager();
            var sessionManager = new SessionManager(connections);
            connections.Start(4000);
            while (true)
            {
                var message = Console.ReadLine();
                if(message == "stop")
                {
                    break;
                }

                sessionManager.Broadcast(message + "\n");
            }
        }
    }
}
