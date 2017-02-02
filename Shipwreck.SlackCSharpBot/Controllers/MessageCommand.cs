using Microsoft.Bot.Connector;
using System.Net.Http;
using System.Threading.Tasks;

namespace Shipwreck.SlackCSharpBot.Controllers
{
    public abstract class MessageCommand
    {
        public abstract Task<HttpResponseMessage> TryExecuteAsync(Activity activity, string text);
    }
}