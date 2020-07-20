namespace OpenNos.DAL.EF.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Aphrodite : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Portal", "LevelRequired", c => c.Short(nullable: false));
            AddColumn("dbo.Portal", "HeroLevelRequired", c => c.Short(nullable: false));
            AddColumn("dbo.Portal", "RequiredItem", c => c.Short(nullable: false));
            AddColumn("dbo.Portal", "ItemName", c => c.String());
            AddColumn("dbo.Portal", "OpenAt", c => c.DateTime(nullable: false));
            AddColumn("dbo.Portal", "CloseAt", c => c.DateTime(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Portal", "CloseAt");
            DropColumn("dbo.Portal", "OpenAt");
            DropColumn("dbo.Portal", "ItemName");
            DropColumn("dbo.Portal", "RequiredItem");
            DropColumn("dbo.Portal", "HeroLevelRequired");
            DropColumn("dbo.Portal", "LevelRequired");
        }
    }
}
