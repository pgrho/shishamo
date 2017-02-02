using Microsoft.Bot.Connector;
using Shipwreck.SlackCSharpBot.Controllers.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace Shipwreck.SlackCSharpBot.Controllers
{
    internal sealed class HelpCommand : RegexMessageCommand
    {
        private static readonly Regex REPLY = new Regex(@"shishamo($|\s|:)", RegexOptions.IgnoreCase);
        private static readonly Regex HELP = new Regex(@"help($|\s)", RegexOptions.IgnoreCase);

        public HelpCommand()
            : base(@"^\s*(@?shishamo:?\s+|!sh?is[hy]amo-)help($|\s|;)")
        {
        }

        public override Task<HttpResponseMessage> TryExecuteAsync(Activity activity, string text)
        {
            if (activity.GetMentions().Any(_ => REPLY.IsMatch(_.Mentioned.Name))
                && HELP.IsMatch(text ?? string.Empty))
            {
                return ExecuteAsyncCore(activity, text);
            }
            return base.TryExecuteAsync(activity, text);
        }

        protected override Task<HttpResponseMessage> ExecuteAsyncCore(Activity activity, string text)
        {
            var sb = new StringBuilder();
            var cmds = MessagesController.GetCommands();

            sb.Append("shishamo C# bot:").NewLine();
            foreach (var c in cmds.OfType<NamedMessageCommand>())
            {
                sb.Append('!').Append(c.Name).Append(": ").Append(c.Help).NewLine();
            }

            return activity.ReplyToAsync(sb.ToString());
        }
    }

}