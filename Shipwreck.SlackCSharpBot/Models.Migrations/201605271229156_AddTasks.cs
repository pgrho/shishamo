namespace Shipwreck.SlackCSharpBot.Models.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddTasks : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.TaskRecords",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        UserName = c.String(nullable: false, maxLength: 32, unicode: false),
                        Description = c.String(nullable: false, maxLength: 255),
                        CreatedAt = c.DateTime(nullable: false, storeType: "date"),
                        DoneAt = c.DateTime(storeType: "date"),
                        IsDone = c.Boolean(nullable: false),
                        IsDeleted = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.TaskRecords");
        }
    }
}
