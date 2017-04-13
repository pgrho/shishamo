using Microsoft.Bot.Connector;
using Shipwreck.SlackCSharpBot.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace Shipwreck.SlackCSharpBot.Controllers
{
    internal sealed class EchoSharpCommand : NamedMessageCommand
    {
        private static Regex LIST = new Regex(@"^\s*list", RegexOptions.IgnoreCase);
        private static Regex UPDATE = new Regex(@"^\s*(?<s>add|create|upd(ate)?)\s+(?<n>\S+)\s+(?<p>\S+)\s+(?<c>.+)$", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        private static Regex SHOW = new Regex(@"^\s*show\s+(?<n>\S+)\s*$", RegexOptions.IgnoreCase);
        private static Regex DELETE = new Regex(@"^\s*del(elte)?\s+(?<n>\S+)\s*$", RegexOptions.IgnoreCase);

        public EchoSharpCommand()
            : base("echo#", "shishamoコマンドの登録")
        {
        }

        protected override async Task<HttpResponseMessage> ExecuteAsyncCore(Activity activity, string text)
        {
            return await HandleListAsync(activity, text)
                ?? await HandleAddAsync(activity, text)
                ?? await HandleShowAsync(activity, text)
                ?? await HandleDeleteAsync(activity, text)
                ?? await HandleHelpAsync(activity, text);
        }

        private async Task<HttpResponseMessage> HandleListAsync(Activity activity, string text)
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
                return await activity.ReplyToAsync(string.Join(" ", mc.Entries.Select(_ => $"`{_.Name}`")));
            }
            return await activity.ReplyToAsync("コマンドがありません。");
        }

        private async Task<HttpResponseMessage> HandleAddAsync(Activity activity, string text)
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
                return await activity.ReplyToAsync($"{StringBuilderHelper.ERROR}正規表現が無効です。");
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
                return await activity.ReplyToAsync($"{StringBuilderHelper.ERROR}コマンドの追加に失敗しました。");
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

            return await activity.ReplyToAsync(sb.ToString());
        }

        private async Task<HttpResponseMessage> HandleShowAsync(Activity activity, string text)
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
                return await activity.ReplyToAsync($"{StringBuilderHelper.WARNING}指定された名前のコマンドが存在しません。");
            }

            var sb = new StringBuilder();

            sb.Append("Name: ").Append(e.Name).NewLine();
            sb.Append("Pattern: ").Append(e.Pattern).NewLine();
            sb.Append("Command: ").Append(e.Command);

            return await activity.ReplyToAsync(sb.ToString());
        }

        private async Task<HttpResponseMessage> HandleDeleteAsync(Activity activity, string text)
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
                    db.EchoSharpEntries.RemoveRange(l);
                    await db.SaveChangesAsync();
                }
            }

            await mc.InitEntries();

            rc += mc.Entries.RemoveAll(_ => n.Equals(_.Name, StringComparison.InvariantCultureIgnoreCase));

            if (rc == 0)
            {
                return await activity.ReplyToAsync($"{StringBuilderHelper.WARNING}指定された名前のコマンドが存在しません。");
            }
            return await activity.ReplyToAsync($"{StringBuilderHelper.SUCCESS}{(rc + 1) / 2}個のコマンドを削除しました。");
        }

        private Task<HttpResponseMessage> HandleHelpAsync(Activity activity, string text)
        {
            var sb = new StringBuilder();
            sb.Append("* list").NewLine();
            sb.Append("* add name pattern commandReplacement").NewLine();
            sb.Append("* upd name pattern commandReplacement").NewLine();
            sb.Append("* show name").NewLine();
            sb.Append("* del name");

            return activity.ReplyToAsync(sb.ToString());
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