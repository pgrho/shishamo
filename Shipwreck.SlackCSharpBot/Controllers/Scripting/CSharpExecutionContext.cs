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
        internal static readonly CSharpDirective[] _DIRECTIVES = {
            new CSharpDirective("reset", "reset", (m, s, r) =>
            {
                s.ResetState();
                r.ShowNamespaces = true;
            }, "C#スクリプトの状態を初期化します。"),
            new CSharpDirective("context", "(context|ns|namespaces?)", (m, s, r) => r.ShowNamespaces = true, "usingされている名前空間を表示します。"),
            new CSharpDirective("using",
                    "using",
                    @"([A-Z_][A-Z0-9_]*\s*\.\s*)*[A-Z_][A-Z0-9_]*",
                    (m, s, r) =>
                    {
                        s.AddNamespace(m.Groups["v"].Value);
                        r.ShowNamespaces = true;
                    }, "usingする名前空間を追加します。"),

            new CSharpDirective(
                    "format",
                    "format",
                    "true|t|yes|y|formatted|false|f|no|n|raw",
                    (m, s, r) => r.IsFormatted  = Regex.IsMatch(m.Groups["v"].Value,"^(true|t|yes|y|formatted)$", RegexOptions.IgnoreCase),
                    "出力を整形するかどうか指定します。"),
            new CSharpDirective("raw", "raw(output)?", (m, s, r) => r.IsFormatted = false, "出力を整形しないことを指定します。"),

            new CSharpDirective(
                    "state",
                    "state",
                    "true|t|yes|y|full|false|f|no|n|none",
                    (m, s, r) => r.IsStatefull  = Regex.IsMatch(m.Groups["v"].Value,"^(true|t|yes|y|false)$", RegexOptions.IgnoreCase),
                    "C#スクリプトの状態を使用するかどうかを指定します。"),
            new CSharpDirective("statefull","statefull", (m, s, r) => r.IsStatefull = true, "C#スクリプトの状態を使用することを指定します。"),
            new CSharpDirective("stateless","stateless", (m, s, r) => r.IsStatefull = false, "C#スクリプトの状態を使用しないことを指定します。"),

            new CSharpDirective("source", "(source|code|sourcecode)", (m, s, r) => r.Code = s.GetCode() ?? string.Empty, "直前の状態で評価済みのソースコードを出力します。"),

            new CSharpDirective("scope", "(scope|variables?)", (m, s, r) => r.Variables = s.GetVariables().ToArray(), "直前の状態で評価されている変数を出力します。"),

            new CSharpDirective("log", "(log|debug|request)", (m, s, r) => r.OutputLog = true, null)
        };

        private readonly CSharpScriptState _State;

        private string _Code;

        public CSharpExecutionContext(CSharpScriptState command, string text)
        {
            _State = command;
            _Code = HttpUtility.HtmlDecode(text);
        }

        public async Task<CSharpScriptResult> ExecuteAsync()
        {
            var result = new CSharpScriptResult();

            if (Regex.IsMatch(_Code, @"^\s*\/?(help|usage|\?|h|u)\s*$", RegexOptions.IgnoreCase))
            {
                result.ShowHelp = true;
                return result;
            }

            int pc;
            do
            {
                pc = _Code.Length;
                foreach (var d in _DIRECTIVES)
                {
                    d.Apply(ref _Code, _State, result);
                }
            } while (_Code.Length < pc);

            // 設定の表示
            if (result.ShowNamespaces)
            {
                result.Namespaces = _State.GetNamespaces();
            }

            if (!result.IsStatefull)
            {
                MessagesController.ReleaseMutex();
            }

            // コードの評価
            if (!string.IsNullOrWhiteSpace(_Code))
            {
                result.CodeEvaluated = true;
                try
                {
                    result.ReturnValue = result.IsStatefull ? (await _State.RunAsync(_Code)).ReturnValue : await CSharpScript.EvaluateAsync(_Code);
                }
                catch (Exception ex)
                {
                    result.Exception = ex;
                }
            }

            return result;
        }
    }
}