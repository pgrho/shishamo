namespace Shipwreck.SlackCSharpBot.Models.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddLastUpdatedAt : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.SachikoOtsukais", "LastUpdatedAt", c => c.DateTime());
        }
        
        public override void Down()
        {
            DropColumn("dbo.SachikoOtsukais", "LastUpdatedAt");
        }
    }
}
