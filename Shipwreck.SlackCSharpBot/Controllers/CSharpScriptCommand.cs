using Microsoft.Bot.Connector;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using System.Xml.Linq;

namespace Shipwreck.SlackCSharpBot
{
    public sealed class CSharpScriptCommand : RegexMessageCommand
    {
        private class ExecutionContext
        {
            private static readonly Regex RESET = new Regex(@"^\s*#reset(\s+|$)", RegexOptions.IgnoreCase);
            private static readonly Regex CONTEXT = new Regex(@"^\s*#context(\s+|$)", RegexOptions.IgnoreCase);
            private static readonly Regex RAW = new Regex(@"^\s*#raw(output)?(\s+|$)", RegexOptions.IgnoreCase);
            private static readonly Regex SOURCE = new Regex(@"^\s*#(source|code)?(\s+|$)", RegexOptions.IgnoreCase);
            private static readonly Regex SCOPE = new Regex(@"^\s*#(scope|variables?)(\s+|$)", RegexOptions.IgnoreCase);
            private static readonly Regex USING = new Regex(@"^\s*#using\s+(?<ns>([A-Z_][A-Z0-9_]*\s*\.\s*)*[A-Z_][A-Z0-9_]*)\s*;", RegexOptions.IgnoreCase);

            private readonly CSharpScriptCommand _Command;
            private readonly Message _Message;
            private readonly StringBuilder _Result;
            private string _Code;

            public ExecutionContext(CSharpScriptCommand command, Message message, string text)
            {
                _Result = new StringBuilder();
                _Command = command;
                _Message = message;
                _Code = HttpUtility.HtmlDecode(text);
            }

            public async Task<Message> ExecuteAsync()
            {
                var printSettings = HandleReset();
                printSettings |= HandleContext();

                var raw = HandleRaw();

                printSettings |= HandleUsing();
                HandleCode();
                HandleScope();

                // 設定の表示
                if (printSettings)
                {
                    PrintNamespaces();
                }

                // コードの評価
                if (!string.IsNullOrWhiteSpace(_Code))
                {
                    try
                    {
                        var state = _Command._State;
                        if (state == null)
                        {
                            // 初回
                            state = await CSharpScript.RunAsync(_Code, _Command.GetScriptOptions());
                        }
                        else
                        {
                            if (printSettings)
                            {
                                // NS追加
                                state = await state.ContinueWithAsync(_Code, _Command.GetScriptOptions());
                            }
                            else
                            {
                                // 二回目以降
                                state = await state.ContinueWithAsync(_Code);
                            }
                        }
                        _Command._State = state;

                        if (raw)
                        {
                            _Result.Append(state.ReturnValue);
                        }
                        else
                        {
                            _Result.AppendLine("```");

                            AppendNiceString(_Result, state.ReturnValue);
                            _Result.AppendLine();

                            _Result.AppendLine("```");
                        }
                    }
                    catch (Exception ex)
                    {
                        _Result.Append("> ").AppendLine(ex.Message);
                    }
                }
                else
                {
                    var state = _Command._State; ;
                    if (state == null)
                    {

                    }
                }

                return _Message.CreateReplyMessage(_Result.ToString());
            }

            private void PrintNamespaces()
            {
                lock (_Command._Assemblies)
                {
                    _Result.AppendLine("# Namespaces #");
                    foreach (var a in _Command._Namespaces)
                    {
                        _Result.Append(" - ").AppendLine(a);
                    }
                    _Result.AppendLine();
                }
            }

            #region ディレクティブ

            private bool HandleReset()
            {
                var m = RESET.Match(_Code);

                if (m.Success)
                {
                    _Code = _Code.Substring(m.Length);
                    _Command.ResetState();
                    return true;
                }

                return false;
            }

            private bool HandleContext()
            {
                var m = CONTEXT.Match(_Code);

                if (m.Success)
                {
                    _Code = _Code.Substring(m.Length);
                    return true;
                }

                return false;
            }

            private bool HandleRaw()
            {
                var raw = false;
                for (var m = RAW.Match(_Code); m.Success; m = RAW.Match(_Code))
                {
                    raw = true;
                    _Code = _Code.Substring(m.Length);
                }

                return raw;
            }

            private bool HandleUsing()
            {
                List<string> newNs = null;
                for (var m = USING.Match(_Code); m.Success; m = USING.Match(_Code))
                {
                    (newNs ?? (newNs = new List<string>())).Add(m.Groups["ns"].Value);
                    _Code = _Code.Substring(m.Length);
                }

                if (newNs != null)
                {
                    lock (_Command._Assemblies)
                    {
                        foreach (var ns in newNs)
                        {
                            if (!_Command._Namespaces.Contains(ns))
                            {
                                _Command._Namespaces.Add(ns);
                            }
                        }
                    }
                }

                return newNs != null;
            }

