namespace Mud.Tests
{
    using System.Linq;
    using System.Text;

    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Mud.Communication;

    [TestClass]
    public class MessageBufferTests
    {
        [TestMethod]
        public void Append_EmitsMessageWhenNewLineReceived()
        {
            var buffer = new MessageBuffer();

            Assert.AreEqual(0, Push(buffer, "h").Count);
            Assert.AreEqual(0, Push(buffer, "i").Count);

            var messages = Push(buffer, "\n");

            Assert.AreEqual(1, messages.Count);
            Assert.AreEqual("hi", messages[0]);
        }

        [TestMethod]
        public void Append_SupportsMultipleCommandsInOnePacket()
        {
            var buffer = new MessageBuffer();

            var messages = Push(buffer, "look\r\nsay hello\r\nnorth\r\n");

            CollectionAssert.AreEqual(new[] { "look", "say hello", "north" }, messages.ToArray());
        }

        [TestMethod]
        public void Append_HandlesCommandSplitAcrossPackets()
        {
            var buffer = new MessageBuffer();

            Assert.AreEqual(0, Push(buffer, "say hel").Count);
            var messages = Push(buffer, "lo\r\nwho");

            Assert.AreEqual(1, messages.Count);
            Assert.AreEqual("say hello", messages[0]);

            messages = Push(buffer, "\r\n");
            Assert.AreEqual(1, messages.Count);
            Assert.AreEqual("who", messages[0]);
        }

        [TestMethod]
        public void Append_TreatsCrLfAsSingleLineEnding()
        {
            var buffer = new MessageBuffer();

            var messages = Push(buffer, "hi\r\n");

            Assert.AreEqual(1, messages.Count);
            Assert.AreEqual("hi", messages[0]);
        }

        [TestMethod]
        public void Append_HandlesCrLfSplitAcrossPackets()
        {
            var buffer = new MessageBuffer();

            var messages = Push(buffer, "hi\r");
            Assert.AreEqual(1, messages.Count);

            // The \n completing the CRLF pair must not produce an empty message.
            Assert.AreEqual(0, Push(buffer, "\n").Count);
        }

        [TestMethod]
        public void Append_EmitsEmptyMessageForBlankLine()
        {
            var buffer = new MessageBuffer();

            var messages = Push(buffer, "\r\n\r\n");

            Assert.AreEqual(2, messages.Count);
            Assert.AreEqual(string.Empty, messages[0]);
            Assert.AreEqual(string.Empty, messages[1]);
        }

        [TestMethod]
        public void Append_IgnoresNonPrintableBytes()
        {
            var buffer = new MessageBuffer();

            // Telnet IAC WILL ECHO negotiation bytes followed by a command.
            var data = new byte[] { 255, 251, 1, (byte)'h', (byte)'i', (byte)'\r', (byte)'\n' };
            var messages = buffer.Append(data, data.Length);

            Assert.AreEqual(1, messages.Count);
            Assert.AreEqual("hi", messages[0]);
        }

        [TestMethod]
        public void Append_ThrowsWhenCommandExceedsMaxLength()
        {
            var buffer = new MessageBuffer(maxMessageLength: 8);

            Assert.ThrowsExactly<MessageTooLongException>(() => Push(buffer, "123456789"));
        }

        [TestMethod]
        public void Append_RecoversAfterOversizedCommand()
        {
            var buffer = new MessageBuffer(maxMessageLength: 8);

            Assert.ThrowsExactly<MessageTooLongException>(() => Push(buffer, "123456789"));

            var messages = Push(buffer, "ok\r\n");
            Assert.AreEqual(1, messages.Count);
            Assert.AreEqual("ok", messages[0]);
        }

        private static System.Collections.Generic.IReadOnlyList<string> Push(MessageBuffer buffer, string text)
        {
            var data = Encoding.ASCII.GetBytes(text);
            return buffer.Append(data, data.Length);
        }
    }
}
