using Microsoft.Bot.Connector;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using System.Xml.Linq;

namespace Shipwreck.SlackCSharpBot.Controllers.Scripting
{
    internal sealed class CSharpExecutionContext
    {
        private static readonly Regex RESET = new Regex(@"^\s*#reset(\s+|$)", RegexOptions.IgnoreCase);
        private static readonly Regex CONTEXT = new Regex(@"^\s*#(context|ns|namespaces?)(\s+|$)", RegexOptions.IgnoreCase);
        private static readonly Regex RAW = new Regex(@"^\s*#raw(output)?(\s+|$)", RegexOptions.IgnoreCase);
        private static readonly Regex SOURCE = new Regex(@"^\s*#(source|code)?(\s+|$)", RegexOptions.IgnoreCase);
        private static readonly Regex SCOPE = new Regex(@"^\s*#(scope|variables?)(\s+|$)", RegexOptions.IgnoreCase);
        private static readonly Regex USING = new Regex(@"^\s*#using\s+(?<ns>([A-Z_][A-Z0-9_]*\s*\.\s*)*[A-Z_][A-Z0-9_]*)\s*;", RegexOptions.IgnoreCase);
        private static readonly Regex LOG = new Regex(@"^\s*#(log|debug|request)?(\s+|$)", RegexOptions.IgnoreCase);

        private readonly CSharpScriptState _State;

        //  private readonly StringBuilder _Result;
        private string _Code;

        public CSharpExecutionContext(CSharpScriptState command, string text)
        {
            _State = command;
            _Code = HttpUtility.HtmlDecode(text);
        }

        public async Task<CSharpScriptResult> ExecuteAsync()
        {
            var result = new CSharpScriptResult();

            var printSettings = false;

            int pc;
            do
            {
                pc = _Code.Length;
                printSettings |= HandleReset();
                printSettings |= HandleContext();

                result.IsRaw |= HandleRaw();

                printSettings |= HandleUsing();
                result.Code = HandleCode() ?? result.Code;
                result.Variables = HandleScope() ?? result.Variables;
                result.OutputLog |= HandleLog();
            } while (_Code.Length < pc);

            // 設定の表示
            if (printSettings)
            {
                result.Namespaces = _State.GetNamespaces();
            }

            // コードの評価
            if (!string.IsNullOrWhiteSpace(_Code))
            {
                result.Evaluated = true;
                try
                {
                    var state = await _State.RunAsync(_Code);
                    result.ReturnValue = state.ReturnValue;
                }
                catch (Exception ex)
                {
                    result.Exception = ex;
                }
            }

            return result;
        }

        #region ディレクティブ

        private bool HandleReset()
        {
            var m = RESET.Match(_Code);

            if (m.Success)
            {
                _Code = _Code.Substring(m.Length);
                _State.ResetState();
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

            _State.AddNamespaces(newNs);

            return newNs != null;
        }

        private string HandleCode()
        {
            var m = SOURCE.Match(_Code);
            if (m.Success)
            {
                _Code = _Code.Substring(m.Length);

                return _State.GetCode() ?? string.Empty;
            }
            return null;
        }

        private ScriptVariable[] HandleScope()
        {
            var m = SCOPE.Match(_Code);
            if (m.Success)
            {
                _Code = _Code.Substring(m.Length);

                return _State.GetVariables().ToArray();
            }
            return null;
        }
        private bool HandleLog()
        {
            var m = LOG.Match(_Code);

            if (m.Success)
            {
                _Code = _Code.Substring(m.Length);
                return true;
            }

            return false;
        }

        #endregion ディレクティブ
    }
}