using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Bot.Connector;
using System.Net;
using System.Runtime.Serialization;
using System.IO;
using System.Text;
using System.Net.Http;

namespace Shipwreck.SlackCSharpBot.Controllers
{
    internal sealed class FishPixCommand : NamedMessageCommand
    {
        public enum MatchOperator
        {
            Equal,
            Contains,
            StartsWith,
            EndsWith
        }

        [DataContract]
        public class FishImageResult
        {
            [DataMember]
            public IReadOnlyList<FishImage> Items { get; set; }
        }

        [DataContract]
        public class FishImage
        {
            [DataMember]
            public string Id { get; set; }
            [DataMember]
            public string ImageUrl { get; set; }
            [DataMember]
            public string JapaneseName { get; set; }
            [DataMember]
            public string LatinName { get; set; }
        }

        public FishPixCommand()
            : base("fish", "魚類写真資料データベースの検索を行います。")
        {
        }

        protected override async Task<HttpResponseMessage> ExecuteAsyncCore(Activity activity, string text)
        {
            var n = (text ?? string.Empty).Trim();
            var f = (await GetImage(n, MatchOperator.Equal))
                    ?? (await GetImage(n, MatchOperator.EndsWith))
                    ?? (await GetImage(n, MatchOperator.Contains));

            if (f == null)
            {
                return await activity.ReplyToAsync("該当する:fish:が見つかりませんでした。");
            }
            return await activity.ReplyToAsync($":fish:和名: {f.JapaneseName}\n\n:fish:学名: {f.LatinName}\n\n{f.ImageUrl}#{DateTime.Now.Ticks}");
        }

        private async static Task<FishImage> GetImage(string name, MatchOperator @operator)
        {
            var u = Uri.EscapeUriString($"http://shipwreck.jp/fishpix?name={name}&nameOperator={@operator}");

            var res = await MessagesController.HttpClient.GetAsync(u);

            var r = await res.Content.ReadAsAsync<FishImageResult>();

            if (r.Items.Any())
            {
                var rd = new Random();
                return r.Items.OrderBy(_ => rd.Next()).FirstOrDefault();
            }
            return null;
        }
    }
}