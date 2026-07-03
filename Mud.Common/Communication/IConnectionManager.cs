namespace Mud.Common.Communication
{
    using System;

    public interface IConnectionManager
    {
        event Action<IConnection> ConnectionAccepted;

        void Start();

        void Stop();
    }
}
