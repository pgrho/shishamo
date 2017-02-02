using Microsoft.Bot.Connector;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Shipwreck.SlackCSharpBot.Controllers
{
    internal static class ActivityHelper
    {
        public static async Task<HttpResponseMessage> ReplyToAsync(this Activity activity, string text)
        {
            var connector = new ConnectorClient(new Uri(activity.ServiceUrl));
            await connector.Conversations.ReplyToActivityAsync(activity.CreateReply(text));
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}