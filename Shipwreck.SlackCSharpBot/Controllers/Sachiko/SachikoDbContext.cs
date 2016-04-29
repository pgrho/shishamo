using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace Shipwreck.SlackCSharpBot.Controllers.Sachiko
{
    internal sealed class SachikoDbContext : DbContext
    {
        public DbSet<SachikoRecord> Records { get; set; }
    }
}