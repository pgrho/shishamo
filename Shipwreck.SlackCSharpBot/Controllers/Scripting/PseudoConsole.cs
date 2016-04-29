using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace Shipwreck.SlackCSharpBot.Controllers.Scripting
{
    public sealed class PseudoConsole
    {
        private StringBuilder _Buffer;

        private StringBuilder Buffer
            => _Buffer ?? (_Buffer = new StringBuilder());

        public string Result => _Buffer?.ToString();

        public void Clear() => _Buffer?.Clear();

        public void Write(long value) => Buffer.Append(value);

        public void Write(string value) => Buffer.Append(value);

        public void Write(ulong value) => Buffer.Append(value);

        public void Write(uint value) => Buffer.Append(value);

        public void Write(object value) => Buffer.Append(value);

        public void Write(float value) => Buffer.Append(value);

        public void Write(decimal value) => Buffer.Append(value);

        public void Write(double value) => Buffer.Append(value);

        public void Write(char[] buffer) => Buffer.Append(buffer);

        public void Write(char value) => Buffer.Append(value);

        public void Write(bool value) => Buffer.Append(value);

        public void Write(int value) => Buffer.Append(value);

        public void Write(string format, object arg0) => Buffer.Append(string.Format(format, arg0));

        public void Write(string format, params object[] arg) => Buffer.AppendFormat(format, arg);

        public void Write(string format, object arg0, object arg1) => Buffer.Append(string.Format(format, arg0, arg1));

        public void Write(char[] buffer, int index, int count) => Buffer.Append(buffer, index, count);

        public void Write(string format, object arg0, object arg1, object arg2) => Buffer.Append(string.Format(format, arg0, arg1, arg2));

        public void Write(string format, object arg0, object arg1, object arg2, object arg3) => Buffer.Append(string.Format(format, arg0, arg1, arg2, arg3));

        public void WriteLine() => Buffer.NewLine();

        public void WriteLine(bool value) => Buffer.Append(value).NewLine();

        public void WriteLine(float value) => Buffer.Append(value).NewLine();

        public void WriteLine(int value) => Buffer.Append(value).NewLine();

        public void WriteLine(uint value) => Buffer.Append(value).NewLine();

        public void WriteLine(long value) => Buffer.Append(value).NewLine();

        public void WriteLine(ulong value) => Buffer.Append(value).NewLine();

        public void WriteLine(object value) => Buffer.Append(value).NewLine();

        public void WriteLine(string value) => Buffer.Append(value).NewLine();

        public void WriteLine(double value) => Buffer.Append(value).NewLine();

        public void WriteLine(decimal value) => Buffer.Append(value).NewLine();

        public void WriteLine(char[] buffer) => Buffer.Append(buffer).NewLine();

        public void WriteLine(char value) => Buffer.Append(value).NewLine();

        public void WriteLine(string format, object arg0) => Buffer.Append(string.Format(format, arg0)).NewLine();

        public void WriteLine(string format, params object[] arg) => Buffer.AppendFormat(format, arg).NewLine();

        public void WriteLine(string format, object arg0, object arg1) => Buffer.Append(string.Format(format, arg0, arg1)).NewLine();

        public void WriteLine(char[] buffer, int index, int count) => Buffer.Append(buffer, index, count).NewLine();

        public void WriteLine(string format, object arg0, object arg1, object arg2) => Buffer.Append(string.Format(format, arg0, arg1, arg2)).NewLine();

        public void WriteLine(string format, object arg0, object arg1, object arg2, object arg3) => Buffer.Append(string.Format(format, arg0, arg1, arg2, arg3)).NewLine();
    }
}