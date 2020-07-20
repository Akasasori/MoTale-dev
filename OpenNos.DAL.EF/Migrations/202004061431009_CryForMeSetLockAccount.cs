namespace OpenNos.DAL.EF.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CryForMeSetLockAccount : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Account", "LockCode", c => c.String());
            AddColumn("dbo.Account", "VerifiedLock", c => c.Boolean(nullable: false));
            DropColumn("dbo.Character", "LockCode");
            DropColumn("dbo.Character", "VerifiedLock");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Character", "VerifiedLock", c => c.Boolean(nullable: false));
            AddColumn("dbo.Character", "LockCode", c => c.String());
            DropColumn("dbo.Account", "VerifiedLock");
            DropColumn("dbo.Account", "LockCode");
        }
    }
}
