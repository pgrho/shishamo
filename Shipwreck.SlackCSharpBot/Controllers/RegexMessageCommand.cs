using Microsoft.Bot.Connector;
using System.Net.Http;
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

        public override Task<HttpResponseMessage> TryExecuteAsync(Activity activity, string text)
        {
            if (text == null)
            {
                return null;
            }
            var m = _Pattern.Match(text);

            if (m.Success)
            {
                return ExecuteAsyncCore(activity, text.Substring(m.Length));
            }

            return null;
        }

        protected abstract Task<HttpResponseMessage> ExecuteAsyncCore(Activity activity, string text);
    }
}