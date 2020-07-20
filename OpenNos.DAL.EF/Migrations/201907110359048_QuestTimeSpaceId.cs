namespace OpenNos.DAL.EF.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class QuestTimeSpaceId : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ScriptedInstance", "QuestTimeSpaceId", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.ScriptedInstance", "QuestTimeSpaceId");
        }
    }
}
