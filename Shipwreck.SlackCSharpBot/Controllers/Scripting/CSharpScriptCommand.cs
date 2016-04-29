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
    internal sealed class CSharpScriptCommand : NamedMessageCommand
    {
        private readonly CSharpScriptState _State;

        public CSharpScriptCommand()
            : base("cs", "C#スクリプトの評価を行います。")
        {
            _State = new CSharpScriptState();
        }

        protected override async Task<Message> ExecuteAsyncCore(Message message, string text)
        {
            var sb = new StringBuilder();
            var r = await new CSharpExecutionContext(_State, text).ExecuteAsync();

            if (r.ShowHelp)
            {
                sb.Append("C# Script Evaluator").NewLine();
                sb.Append("See https://blogs.msdn.microsoft.com/cdndevs/2015/12/01/adding-c-scripting-to-your-development-arsenal-part-1/ for language background.").NewLine();
                sb.NewLine();
                sb.Append("Custom directives:").NewLine();
                foreach (var d in CSharpExecutionContext._DIRECTIVES)
                {
                    if (d.Help != null)
                    {
                        sb.Append(d.Name).Append(": ").Append(d.Help).NewLine();
                    }
                }
            }
            else
            {
                var ns = _State.GetNamespaces();
                if (r.Namespaces != null)
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

                if (r.Code != null)
                {
                    sb.Append("# 評価済みのコード").NewLine();

                    if (string.IsNullOrWhiteSpace(r.Code))
                    {
                        sb.Warning().Append("評価済みのコードはありません").NewLine();
                    }
                    else
                    {
                        sb.Append("```").Append('\n');
                        sb.Append(r.Code.Replace(Environment.NewLine, StringBuilderHelper.NEW_LINE)).Append('\n');
                        sb.Append("```").Append('\n');
                    }
                    sb.NewLine();
                }
                if (r.Variables != null)
                {
                    sb.Append("# 評価済みの変数").NewLine();

                    if (r.Variables.Any())
                    {
                        foreach (var v in r.Variables)
                        {
                            sb.AppendType(v.Type, ns).Append(' ').Append(v.Name).Append(" = ```").AppendNiceString(v.Value, ns, false).Append("```;\n");
                        }
                    }
                    else
                    {
                        sb.Warning().Append("評価済みの変数はありません").NewLine();
                    }
                    sb.NewLine();
                }
                if (r.CodeEvaluated)
                {
                    if (r.Exception != null)
                    {
                        sb.Error().Append('`').Append(r.Exception.GetType().FullName).Append('`').NewLine();
                        sb.Append("> ").Append(r.Exception.Message).NewLine();
                    }
                    else
                    {
                        if (sb.Length > 0)
                        {
                            sb.Append("# コードの評価結果").NewLine();
                        }

                        if (r.IsFormatted)
                        {
                            sb.Append("```\n");
                            sb.AppendNiceString(r.ReturnValue, ns, true);
                            sb.Append("```\n");
                        }
                        else
                        {
                            var str = (r.ReturnValue ?? "null").ToString();
                            sb.Append(str.Replace(Environment.NewLine, StringBuilderHelper.NEW_LINE));
                        }
                    }
                }

                if (r.OutputLog || r.Exception != null)
                {
                    HttpContext.Current.Request.SaveAs(Path.Combine(Path.GetTempPath(), $"{nameof(CSharpScriptCommand)}.{DateTime.Now:yyyyMMddHHmmssffffff}.txt"), true);
                }
            }

            var m = message.CreateReplyMessage(sb.ToString());

            return m;
        }
    }
}