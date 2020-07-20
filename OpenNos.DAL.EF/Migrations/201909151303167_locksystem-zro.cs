namespace OpenNos.DAL.EF.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class locksystemzro : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Character", "LockCode", c => c.String());
            AddColumn("dbo.Character", "VerifiedLock", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Character", "VerifiedLock");
            DropColumn("dbo.Character", "LockCode");
        }
    }
}
