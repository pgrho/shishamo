using Microsoft.Bot.Connector;
using System.Threading.Tasks;

namespace Shipwreck.SlackCSharpBot.Controllers
{
    public abstract class MessageCommand
    {
        public abstract Task<Message> TryExecuteAsync(Message message, string text);
    }
}