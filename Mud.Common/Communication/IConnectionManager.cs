namespace Mud.Common.Communication
{
    using System;

    public interface IConnectionManager
    {
        event Action<IConnection> UserConnected;

        event Action<IConnection> UserDisconnected;

        void Start();

        void Stop();
    }
}
