using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace Shipwreck.SlackCSharpBot.Models
{
    internal sealed class ShishamoDbContext : DbContext
    {
        public DbSet<EchoSharpEntry> EchoSharpEntries { get; set; }

        public DbSet<SachikoAdmin> SachikoAdmins { get; set; }

        public DbSet<SachikoRecord> SachikoRecords { get; set; }
    }
}