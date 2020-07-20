namespace OpenNos.DAL.EF.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class BoxItem : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.BoxItem",
                c => new
                    {
                        BoxItemId = c.Long(nullable: false, identity: true),
                        OriginalItemVNum = c.Short(nullable: false),
                        OriginalItemDesign = c.Short(nullable: false),
                        ItemGeneratedAmount = c.Short(nullable: false),
                        ItemGeneratedVNum = c.Short(nullable: false),
                        ItemGeneratedDesign = c.Short(nullable: false),
                        ItemGeneratedRare = c.Byte(nullable: false),
                        ItemGeneratedUpgrade = c.Byte(nullable: false),
                        Probability = c.Byte(nullable: false),
                    })
                .PrimaryKey(t => t.BoxItemId);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.BoxItem");
        }
    }
}
