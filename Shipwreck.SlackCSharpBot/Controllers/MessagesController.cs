using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Utilities;
using System.Threading.Tasks;
using System.Web.Http;

namespace Shipwreck.SlackCSharpBot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        private static readonly MessageCommand[] _Commands = {
            new CSharpScriptCommand()
        };

        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public Task<Message> Post([FromBody]Message message)
        {
            if (message.Type == "Message")
            {
                var code = message.Text;

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
            else
            {
                return Task.FromResult(HandleSystemMessage(message));
            }
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