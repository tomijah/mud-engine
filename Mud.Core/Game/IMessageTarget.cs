namespace Mud.Core.Game
{
    /// <summary>
    /// Something that can receive game output (implemented by sessions).
    /// Implementations must be thread-safe and non-blocking, because the world
    /// delivers messages while holding its internal lock.
    /// </summary>
    public interface IMessageTarget
    {
        void SendMessage(string message);
    }
}
