namespace Mud.Common.Communication
{
    using System;
    using System.Net;

    public interface IConnection
    {
        event Action<IConnection, string> MessageReceived;

        event Action<IConnection> Disconnected;

        IPAddress Ip { get; }

        Guid Id { get; }

        void Disconnect();

        void Send(string message);
    }
}
