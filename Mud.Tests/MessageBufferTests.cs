namespace Mud.Tests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Mud.Communication;

    [TestClass]
    public class MessageBufferTests
    {
        [TestMethod]
        public void Push_EmitsMessageWhenNewLineReceived()
        {
            var buffer = new MessageBuffer();
            string received = null;

            buffer.Message += message => received = message;

            buffer.Push(new byte[] { (byte)'h' });
            buffer.Push(new byte[] { (byte)'i' });
            buffer.Push(new byte[] { (byte)'\n' });

            Assert.AreEqual("hi", received);
        }
    }
}
