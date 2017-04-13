using Microsoft.Bot.Connector;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using System.Xml.Linq;

namespace Shipwreck.SlackCSharpBot.Controllers
{
    internal static class StringBuilderHelper
    {
        public const string NEW_LINE = "\n\n";
        public const string SUCCESS = ":ok_hand:";
        public const string ERROR = ":exclamation:";
        public const string WARNING = ":warning:";

        public static StringBuilder NewLine(this StringBuilder sb) => sb.Append(NEW_LINE);

        public static StringBuilder Success(this StringBuilder sb) => sb.Append(":ok_hand:");

        public static StringBuilder Error(this StringBuilder sb) => sb.Append(":exclamation:");

        public static StringBuilder Warning(this StringBuilder sb) => sb.Append(":warning:");

        public static StringBuilder List(this StringBuilder sb, IEnumerable<object> values)
        {
            foreach (var v in values)
            {
                sb.Append("* ").Append(v).NewLine();
            }
            return sb;
        }

        public static StringBuilder AppendType(this StringBuilder sb, Type t, IEnumerable<string> ns)
        {
            if (t == typeof(object))
            {
                return sb.Append("object");
            }

            if (t == typeof(bool))
            {
                return sb.Append("bool");
            }
            if (t == typeof(byte))
            {
                return sb.Append("byte");
            }
            if (t == typeof(sbyte))
            {
                return sb.Append("sbyte");
            }
            if (t == typeof(short))
            {
                return sb.Append("short");
            }
            if (t == typeof(ushort))
            {
                return sb.Append("ushort");
            }
            if (t == typeof(int))
            {
                return sb.Append("int");
            }
            if (t == typeof(uint))
            {
                return sb.Append("uint");
            }
            if (t == typeof(long))
            {
                return sb.Append("long");
            }
            if (t == typeof(ulong))
            {
                return sb.Append("ulong");
            }

            if (t == typeof(float))
            {
                return sb.Append("float");
            }
            if (t == typeof(double))
            {
                return sb.Append("double");
            }
            if (t == typeof(decimal))
            {
                return sb.Append("decimal");
            }

            if (t == typeof(char))
            {
                return sb.Append("char");
            }
            if (t == typeof(string))
            {
                return sb.Append("string");
            }

            if (t.IsGenericType)
            {
                if (t.IsValueType && t.IsConstructedGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    return sb.AppendType(t.GetGenericArguments()[0], ns).Append('?');
                }

                if (!ns.Contains(t.Namespace))
                {
                    sb.Append(t.Namespace).Append('.');
                }
                sb.Append(t.Name.Split('`').FirstOrDefault());
                sb.Append('<');
                foreach (var p in t.GetGenericArguments())
                {
                    sb.AppendType(p, ns);
                    sb.Append(',');
                }
                sb.Length--;

                sb.Append('>');

                return sb;
            }
            if (t.IsArray)
            {
                var i = t.GetArrayRank();
                return sb.AppendType(t.UnderlyingSystemType, ns).Append('[').Append(',', i - 1).Append(']');
            }
            if (t.IsGenericParameter)
            {
                return sb.Append(t.Name);
            }

            if (!ns.Contains(t.Namespace))
            {
                sb.Append(t.Namespace).Append('.');
            }
            return sb.Append(t.Name);
        }

        public static StringBuilder AppendNiceString(this StringBuilder sb, object v, IReadOnlyCollection<string> ns, bool explicitType)
        {
            if (v == null)
            {
                sb.Append("null");
            }
            else if (v is string)
            {
                AppendString(sb, (string)v);
            }
            else if (v is byte || v is sbyte || v is short || v is ushort)
            {
                if (explicitType)
                {
                    sb.Append("(").AppendType(v.GetType(), ns).Append(")");
                }
                sb.Append(v);
            }
            else if (v is int)
            {
                sb.Append(v);
            }
            else if (v is uint)
            {
                sb.Append(v).Append('u');
            }
            else if (v is long)
            {
                sb.Append(v).Append('L');
            }
            else if (v is ulong)
            {
                sb.Append(v).Append("ul");
            }
            else if (v is float)
            {
                sb.Append(v).Append('f');
            }
            else if (v is double)
            {
                sb.Append(v).Append('d');
            }
            else if (v is decimal)
            {
                sb.Append(v).Append('m');
            }
            else if (v is char)
            {
                sb.Append('\'').AppendCharCore((char)v).Append('\'');
            }
            else if (v is Enum)
            {
                sb.AppendType(v.GetType(), ns).Append('.').Append(v);
            }
            else if (v is IDictionary)
            {
                var de = ((IDictionary)v).GetEnumerator();
                sb.AppendType(v.GetType(), ns);
                sb.Append(" {");
                var l = sb.Length;
                while (de.MoveNext())
                {
                    sb.Append("{ ").AppendNiceString(de.Key, ns, false).Append(", ").AppendNiceString(de.Value, ns, false).Append("}, ");
                }
                sb.Length = Math.Max(sb.Length - 2, l);
                sb.Append(" {");

                (de as IDisposable).Dispose();
            }
            else if (v is IEnumerable)
            {
                sb.AppendType(v.GetType(), ns);
                sb.Append(" {");
                var l = sb.Length;
                foreach (var c in ((IEnumerable)v).Cast<object>())
                {
                    AppendNiceString(sb, c, ns, false);
                    sb.Append(", ");
                }
                sb.Length = Math.Max(sb.Length - 2, l);
                sb.Append("}");
            }
            else
            {
                sb.Append(v);

                if (explicitType)
                {
                    sb.Append(" {").AppendType(v.GetType(), ns).Append("}");
                }
            }
            return sb;
        }

        private static void AppendString(this StringBuilder sb, string v)
        {
            sb.Append('"');
            foreach (var c in v)
            {
                AppendCharCore(sb, c);
            }
            sb.Append('"');
        }

        private static StringBuilder AppendCharCore(this StringBuilder sb, char c)
        {
            switch (c)
            {
                case '\n':
                    return sb.Append("\\n");

                case '\r':
                    return sb.Append("\\r");

                case '\t':
                    return sb.Append("\\t");

                case '"':
                    return sb.Append("\\\"");

                default:
                    return sb.Append(c);
            }
        }

        public static string ToMarkup(this object obj, IReadOnlyCollection<string> namespaces, bool blockQuote)
        {
            var sb = new StringBuilder();
            if (blockQuote)
            {
                sb.Append("```\n");
            }
            sb.AppendNiceString(obj, namespaces, true);
            if (blockQuote)
            {
                sb.Append("```\n");
            }
            return sb.ToString();
        }

        public static string GetSourceCode(this Script st)
        {
            if (st == null)
            {
                return null;
            }
            var sb = new StringBuilder();

            if (st != null)
            {
                var stack = new Stack<Script>();
                while (st != null)
                {
                    stack.Push(st);
                    st = st.Previous;
                }

                foreach (var s in stack)
                {
                    if (!string.IsNullOrWhiteSpace(s.Code))
                    {
                        sb.Append(s.Code);
                        if (sb[sb.Length - 1] == ';')
                        {
                            sb.AppendLine();
                        }
                        else
                        {
                            sb.AppendLine(";");
                        }
                        sb.AppendLine();
                    }
                }
            }
            return sb.ToString();
        }
    }
}