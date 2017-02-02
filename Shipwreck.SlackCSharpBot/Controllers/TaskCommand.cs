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
    internal sealed class TaskCommand : NamedMessageCommand
    {
        private static readonly Regex HELP = new Regex(@"^\s*help(\s+|$)", RegexOptions.IgnoreCase);
        private static readonly Regex ADD = new Regex(@"^\s*(add|create)\s+((?<u>\S+)\s)?(?<d>.*)$", RegexOptions.IgnoreCase);
        private static readonly Regex UPDATE = new Regex(@"^\s*update\s+(?<t>[0-2]?[0-9]{1,8})\s+(?<d>.*)$", RegexOptions.IgnoreCase);
        private static readonly Regex LIST = new Regex(@"^\s*list(\s+(?<u>\S*))?\s*$", RegexOptions.IgnoreCase);
        private static readonly Regex DELETE = new Regex(@"^\s*(?<c>done|delete|remove)\s+(?<t>[0-2]?[0-9]{1,8})\s*$", RegexOptions.IgnoreCase);

        public TaskCommand()
            : base("task", "タスク管理")
        {
        }

        protected override async Task<HttpResponseMessage> ExecuteAsyncCore(Activity activity, string text)
        {
            return await HandleHelpAsync(activity, text)
                    ?? await HandleAddAsync(activity, text)
                    ?? await HandleListAsync(activity, text)
                    ?? await HandleDeleteAsync(activity, text)
                    ?? await HandleUpdateAsync(activity, text)
                    ?? await activity.ReplyToAsync("コマンドが無効です。");
        }

        private Task<HttpResponseMessage> HandleHelpAsync(Activity activity, string text)
        {
            var m = HELP.Match(text);

            if (!m.Success)
            {
                return Task.FromResult<HttpResponseMessage>(null);
            }

            var sb = new StringBuilder();
            sb.Append("タスク管理コマンド").NewLine();
            sb.NewLine();
            sb.Append(" * add [user] description").NewLine();
            sb.Append(" * update task description").NewLine();
            sb.Append(" * list [user]").NewLine();
            sb.Append(" * done task").NewLine();
            sb.Append(" * delete task").NewLine();

            return activity.ReplyToAsync(sb.ToString());
        }

        private async Task<HttpResponseMessage> HandleAddAsync(Activity activity, string text)
        {
            var m = ADD.Match(text);
            if (!m.Success)
            {
                return null;
            }

            var ug = m.Groups["u"];

            string uid;
            var d = m.Groups["d"].Value;
            if (ug.Success)
            {
                uid = ug.Value;

                if (activity.GetMentions()?.Any(_ => _.Mentioned.Name.Equals(uid, StringComparison.InvariantCultureIgnoreCase)) == false)
                {
                    uid = activity.From.Name;
                    d = text.Substring(ug.Index);
                }
            }
            else
            {
                uid = activity.From.Name;
            }

            using (var db = new ShishamoDbContext())
            {
                var t = new TaskRecord()
                {
                    UserName = uid,
                    Description = d,
                    CreatedAt = DateTime.Now
                };
                db.Tasks.Add(t);

                await db.SaveChangesAsync();

                return await activity.ReplyToAsync(StringBuilderHelper.SUCCESS + $"ユーザー'{uid}'のタスク'{t.Id}'を追加しました。");
            }
        }

        private async Task<HttpResponseMessage> HandleUpdateAsync(Activity activity, string text)
        {
            var m = UPDATE.Match(text);
            if (!m.Success)
            {
                return null;
            }

            var tid = int.Parse(m.Groups["t"].Value);

            using (var db = new ShishamoDbContext())
            {
                var e = await db.Tasks.FirstOrDefaultAsync(_ => _.Id == tid && !_.IsDone && !_.IsDeleted);

                if (e == null)
                {
                    return await activity.ReplyToAsync(StringBuilderHelper.WARNING + "該当するタスクが存在しません。");
                }

                e.Description = m.Groups["d"].Value;
                await db.SaveChangesAsync();

                return await activity.ReplyToAsync(StringBuilderHelper.SUCCESS + $"タスク'{tid}'を更新しました。");
            }
        }

        private async Task<HttpResponseMessage> HandleListAsync(Activity activity, string text)
        {
            var m = LIST.Match(text);
            if (!m.Success)
            {
                return null;
            }
            var ug = m.Groups["u"];
            var uid = ug.Success ? ug.Value : activity.From.Name;

            using (var db = new ShishamoDbContext())
            {
                var l = await db.Tasks.Where(_ => _.UserName == uid && !_.IsDeleted && !_.IsDone).ToListAsync();
                if (l.Any())
                {
                    var sb = new StringBuilder();
                    foreach (var t in l)
                    {
                        sb.Append(" * ").Append(t.Id).Append(' ').Append(t.Description).NewLine();
                    }
                    return await activity.ReplyToAsync(sb.ToString());
                }
                else
                {
                    return await activity.ReplyToAsync(StringBuilderHelper.WARNING + $"ユーザー'{uid}'のタスクはありません。");
                }
            }
        }

        private async Task<HttpResponseMessage> HandleDeleteAsync(Activity activity, string text)
        {
            var m = DELETE.Match(text);
            if (!m.Success)
            {
                return null;
            }

            var tid = int.Parse(m.Groups["t"].Value);

            using (var db = new ShishamoDbContext())
            {
                var e = await db.Tasks.FirstOrDefaultAsync(_ => _.Id == tid && !_.IsDone && !_.IsDeleted);

                if (e == null)
                {
                    return await activity.ReplyToAsync(StringBuilderHelper.WARNING + "該当するタスクが存在しません。");
                }

                if ("DONE".Equals(m.Groups["c"].Value, StringComparison.InvariantCultureIgnoreCase))
                {
                    e.IsDone = true;
                    e.DoneAt = DateTime.Now;

                    await db.SaveChangesAsync();

                    return await activity.ReplyToAsync(StringBuilderHelper.SUCCESS + $"タスク'{tid}'を完了しました。");
                }
                else
                {
                    e.IsDeleted = true;

                    await db.SaveChangesAsync();

                    return await activity.ReplyToAsync(StringBuilderHelper.SUCCESS + $"タスク'{tid}'を削除しました。");
                }
            }
        }
    }
}