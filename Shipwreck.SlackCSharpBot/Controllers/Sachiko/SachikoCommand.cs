using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace Shipwreck.SlackCSharpBot.Controllers.Sachiko
{
    internal sealed class SachikoCommand : NamedMessageCommand
    {
        private static readonly Regex HELP = new Regex(@"^\s*([/-]\?|usage|-{0,2}help)", RegexOptions.IgnoreCase);
        private static readonly Regex PUSH = new Regex(@"^\s*push\s+(?<i>-i\s+)?(-d\s+(?<d>[^\s]+)\s+)?(?<m>[-+]?\d+(,\d+)*)(\s+(?<c>.*))?$", RegexOptions.IgnoreCase);
        private static readonly Regex POP = new Regex(@"^\s*pop(\s+-d\s+(?<d>[^\s]+))?(\s+|$)", RegexOptions.IgnoreCase);
        private static readonly Regex CURRENT = new Regex(@"^\s*(total|current)(\s+|$)", RegexOptions.IgnoreCase);
        private static readonly Regex HISTORY = new Regex(@"^\s*(history|list|log)(\s+-c\s+(?<c>\d+))?(\s+|$)", RegexOptions.IgnoreCase);
        private static readonly Regex SHOW_ADMIN = new Regex(@"^\s*admin(\s+|$)", RegexOptions.IgnoreCase);
        private static readonly Regex ADD_ADMIN = new Regex(@"^\s*addadmin\s+(.*)", RegexOptions.IgnoreCase);
        private static readonly Regex DEL_ADMIN = new Regex(@"^\s*deladmin\s+(.*)", RegexOptions.IgnoreCase);

        public SachikoCommand()
            : base("sachiko", "さちこ")
        {
        }

        protected override async Task<Message> ExecuteAsyncCore(Message message, string text)
        {
            return await HandleHelp(message, text)
                    ?? await HandlePush(message, text)
                    ?? await HandlePop(message, text)
                    ?? await HandleCurrent(message, text)
                    ?? await HandleHistory(message, text)
                    ?? await HandleShowAdmin(message, text)
                    ?? await HandleAddAdmin(message, text)
                    ?? await HandleDelAdmin(message, text)
                    ?? await CreateErrorMessage(message, Regex.IsMatch(text, @"^\s*(|かわいい|[ck]awaii)\s*$", RegexOptions.IgnoreCase) ? "" : "コマンドが無効です。");
        }

        private async Task<Message> HandleHelp(Message message, string text)
        {
            if (!HELP.IsMatch(text))
            {
                return null;
            }
            var sb = new StringBuilder();

            sb.Append("push [-d date] value [comment]").NewLine();
            sb.Append("pop [-d date]").NewLine();
            sb.Append("total").NewLine();
            sb.Append("log [-c count]").NewLine().NewLine();

            sb.Append("admin").NewLine();
            sb.Append("addadmin user").NewLine();
            sb.Append("deladmin user").NewLine().NewLine();

            sb.Append("kawaii").NewLine();

            await AppendSachiko(sb);

            return message.CreateReplyMessage(sb.ToString());
        }

        private async Task<Message> HandlePush(Message message, string text)
        {
            var m = PUSH.Match(text);
            if (!m.Success)
            {
                return null;
            }

            var sb = new StringBuilder();
            using (var db = new SachikoDbContext())
            {
                if (!await IsAuthorizedAsync(message, db))
                {
                    return await CreateErrorMessage(message, "権限がありません。");
                }

                var d = m.Groups["d"].Value;
                var v = m.Groups["m"].Value;
                var c = m.Groups["c"].Value;
                var insert = m.Groups["i"].Success;

                var rec = new SachikoRecord();

                long lv;

                if (!long.TryParse(v, out lv))
                {
                    return await CreateErrorMessage(message, "金額が無効です。");
                }
                if (lv == 0)
                {
                    return await CreateErrorMessage(message, "金額に0を指定することはできません。");
                }

                rec.Difference = lv;

                DateTime dv;

                if (string.IsNullOrEmpty(d))
                {
                    dv = DateTime.Now;
                }
                else if (!DateTime.TryParse(d, out dv))
                {
                    return await CreateErrorMessage(message, "日付が無効です。");
                }

                rec.Date = dv;

                rec.Comment = string.IsNullOrWhiteSpace(c) ? null : c;

                var pt = await db.Records
                                    .Where(_ => _.Date <= dv)
                                    .OrderByDescending(_ => _.Date)
                                    .ThenByDescending(_ => _.Id)
                                    .Select(_ => (long?)_.Total)
                                    .FirstOrDefaultAsync();

                rec.Total = (pt ?? 0) + lv;

                var aft = await db.Records.Where(_ => _.Date > dv)
                                            .OrderBy(_ => _.Date)
                                            .ThenBy(_ => _.Id)
                                            .ToArrayAsync();

                foreach (var r in aft)
                {
                    r.Total += lv;
                }

                db.Records.Add(rec);

                await db.SaveChangesAsync();
                sb.Success().AppendFormat(
                    @"{0:yyyy/MM/dd}の{1:""\""#,0}の{2} ({3})を追加しました。合計金額は{4:""\""#,0}です。",
                    rec.Date,
                    Math.Abs(rec.Difference),
                    rec.Difference > 0 ? "収入" : "支出",
                    rec.Comment ?? "コメントなし",
                    (aft.LastOrDefault() ?? rec).Total);

                return message.CreateReplyMessage(sb.ToString());
            }
        }

        private async Task<Message> HandlePop(Message message, string text)
        {
            var m = POP.Match(text);
            if (!m.Success)
            {
                return null;
            }
            var sb = new StringBuilder();
            using (var db = new SachikoDbContext())
            {
                if (!await IsAuthorizedAsync(message, db))
                {
                    return await CreateErrorMessage(message, "権限がありません。");
                }

                IQueryable<SachikoRecord> q = db.Records;

                var d = m.Groups["d"].Value;
                if (!string.IsNullOrEmpty(d))
                {
                    DateTime dt;
                    if (!DateTime.TryParse(d, out dt))
                    {
                        return await CreateErrorMessage(message, "日付が無効です。");
                    }

                    var l = dt.Date;
                    var u = dt.AddDays(1);
                    q = q.Where(_ => l <= _.Date && _.Date < u);
                }

                var r = await q.OrderByDescending(_ => _.Date)
                                .ThenByDescending(_ => _.Id)
                                .FirstOrDefaultAsync();
                if (r != null)
                {
                    db.Records.Remove(r);

                    foreach (var aft in await db.Records.Where(_ => _.Date > r.Date || (_.Date == r.Date && _.Id > r.Id)).ToListAsync())
                    {
                        aft.Total -= r.Difference;
                    }

                    await db.SaveChangesAsync();
                    sb.Success().AppendFormat(@"{0:yyyy/MM/dd}の{1:""\""#,0}の{2} ({3})を取り消しました。", r.Date, Math.Abs(r.Difference), r.Difference > 0 ? "収入" : "支出", r.Comment ?? "コメントなし");
                }
                else
                {
                    return await CreateErrorMessage(message, "レコードがありません。");
                }
                return message.CreateReplyMessage(sb.ToString());
            }
        }

        private async Task<Message> HandleCurrent(Message message, string text)
        {
            if (!CURRENT.IsMatch(text))
            {
                return null;
            }
            var sb = new StringBuilder();
            using (var db = new SachikoDbContext())
            {
                var pt = await db.Records
                                    .OrderByDescending(_ => _.Date)
                                    .ThenByDescending(_ => _.Id)
                                    .Select(_ => (long?)_.Total)
                                    .FirstOrDefaultAsync();

                if (pt == null)
                {
                    return await CreateErrorMessage(message, "レコードがありません。");
                }
                else
                {
                    sb.Success().AppendFormat(@"合計金額は{0:""\""#,0}です。", pt);
                }
                return message.CreateReplyMessage(sb.ToString());
            }
        }

        private async Task<Message> HandleHistory(Message message, string text)
        {
            var m = HISTORY.Match(text);
            if (!m.Success)
            {
                return null;
            }
            var sb = new StringBuilder();
            using (var db = new SachikoDbContext())
            {
                var c = m.Groups["c"].Value;
                var cv = string.IsNullOrEmpty(c) ? 10 : Math.Max(int.Parse(c), 1);

                var pt = (await db.Records
                                    .OrderByDescending(_ => _.Date)
                                    .ThenByDescending(_ => _.Id)
                                    .Take(cv)
                                    .ToArrayAsync())
                                    .Reverse().ToArray();

                if (pt.Any())
                {
                    var cs = pt.Select(_ => Math.Abs(_.Difference).ToString(@"""\""#,0")).ToArray();

                    var ml = cs.Max(_ => _.Length);

                    for (var i = 0; i < pt.Length; i++)
                    {
                        var r = pt[i];
                        var s = cs[i];
                        sb.AppendFormat(r.Date.ToString("yyyy/MM/dd"))
                            .Append(' ')
                            .Append(r.Difference > 0 ? "収入" : "支出")
                            .Append(' ', ml - s.Length + 1)
                            .Append(s)
                            .Append(' ')
                            .Append(r.Comment)
                            .NewLine();
                    }
                    sb.NewLine();
                    sb.Success().AppendFormat(@"合計金額は{0:""\""#,0}です。", pt.Last().Total);
                }
                else
                {
                    return await CreateErrorMessage(message, "レコードがありません。");
                }
                return message.CreateReplyMessage(sb.ToString());
            }
        }

        private async Task<Message> HandleShowAdmin(Message message, string text)
        {
            var m = SHOW_ADMIN.Match(text);
            if (!m.Success)
            {
                return null;
            }

            var sb = new StringBuilder();
            using (var db = new SachikoDbContext())
            {
                var l = await db.Admins.ToListAsync();

                if (!l.Any())
                {
                    l.Add(new SachikoAdmin()
                    {
                        Name = message.From.Name
                    });

                    db.Admins.Add(l.Last());

                    await db.SaveChangesAsync();
                }

                foreach (var a in l)
                {
                    sb.Append("* ").Append(a.Name).NewLine();
                }

                return message.CreateReplyMessage(sb.ToString());
            }
        }

        private async Task<Message> HandleAddAdmin(Message message, string text)
        {
            var m = ADD_ADMIN.Match(text);
            if (!m.Success)
            {
                return null;
            }

            var sb = new StringBuilder();
            using (var db = new SachikoDbContext())
            {
                if (!await IsAuthorizedAsync(message, db))
                {
                    return await CreateErrorMessage(message, "権限がありません。");
                }

                var u = m.Groups[1].Value;

                if (!await db.Admins.AnyAsync(_ => _.Name == u))
                {
                    db.Admins.Add(new SachikoAdmin()
                    {
                        Name = u
                    });
                }

                await db.SaveChangesAsync();

                return await CreateErrorMessage(message, null);
            }
        }
        private async Task<Message> HandleDelAdmin(Message message, string text)
        {
            var m = DEL_ADMIN.Match(text);
            if (!m.Success)
            {
                return null;
            }

            var sb = new StringBuilder();
            using (var db = new SachikoDbContext())
            {
                if (!await IsAuthorizedAsync(message, db))
                {
                    return await CreateErrorMessage(message, "権限がありません。");
                }

                var u = m.Groups[1].Value;

                db.Admins.RemoveRange(await db.Admins.Where(_ => _.Name == u).ToListAsync());

                await db.SaveChangesAsync();

                return await CreateErrorMessage(message, null);
            }
        }

        private Task<bool> IsAuthorizedAsync(Message message, SachikoDbContext db)
            => db.Admins.AnyAsync(_ => _.Name == message.From.Name);

        private async Task<Message> CreateErrorMessage(Message message, string error)
        {
            var sb = new StringBuilder();

            if (!string.IsNullOrEmpty(error))
            {
                sb.Error().Append(error);
            }

            await AppendSachiko(sb);

            return message.CreateReplyMessage(sb.ToString());
        }

        private async Task AppendSachiko(StringBuilder sb)
        {
            var img = await (MessagesController.GetCommands()
                                    .OfType<IdolMasterCommand>()
                                    .FirstOrDefault()
                    ?? new IdolMasterCommand()).GetRandomImageAsync("輿水幸子");
            if (img != null)
            {
                if (sb.Length > 0)
                {
                    sb.NewLine();
                }
                sb.Append(img.ImageUrl).Append('#').Append(DateTime.Now.Ticks);
            }
        }
    }
}