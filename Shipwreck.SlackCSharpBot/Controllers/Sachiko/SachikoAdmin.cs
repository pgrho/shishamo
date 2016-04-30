using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Bot.Connector;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace Shipwreck.SlackCSharpBot.Controllers.Sachiko
{
    internal sealed class SachikoAdmin
    {
        [Key]
        public string Name { get; set; }
    }
}