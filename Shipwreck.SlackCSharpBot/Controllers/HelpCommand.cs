using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Utilities;
using Shipwreck.SlackCSharpBot.Controllers.Scripting;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Text;

namespace Shipwreck.SlackCSharpBot.Controllers
{
    internal sealed class HelpCommand : RegexMessageCommand
    {
        public HelpCommand()
            : base(@"^\s*(@?shishamo:?\s+|!sh?is[hy]amo-)help($|\s|;)")
        {
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