namespace Mud.Server
{
    using System;
    using Communication.Tcp;
    using Common.Communication;

    class Program
    {
        static void Main(string[] args)
        {
            var connections = new TcpConnectionManager();
            connections.Start(4000);
            connections.UserConnected += OnUserConnected;

            while (true)
            {
                var message = Console.ReadLine();
                if(message == "stop")
                {
                    break;
                }

                connections.Broadcast(message);
            }
        }

        private static void MessageReceived(IConnection connection, string message)
        {
            Console.WriteLine(message);
        }

        private static void OnUserConnected(IConnection connection)
        {
            connection.MessageReceived += MessageReceived;
        }
    }
}
