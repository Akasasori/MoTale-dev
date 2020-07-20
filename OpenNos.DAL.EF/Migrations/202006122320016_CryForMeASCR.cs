namespace OpenNos.DAL.EF.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CryForMeASCR : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Character", "ArenaDie", c => c.Long(nullable: false));
            AddColumn("dbo.Character", "ArenaKill", c => c.Long(nullable: false));
            AddColumn("dbo.Character", "ArenaTc", c => c.Long(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Character", "ArenaTc");
            DropColumn("dbo.Character", "ArenaKill");
            DropColumn("dbo.Character", "ArenaDie");
        }
    }
}
