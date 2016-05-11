namespace Shipwreck.SlackCSharpBot.Models.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddSachikoOtsukai : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.SachikoOtsukais",
                c => new
                    {
                        Asin = c.String(nullable: false, maxLength: 10, fixedLength: true, unicode: false),
                        Quantity = c.Byte(nullable: false),
                        Price = c.Int(nullable: false),
                        Title = c.String(),
                    })
                .PrimaryKey(t => t.Asin);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.SachikoOtsukais");
        }
    }
}
