using Microsoft.Bot.Connector;
using Shipwreck.SlackCSharpBot.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace Shipwreck.SlackCSharpBot.Controllers
{
    internal sealed class EchoSharpUserCommand : MessageCommand
    {
        internal List<EchoSharpEntry> Entries;

        internal async Task InitEntries()
        {
            if (Entries == null)
            {
                using (var db = new ShishamoDbContext())
                {
                    db.Configuration.ProxyCreationEnabled = false;
                    Entries = await db.EchoSharpEntries.ToListAsync();
                }
            }
        }

        public override async Task<Message> TryExecuteAsync(Message message, string text)
        {
            await InitEntries();

            var list = Entries.Select(e =>
            {
                try
                {
                    var m = e.Regex.Match(text);
                    if (m.Success)
                    {
                        return new { e, m };
                    }
                }
                catch { }
                return new { e, m = (Match)null };
            }).Where(_ => _.m != null).ToList();

            if (list.Any())
            {
                var e = list.Count == 1 ? list[0] : list[new Random().Next(list.Count)];

                string c;
                try
                {
                    c = e.m.Result(e.e.Command);
                }
                catch (Exception ex)
                {
                    return message.CreateReplyMessage($"{StringBuilderHelper.ERROR}{e.e.Name}の置換でエラーが発生しました。{StringBuilderHelper.NEW_LINE}> {ex.Message}");
                }

                return await new MessagesController().PostCore(message, c);
            }

            return null;
        }
    }
}