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

namespace Shipwreck.SlackCSharpBot.Controllers.Sachiko
{
    internal sealed class IdolMasterCommand : NamedMessageCommand
    {
        private class Idol
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Kana { get; set; }

            //    public string[] ImageTokens { get; set; }

            public IdolImage PickRandom(string rarity = null)
            {
                var isNormal = rarity?.StartsWith("n", StringComparison.InvariantCultureIgnoreCase) == true;
                var isRare = rarity?.StartsWith("r", StringComparison.InvariantCultureIgnoreCase) == true;
                var isSRare = rarity?.StartsWith("sr", StringComparison.InvariantCultureIgnoreCase) == true;
                var isPlus = rarity?.EndsWith("+");

                var ary = Images.Where(_ => (!isNormal || _.Rarity?.Equals("normal", StringComparison.InvariantCultureIgnoreCase) == true)
                                  && (!isRare || _.Rarity?.Equals("rare", StringComparison.InvariantCultureIgnoreCase) == true)
                                  && (!isSRare || _.Rarity?.Equals("srare", StringComparison.InvariantCultureIgnoreCase) == true)
                                  && (isPlus == null || _.Headline?.EndsWith("+") == isPlus)).ToArray();

                if (ary.Any())
                {
                    var img = ary[new Random().Next(ary.Length)];

                    return img;
                }

                return null;
            }

            public IdolImage[] Images { get; set; }

            public async Task InitImageTokensAsync()
            {
                var doc = new HtmlDocument();

                using (var wc = new WebClient())
                {
                    var data = await wc.DownloadDataTaskAsync("http://gamedb.squares.net/idolmaster/idol.php?id=" + Id);

                    using (var ms = new MemoryStream(data))
                    using (var sr = new StreamReader(ms, Encoding.UTF8))
                    {
                        doc.Load(sr);
                    }
                }

                var imgs = new List<IdolImage>();
                foreach (var a in doc.DocumentNode.Descendants("a").Where(a => a.GetAttributeValue("class", "") == "swap-card"))
                {
                    var div = a.ParentNode.ParentNode;

                    var inftable = div.Descendants("table").Where(_ => _.GetAttributeValue("class", "") == "bcinf").FirstOrDefault(_ => _.Descendants("th").Any(h => h.InnerText.Contains("レア度")));

                    var tr1 = inftable.Descendants("tr").ElementAt(1).Descendants("td").ToArray();
                    var tr3 = inftable.Descendants("tr").ElementAt(3).Descendants("td").ToArray();

                    var img = new IdolImage()
                    {
                        Headline = div.Descendants("h2").FirstOrDefault(_ => _.GetAttributeValue("class", "") == "headline")?.InnerText?.Trim(),
                        Hash = a.GetAttributeValue("href", "").Split('=').Last(),

                        Rarity = tr1[0].InnerText,
                        Type = tr1[1].InnerText,
                        BloodType = tr1[2].InnerText,
                        Height = tr1[3].InnerText,
                        Weight = tr1[4].InnerText,
                        ThreeSize = tr1[5].InnerText,

                        Age = tr3[0].InnerText,
                        Birthday = tr3[1].InnerText,
                        SunSign = tr3[2].InnerText,
                        Birthplace = tr3[3].InnerText,
                        Hobby = tr3[4].InnerText,
                        Handedness = tr3[5].InnerText,
                    };

                    imgs.Add(img);
                }

                Images = imgs.ToArray();
            }
        }

        private class IdolImage
        {
            public string Headline { get; set; }

            public string Hash { get; set; }

            public string Rarity { get; set; }
            public string Type { get; set; }

            public string BloodType { get; set; }

            public string Height { get; set; }

            public string Weight { get; set; }

            public string ThreeSize { get; set; }

            public string Age { get; set; }
            public string Birthday { get; set; }
            public string SunSign { get; set; }

            public string Birthplace { get; set; }
            public string Hobby { get; set; }
            public string Handedness { get; set; }

            public string ImageUrl
                => $"http://gamedb.squares.net/idolmaster/image_sp/card/l/{Hash}.jpg";

            public string NoFrameImageUrl
                => $"http://gamedb.squares.net/idolmaster/image_sp/card/l_noframe/{Hash}.jpg";

            public string QuestImageUrl
                => $"http://gamedb.squares.net/idolmaster/image_sp/card/quest/{Hash}.jpg";

            public string LSImageUrl
                => $"http://gamedb.squares.net/idolmaster/image_sp/card/ls/{Hash}.jpg";

            public string XSImageUrl
                => $"http://gamedb.squares.net/idolmaster/image_sp/card/xs/{Hash}.jpg";
        }

        private static Idol[] Idols;

        public IdolMasterCommand()
            : base("imascg", "アイマス シンデレラガールズ")
        {
        }

        protected override async Task<Message> ExecuteAsyncCore(Message message, string text)
        {
            var tr = text.Trim();
            var sp = tr.Split(new[] { ' ' }, 2);
            var url = await GetIdolImageUrlAsync(sp.FirstOrDefault(), sp.ElementAtOrDefault(1));
            if (url != null)
            {
                return message.CreateReplyMessage($"{url}#{DateTime.Now.Ticks}");
            }
            return message.CreateReplyMessage("該当する画像が見つかりませんでした。");
        }

        public async Task<string> GetIdolImageUrlAsync(string name, string rarity = null)
        {
            if (Idols == null)
            {
                await InitIdolsAsync();
            }

            var n = Idols.FirstOrDefault(_ => _.Name == name)
                    ?? Idols.FirstOrDefault(_ => _.Kana == name)
                    ?? Idols.FirstOrDefault(_ => _.Name.Contains(name))
                    ?? Idols.FirstOrDefault(_ => _.Kana.Contains(name));

            if (n != null)
            {
                if (n.Images == null)
                {
                    await n.InitImageTokensAsync();
                }

                var img = n.PickRandom(rarity);

                if (img != null)
                {
                    return img.ImageUrl;
                }
            }

            return null;
        }

        private async Task InitIdolsAsync()
        {
            var doc = new HtmlDocument();
            using (var wc = new WebClient())
            {
                var data = await wc.DownloadDataTaskAsync("http://gamedb.squares.net/idolmaster/?name=&attr=all&age=all&height=all&weight=all&blood=all&arm=all");

                using (var ms = new MemoryStream(data))
                using (var sr = new StreamReader(ms, Encoding.UTF8))
                {
                    doc.Load(sr);
                }
            }

            var idols = new List<Idol>();
            foreach (var anchor in doc.DocumentNode.Descendants("a"))
            {
                if (!anchor.GetAttributeValue("class", "").Contains("idol-link"))
                {
                    continue;
                }

                var href = anchor.GetAttributeValue("href", string.Empty);

                var li = anchor.Ancestors("li").First();

                var nameValue = li.GetAttributeValue("n", string.Empty);

                var m = Regex.Match(nameValue, @"^(\[[^\]]+\])?(?<n>[^\s]+)\s*(?<k>.+)$");

                int id;
                var ng = m.Groups["n"];
                var kg = m.Groups["k"];

                if (int.TryParse(href.Split('=').Last(), out id) && ng.Success && kg.Success)
                {
                    idols.Add(new Idol()
                    {
                        Id = id,
                        Name = ng.Value,
                        Kana = kg.Value
                    });
                }
            }

            Idols = idols.ToArray();
        }
    }
}