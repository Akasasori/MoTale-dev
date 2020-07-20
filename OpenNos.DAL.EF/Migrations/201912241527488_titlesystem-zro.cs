namespace OpenNos.DAL.EF.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class titlesystemzro : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CharacterTitles",
                c => new
                {
                    TitleKey = c.Long(nullable: false, identity: true),
                    CharacterId = c.Long(nullable: false),
                    TitleId = c.Int(nullable: false),
                })
                .PrimaryKey(t => t.TitleKey);

            AddColumn("dbo.Character", "VisTit", c => c.Short(nullable: false));
            AddColumn("dbo.Character", "EffTit", c => c.Short(nullable: false));

        }

        public override void Down()
        {
            DropColumn("dbo.Character", "EffTit");
            DropColumn("dbo.Character", "VisTit");
            DropTable("dbo.CharacterTitles");
        }
    }
}
