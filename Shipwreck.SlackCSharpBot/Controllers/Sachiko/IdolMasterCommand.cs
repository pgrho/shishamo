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

            public string[] ImageTokens { get; set; }

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

                ImageTokens = doc.DocumentNode.Descendants("a").Where(a => a.GetAttributeValue("class", "").Contains("swap-card")).Select(a => a.GetAttributeValue("href", "").Split('=').Last()).ToArray();
            }
        }

        private static Idol[] Idols;

        public IdolMasterCommand()
            : base("imascg", "アイマス シンデレラガールズ")
        {
        }

        protected override async Task<Message> ExecuteAsyncCore(Message message, string text)
        {
            var url = await GetIdolImageUrlAsync(text.Trim());
            if (url != null)
            {
                return message.CreateReplyMessage($"{url}#{DateTime.Now.Ticks}");
            }
            return null;
        }

        public async Task<string> GetIdolImageUrlAsync(string name)
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
                if (n.ImageTokens == null)
                {
                    await n.InitImageTokensAsync();
                }
                if (n.ImageTokens.Any())
                {
                    var id = n.ImageTokens[new Random().Next(n.ImageTokens.Length)];

                    return $"http://gamedb.squares.net/idolmaster/image_sp/card/l/{id}.jpg";
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