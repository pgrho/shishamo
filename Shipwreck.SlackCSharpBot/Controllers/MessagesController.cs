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
    [CustomBotAuthentication]
    public class MessagesController : ApiController
    {
        private static readonly Regex UserPattern = new Regex(@"<(?<type>[#@])(?<id>[^>|]+)(\|(?<disp>[^>]+))?>");
        private static readonly Regex UrlPattern = new Regex(@"<(?<url>(https?|ftp):\/\/[^>|]+)(\|(?<disp>[^>]+))?>");

        private static readonly List<MessageCommand> _Commands;

        internal static MessageCommand[] GetCommands()
            => _Commands.ToArray();

        static MessagesController()
        {
            _Commands = new List<MessageCommand>()
            {
                new HelpCommand(),
                new CSharpScriptCommand(),
                new EchoSharpCommand(),
                new FishPixCommand(),
                new SachikoCommand(),
                new IdolMasterCommand(),
                new EchoSharpUserCommand()
            };
        }

        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public Task<Message> Post([FromBody]Message message)
        {
            if (message.Type == "Message"
                && message.From?.IsBot == false)
            {
                var code = message.Text;
                code = UserPattern.Replace(code, m => m.Groups["type"].Value + (m.Groups["disp"].Success ? m.Groups["disp"].Value : m.Groups["id"].Value));
                code = UrlPattern.Replace(code, m => m.Groups["disp"].Success ? m.Groups["disp"].Value : m.Groups["url"].Value);

                return PostCore(message, code);
            }
            else
            {
                return Task.FromResult(HandleSystemMessage(message));
            }
        }

        internal Task<Message> PostCore(Message message, string code)
        {
            foreach (var cmd in _Commands)
            {
                var t = cmd.TryExecuteAsync(message, code);

                if (t != null)
                {
                    return t;
                }
            }

            return Task.FromResult<Message>(null);
        }

        private Message HandleSystemMessage(Message message)
        {
            if (message.Type == "Ping")
            {
                Message reply = message.CreateReplyMessage();
                reply.Type = "Ping";
                return reply;
            }
            else if (message.Type == "DeleteUserData")
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == "BotAddedToConversation")
            {
            }
            else if (message.Type == "BotRemovedFromConversation")
            {
            }
            else if (message.Type == "UserAddedToConversation")
            {
            }
            else if (message.Type == "UserRemovedFromConversation")
            {
            }
            else if (message.Type == "EndOfConversation")
            {
            }

            return null;
        }
    }
}