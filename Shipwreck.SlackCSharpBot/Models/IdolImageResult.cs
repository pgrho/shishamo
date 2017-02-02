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
    internal sealed class IdolImageResult
    {
        public List<IdolImage> Items { get; set; }
    }
}