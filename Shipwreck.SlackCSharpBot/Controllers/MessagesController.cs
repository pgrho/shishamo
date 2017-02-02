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
using System.Web.Http.Description;

namespace Shipwreck.SlackCSharpBot.Controllers
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        private static readonly Regex UserPattern = new Regex(@"<(?<type>[#@])(?<id>[^>|]+)(\|(?<disp>[^>]+))?>");
        private static readonly Regex UrlPattern = new Regex(@"<(?<url>(https?|ftp):\/\/[^>|]+)(\|(?<disp>[^>]+))?>");

        private static readonly List<MessageCommand> _Commands;

        internal static readonly HttpClient HttpClient = new HttpClient();


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
                new TaskCommand(),
                new EchoSharpUserCommand()
            };
        }

        [ResponseType(typeof(void))]
        public virtual Task<HttpResponseMessage> Post([FromBody] Activity activity)
        {
            if (activity.GetActivityType() == ActivityTypes.Message)
            {
                // TODO: activity.From?.IsBot == false

                var code = activity.Text;
                code = UserPattern.Replace(code, m => m.Groups["type"].Value + (m.Groups["disp"].Success ? m.Groups["disp"].Value : m.Groups["id"].Value));
                code = UrlPattern.Replace(code, m => m.Groups["disp"].Success ? m.Groups["disp"].Value : m.Groups["url"].Value);

                return PostCore(activity, code);
            }
            else
            {
                return Task.FromResult(HandleSystemMessage(activity));
            }
        }

        internal Task<HttpResponseMessage> PostCore(Activity activity, string code)
        {
            foreach (var cmd in _Commands)
            {
                var t = cmd.TryExecuteAsync(activity, code);

                if (t != null)
                {
                    return t;
                }
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));
        }

        private HttpResponseMessage HandleSystemMessage(Activity activity)
        {
            //if (activity.Type == "Ping")
            //{
            //    //Message reply = activity.CreateReplyMessage();
            //    //reply.Type = "Ping";
            //    //return reply;
            //}
            //else if (activity.Type == "DeleteUserData")
            //{
            //    // Implement user deletion here
            //    // If we handle user deletion, return a real activity
            //}
            //else if (activity.Type == "BotAddedToConversation")
            //{
            //}
            //else if (activity.Type == "BotRemovedFromConversation")
            //{
            //}
            //else if (activity.Type == "UserAddedToConversation")
            //{
            //}
            //else if (activity.Type == "UserRemovedFromConversation")
            //{
            //}
            //else if (activity.Type == "EndOfConversation")
            //{
            //}

            return new HttpResponseMessage(HttpStatusCode.Accepted);
        }
    }
}