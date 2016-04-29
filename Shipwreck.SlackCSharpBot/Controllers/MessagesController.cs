using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Utilities;
using Shipwreck.SlackCSharpBot.Controllers.Scripting;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System;
using System.Text;
using System.Linq;

namespace Shipwreck.SlackCSharpBot.Controllers
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        private static readonly Regex UserPattern = new Regex(@"<(?<type>[#@])(?<id>[^>|]+)(\|(?<disp>[^>]+))?>");
        private static readonly Regex UrlPattern = new Regex(@"<(?<url>(https?|ftp):\/\/[^>|]+)(\|(?<disp>[^>]+))?>");

        private static readonly List<MessageCommand> _Commands;

        internal static MessageCommand[] GetCommands()
            => _Commands.ToArray();

        static MessagesController()
        {
            _Mutex = new EventWaitHandle(true, EventResetMode.ManualReset);


            _Commands = new List<MessageCommand>()
            {
                new HelpCommand(),
                new FishPixCommand(),
                new CSharpScriptCommand()
            };
        }

        private static readonly EventWaitHandle _Mutex;

        internal static void ReleaseMutex()
            => _Mutex.Set();

        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<Message> Post([FromBody]Message message)
        {
            if (message.Type == "Message")
            {
                var code = message.Text;

                code = UserPattern.Replace(code, m => m.Groups["type"].Value + (m.Groups["disp"].Success ? m.Groups["disp"].Value : m.Groups["id"].Value));
                code = UrlPattern.Replace(code, m => m.Groups["disp"].Success ? m.Groups["disp"].Value : m.Groups["url"].Value);

                if (_Mutex.WaitOne(15000))
                {
                    _Mutex.Reset();
                    try
                    {
                        foreach (var cmd in _Commands)
                        {
                            var t = cmd.TryExecuteAsync(message, code);

                            if (t != null)
                            {
                                return await t;
                            }
                        }
                    }
                    finally
                    {
                        _Mutex.Set();
                    }
                }
                else
                {
                    message.CreateReplyMessage(":exclamation:15秒以内に直前のコマンドが終了しませんでした。");
                }

                return null;
            }
            else
            {
                return HandleSystemMessage(message);
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