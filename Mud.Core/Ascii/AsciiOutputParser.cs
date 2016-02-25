namespace Mud.Core.Ascii
{
    using Common.Output;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    public class AsciiOutputParser : IOutputParser
    {
        private const string Esc = "\x1B";

        private static readonly Dictionary<char, string> Colors = new Dictionary<char, string>();

        static AsciiOutputParser()
        {
            Colors.Add('R', GetSequence(AnsiForegroundColor.Red, 1));
            Colors.Add('r', GetSequence(AnsiForegroundColor.Red, 0));
            Colors.Add('w', GetSequence(AnsiForegroundColor.White, 1));
            Colors.Add('W', GetSequence(AnsiForegroundColor.White, 0));
            Colors.Add('Y', GetSequence(AnsiForegroundColor.Yellow, 1));
            Colors.Add('y', GetSequence(AnsiForegroundColor.Yellow, 0));
            Colors.Add('G', GetSequence(AnsiForegroundColor.Green, 1));
            Colors.Add('g', GetSequence(AnsiForegroundColor.Green, 0));
        }

        public string Parse(string message)
        {
            var sb = new StringBuilder();
            var sr = new StringReader(message);
            var buf = new char[1];
            while (sr.Read(buf, 0, 1) > 0)
            {
                if (buf[0] == '&')
                {
                    if (sr.Read(buf, 0, 1) > 0)
                    {
                        if (buf[0] == '&')
                        {
                            sb.Append('&');
                        }
                        else
                        {
                            sb.Append(this.GetColor(buf[0]));
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    sb.Append(buf[0]);
                }
            }

            return sb.ToString();
        }

        private static string GetSequence(AnsiForegroundColor color, int bright)
        {
            return string.Format("{0}[{2}m{0}[{1}m", Esc, (int)color, bright);
        }

        private string GetColor(char input)
        {
            if (Colors.ContainsKey(input))
            {
                return Colors[input];
            }

            return GetSequence(AnsiForegroundColor.Default, 1);
        }
    }
}
