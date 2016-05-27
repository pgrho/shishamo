using Shipwreck.SlackCSharpBot.Models.Migrations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace Shipwreck.SlackCSharpBot.Models
{
    internal sealed class ShishamoDbContext : DbContext
    {
        static ShishamoDbContext()
        {
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<ShishamoDbContext, Configuration>());
        }

        public DbSet<EchoSharpEntry> EchoSharpEntries { get; set; }

        public DbSet<SachikoAdmin> SachikoAdmins { get; set; }

        public DbSet<SachikoRecord> SachikoRecords { get; set; }

        public DbSet<SachikoOtsukai> SachikoOtsukai { get; set; }

        public DbSet<TaskRecord> Tasks { get; set; }
    }
}