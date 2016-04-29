using Microsoft.Bot.Connector;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Shipwreck.SlackCSharpBot.Controllers
{
    public abstract class RegexMessageCommand : MessageCommand
    {
        protected readonly Regex _Pattern;

        protected RegexMessageCommand(string pattern)
        {
            _Pattern = new Regex(pattern, RegexOptions.IgnoreCase);
        }

        public override Task<Message> TryExecuteAsync(Message message, string text)
        {
            if (text == null)
            {
                return null;
            }
            var m = _Pattern.Match(text);

            if (m.Success)
            {
                return ExecuteAsyncCore(message, text.Substring(m.Length));
            }

            return null;
        }

        protected abstract Task<Message> ExecuteAsyncCore(Message message, string text);
    }
}