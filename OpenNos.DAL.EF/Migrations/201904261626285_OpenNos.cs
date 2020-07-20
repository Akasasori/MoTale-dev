namespace OpenNos.DAL.EF.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class OpenNos : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Account",
                c => new
                    {
                        AccountId = c.Long(nullable: false, identity: true),
                        Authority = c.Short(nullable: false),
                        Email = c.String(maxLength: 255),
                        Name = c.String(maxLength: 255),
                        Password = c.String(maxLength: 255, unicode: false),
                        ReferrerId = c.Long(nullable: false),
                        RegistrationIP = c.String(maxLength: 45),
                        VerificationToken = c.String(maxLength: 32),
                        DailyRewardSent = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.AccountId);
            
            CreateTable(
                "dbo.Character",
                c => new
                    {
                        CharacterId = c.Long(nullable: false, identity: true),
                        AccountId = c.Long(nullable: false),
                        Act4Dead = c.Int(nullable: false),
                        Act4Kill = c.Int(nullable: false),
                        Act4Points = c.Int(nullable: false),
                        ArenaWinner = c.Int(nullable: false),
                        Biography = c.String(maxLength: 255),
                        BuffBlocked = c.Boolean(nullable: false),
                        Class = c.Byte(nullable: false),
                        Compliment = c.Short(nullable: false),
                        Dignity = c.Single(nullable: false),
                        EmoticonsBlocked = c.Boolean(nullable: false),
                        ExchangeBlocked = c.Boolean(nullable: false),
                        Faction = c.Byte(nullable: false),
                        FamilyRequestBlocked = c.Boolean(nullable: false),
                        FriendRequestBlocked = c.Boolean(nullable: false),
                        Gender = c.Byte(nullable: false),
                        Gold = c.Long(nullable: false),
                        GoldBank = c.Long(nullable: false),
                        GroupRequestBlocked = c.Boolean(nullable: false),
                        HairColor = c.Byte(nullable: false),
                        HairStyle = c.Byte(nullable: false),
                        HeroChatBlocked = c.Boolean(nullable: false),
                        HeroLevel = c.Byte(nullable: false),
                        HeroXp = c.Long(nullable: false),
                        Hp = c.Int(nullable: false),
                        HpBlocked = c.Boolean(nullable: false),
                        IsPetAutoRelive = c.Boolean(nullable: false),
                        IsPartnerAutoRelive = c.Boolean(nullable: false),
                        IsSeal = c.Boolean(nullable: false),
                        JobLevel = c.Byte(nullable: false),
                        JobLevelXp = c.Long(nullable: false),
                        LastFamilyLeave = c.Long(nullable: false),
                        Level = c.Byte(nullable: false),
                        LevelXp = c.Long(nullable: false),
                        MapId = c.Short(nullable: false),
                        MapX = c.Short(nullable: false),
                        MapY = c.Short(nullable: false),
                        MasterPoints = c.Int(nullable: false),
                        MasterTicket = c.Int(nullable: false),
                        MaxMateCount = c.Byte(nullable: false),
                        MinilandInviteBlocked = c.Boolean(nullable: false),
                        MinilandMessage = c.String(maxLength: 255),
                        MinilandPoint = c.Short(nullable: false),
                        MinilandState = c.Byte(nullable: false),
                        MouseAimLock = c.Boolean(nullable: false),
                        Mp = c.Int(nullable: false),
                        Name = c.String(maxLength: 255, unicode: false),
                        QuickGetUp = c.Boolean(nullable: false),
                        RagePoint = c.Long(nullable: false),
                        Reputation = c.Long(nullable: false),
                        Slot = c.Byte(nullable: false),
                        SpAdditionPoint = c.Int(nullable: false),
                        SpPoint = c.Int(nullable: false),
                        State = c.Byte(nullable: false),
                        TalentLose = c.Int(nullable: false),
                        TalentSurrender = c.Int(nullable: false),
                        TalentWin = c.Int(nullable: false),
                        WhisperBlocked = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.CharacterId)
                .ForeignKey("dbo.Map", t => t.MapId)
                .ForeignKey("dbo.Account", t => t.AccountId)
                .Index(t => t.AccountId)
                .Index(t => t.MapId);
            
            CreateTable(
                "dbo.BazaarItem",
                c => new
                    {
                        BazaarItemId = c.Long(nullable: false, identity: true),
                        Amount = c.Short(nullable: false),
                        DateStart = c.DateTime(nullable: false),
                        Duration = c.Short(nullable: false),
                        IsPackage = c.Boolean(nullable: false),
                        ItemInstanceId = c.Guid(nullable: false),
                        MedalUsed = c.Boolean(nullable: false),
                        Price = c.Long(nullable: false),
                        SellerId = c.Long(nullable: false),
                    })
                .PrimaryKey(t => t.BazaarItemId)
                .ForeignKey("dbo.Character", t => t.SellerId)
                .ForeignKey("dbo.ItemInstance", t => t.ItemInstanceId)
                .Index(t => t.ItemInstanceId)
                .Index(t => t.SellerId);
            
            CreateTable(
                "dbo.ItemInstance",
                c => new
                    {
                        Id = c.Guid(nullable: false),
                        Amount = c.Int(nullable: false),
                        BazaarItemId = c.Long(),
                        BoundCharacterId = c.Long(),
                        CharacterId = c.Long(nullable: false),
                        Design = c.Short(nullable: false),
                        DurabilityPoint = c.Int(nullable: false),
                        ItemDeleteTime = c.DateTime(),
                        ItemVNum = c.Short(nullable: false),
                        Rare = c.Short(nullable: false),
                        Slot = c.Short(nullable: false),
                        Type = c.Byte(nullable: false),
                        Upgrade = c.Byte(nullable: false),
                        HoldingVNum = c.Short(),
                        ShellRarity = c.Short(),
                        SlDamage = c.Short(),
                        SlDefence = c.Short(),
                        SlElement = c.Short(),
                        SlHP = c.Short(),
                        SpDamage = c.Byte(),
                        SpDark = c.Byte(),
                        SpDefence = c.Byte(),
                        SpElement = c.Byte(),
                        SpFire = c.Byte(),
                        SpHP = c.Byte(),
                        SpLevel = c.Byte(),
                        SpLight = c.Byte(),
                        SpStoneUpgrade = c.Byte(),
                        SpWater = c.Byte(),
                        Ammo = c.Byte(),
                        Cellon = c.Byte(),
                        CloseDefence = c.Short(),
                        Concentrate = c.Short(),
                        CriticalDodge = c.Short(),
                        CriticalLuckRate = c.Byte(),
                        CriticalRate = c.Short(),
                        DamageMaximum = c.Short(),
                        DamageMinimum = c.Short(),
                        DarkElement = c.Byte(),
                        DarkResistance = c.Short(),
                        DefenceDodge = c.Short(),
                        DistanceDefence = c.Short(),
                        DistanceDefenceDodge = c.Short(),
                        ElementRate = c.Short(),
                        EquipmentSerialId = c.Guid(),
                        FireElement = c.Byte(),
                        FireResistance = c.Short(),
                        HitRate = c.Short(),
                        HP = c.Short(),
                        IsEmpty = c.Boolean(),
                        IsFixed = c.Boolean(),
                        IsPartnerEquipment = c.Boolean(),
                        LightElement = c.Byte(),
                        LightResistance = c.Short(),
                        MagicDefence = c.Short(),
                        MaxElementRate = c.Short(),
                        MP = c.Short(),
                        WaterElement = c.Byte(),
                        WaterResistance = c.Short(),
                        XP = c.Long(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Character", t => t.BoundCharacterId)
                .ForeignKey("dbo.Item", t => t.ItemVNum)
                .ForeignKey("dbo.Character", t => t.CharacterId)
                .Index(t => t.BoundCharacterId)
                .Index(t => new { t.CharacterId, t.Slot, t.Type }, name: "IX_SlotAndType")
                .Index(t => t.ItemVNum);
            
            CreateTable(
                "dbo.Item",
                c => new
                    {
                        VNum = c.Short(nullable: false),
                        BasicUpgrade = c.Byte(nullable: false),
                        CellonLvl = c.Byte(nullable: false),
                        Class = c.Byte(nullable: false),
                        CloseDefence = c.Short(nullable: false),
                        Color = c.Byte(nullable: false),
                        Concentrate = c.Short(nullable: false),
                        CriticalLuckRate = c.Byte(nullable: false),
                        CriticalRate = c.Short(nullable: false),
                        DamageMaximum = c.Short(nullable: false),
                        DamageMinimum = c.Short(nullable: false),
                        DarkElement = c.Byte(nullable: false),
                        DarkResistance = c.Short(nullable: false),
                        DefenceDodge = c.Short(nullable: false),
                        DistanceDefence = c.Short(nullable: false),
                        DistanceDefenceDodge = c.Short(nullable: false),
                        Effect = c.Short(nullable: false),
                        EffectValue = c.Int(nullable: false),
                        Element = c.Byte(nullable: false),
                        ElementRate = c.Short(nullable: false),
                        EquipmentSlot = c.Byte(nullable: false),
                        FireElement = c.Byte(nullable: false),
                        FireResistance = c.Short(nullable: false),
                        Height = c.Byte(nullable: false),
                        HitRate = c.Short(nullable: false),
                        Hp = c.Short(nullable: false),
                        HpRegeneration = c.Short(nullable: false),
                        IsBlocked = c.Boolean(nullable: false),
                        IsColored = c.Boolean(nullable: false),
                        IsConsumable = c.Boolean(nullable: false),
                        IsDroppable = c.Boolean(nullable: false),
                        IsHeroic = c.Boolean(nullable: false),
                        IsHolder = c.Boolean(nullable: false),
                        IsMinilandObject = c.Boolean(nullable: false),
                        IsSoldable = c.Boolean(nullable: false),
                        IsTradable = c.Boolean(nullable: false),
                        ItemSubType = c.Byte(nullable: false),
                        ItemType = c.Byte(nullable: false),
                        ItemValidTime = c.Long(nullable: false),
                        LevelJobMinimum = c.Byte(nullable: false),
                        LevelMinimum = c.Byte(nullable: false),
                        LightElement = c.Byte(nullable: false),
                        LightResistance = c.Short(nullable: false),
                        MagicDefence = c.Short(nullable: false),
                        MaxCellon = c.Byte(nullable: false),
                        MaxCellonLvl = c.Byte(nullable: false),
                        MaxElementRate = c.Short(nullable: false),
                        MaximumAmmo = c.Byte(nullable: false),
                        MinilandObjectPoint = c.Int(nullable: false),
                        MoreHp = c.Short(nullable: false),
                        MoreMp = c.Short(nullable: false),
                        Morph = c.Short(nullable: false),
                        Mp = c.Short(nullable: false),
                        MpRegeneration = c.Short(nullable: false),
                        Name = c.String(maxLength: 255),
                        Price = c.Long(nullable: false),
                        SellToNpcPrice = c.Long(nullable: false),
                        PvpDefence = c.Short(nullable: false),
                        PvpStrength = c.Byte(nullable: false),
                        ReduceOposantResistance = c.Short(nullable: false),
                        ReputationMinimum = c.Byte(nullable: false),
                        ReputPrice = c.Long(nullable: false),
                        SecondaryElement = c.Byte(nullable: false),
                        Sex = c.Byte(nullable: false),
                        Speed = c.Byte(nullable: false),
                        SpType = c.Byte(nullable: false),
                        Type = c.Byte(nullable: false),
                        WaitDelay = c.Short(nullable: false),
                        WaterElement = c.Byte(nullable: false),
                        WaterResistance = c.Short(nullable: false),
                        Width = c.Byte(nullable: false),
                    })
                .PrimaryKey(t => t.VNum);
            
            CreateTable(
                "dbo.BCard",
                c => new
                    {
                        BCardId = c.Int(nullable: false, identity: true),
                        CardId = c.Short(),
                        CastType = c.Byte(nullable: false),
                        FirstData = c.Int(nullable: false),
                        IsLevelDivided = c.Boolean(nullable: false),
                        IsLevelScaled = c.Boolean(nullable: false),
                        ItemVNum = c.Short(),
                        NpcMonsterVNum = c.Short(),
                        SecondData = c.Int(nullable: false),
                        SkillVNum = c.Short(),
                        SubType = c.Byte(nullable: false),
                        ThirdData = c.Int(nullable: false),
                        Type = c.Byte(nullable: false),
                    })
                .PrimaryKey(t => t.BCardId)
                .ForeignKey("dbo.Card", t => t.CardId)
                .ForeignKey("dbo.Item", t => t.ItemVNum)
                .ForeignKey("dbo.NpcMonster", t => t.NpcMonsterVNum)
                .ForeignKey("dbo.Skill", t => t.SkillVNum)
                .Index(t => t.CardId)
                .Index(t => t.ItemVNum)
                .Index(t => t.NpcMonsterVNum)
                .Index(t => t.SkillVNum);
            
            CreateTable(
                "dbo.Card",
                c => new
                    {
                        CardId = c.Short(nullable: false),
                        BuffType = c.Byte(nullable: false),
                        Delay = c.Int(nullable: false),
                        Duration = c.Int(nullable: false),
                        EffectId = c.Int(nullable: false),
                        Level = c.Byte(nullable: false),
                        Name = c.String(maxLength: 255),
                        Propability = c.Byte(nullable: false),
                        TimeoutBuff = c.Short(nullable: false),
                        TimeoutBuffChance = c.Byte(nullable: false),
                    })
                .PrimaryKey(t => t.CardId);
            
            CreateTable(
                "dbo.StaticBuff",
                c => new
                    {
                        StaticBuffId = c.Long(nullable: false, identity: true),
                        CardId = c.Short(nullable: false),
                        CharacterId = c.Long(nullable: false),
                        RemainingTime = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.StaticBuffId)
                .ForeignKey("dbo.Card", t => t.CardId)
                .ForeignKey("dbo.Character", t => t.CharacterId)
                .Index(t => t.CardId)
                .Index(t => t.CharacterId);
            
            CreateTable(
                "dbo.NpcMonster",
                c => new
                    {
                        NpcMonsterVNum = c.Short(nullable: false),
                        AmountRequired = c.Short(nullable: false),
                        AttackClass = c.Byte(nullable: false),
                        AttackUpgrade = c.Byte(nullable: false),
                        BasicArea = c.Byte(nullable: false),
                        BasicCooldown = c.Short(nullable: false),
                        BasicRange = c.Byte(nullable: false),
                        BasicSkill = c.Short(nullable: false),
                        Catch = c.Boolean(nullable: false),
                        CloseDefence = c.Short(nullable: false),
                        Concentrate = c.Short(nullable: false),
                        CriticalChance = c.Byte(nullable: false),
                        CriticalRate = c.Short(nullable: false),
                        DamageMaximum = c.Short(nullable: false),
                        DamageMinimum = c.Short(nullable: false),
                        DarkResistance = c.Short(nullable: false),
                        DefenceDodge = c.Short(nullable: false),
                        DefenceUpgrade = c.Byte(nullable: false),
                        DistanceDefence = c.Short(nullable: false),
                        DistanceDefenceDodge = c.Short(nullable: false),
                        Element = c.Byte(nullable: false),
                        ElementRate = c.Short(nullable: false),
                        FireResistance = c.Short(nullable: false),
                        HeroLevel = c.Byte(nullable: false),
                        HeroXP = c.Int(nullable: false),
                        IsHostile = c.Boolean(nullable: false),
                        JobXP = c.Int(nullable: false),
                        Level = c.Byte(nullable: false),
                        LightResistance = c.Short(nullable: false),
                        MagicDefence = c.Short(nullable: false),
                        MaxHP = c.Int(nullable: false),
                        MaxMP = c.Int(nullable: false),
                        MonsterType = c.Byte(nullable: false),
                        Name = c.String(maxLength: 255),
                        NoAggresiveIcon = c.Boolean(nullable: false),
                        NoticeRange = c.Byte(nullable: false),
                        OriginalNpcMonsterVNum = c.Short(nullable: false),
                        Race = c.Byte(nullable: false),
                        RaceType = c.Byte(nullable: false),
                        RespawnTime = c.Int(nullable: false),
                        Speed = c.Byte(nullable: false),
                        VNumRequired = c.Short(nullable: false),
                        WaterResistance = c.Short(nullable: false),
                        XP = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.NpcMonsterVNum);
            
            CreateTable(
                "dbo.Drop",
                c => new
                    {
                        DropId = c.Short(nullable: false, identity: true),
                        Amount = c.Int(nullable: false),
                        DropChance = c.Int(nullable: false),
                        ItemVNum = c.Short(nullable: false),
                        MapTypeId = c.Short(),
                        MonsterVNum = c.Short(),
                    })
                .PrimaryKey(t => t.DropId)
                .ForeignKey("dbo.MapType", t => t.MapTypeId)
                .ForeignKey("dbo.NpcMonster", t => t.MonsterVNum)
                .ForeignKey("dbo.Item", t => t.ItemVNum)
                .Index(t => t.ItemVNum)
                .Index(t => t.MapTypeId)
                .Index(t => t.MonsterVNum);
            
            CreateTable(
                "dbo.MapType",
                c => new
                    {
                        MapTypeId = c.Short(nullable: false, identity: true),
                        MapTypeName = c.String(),
                        PotionDelay = c.Short(nullable: false),
                        RespawnMapTypeId = c.Long(),
                        ReturnMapTypeId = c.Long(),
                    })
                .PrimaryKey(t => t.MapTypeId)
                .ForeignKey("dbo.RespawnMapType", t => t.RespawnMapTypeId)
                .ForeignKey("dbo.RespawnMapType", t => t.ReturnMapTypeId)
                .Index(t => t.RespawnMapTypeId)
                .Index(t => t.ReturnMapTypeId);
            
            CreateTable(
                "dbo.MapTypeMap",
                c => new
                    {
                        MapId = c.Short(nullable: false),
                        MapTypeId = c.Short(nullable: false),
                    })
                .PrimaryKey(t => new { t.MapId, t.MapTypeId })
                .ForeignKey("dbo.Map", t => t.MapId)
                .ForeignKey("dbo.MapType", t => t.MapTypeId)
                .Index(t => t.MapId)
                .Index(t => t.MapTypeId);
            
            CreateTable(
                "dbo.Map",
                c => new
                    {
                        MapId = c.Short(nullable: false),
                        Data = c.Binary(),
                        GridMapId = c.Short(nullable: false),
                        Music = c.Int(nullable: false),
                        Name = c.String(maxLength: 255),
                        ShopAllowed = c.Boolean(nullable: false),
                        XpRate = c.Byte(nullable: false),
                    })
                .PrimaryKey(t => t.MapId);
            
            CreateTable(
                "dbo.MapMonster",
                c => new
                    {
                        MapMonsterId = c.Int(nullable: false),
                        IsDisabled = c.Boolean(nullable: false),
                        IsMoving = c.Boolean(nullable: false),
                        MapId = c.Short(nullable: false),
                        MapX = c.Short(nullable: false),
                        MapY = c.Short(nullable: false),
                        MonsterVNum = c.Short(nullable: false),
                        Name = c.String(),
                        Position = c.Byte(nullable: false),
                    })
                .PrimaryKey(t => t.MapMonsterId)
                .ForeignKey("dbo.Map", t => t.MapId)
                .ForeignKey("dbo.NpcMonster", t => t.MonsterVNum)
                .Index(t => t.MapId)
                .Index(t => t.MonsterVNum);
            
            CreateTable(
                "dbo.MapNpc",
                c => new
                    {
                        MapNpcId = c.Int(nullable: false),
                        Dialog = c.Short(nullable: false),
                        Effect = c.Short(nullable: false),
                        EffectDelay = c.Short(nullable: false),
                        IsDisabled = c.Boolean(nullable: false),
                        IsMoving = c.Boolean(nullable: false),
                        IsSitting = c.Boolean(nullable: false),
                        MapId = c.Short(nullable: false),
                        MapX = c.Short(nullable: false),
                        MapY = c.Short(nullable: false),
                        Name = c.String(),
                        NpcVNum = c.Short(nullable: false),
                        Position = c.Byte(nullable: false),
                    })
                .PrimaryKey(t => t.MapNpcId)
                .ForeignKey("dbo.Map", t => t.MapId)
                .ForeignKey("dbo.NpcMonster", t => t.NpcVNum)
                .Index(t => t.MapId)
                .Index(t => t.NpcVNum);
            
            CreateTable(
                "dbo.RecipeList",
                c => new
                    {
                        RecipeListId = c.Int(nullable: false, identity: true),
                        ItemVNum = c.Short(),
                        MapNpcId = c.Int(),
                        RecipeId = c.Short(nullable: false),
                    })
                .PrimaryKey(t => t.RecipeListId)
                .ForeignKey("dbo.Item", t => t.ItemVNum)
                .ForeignKey("dbo.MapNpc", t => t.MapNpcId)
                .ForeignKey("dbo.Recipe", t => t.RecipeId)
                .Index(t => t.ItemVNum)
                .Index(t => t.MapNpcId)
                .Index(t => t.RecipeId);
            
            CreateTable(
                "dbo.Recipe",
                c => new
                    {
                        RecipeId = c.Short(nullable: false, identity: true),
                        Amount = c.Short(nullable: false),
                        ItemVNum = c.Short(nullable: false),
                    })
                .PrimaryKey(t => t.RecipeId)
                .ForeignKey("dbo.Item", t => t.ItemVNum)
                .Index(t => t.ItemVNum);
            
            CreateTable(
                "dbo.RecipeItem",
                c => new
                    {
                        RecipeItemId = c.Short(nullable: false, identity: true),
                        Amount = c.Short(nullable: false),
                        ItemVNum = c.Short(nullable: false),
                        RecipeId = c.Short(nullable: false),
                    })
                .PrimaryKey(t => t.RecipeItemId)
                .ForeignKey("dbo.Recipe", t => t.RecipeId)
                .ForeignKey("dbo.Item", t => t.ItemVNum)
                .Index(t => t.ItemVNum)
                .Index(t => t.RecipeId);
            
            CreateTable(
                "dbo.Shop",
                c => new
                    {
                        ShopId = c.Int(nullable: false, identity: true),
                        MapNpcId = c.Int(nullable: false),
                        MenuType = c.Byte(nullable: false),
                        Name = c.String(maxLength: 255),
                        ShopType = c.Byte(nullable: false),
                    })
                .PrimaryKey(t => t.ShopId)
                .ForeignKey("dbo.MapNpc", t => t.MapNpcId)
                .Index(t => t.MapNpcId);
            
            CreateTable(
                "dbo.ShopItem",
                c => new
                    {
                        ShopItemId = c.Int(nullable: false, identity: true),
                        Color = c.Byte(nullable: false),
                        ItemVNum = c.Short(nullable: false),
                        Rare = c.Short(nullable: false),
                        ShopId = c.Int(nullable: false),
                        Slot = c.Byte(nullable: false),
                        Type = c.Byte(nullable: false),
                        Upgrade = c.Byte(nullable: false),
                    })
                .PrimaryKey(t => t.ShopItemId)
                .ForeignKey("dbo.Shop", t => t.ShopId)
                .ForeignKey("dbo.Item", t => t.ItemVNum)
                .Index(t => t.ItemVNum)
                .Index(t => t.ShopId);
            
            CreateTable(
                "dbo.ShopSkill",
                c => new
                    {
                        ShopSkillId = c.Int(nullable: false, identity: true),
                        ShopId = c.Int(nullable: false),
                        SkillVNum = c.Short(nullable: false),
                        Slot = c.Byte(nullable: false),
                        Type = c.Byte(nullable: false),
                    })
                .PrimaryKey(t => t.ShopSkillId)
                .ForeignKey("dbo.Skill", t => t.SkillVNum)
                .ForeignKey("dbo.Shop", t => t.ShopId)
                .Index(t => t.ShopId)
                .Index(t => t.SkillVNum);
            
            CreateTable(
                "dbo.Skill",
                c => new
                    {
                        SkillVNum = c.Short(nullable: false),
                        AttackAnimation = c.Short(nullable: false),
                        CastAnimation = c.Short(nullable: false),
                        CastEffect = c.Short(nullable: false),
                        CastId = c.Short(nullable: false),
                        CastTime = c.Short(nullable: false),
                        Class = c.Byte(nullable: false),
                        Cooldown = c.Short(nullable: false),
                        CPCost = c.Byte(nullable: false),
                        Duration = c.Short(nullable: false),
                        Effect = c.Short(nullable: false),
                        Element = c.Byte(nullable: false),
                        HitType = c.Byte(nullable: false),
                        ItemVNum = c.Short(nullable: false),
                        Level = c.Byte(nullable: false),
                        LevelMinimum = c.Byte(nullable: false),
                        MinimumAdventurerLevel = c.Byte(nullable: false),
                        MinimumArcherLevel = c.Byte(nullable: false),
                        MinimumMagicianLevel = c.Byte(nullable: false),
                        MinimumSwordmanLevel = c.Byte(nullable: false),
                        MpCost = c.Short(nullable: false),
                        Name = c.String(maxLength: 255),
                        Price = c.Int(nullable: false),
                        Range = c.Byte(nullable: false),
                        SkillType = c.Byte(nullable: false),
                        TargetRange = c.Byte(nullable: false),
                        TargetType = c.Byte(nullable: false),
                        Type = c.Byte(nullable: false),
                        UpgradeSkill = c.Short(nullable: false),
                        UpgradeType = c.Short(nullable: false),
                    })
                .PrimaryKey(t => t.SkillVNum);
            
            CreateTable(
                "dbo.CharacterSkill",
                c => new
                    {
                        Id = c.Guid(nullable: false),
                        CharacterId = c.Long(nullable: false),
                        SkillVNum = c.Short(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Skill", t => t.SkillVNum)
                .ForeignKey("dbo.Character", t => t.CharacterId)
                .Index(t => t.CharacterId)
                .Index(t => t.SkillVNum);
            
            CreateTable(
                "dbo.Combo",
                c => new
                    {
                        ComboId = c.Int(nullable: false, identity: true),
                        Animation = c.Short(nullable: false),
                        Effect = c.Short(nullable: false),
                        Hit = c.Short(nullable: false),
                        SkillVNum = c.Short(nullable: false),
                    })
                .PrimaryKey(t => t.ComboId)
                .ForeignKey("dbo.Skill", t => t.SkillVNum)
                .Index(t => t.SkillVNum);
            
            CreateTable(
                "dbo.NpcMonsterSkill",
                c => new
                    {
                        NpcMonsterSkillId = c.Long(nullable: false, identity: true),
                        NpcMonsterVNum = c.Short(nullable: false),
                        Rate = c.Short(nullable: false),
                        SkillVNum = c.Short(nullable: false),
                    })
                .PrimaryKey(t => t.NpcMonsterSkillId)
                .ForeignKey("dbo.Skill", t => t.SkillVNum)
                .ForeignKey("dbo.NpcMonster", t => t.NpcMonsterVNum)
                .Index(t => t.NpcMonsterVNum)
                .Index(t => t.SkillVNum);
            
            CreateTable(
                "dbo.Teleporter",
                c => new
                    {
                        TeleporterId = c.Short(nullable: false, identity: true),
                        Index = c.Short(nullable: false),
                        Type = c.Byte(nullable: false),
                        MapId = c.Short(nullable: false),
                        MapNpcId = c.Int(nullable: false),
                        MapX = c.Short(nullable: false),
                        MapY = c.Short(nullable: false),
                    })
                .PrimaryKey(t => t.TeleporterId)
                .ForeignKey("dbo.MapNpc", t => t.MapNpcId)
                .ForeignKey("dbo.Map", t => t.MapId)
                .Index(t => t.MapId)
                .Index(t => t.MapNpcId);
            
            CreateTable(
                "dbo.Portal",
                c => new
                    {
                        PortalId = c.Int(nullable: false, identity: true),
                        DestinationMapId = c.Short(nullable: false),
                        DestinationX = c.Short(nullable: false),
                        DestinationY = c.Short(nullable: false),
                        IsDisabled = c.Boolean(nullable: false),
                        SourceMapId = c.Short(nullable: false),
                        SourceX = c.Short(nullable: false),
                        SourceY = c.Short(nullable: false),
                        Type = c.Short(nullable: false),
                    })
                .PrimaryKey(t => t.PortalId)
                .ForeignKey("dbo.Map", t => t.DestinationMapId)
                .ForeignKey("dbo.Map", t => t.SourceMapId)
                .Index(t => t.DestinationMapId)
                .Index(t => t.SourceMapId);
            
            CreateTable(
                "dbo.Respawn",
                c => new
                    {
                        RespawnId = c.Long(nullable: false, identity: true),
                        CharacterId = c.Long(nullable: false),
                        MapId = c.Short(nullable: false),
                        RespawnMapTypeId = c.Long(nullable: false),
                        X = c.Short(nullable: false),
                        Y = c.Short(nullable: false),
                    })
                .PrimaryKey(t => t.RespawnId)
                .ForeignKey("dbo.Map", t => t.MapId)
                .ForeignKey("dbo.RespawnMapType", t => t.RespawnMapTypeId)
                .ForeignKey("dbo.Character", t => t.CharacterId)
                .Index(t => t.CharacterId)
                .Index(t => t.MapId)
                .Index(t => t.RespawnMapTypeId);
            
            CreateTable(
                "dbo.RespawnMapType",
                c => new
                    {
                        RespawnMapTypeId = c.Long(nullable: false),
                        DefaultMapId = c.Short(nullable: false),
                        DefaultX = c.Short(nullable: false),
                        DefaultY = c.Short(nullable: false),
                        Name = c.String(maxLength: 255),
                    })
                .PrimaryKey(t => t.RespawnMapTypeId)
                .ForeignKey("dbo.Map", t => t.DefaultMapId)
                .Index(t => t.DefaultMapId);
            
            CreateTable(
                "dbo.ScriptedInstance",
                c => new
                    {
                        ScriptedInstanceId = c.Short(nullable: false, identity: true),
                        MapId = c.Short(nullable: false),
                        PositionX = c.Short(nullable: false),
                        PositionY = c.Short(nullable: false),
                        Script = c.String(),
                        Type = c.Byte(nullable: false),
                        Label = c.String(),
                    })
                .PrimaryKey(t => t.ScriptedInstanceId)
                .ForeignKey("dbo.Map", t => t.MapId)
                .Index(t => t.MapId);
            
            CreateTable(
                "dbo.Mate",
                c => new
                    {
                        MateId = c.Long(nullable: false, identity: true),
                        Attack = c.Byte(nullable: false),
                        CanPickUp = c.Boolean(nullable: false),
                        CharacterId = c.Long(nullable: false),
                        Defence = c.Byte(nullable: false),
                        Direction = c.Byte(nullable: false),
                        Experience = c.Long(nullable: false),
                        Hp = c.Double(nullable: false),
                        IsSummonable = c.Boolean(nullable: false),
                        IsTeamMember = c.Boolean(nullable: false),
                        Level = c.Byte(nullable: false),
                        Loyalty = c.Short(nullable: false),
                        MapX = c.Short(nullable: false),
                        MapY = c.Short(nullable: false),
                        MateType = c.Byte(nullable: false),
                        Mp = c.Double(nullable: false),
                        Name = c.String(maxLength: 255),
                        NpcMonsterVNum = c.Short(nullable: false),
                        Skin = c.Short(nullable: false),
                    })
                .PrimaryKey(t => t.MateId)
                .ForeignKey("dbo.NpcMonster", t => t.NpcMonsterVNum)
                .ForeignKey("dbo.Character", t => t.CharacterId)
                .Index(t => t.CharacterId)
                .Index(t => t.NpcMonsterVNum);
            
            CreateTable(
                "dbo.Mail",
                c => new
                    {
                        MailId = c.Long(nullable: false, identity: true),
                        AttachmentAmount = c.Short(nullable: false),
                        AttachmentLevel = c.Byte(nullable: false),
                        AttachmentRarity = c.Byte(nullable: false),
                        AttachmentUpgrade = c.Byte(nullable: false),
                        AttachmentDesign = c.Short(nullable: false),
                        AttachmentVNum = c.Short(),
                        Date = c.DateTime(nullable: false),
                        EqPacket = c.String(maxLength: 255),
                        IsOpened = c.Boolean(nullable: false),
                        IsSenderCopy = c.Boolean(nullable: false),
                        Message = c.String(maxLength: 255),
                        ReceiverId = c.Long(nullable: false),
                        SenderClass = c.Byte(nullable: false),
                        SenderGender = c.Byte(nullable: false),
                        SenderHairColor = c.Byte(nullable: false),
                        SenderHairStyle = c.Byte(nullable: false),
                        SenderId = c.Long(nullable: false),
                        SenderMorphId = c.Short(nullable: false),
                        Title = c.String(maxLength: 255),
                    })
                .PrimaryKey(t => t.MailId)
                .ForeignKey("dbo.Item", t => t.AttachmentVNum)
                .ForeignKey("dbo.Character", t => t.SenderId)
                .ForeignKey("dbo.Character", t => t.ReceiverId)
                .Index(t => t.AttachmentVNum)
                .Index(t => t.ReceiverId)
                .Index(t => t.SenderId);
            
            CreateTable(
                "dbo.RollGeneratedItem",
                c => new
                    {
                        RollGeneratedItemId = c.Short(nullable: false, identity: true),
                        IsRareRandom = c.Boolean(nullable: false),
                        ItemGeneratedAmount = c.Short(nullable: false),
                        ItemGeneratedVNum = c.Short(nullable: false),
                        ItemGeneratedDesign = c.Short(nullable: false),
                        MaximumOriginalItemRare = c.Byte(nullable: false),
                        MinimumOriginalItemRare = c.Byte(nullable: false),
                        OriginalItemDesign = c.Short(nullable: false),
                        OriginalItemVNum = c.Short(nullable: false),
                        Probability = c.Short(nullable: false),
                    })
                .PrimaryKey(t => t.RollGeneratedItemId)
                .ForeignKey("dbo.Item", t => t.ItemGeneratedVNum)
                .ForeignKey("dbo.Item", t => t.OriginalItemVNum)
                .Index(t => t.ItemGeneratedVNum)
                .Index(t => t.OriginalItemVNum);
            
            CreateTable(
                "dbo.MinilandObject",
                c => new
                    {
                        MinilandObjectId = c.Long(nullable: false, identity: true),
                        CharacterId = c.Long(nullable: false),
                        ItemInstanceId = c.Guid(),
                        Level1BoxAmount = c.Byte(nullable: false),
                        Level2BoxAmount = c.Byte(nullable: false),
                        Level3BoxAmount = c.Byte(nullable: false),
                        Level4BoxAmount = c.Byte(nullable: false),
                        Level5BoxAmount = c.Byte(nullable: false),
                        MapX = c.Short(nullable: false),
                        MapY = c.Short(nullable: false),
                    })
                .PrimaryKey(t => t.MinilandObjectId)
                .ForeignKey("dbo.ItemInstance", t => t.ItemInstanceId)
                .ForeignKey("dbo.Character", t => t.CharacterId)
                .Index(t => t.CharacterId)
                .Index(t => t.ItemInstanceId);
            
            CreateTable(
                "dbo.CharacterRelation",
                c => new
                    {
                        CharacterRelationId = c.Long(nullable: false, identity: true),
                        CharacterId = c.Long(nullable: false),
                        RelatedCharacterId = c.Long(nullable: false),
                        RelationType = c.Short(nullable: false),
                    })
                .PrimaryKey(t => t.CharacterRelationId)
                .ForeignKey("dbo.Character", t => t.CharacterId)
                .ForeignKey("dbo.Character", t => t.RelatedCharacterId)
                .Index(t => t.CharacterId)
                .Index(t => t.RelatedCharacterId);
            
            CreateTable(
                "dbo.FamilyCharacter",
                c => new
                    {
                        FamilyCharacterId = c.Long(nullable: false, identity: true),
                        Authority = c.Byte(nullable: false),
                        CharacterId = c.Long(nullable: false),
                        DailyMessage = c.String(maxLength: 255),
                        Experience = c.Int(nullable: false),
                        FamilyId = c.Long(nullable: false),
                        Rank = c.Byte(nullable: false),
                    })
                .PrimaryKey(t => t.FamilyCharacterId)
                .ForeignKey("dbo.Character", t => t.CharacterId)
                .ForeignKey("dbo.Family", t => t.FamilyId)
                .Index(t => t.CharacterId)
                .Index(t => t.FamilyId);
            
            CreateTable(
                "dbo.Family",
                c => new
                    {
                        FamilyId = c.Long(nullable: false, identity: true),
                        FamilyExperience = c.Int(nullable: false),
                        FamilyFaction = c.Byte(nullable: false),
                        FamilyHeadGender = c.Byte(nullable: false),
                        FamilyLevel = c.Byte(nullable: false),
                        FamilyMessage = c.String(maxLength: 255),
                        LastFactionChange = c.Long(nullable: false),
                        ManagerAuthorityType = c.Byte(nullable: false),
                        ManagerCanGetHistory = c.Boolean(nullable: false),
                        ManagerCanInvite = c.Boolean(nullable: false),
                        ManagerCanNotice = c.Boolean(nullable: false),
                        ManagerCanShout = c.Boolean(nullable: false),
                        MaxSize = c.Short(nullable: false),
                        MemberAuthorityType = c.Byte(nullable: false),
                        MemberCanGetHistory = c.Boolean(nullable: false),
                        Name = c.String(maxLength: 255),
                        WarehouseSize = c.Byte(nullable: false),
                    })
                .PrimaryKey(t => t.FamilyId);
            
            CreateTable(
                "dbo.FamilyLog",
                c => new
                    {
                        FamilyLogId = c.Long(nullable: false, identity: true),
                        FamilyId = c.Long(nullable: false),
                        FamilyLogData = c.String(maxLength: 255),
                        FamilyLogType = c.Byte(nullable: false),
                        Timestamp = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.FamilyLogId)
                .ForeignKey("dbo.Family", t => t.FamilyId)
                .Index(t => t.FamilyId);
            
            CreateTable(
                "dbo.GeneralLog",
                c => new
                    {
                        LogId = c.Long(nullable: false, identity: true),
                        AccountId = c.Long(),
                        CharacterId = c.Long(),
                        IpAddress = c.String(maxLength: 255),
                        LogData = c.String(maxLength: 255),
                        LogType = c.String(),
                        Timestamp = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.LogId)
                .ForeignKey("dbo.Account", t => t.AccountId)
                .ForeignKey("dbo.Character", t => t.CharacterId)
                .Index(t => t.AccountId)
                .Index(t => t.CharacterId);
            
            CreateTable(
                "dbo.MinigameLog",
                c => new
                    {
                        MinigameLogId = c.Long(nullable: false, identity: true),
                        StartTime = c.Long(nullable: false),
                        EndTime = c.Long(nullable: false),
                        Score = c.Int(nullable: false),
                        Minigame = c.Byte(nullable: false),
                        CharacterId = c.Long(nullable: false),
                    })
                .PrimaryKey(t => t.MinigameLogId)
                .ForeignKey("dbo.Character", t => t.CharacterId)
                .Index(t => t.CharacterId);
            
            CreateTable(
                "dbo.QuicklistEntry",
                c => new
                    {
                        Id = c.Guid(nullable: false),
                        CharacterId = c.Long(nullable: false),
                        Morph = c.Short(nullable: false),
                        Pos = c.Short(nullable: false),
                        Q1 = c.Short(nullable: false),
                        Q2 = c.Short(nullable: false),
                        Slot = c.Short(nullable: false),
                        Type = c.Short(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Character", t => t.CharacterId)
                .Index(t => t.CharacterId);
            
            CreateTable(
                "dbo.StaticBonus",
                c => new
                    {
                        StaticBonusId = c.Long(nullable: false, identity: true),
                        CharacterId = c.Long(nullable: false),
                        DateEnd = c.DateTime(nullable: false),
                        StaticBonusType = c.Byte(nullable: false),
                    })
                .PrimaryKey(t => t.StaticBonusId)
                .ForeignKey("dbo.Character", t => t.CharacterId)
                .Index(t => t.CharacterId);
            
            CreateTable(
                "dbo.PenaltyLog",
                c => new
                    {
                        PenaltyLogId = c.Int(nullable: false, identity: true),
                        AccountId = c.Long(nullable: false),
                        IP = c.String(),
                        AdminName = c.String(),
                        DateEnd = c.DateTime(nullable: false),
                        DateStart = c.DateTime(nullable: false),
                        Penalty = c.Byte(nullable: false),
                        Reason = c.String(maxLength: 255),
                    })
                .PrimaryKey(t => t.PenaltyLogId)
                .ForeignKey("dbo.Account", t => t.AccountId)
                .Index(t => t.AccountId);
            
            CreateTable(
                "dbo.CellonOption",
                c => new
                    {
                        CellonOptionId = c.Long(nullable: false, identity: true),
                        EquipmentSerialId = c.Guid(nullable: false),
                        Level = c.Byte(nullable: false),
                        Type = c.Byte(nullable: false),
                        Value = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.CellonOptionId);
            
            CreateTable(
                "dbo.CharacterQuest",
                c => new
                    {
                        Id = c.Guid(nullable: false),
                        CharacterId = c.Long(nullable: false),
                        QuestId = c.Long(nullable: false),
                        FirstObjective = c.Int(nullable: false),
                        SecondObjective = c.Int(nullable: false),
                        ThirdObjective = c.Int(nullable: false),
                        FourthObjective = c.Int(nullable: false),
                        FifthObjective = c.Int(nullable: false),
                        IsMainQuest = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Quest", t => t.QuestId, cascadeDelete: true)
                .Index(t => t.QuestId);
            
            CreateTable(
                "dbo.Quest",
                c => new
                    {
                        QuestId = c.Long(nullable: false),
                        QuestType = c.Int(nullable: false),
                        LevelMin = c.Byte(nullable: false),
                        LevelMax = c.Byte(nullable: false),
                        StartDialogId = c.Int(),
                        EndDialogId = c.Int(),
                        DialogNpcVNum = c.Int(),
                        DialogNpcId = c.Int(),
                        TargetMap = c.Short(),
                        TargetX = c.Short(),
                        TargetY = c.Short(),
                        InfoId = c.Int(nullable: false),
                        NextQuestId = c.Long(),
                        IsDaily = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.QuestId);
            
            CreateTable(
                "dbo.MaintenanceLog",
                c => new
                    {
                        LogId = c.Long(nullable: false, identity: true),
                        DateEnd = c.DateTime(nullable: false),
                        DateStart = c.DateTime(nullable: false),
                        Reason = c.String(maxLength: 255),
                    })
                .PrimaryKey(t => t.LogId);
            
            CreateTable(
                "dbo.PartnerSkill",
                c => new
                    {
                        PartnerSkillId = c.Long(nullable: false, identity: true),
                        EquipmentSerialId = c.Guid(nullable: false),
                        SkillVNum = c.Short(nullable: false),
                        Level = c.Byte(nullable: false),
                    })
                .PrimaryKey(t => t.PartnerSkillId);
            
            CreateTable(
                "dbo.QuestLog",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        CharacterId = c.Long(nullable: false),
                        QuestId = c.Long(nullable: false),
                        IpAddress = c.String(),
                        LastDaily = c.DateTime(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.QuestObjective",
                c => new
                    {
                        QuestObjectiveId = c.Int(nullable: false, identity: true),
                        QuestId = c.Int(nullable: false),
                        Data = c.Int(),
                        Objective = c.Int(),
                        SpecialData = c.Int(),
                        DropRate = c.Int(),
                        ObjectiveIndex = c.Byte(nullable: false),
                    })
                .PrimaryKey(t => t.QuestObjectiveId);
            
            CreateTable(
                "dbo.QuestReward",
                c => new
                    {
                        QuestRewardId = c.Long(nullable: false, identity: true),
                        RewardType = c.Byte(nullable: false),
                        Data = c.Int(nullable: false),
                        Design = c.Byte(nullable: false),
                        Rarity = c.Byte(nullable: false),
                        Upgrade = c.Byte(nullable: false),
                        Amount = c.Int(nullable: false),
                        QuestId = c.Long(nullable: false),
                    })
                .PrimaryKey(t => t.QuestRewardId);
            
            CreateTable(
                "dbo.ShellEffect",
                c => new
                    {
                        ShellEffectId = c.Long(nullable: false, identity: true),
                        Effect = c.Byte(nullable: false),
                        EffectLevel = c.Byte(nullable: false),
                        EquipmentSerialId = c.Guid(nullable: false),
                        Value = c.Short(nullable: false),
                    })
                .PrimaryKey(t => t.ShellEffectId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.CharacterQuest", "QuestId", "dbo.Quest");
            DropForeignKey("dbo.PenaltyLog", "AccountId", "dbo.Account");
            DropForeignKey("dbo.Character", "AccountId", "dbo.Account");
            DropForeignKey("dbo.StaticBuff", "CharacterId", "dbo.Character");
            DropForeignKey("dbo.StaticBonus", "CharacterId", "dbo.Character");
            DropForeignKey("dbo.Respawn", "CharacterId", "dbo.Character");
            DropForeignKey("dbo.QuicklistEntry", "CharacterId", "dbo.Character");
            DropForeignKey("dbo.MinilandObject", "CharacterId", "dbo.Character");
            DropForeignKey("dbo.MinigameLog", "CharacterId", "dbo.Character");
            DropForeignKey("dbo.Mate", "CharacterId", "dbo.Character");
            DropForeignKey("dbo.Mail", "ReceiverId", "dbo.Character");
            DropForeignKey("dbo.Mail", "SenderId", "dbo.Character");
            DropForeignKey("dbo.ItemInstance", "CharacterId", "dbo.Character");
            DropForeignKey("dbo.GeneralLog", "CharacterId", "dbo.Character");
            DropForeignKey("dbo.GeneralLog", "AccountId", "dbo.Account");
            DropForeignKey("dbo.FamilyCharacter", "FamilyId", "dbo.Family");
            DropForeignKey("dbo.FamilyLog", "FamilyId", "dbo.Family");
            DropForeignKey("dbo.FamilyCharacter", "CharacterId", "dbo.Character");
            DropForeignKey("dbo.CharacterSkill", "CharacterId", "dbo.Character");
            DropForeignKey("dbo.CharacterRelation", "RelatedCharacterId", "dbo.Character");
            DropForeignKey("dbo.CharacterRelation", "CharacterId", "dbo.Character");
            DropForeignKey("dbo.BazaarItem", "ItemInstanceId", "dbo.ItemInstance");
            DropForeignKey("dbo.MinilandObject", "ItemInstanceId", "dbo.ItemInstance");
            DropForeignKey("dbo.ShopItem", "ItemVNum", "dbo.Item");
            DropForeignKey("dbo.RollGeneratedItem", "OriginalItemVNum", "dbo.Item");
            DropForeignKey("dbo.RollGeneratedItem", "ItemGeneratedVNum", "dbo.Item");
            DropForeignKey("dbo.RecipeItem", "ItemVNum", "dbo.Item");
            DropForeignKey("dbo.Recipe", "ItemVNum", "dbo.Item");
            DropForeignKey("dbo.Mail", "AttachmentVNum", "dbo.Item");
            DropForeignKey("dbo.ItemInstance", "ItemVNum", "dbo.Item");
            DropForeignKey("dbo.Drop", "ItemVNum", "dbo.Item");
            DropForeignKey("dbo.BCard", "SkillVNum", "dbo.Skill");
            DropForeignKey("dbo.BCard", "NpcMonsterVNum", "dbo.NpcMonster");
            DropForeignKey("dbo.NpcMonsterSkill", "NpcMonsterVNum", "dbo.NpcMonster");
            DropForeignKey("dbo.Mate", "NpcMonsterVNum", "dbo.NpcMonster");
            DropForeignKey("dbo.MapNpc", "NpcVNum", "dbo.NpcMonster");
            DropForeignKey("dbo.MapMonster", "MonsterVNum", "dbo.NpcMonster");
            DropForeignKey("dbo.Drop", "MonsterVNum", "dbo.NpcMonster");
            DropForeignKey("dbo.MapType", "ReturnMapTypeId", "dbo.RespawnMapType");
            DropForeignKey("dbo.MapType", "RespawnMapTypeId", "dbo.RespawnMapType");
            DropForeignKey("dbo.MapTypeMap", "MapTypeId", "dbo.MapType");
            DropForeignKey("dbo.MapTypeMap", "MapId", "dbo.Map");
            DropForeignKey("dbo.Teleporter", "MapId", "dbo.Map");
            DropForeignKey("dbo.ScriptedInstance", "MapId", "dbo.Map");
            DropForeignKey("dbo.Respawn", "RespawnMapTypeId", "dbo.RespawnMapType");
            DropForeignKey("dbo.RespawnMapType", "DefaultMapId", "dbo.Map");
            DropForeignKey("dbo.Respawn", "MapId", "dbo.Map");
            DropForeignKey("dbo.Portal", "SourceMapId", "dbo.Map");
            DropForeignKey("dbo.Portal", "DestinationMapId", "dbo.Map");
            DropForeignKey("dbo.MapNpc", "MapId", "dbo.Map");
            DropForeignKey("dbo.Teleporter", "MapNpcId", "dbo.MapNpc");
            DropForeignKey("dbo.Shop", "MapNpcId", "dbo.MapNpc");
            DropForeignKey("dbo.ShopSkill", "ShopId", "dbo.Shop");
            DropForeignKey("dbo.ShopSkill", "SkillVNum", "dbo.Skill");
            DropForeignKey("dbo.NpcMonsterSkill", "SkillVNum", "dbo.Skill");
            DropForeignKey("dbo.Combo", "SkillVNum", "dbo.Skill");
            DropForeignKey("dbo.CharacterSkill", "SkillVNum", "dbo.Skill");
            DropForeignKey("dbo.ShopItem", "ShopId", "dbo.Shop");
            DropForeignKey("dbo.RecipeList", "RecipeId", "dbo.Recipe");
            DropForeignKey("dbo.RecipeItem", "RecipeId", "dbo.Recipe");
            DropForeignKey("dbo.RecipeList", "MapNpcId", "dbo.MapNpc");
            DropForeignKey("dbo.RecipeList", "ItemVNum", "dbo.Item");
            DropForeignKey("dbo.MapMonster", "MapId", "dbo.Map");
            DropForeignKey("dbo.Character", "MapId", "dbo.Map");
            DropForeignKey("dbo.Drop", "MapTypeId", "dbo.MapType");
            DropForeignKey("dbo.BCard", "ItemVNum", "dbo.Item");
            DropForeignKey("dbo.BCard", "CardId", "dbo.Card");
            DropForeignKey("dbo.StaticBuff", "CardId", "dbo.Card");
            DropForeignKey("dbo.ItemInstance", "BoundCharacterId", "dbo.Character");
            DropForeignKey("dbo.BazaarItem", "SellerId", "dbo.Character");
            DropIndex("dbo.CharacterQuest", new[] { "QuestId" });
            DropIndex("dbo.PenaltyLog", new[] { "AccountId" });
            DropIndex("dbo.StaticBonus", new[] { "CharacterId" });
            DropIndex("dbo.QuicklistEntry", new[] { "CharacterId" });
            DropIndex("dbo.MinigameLog", new[] { "CharacterId" });
            DropIndex("dbo.GeneralLog", new[] { "CharacterId" });
            DropIndex("dbo.GeneralLog", new[] { "AccountId" });
            DropIndex("dbo.FamilyLog", new[] { "FamilyId" });
            DropIndex("dbo.FamilyCharacter", new[] { "FamilyId" });
            DropIndex("dbo.FamilyCharacter", new[] { "CharacterId" });
            DropIndex("dbo.CharacterRelation", new[] { "RelatedCharacterId" });
            DropIndex("dbo.CharacterRelation", new[] { "CharacterId" });
            DropIndex("dbo.MinilandObject", new[] { "ItemInstanceId" });
            DropIndex("dbo.MinilandObject", new[] { "CharacterId" });
            DropIndex("dbo.RollGeneratedItem", new[] { "OriginalItemVNum" });
            DropIndex("dbo.RollGeneratedItem", new[] { "ItemGeneratedVNum" });
            DropIndex("dbo.Mail", new[] { "SenderId" });
            DropIndex("dbo.Mail", new[] { "ReceiverId" });
            DropIndex("dbo.Mail", new[] { "AttachmentVNum" });
            DropIndex("dbo.Mate", new[] { "NpcMonsterVNum" });
            DropIndex("dbo.Mate", new[] { "CharacterId" });
            DropIndex("dbo.ScriptedInstance", new[] { "MapId" });
            DropIndex("dbo.RespawnMapType", new[] { "DefaultMapId" });
            DropIndex("dbo.Respawn", new[] { "RespawnMapTypeId" });
            DropIndex("dbo.Respawn", new[] { "MapId" });
            DropIndex("dbo.Respawn", new[] { "CharacterId" });
            DropIndex("dbo.Portal", new[] { "SourceMapId" });
            DropIndex("dbo.Portal", new[] { "DestinationMapId" });
            DropIndex("dbo.Teleporter", new[] { "MapNpcId" });
            DropIndex("dbo.Teleporter", new[] { "MapId" });
            DropIndex("dbo.NpcMonsterSkill", new[] { "SkillVNum" });
            DropIndex("dbo.NpcMonsterSkill", new[] { "NpcMonsterVNum" });
            DropIndex("dbo.Combo", new[] { "SkillVNum" });
            DropIndex("dbo.CharacterSkill", new[] { "SkillVNum" });
            DropIndex("dbo.CharacterSkill", new[] { "CharacterId" });
            DropIndex("dbo.ShopSkill", new[] { "SkillVNum" });
            DropIndex("dbo.ShopSkill", new[] { "ShopId" });
            DropIndex("dbo.ShopItem", new[] { "ShopId" });
            DropIndex("dbo.ShopItem", new[] { "ItemVNum" });
            DropIndex("dbo.Shop", new[] { "MapNpcId" });
            DropIndex("dbo.RecipeItem", new[] { "RecipeId" });
            DropIndex("dbo.RecipeItem", new[] { "ItemVNum" });
            DropIndex("dbo.Recipe", new[] { "ItemVNum" });
            DropIndex("dbo.RecipeList", new[] { "RecipeId" });
            DropIndex("dbo.RecipeList", new[] { "MapNpcId" });
            DropIndex("dbo.RecipeList", new[] { "ItemVNum" });
            DropIndex("dbo.MapNpc", new[] { "NpcVNum" });
            DropIndex("dbo.MapNpc", new[] { "MapId" });
            DropIndex("dbo.MapMonster", new[] { "MonsterVNum" });
            DropIndex("dbo.MapMonster", new[] { "MapId" });
            DropIndex("dbo.MapTypeMap", new[] { "MapTypeId" });
            DropIndex("dbo.MapTypeMap", new[] { "MapId" });
            DropIndex("dbo.MapType", new[] { "ReturnMapTypeId" });
            DropIndex("dbo.MapType", new[] { "RespawnMapTypeId" });
            DropIndex("dbo.Drop", new[] { "MonsterVNum" });
            DropIndex("dbo.Drop", new[] { "MapTypeId" });
            DropIndex("dbo.Drop", new[] { "ItemVNum" });
            DropIndex("dbo.StaticBuff", new[] { "CharacterId" });
            DropIndex("dbo.StaticBuff", new[] { "CardId" });
            DropIndex("dbo.BCard", new[] { "SkillVNum" });
            DropIndex("dbo.BCard", new[] { "NpcMonsterVNum" });
            DropIndex("dbo.BCard", new[] { "ItemVNum" });
            DropIndex("dbo.BCard", new[] { "CardId" });
            DropIndex("dbo.ItemInstance", new[] { "ItemVNum" });
            DropIndex("dbo.ItemInstance", "IX_SlotAndType");
            DropIndex("dbo.ItemInstance", new[] { "BoundCharacterId" });
            DropIndex("dbo.BazaarItem", new[] { "SellerId" });
            DropIndex("dbo.BazaarItem", new[] { "ItemInstanceId" });
            DropIndex("dbo.Character", new[] { "MapId" });
            DropIndex("dbo.Character", new[] { "AccountId" });
            DropTable("dbo.ShellEffect");
            DropTable("dbo.QuestReward");
            DropTable("dbo.QuestObjective");
            DropTable("dbo.QuestLog");
            DropTable("dbo.PartnerSkill");
            DropTable("dbo.MaintenanceLog");
            DropTable("dbo.Quest");
            DropTable("dbo.CharacterQuest");
            DropTable("dbo.CellonOption");
            DropTable("dbo.PenaltyLog");
            DropTable("dbo.StaticBonus");
            DropTable("dbo.QuicklistEntry");
            DropTable("dbo.MinigameLog");
            DropTable("dbo.GeneralLog");
            DropTable("dbo.FamilyLog");
            DropTable("dbo.Family");
            DropTable("dbo.FamilyCharacter");
            DropTable("dbo.CharacterRelation");
            DropTable("dbo.MinilandObject");
            DropTable("dbo.RollGeneratedItem");
            DropTable("dbo.Mail");
            DropTable("dbo.Mate");
            DropTable("dbo.ScriptedInstance");
            DropTable("dbo.RespawnMapType");
            DropTable("dbo.Respawn");
            DropTable("dbo.Portal");
            DropTable("dbo.Teleporter");
            DropTable("dbo.NpcMonsterSkill");
            DropTable("dbo.Combo");
            DropTable("dbo.CharacterSkill");
            DropTable("dbo.Skill");
            DropTable("dbo.ShopSkill");
            DropTable("dbo.ShopItem");
            DropTable("dbo.Shop");
            DropTable("dbo.RecipeItem");
            DropTable("dbo.Recipe");
            DropTable("dbo.RecipeList");
            DropTable("dbo.MapNpc");
            DropTable("dbo.MapMonster");
            DropTable("dbo.Map");
            DropTable("dbo.MapTypeMap");
            DropTable("dbo.MapType");
            DropTable("dbo.Drop");
            DropTable("dbo.NpcMonster");
            DropTable("dbo.StaticBuff");
            DropTable("dbo.Card");
            DropTable("dbo.BCard");
            DropTable("dbo.Item");
            DropTable("dbo.ItemInstance");
            DropTable("dbo.BazaarItem");
            DropTable("dbo.Character");
            DropTable("dbo.Account");
        }
    }
}
