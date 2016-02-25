namespace Mud.Communication
{
    using System;
    using System.Text;

    public class MessageBuffer
    {
        public const string NewLineMarker = "\r";

        private readonly StringBuilder sb;

        public MessageBuffer()
        {
            this.sb = new StringBuilder();
        }

        public event Action<string> Message;

        /// <summary>
        /// Only 1 element array should be passed
        /// </summary>
        /// <param name="data">Data (1 element byte array)</param>
        public void Push(byte[] data)
        {
            string input = Encoding.ASCII.GetString(data);
            input = input.Replace("\n", NewLineMarker);
            this.sb.Append(input);

            this.CheckMessage();
        }

        private void CheckMessage()
        {
            var buff = this.sb.ToString();
            if (buff.Contains(NewLineMarker))
            {
                this.OnMessage(buff.Replace(NewLineMarker, string.Empty));
                this.sb.Clear();
            }
        }

        private void OnMessage(string message)
        {
            if (this.Message != null)
            {
                this.Message(message);
            }
        }
    }
}
