using HtmlAgilityPack;
using Microsoft.Bot.Connector;
using Shipwreck.SlackCSharpBot.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;

namespace Shipwreck.SlackCSharpBot.Controllers
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
            var sp = tr.Split(new[] { ' ' }, 3);

            if (sp.FirstOrDefault() == "-l")
            {
                var q = sp.ElementAtOrDefault(1) ?? string.Empty;
                var r = sp.ElementAtOrDefault(2) ?? string.Empty;

                var rarity = GetRarity(r);
                var isPlus = IsPlus(r, rarity);

                var l = await GetImageAsync(headline: q, rarity: rarity, isPlus: isPlus);

                if (!l.Any())
                {
                    l = await GetImageAsync(kana: q, kanaOperator: "Contains", rarity: rarity, isPlus: isPlus);
                }

                if (l.Any())
                {
                    var sb = new StringBuilder();

                    foreach (var img in l)
                    {
                        sb.Append(img.Headline).NewLine();
                        sb.Append(img.ImageUrl).NewLine();
                    }

                    return message.CreateReplyMessage(sb.ToString());
                }
                else
                {
                    return message.CreateReplyMessage("該当する画像が見つかりませんでした。");
                }
            }
            else
            {
                var q = sp.FirstOrDefault();
                var r = sp.ElementAtOrDefault(1) ?? string.Empty;

                var rarity = GetRarity(r);
                var isPlus = IsPlus(r, rarity);

                var img = await GetRandomImageAsync(q, rarity: rarity, isPlus: isPlus);

                if (img != null)
                {
                    return message.CreateReplyMessage($"{img.ImageUrl}#{DateTime.Now.Ticks}");
                }

                return message.CreateReplyMessage("該当する画像が見つかりませんでした。");
            }
        }

        private static string GetRarity(string r)
        {
            return r.StartsWith("n", StringComparison.InvariantCultureIgnoreCase) ? "Normal"
                            : r.StartsWith("r", StringComparison.InvariantCultureIgnoreCase) ? "Rare"
                            : r.StartsWith("sr", StringComparison.InvariantCultureIgnoreCase) ? "SRare"
                            : "";
        }

        private static bool? IsPlus(string r, string rarity)
        {
            return rarity == "" ? (bool?)null : r.EndsWith("+");
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

            using (var hc = new HttpClient())
            {
                var res = await hc.GetAsync(u);
                var ir = await res.Content.ReadAsAsync<IdolImageResult>();

                return ir.Items;
            }
        }
    }
}