            private void HandleCode()
            {
                var m = SOURCE.Match(_Code);
                if (m.Success)
                {
                    _Code = _Code.Substring(m.Length);

                    var st = _Command._State?.Script;

                    if (st != null)
                    {
                        var stack = new Stack<Script>();
                        while (st != null)
                        {
                            stack.Push(st);
                            st = st.Previous;
                        }

                        _Result.AppendLine("```");
                        foreach (var s in stack)
                        {
                            if (!string.IsNullOrWhiteSpace(s.Code))
                            {
                                _Result.Append(s.Code);
                                if (_Result[_Result.Length - 1] == ';')
                                {
                                    _Result.AppendLine();
                                }
                                else
                                {
                                    _Result.AppendLine(";");
                                }
                                _Result.AppendLine();
                            }
                        }
                        _Result.AppendLine("```");
                    }
                }
            }

            private void HandleScope()
            {
                var m = SCOPE.Match(_Code);
                if (m.Success)
                {
                    _Code = _Code.Substring(m.Length);

                    var st = _Command._State;

                    if (st != null)
                    {
                        _Result.AppendLine("# Variables #");
                        foreach (var a in st.Variables)
                        {
                            _Result.Append(" - ").Append(a.Type).Append(' ').Append(a.Name).Append(" = `");
                            AppendNiceString(_Result, a.Value);
                            _Result.AppendLine("`;");
                        }
                        _Result.AppendLine();
                    }
                }
            }

            #endregion ディレクティブ

            #region 文字列処理

            private static void AppendNiceString(StringBuilder sb, object v)
            {
                if (v == null)
                {
                    sb.Append("null");
                }
                else if (v is string)
                {
                    AppendString(sb, (string)v);
                }
                else if (v is byte)
                {
                    sb.Append("(byte)").Append(v);
                }
                else if (v is sbyte)
                {
                    sb.Append("(sbyte)").Append(v);
                }
                else if (v is short)
                {
                    sb.Append("(short)").Append(v);
                }
                else if (v is ushort)
                {
                    sb.Append("(ushort)").Append(v);
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
                else if (v is Enum)
                {
                    sb.Append(v.GetType().FullName).Append('.').Append(v);
                }
                else if (v is IEnumerable)
                {
                    sb.Append("[");
                    var l = sb.Length;
                    foreach (var c in ((IEnumerable)v).Cast<object>())
                    {
                        AppendNiceString(sb, c);
                        sb.Append(", ");
                    }
                    sb.Length = Math.Max(sb.Length - 2, l);
                    sb.Append("]");
                }
                else
                {
                    sb.Append(v).Append(" {").Append(v.GetType()).Append("}");
                }
            }

            private static void AppendString(StringBuilder sb, string v)
            {
                sb.Append('"');
                foreach (var c in v)
                {
                    switch (c)
                    {
                        case '\n':
                            sb.Append("\\n");
                            break;

                        case '\r':
                            sb.Append("\\r");
                            break;

                        case '\t':
                            sb.Append("\\t");
                            break;

                        case '"':
                            sb.Append("\\\"");
                            break;

                        default:
                            sb.Append(c);
                            break;
                    }
                }
                sb.Append('"');
            }

            #endregion 文字列処理
        }

        private readonly List<Assembly> _Assemblies = new List<Assembly>();
        private readonly List<string> _Namespaces = new List<string>();

        private ScriptState _State;

        public CSharpScriptCommand()
            : base(@"^\s*(run|eval(uate)?|csharp|cs|c#)\s+")
        {
            ResetState();
        }

        protected override Task<Message> ExecuteAsyncCore(Message message, string text)
            => new ExecutionContext(this, message, text).ExecuteAsync();

        private ScriptOptions GetScriptOptions()
            => ScriptOptions.Default.AddReferences(_Assemblies).AddImports(_Namespaces);

        private void ResetState()
        {
            lock (_Assemblies)
            {
                _State = null;

                _Assemblies.Clear();
                _Assemblies.AddRange(new[] { typeof(object).Assembly, typeof(Enumerable).Assembly, typeof(XmlDocument).Assembly, typeof(XDocument).Assembly });

                _Namespaces.Clear();
                _Namespaces.AddRange(new[]
                {
                    typeof(object).Namespace,
                    typeof(List<>).Namespace,
                    typeof(Enumerable).Namespace,
                    typeof(Task).Namespace,
                    typeof(StringBuilder).Namespace,
                    typeof(XmlDocument).Namespace,
                    typeof(XDocument).Namespace
                });
            }
        }
    }
}