using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Utilities;
using Shipwreck.SlackCSharpBot.Controllers.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public override Task<Message> TryExecuteAsync(Message message, string text)
        {
            if (REPLY.IsMatch(message.To?.Name ?? string.Empty)
                && HELP.IsMatch(text ?? string.Empty))
            {
                return ExecuteAsyncCore(message, text);
            }
            return base.TryExecuteAsync(message, text);
        }

        protected override Task<Message> ExecuteAsyncCore(Message message, string text)
        {
            var sb = new StringBuilder();
            var cmds = MessagesController.GetCommands();

            sb.Append("shishamo C# bot:").NewLine();
            foreach (var c in cmds.OfType<NamedMessageCommand>())
            {
                sb.Append('!').Append(c.Name).Append(": ").Append(c.Help).NewLine();
            }

            return Task.FromResult(message.CreateReplyMessage(sb.ToString()));
        }
    }
}