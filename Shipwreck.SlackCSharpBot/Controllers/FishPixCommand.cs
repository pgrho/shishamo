using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Bot.Connector;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Web.Script.Serialization;
using System.Text;

namespace Shipwreck.SlackCSharpBot.Controllers
{
    internal sealed class FishPixCommand : RegexMessageCommand
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
            : base(@"^\s*(魚類?(写真)?(資料)?(|DB|データベース)|sakana|fish)\s+")
        {
        }

        protected override async Task<Message> ExecuteAsyncCore(Message message, string text)
        {
            var n = (text ?? string.Empty).Trim();
            var f = (await GetImage(n, MatchOperator.Equal))
                    ?? (await GetImage(n, MatchOperator.EndsWith))
                    ?? (await GetImage(n, MatchOperator.Contains));

            if (f == null)
            {
                return message.CreateReplyMessage("該当する:fish:が見つかりませんでした。");
            }
            return message.CreateReplyMessage($":fish:和名: {f.JapaneseName}\n\n:fish:学名: {f.LatinName}\n\n{f.ImageUrl}#{DateTime.Now.Ticks}");
        }

        private async static Task<FishImage> GetImage(string name, MatchOperator @operator)
        {
            var u = Uri.EscapeUriString($"http://shipwreck.jp/fishpix?name={name}&nameOperator={@operator}");

            var req = (HttpWebRequest)WebRequest.Create(u);

            using (var res = await req.GetResponseAsync())
            using (var s = res.GetResponseStream())
            using (var sr = new StreamReader(s, Encoding.UTF8))
            {
                var t = await sr.ReadToEndAsync();
                var r = new JavaScriptSerializer().Deserialize<FishImageResult>(t);

                if (r.Items.Any())
                {
                    var rd = new Random();
                    return r.Items.OrderBy(_ => rd.Next()).FirstOrDefault();
                }
                return null;
            }
        }
    }
}