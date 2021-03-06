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

namespace Shipwreck.SlackCSharpBot.Models
{
    internal sealed class IdolImage
    {
        public string Headline { get; set; }
        public string Name { get; set; }
        public string ImageUrl { get; set; }

        public string IconImageUrl { get; set; }
    }
}