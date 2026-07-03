namespace Mud.Communication
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Accumulates raw bytes from the socket and splits them into complete
    /// line-terminated commands. Handles CRLF/LF/CR line endings, multiple
    /// commands per packet, and partial commands across packets. Non-printable
    /// bytes (including telnet negotiation) are ignored.
    /// </summary>
    public sealed class MessageBuffer
    {
        public const int DefaultMaxMessageLength = 512;

        private static readonly IReadOnlyList<string> NoMessages = Array.Empty<string>();

        private readonly int maxMessageLength;

        private readonly StringBuilder current = new StringBuilder();

        private bool skipNextLineFeed;

        public MessageBuffer(int maxMessageLength = DefaultMaxMessageLength)
        {
            if (maxMessageLength <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxMessageLength));
            }

            this.maxMessageLength = maxMessageLength;
        }

        /// <summary>
        /// Appends received bytes and returns all commands completed by them.
        /// Throws <see cref="MessageTooLongException"/> when a single command
        /// exceeds the configured maximum length.
        /// </summary>
        public IReadOnlyList<string> Append(byte[] data, int count)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (count < 0 || count > data.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            List<string> messages = null;

            for (int i = 0; i < count; i++)
            {
                var c = (char)data[i];

                if (c == '\n')
                {
                    if (this.skipNextLineFeed)
                    {
                        // Second half of a CRLF pair; the CR already completed the line.
                        this.skipNextLineFeed = false;
                        continue;
                    }

                    this.CompleteLine(ref messages);
                }
                else if (c == '\r')
                {
                    this.skipNextLineFeed = true;
                    this.CompleteLine(ref messages);
                }
                else
                {
                    this.skipNextLineFeed = false;

                    if (c < ' ' || c > '~')
                    {
                        continue;
                    }

                    if (this.current.Length >= this.maxMessageLength)
                    {
                        this.current.Clear();
                        throw new MessageTooLongException(this.maxMessageLength);
                    }

                    this.current.Append(c);
                }
            }

            return messages ?? NoMessages;
        }

        private void CompleteLine(ref List<string> messages)
        {
            messages = messages ?? new List<string>();
            messages.Add(this.current.ToString());
            this.current.Clear();
        }
    }
}
