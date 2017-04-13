using Microsoft.Bot.Connector;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
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
    internal sealed class CSharpScriptCommand : NamedMessageCommand
    {
        internal static readonly CSharpDirective[] _DIRECTIVES;

        private readonly CSharpSandbox _Sandbox;

        static CSharpScriptCommand()
        {
            var dirs = new List<CSharpDirective>(16);
            dirs.Add(new CSharpDirective("reset", "reset", (m, p) =>
            {
                p.ResetState = true;
                p.ReturnsNamespaces = true;
            }, "C#スクリプトの状態を初期化します。"));
            dirs.Add(new CSharpDirective(
                        "context",
                        "(context|ns|namespaces?)",
                        (m, p) => p.ReturnsNamespaces = true, "usingされている名前空間を表示します。"));
            dirs.Add(new CSharpDirective("using",
                    "using",
                    @"([A-Z_][A-Z0-9_]*\s*\.\s*)*[A-Z_][A-Z0-9_]*",
                    (m, p) =>
                    {
                        p.Namespaces.Add(m.Groups["v"].Value);
                        p.ReturnsNamespaces = true;
                    }, "usingする名前空間を追加します。"));
            dirs.Add(new CSharpDirective(
                    "format",
                    "format",
                    "true|t|yes|y|formatted|false|f|no|n|raw",
                    (m, p) => p.ReturnsRawValue = !Regex.IsMatch(m.Groups["v"].Value, "^(true|t|yes|y|formatted)$", RegexOptions.IgnoreCase),
                    "出力を整形するかどうか指定します。"));
            dirs.Add(new CSharpDirective("raw", "raw(output)?", (m, p) => p.ReturnsRawValue = true, "出力を整形しないことを指定します。"));
            dirs.Add(new CSharpDirective(
                    "state",
                    "state",
                    "true|t|yes|y|full|false|f|no|n|none",
                    (m, p) => p.UsesSeparateContext = !Regex.IsMatch(m.Groups["v"].Value, "^(true|t|yes|y|false)$", RegexOptions.IgnoreCase),
                    "C#スクリプトの状態を使用するかどうかを指定します。"));
            dirs.Add(new CSharpDirective("statefull", "statefull", (m, p) => p.UsesSeparateContext = false, "C#スクリプトの状態を使用することを指定します。"));
            dirs.Add(new CSharpDirective("stateless", "stateless", (m, p) => p.UsesSeparateContext = true, "C#スクリプトの状態を使用しないことを指定します。"));
            dirs.Add(new CSharpDirective("source", "(source|code|sourcecode)", (m, p) => p.ReturnsSourceCode = true, "直前の状態で評価済みのソースコードを出力します。"));
            dirs.Add(new CSharpDirective("scope", "(scope|variables?)", (m, p) => p.ReturnsVariables = true, "直前の状態で評価されている変数を出力します。"));
            // new CSharpDirective("log", "(log|debug|request)", (m, s, r) => r.OutputLog = true, null)

            _DIRECTIVES = dirs.ToArray();
        }

        public CSharpScriptCommand()
            : base("cs", "C#スクリプトの評価を行います。")
        {
            _Sandbox = new CSharpSandbox();
        }

        protected override async Task<HttpResponseMessage> ExecuteAsyncCore(Activity activity, string text)
        {
            var code = HttpUtility.HtmlDecode(text);

            var sb = new StringBuilder();
            if (Regex.IsMatch(code, @"^\s*\/?(help|usage|\?|h|u)\s*$", RegexOptions.IgnoreCase))
            {
                AppendHelp(sb);
            }
            else
            {
                var param = new CSharpSandboxParameter();

                int pc;
                do
                {
                    pc = code.Length;
                    foreach (var d in _DIRECTIVES)
                    {
                        d.Apply(ref code, param);
                    }
                } while (code.Length < pc);

                param.Code = code;

                var r = await _Sandbox.ExecuteAsync(param).ConfigureAwait(false);

                if (param.ReturnsNamespaces)
                {
                    AppendNamespaces(sb, r);
                }

                if (param.ReturnsSourceCode)
                {
                    AppendSourceCode(sb, r);
                }

                if (param.ReturnsVariables)
                {
                    AppendVariables(sb, r);
                }

                if (!string.IsNullOrEmpty(r.StandardOutput))
                {
                    sb.Append("# 標準出力").NewLine();

                    AppendQuoted(sb, r.StandardOutput);
                }
                if (!string.IsNullOrEmpty(r.ErrorOutput))
                {
                    sb.Append("# エラー出力").NewLine();

                    AppendQuoted(sb, r.ErrorOutput);
                }

                if (!string.IsNullOrEmpty(r.Exception))
                {
                    sb.Append("# 例外").NewLine();

                    AppendQuoted(sb, r.Exception);
                }

                if (!string.IsNullOrEmpty(r.ReturnValue))
                {
                    if (sb.Length > 0)
                    {
                        sb.Append("# コードの評価結果").NewLine();
                    }

                    sb.Append(r.ReturnValue);
                }
            }

            return await activity.ReplyToAsync(sb.ToString());
        }

        private static void AppendQuoted(StringBuilder sb, string t)
        {
            using (var sr = new StringReader(t))
            {
                for (var l = sr.ReadLine(); l != null; l = sr.ReadLine())
                {
                    sb.Append("> ").AppendLine(l);
                }
            }
            sb.NewLine();
        }

        private static void AppendVariables(StringBuilder sb, CSharpSandboxResult r)
        {
            sb.Append("# 評価済みの変数").NewLine();

            if (r.Variables.Any())
            {
                foreach (var v in r.Variables)
                {
                    var t = Type.GetType(v.TypeName);
                    if (t != null)
                    {
                        sb.AppendType(t, r.Namespaces);
                    }
                    else
                    {
                        sb.Append('{').Append(v.TypeName).Append('}');
                    }
                    sb.Append(' ').Append(v.Name).Append(" = `").AppendLine(v.Value).Append("`;\n");
                }
            }
            else
            {
                sb.Warning().Append("評価済みの変数はありません").NewLine();
            }
            sb.NewLine();
        }

        private static void AppendSourceCode(StringBuilder sb, CSharpSandboxResult r)
        {
            sb.Append("# 評価済みのコード").NewLine();

            if (string.IsNullOrWhiteSpace(r.SourceCode))
            {
                sb.Warning().Append("評価済みのコードはありません").NewLine();
            }
            else
            {
                sb.Append("```").Append('\n');
                sb.Append(r.SourceCode.Replace(Environment.NewLine, StringBuilderHelper.NEW_LINE)).Append('\n');
                sb.Append("```").Append('\n');
            }
            sb.NewLine();
        }

        private static void AppendNamespaces(StringBuilder sb, CSharpSandboxResult r)
        {
            if (r.HasNamespace)
            {
                sb.Append("# usingされている名前空間").NewLine();

                if (r.Namespaces.Any())
                {
                    sb.List(r.Namespaces);
                }
                else
                {
                    sb.Warning().Append("名前空間はありません").NewLine();
                }
                sb.NewLine();
            }
        }

        private void AppendHelp(StringBuilder sb)
        {
            sb.Append("C# Script Evaluator").NewLine();
            sb.Append("See https://blogs.msdn.microsoft.com/cdndevs/2015/12/01/adding-c-scripting-to-your-development-arsenal-part-1/ for language background.").NewLine();
            sb.NewLine();
            sb.Append("Custom directives:").NewLine();
            foreach (var d in _DIRECTIVES)
            {
                if (d.Help != null)
                {
                    sb.Append(d.Name).Append(": ").Append(d.Help).NewLine();
                }
            }
        }
    }
}