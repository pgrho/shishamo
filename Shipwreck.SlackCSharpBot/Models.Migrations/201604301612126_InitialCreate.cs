namespace Shipwreck.SlackCSharpBot.Models.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.EchoSharpEntries",
                c => new
                    {
                        Name = c.String(nullable: false, maxLength: 64),
                        Pattern = c.String(nullable: false, maxLength: 1024),
                        Command = c.String(nullable: false, maxLength: 2048),
                    })
                .PrimaryKey(t => t.Name);
            
            CreateTable(
                "dbo.SachikoAdmins",
                c => new
                    {
                        Name = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => t.Name);
            
            CreateTable(
                "dbo.SachikoRecords",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Date = c.DateTime(nullable: false),
                        Difference = c.Long(nullable: false),
                        Total = c.Long(nullable: false),
                        Comment = c.String(maxLength: 255),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.SachikoRecords");
            DropTable("dbo.SachikoAdmins");
            DropTable("dbo.EchoSharpEntries");
        }
    }
}
