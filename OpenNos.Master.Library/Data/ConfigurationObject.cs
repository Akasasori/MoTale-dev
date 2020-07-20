using System;

namespace OpenNos.Master.Library.Data
{
    [Serializable]
    public class ConfigurationObject
    {
        public bool IsAntiCheatEnabled { get; set; }

        public string AntiCheatClientKey { get; set; }

        public string AntiCheatServerKey { get; set; }

        public string Act4IP { get; set; }

        public int Act4Port { get; set; }

        public int SessionLimit { get; set; }

        public bool SceneOnCreate { get; set; }

        public bool WorldInformation { get; set; }

        public int RateXP { get; set; }

        public int RateHeroicXP { get; set; }

        public int RateGold { get; set; }

        public int RateGoldDrop { get; set; }

        public int RateReputation { get; set; }

        public long MaxGold { get; set; }

        public int RateDrop { get; set; }

        public byte MaxLevel { get; set; }

        public byte MaxJobLevel { get; set; }

        public byte MaxHeroLevel { get; set; }

        public byte HeroicStartLevel { get; set; }

        public byte MaxSPLevel { get; set; }

        public int RateFairyXP { get; set; }

        public int Tax { get; set; }

        public long PartnerSpXp { get; set; }

        public byte MaxUpgrade { get; set; }

        public string MallBaseURL { get; set; }

        public string MallAPIKey { get; set; }

        public byte CylloanPercentRate { get; set; }

        public int QuestDropRate { get; set; }

        public bool UseLogService { get; set; }

        public bool HalloweenEvent { get; set; }

        public bool EasterEvent {get; set; }

        public bool ChristmasEvent { get; set; }

        public bool LockSystem { get; set; }

        public bool AutoLootEnable { get; set; }

        public bool BCardsInArenaTalent { get; set; }
        public int Act4Rate { get; set; }

        public int SPJLvl { get; set; }

        public bool DoubleXP { get; set; }

        public bool DoubleFairyXP { get; set; }

        public bool DoubleRaidBox { get; set; }

        public bool DoubleSpUp { get; set; }

        public bool DoubleEqUp { get; set; }

        public bool DoubleBet { get; set; }

        public bool DoublePerfectionUp { get; set; }

        public bool DoubleXPFamily { get; set; }

        public bool DoubleGold { get; set; }

        public bool DoubleReput { get; set; }

        public bool DoubleDrop { get; set; }

        public bool MultiEvent { get; set; }
    }
}
