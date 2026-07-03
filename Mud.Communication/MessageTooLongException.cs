namespace Mud.Communication
{
    using System;

    public class MessageTooLongException : Exception
    {
        public MessageTooLongException(int maxMessageLength)
            : base($"Command exceeds the maximum allowed length of {maxMessageLength} characters.")
        {
        }
    }
}
