namespace Mud.Communication
{
    using System;

    public class PortInUseException : Exception
    {
        public const string MessageFormat = "Error number {0}: {1}. Port number {2} is already in use.";

        public PortInUseException(string message)
            : base(message)
        {
        }
    }
}
