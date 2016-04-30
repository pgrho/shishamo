using HtmlAgilityPack;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;

namespace Shipwreck.SlackCSharpBot.Controllers.Sachiko
{
    internal sealed class IdolMasterCommand : NamedMessageCommand
    {
        public IdolMasterCommand()
            : base("imascg", "アイマス シンデレラガールズ")
        {
        }

        protected override async Task<Message> ExecuteAsyncCore(Message message, string text)
        {
            var tr = text.Trim();
            var sp = tr.Split(new[] { ' ' }, 2);

            var q = sp.FirstOrDefault();
            var r = sp.ElementAtOrDefault(1) ?? string.Empty;

            var rarity = r.StartsWith("n", StringComparison.InvariantCultureIgnoreCase) ? "Normal"
                            : r.StartsWith("r", StringComparison.InvariantCultureIgnoreCase) ? "Rare"
                            : r.StartsWith("sr", StringComparison.InvariantCultureIgnoreCase) ? "SRare"
                            : "";

            var isPlus = rarity == "" ? (bool?)null : r.EndsWith("+");

            var img = await GetRandomImageAsync(q, rarity: rarity, isPlus: isPlus);

            if (img != null)
            {
                return message.CreateReplyMessage($"{img.ImageUrl}#{DateTime.Now.Ticks}");
            }

            return message.CreateReplyMessage("該当する画像が見つかりませんでした。");
        }

        internal async Task<IdolImage> GetRandomImageAsync(string query, string rarity = null, bool? isPlus = null)
        {
            var l = await GetImageAsync(headline: query, rarity: rarity, isPlus: isPlus);

            if (!l.Any())
            {
                l = await GetImageAsync(kana: query, kanaOperator: "Contains", rarity: rarity, isPlus: isPlus);
            }

            if (l.Any())
            {
                return l[new Random().Next(l.Count)];
            }

            return null;
        }

        public async Task<IReadOnlyList<IdolImage>> GetImageAsync(string headline = null, string headlineOperator = null, string kana = null, string kanaOperator = null, string rarity = null, bool? isPlus = null)
        {
            var u = $"http://shipwreck.jp/imascg/Image/Search?headline={headline}&headlineOperator={headlineOperator}&kana={kana}&kanaOperator={kanaOperator}&rarity={rarity}&isPlus={isPlus}&count=32";

            var req = (HttpWebRequest)WebRequest.Create(u);

            using (var res = await req.GetResponseAsync())
            using (var s = res.GetResponseStream())
            using (var sr = new StreamReader(s, Encoding.UTF8))
            {
                var t = await sr.ReadToEndAsync();
                var r = new JavaScriptSerializer().Deserialize<IdolImageResult>(t);

                return r.Items;
            }
        }
    }
}