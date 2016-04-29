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
        private static readonly Regex PUSH = new Regex(@"^\s*push\s+(?<i>-i\s+)?(-d\s+(?<d>[^\s]+)\s+)?(?<m>[-+]?\d+(,\d+)*)(\s+(?<c>.*))?$", RegexOptions.IgnoreCase);
        private static readonly Regex POP = new Regex(@"^\s*pop(\s+-d\s+(?<d>[^\s]+))?(\s+|$)", RegexOptions.IgnoreCase);
        private static readonly Regex CURRENT = new Regex(@"^\s*(total|current)", RegexOptions.IgnoreCase);
        private static readonly Regex HISTORY = new Regex(@"^\s*(history|list|log)(\s+-c\s+(?<c>\d+))?(\s+|$)", RegexOptions.IgnoreCase);

        public SachikoCommand()
            : base("sachiko", "さちこ")
        {
        }

        protected override async Task<Message> ExecuteAsyncCore(Message message, string text)
        {
            var sb = new StringBuilder();
            using (var db = new SachikoDbContext())
            {
                var m = PUSH.Match(text);
                if (m.Success)
                {
                    var d = m.Groups["d"].Value;
                    var v = m.Groups["m"].Value;
                    var c = m.Groups["c"].Value;
                    var insert = m.Groups["i"].Success;

                    var rec = new SachikoRecord();

                    var lv = rec.Difference = long.Parse(v);

                    if (lv == 0)
                    {
                        sb.Warning().Append("金額に変動がないためキャンセルされました。");
                        return message.CreateReplyMessage(sb.ToString());
                    }
                    var dv = rec.Date = string.IsNullOrEmpty(d) ? DateTime.Now : DateTime.Parse(d);

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

                m = POP.Match(text);
                if (m.Success)
                {
                    IQueryable<SachikoRecord> q = db.Records;

                    var d = m.Groups["d"].Value;
                    if (!string.IsNullOrEmpty(d))
                    {
                        var dt = DateTime.Parse(d);
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
                        sb.Warning().Append("レコードがありません。");
                    }
                    return message.CreateReplyMessage(sb.ToString());
                }

                if (CURRENT.IsMatch(text))
                {
                    var pt = await db.Records
                                        .OrderByDescending(_ => _.Date)
                                        .ThenByDescending(_ => _.Id)
                                        .Select(_ => (long?)_.Total)
                                        .FirstOrDefaultAsync();

                    if (pt == null)
                    {
                        sb.Warning().Append("レコードがありません。");
                    }
                    else
                    {
                        sb.Success().AppendFormat(@"合計金額は{0:""\""#,0}です。", pt);
                    }
                    return message.CreateReplyMessage(sb.ToString());
                }

                m = HISTORY.Match(text);
                if (m.Success)
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
                        sb.Warning().Append("レコードがありません。");
                    }
                    return message.CreateReplyMessage(sb.ToString());
                }

                var url = await (MessagesController.GetCommands()
                                                    .OfType<IdolMasterCommand>()
                                                    .FirstOrDefault()
                                    ?? new IdolMasterCommand()).GetIdolImageUrlAsync("輿水幸子");

                if (url != null)
                {
                    return message.CreateReplyMessage($"{StringBuilderHelper.ERROR}{url}#{DateTime.Now.Ticks}");
                }

                return null;
            }
        }
    }
}