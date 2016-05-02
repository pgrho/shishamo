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
    internal sealed class EchoSharpCommand : NamedMessageCommand
    {
        private static Regex LIST = new Regex(@"^\s*list", RegexOptions.IgnoreCase);
        private static Regex UPDATE = new Regex(@"^\s*(?<s>add|create|upd(ate)?)\s+(?<n>\S+)\s+(?<p>\S+)\s+(?<c>.+)$", RegexOptions.IgnoreCase);
        private static Regex SHOW = new Regex(@"^\s*show\s+(?<n>\S+)\s*$", RegexOptions.IgnoreCase);
        private static Regex DELETE = new Regex(@"^\s*del(elte)?\s+(?<n>\S+)\s*$", RegexOptions.IgnoreCase);

        public EchoSharpCommand()
            : base("echo#", "shishamoコマンドの登録")
        {
        }

        protected override async Task<Message> ExecuteAsyncCore(Message message, string text)
        {
            return await HandleListAsync(message, text)
                ?? await HandleAddAsync(message, text)
                ?? await HandleShowAsync(message, text)
                ?? await HandleDeleteAsync(message, text)
                ?? HandleHelp(message, text);
        }

        private async Task<Message> HandleListAsync(Message message, string text)
        {
            var m = LIST.Match(text);
            if (!m.Success)
            {
                return null;
            }

            var mc = MessagesController.GetCommands().OfType<EchoSharpUserCommand>().FirstOrDefault();

            if (mc == null)
            {
                return null;
            }
            await mc.InitEntries();

            if (mc.Entries.Any())
            {
                return message.CreateReplyMessage(string.Join(" ", mc.Entries.Select(_ => $"`{_.Name}`")));
            }
            return message.CreateReplyMessage("コマンドがありません。");
        }

        private async Task<Message> HandleAddAsync(Message message, string text)
        {
            var m = UPDATE.Match(text);
            if (!m.Success)
            {
                return null;
            }

            var mc = MessagesController.GetCommands().OfType<EchoSharpUserCommand>().FirstOrDefault();

            if (mc == null)
            {
                return null;
            }

            var e = CreateEntry(m);

            if (e == null)
            {
                return message.CreateReplyMessage($"{StringBuilderHelper.ERROR}正規表現が無効です。");
            }
            var willUpdate = m.Groups["s"].Value.StartsWith("u", StringComparison.InvariantCultureIgnoreCase);
            var didUpdated = false;
            try
            {
                using (var db = new ShishamoDbContext())
                {
                    var f = await db.EchoSharpEntries.FirstOrDefaultAsync(_ => _.Name == e.Name);
                    if (f == null)
                    {
                        db.EchoSharpEntries.Add(e);
                    }
                    else
                    {
                        didUpdated = true;
                        f.Pattern = e.Pattern;
                        f.Command = e.Command;
                    }
                    await db.SaveChangesAsync();
                }
            }
            catch
            {
                return message.CreateReplyMessage($"{StringBuilderHelper.ERROR}コマンドの追加に失敗しました。");
            }

            await mc.InitEntries();

            mc.Entries.RemoveAll(_ => e.Name.Equals(_.Name, StringComparison.InvariantCultureIgnoreCase));
            mc.Entries.Add(e);

            var sb = new StringBuilder();

            if (willUpdate && !didUpdated)
            {
                sb.Warning().Append("更新対象が存在しません。").NewLine();
            }
            else if (!willUpdate && didUpdated)
            {
                sb.Warning().Append("同名のコマンドが存在します。").NewLine();
            }

            sb.Success().Append("コマンド `").Append(e.Name).Append("` を").Append(didUpdated ? "更新" : "作成").Append("しました");

            return message.CreateReplyMessage(sb.ToString());
        }

        private async Task<Message> HandleShowAsync(Message message, string text)
        {
            var m = SHOW.Match(text);
            if (!m.Success)
            {
                return null;
            }

            var mc = MessagesController.GetCommands().OfType<EchoSharpUserCommand>().FirstOrDefault();

            if (mc == null)
            {
                return null;
            }

            var n = m.Groups["n"].Value;

            await mc.InitEntries();

            var e = mc.Entries.FirstOrDefault(_ => n.Equals(_.Name));

            if (e == null)
            {
                return message.CreateReplyMessage($"{StringBuilderHelper.WARNING}指定された名前のコマンドが存在しません。");
            }

            var sb = new StringBuilder();

            sb.Append("Name: ").Append(e.Name).NewLine();
            sb.Append("Pattern: ").Append(e.Pattern).NewLine();
            sb.Append("Command: ").Append(e.Command);

            return message.CreateReplyMessage(sb.ToString());
        }

        private async Task<Message> HandleDeleteAsync(Message message, string text)
        {
            var m = DELETE.Match(text);
            if (!m.Success)
            {
                return null;
            }

            var mc = MessagesController.GetCommands().OfType<EchoSharpUserCommand>().FirstOrDefault();

            if (mc == null)
            {
                return null;
            }

            var n = m.Groups["n"].Value;
            var rc = 0;
            using (var db = new ShishamoDbContext())
            {
                var l = await db.EchoSharpEntries.Where(_ => _.Name == n).ToListAsync();
                rc += l.Count;
                if (l.Any())
                {
                    await db.SaveChangesAsync();
                }
            }

            await mc.InitEntries();

            rc += mc.Entries.RemoveAll(_ => n.Equals(_.Name, StringComparison.InvariantCultureIgnoreCase));

            if (rc == 0)
            {
                return message.CreateReplyMessage($"{StringBuilderHelper.WARNING}指定された名前のコマンドが存在しません。");
            }
            return message.CreateReplyMessage($"{StringBuilderHelper.SUCCESS}{(rc + 1) / 2}個のコマンドを削除しました。");
        }

        private Message HandleHelp(Message message, string text)
        {
            var sb = new StringBuilder();
            sb.Append("* list").NewLine();
            sb.Append("* add name pattern commandReplacement").NewLine();
            sb.Append("* upd name pattern commandReplacement").NewLine();
            sb.Append("* show name").NewLine();
            sb.Append("* del name");

            return message.CreateReplyMessage(sb.ToString());
        }

        private static EchoSharpEntry CreateEntry(Match m)
        {
            try
            {
                var e = new EchoSharpEntry()
                {
                    Name = m.Groups["n"].Value,
                    Pattern = m.Groups["p"].Value,
                    Command = m.Groups["c"].Value
                };
                e.Regex.IsMatch(e.Name);

                return e;
            }
            catch
            {
                return null;
            }
        }
    }
}