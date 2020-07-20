using OpenNos.Domain;
using System.Collections.Generic;

namespace OpenNos.GameObject.Helpers
{
    public class PassiveSkillHelper
    {
        public List<BCard> PassiveSkillToBCards(IEnumerable<CharacterSkill> skills)
        {
            List<BCard> bcards = new List<BCard>();

            if (skills != null)
            {
                foreach (CharacterSkill skill in skills)
                {
                    switch (skill.Skill.CastId)
                    {
                        case 0:
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeSkill,
                                Type = (byte)BCardType.CardType.AttackPower,
                                SubType = (byte)AdditionalTypes.AttackPower.MeleeAttacksIncreased / 10
                            });
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeSkill,
                                Type = (byte)BCardType.CardType.Defence,
                                SubType = (byte)AdditionalTypes.Defence.MeleeIncreased / 10
                            });
                            break;
                        case 1:
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeSkill,
                                Type = (byte)BCardType.CardType.Target,
                                SubType = (byte)AdditionalTypes.Target.AllHitRateIncreased / 10
                            });
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeSkill,
                                Type = (byte)BCardType.CardType.DodgeAndDefencePercent,
                                SubType = (byte)AdditionalTypes.DodgeAndDefencePercent.DodgeIncreased / 10
                            });
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeSkill,
                                Type = (byte)BCardType.CardType.Defence,
                                SubType = (byte)AdditionalTypes.Defence.RangedIncreased / 10
                            });
                            break;
                        case 2:
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeSkill,
                                Type = (byte)BCardType.CardType.AttackPower,
                                SubType = (byte)AdditionalTypes.AttackPower.MagicalAttacksIncreased / 10
                            });
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeSkill,
                                Type = (byte)BCardType.CardType.Defence,
                                SubType = (byte)AdditionalTypes.Defence.MagicalIncreased / 10
                            });
                            break;
                        case 4:
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeSkill,
                                Type = (byte)BCardType.CardType.MaxHPMP,
                                SubType = (byte)AdditionalTypes.MaxHPMP.MaximumHPIncreased / 10
                            });
                            break;
                        case 5:
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeSkill,
                                Type = (byte)BCardType.CardType.MaxHPMP,
                                SubType = (byte)AdditionalTypes.MaxHPMP.MaximumMPIncreased / 10
                            });
                            break;
                        case 6:
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeSkill,
                                Type = (byte)BCardType.CardType.AttackPower,
                                SubType = (byte)AdditionalTypes.AttackPower.AllAttacksIncreased / 10
                            });
                            break;
                        case 7:
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeSkill,
                                Type = (byte)BCardType.CardType.Defence,
                                SubType = (byte)AdditionalTypes.Defence.AllIncreased / 10
                            });
                            break;
                        case 8:
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeSkill,
                                Type = (byte)BCardType.CardType.Recovery,
                                SubType = (byte)AdditionalTypes.Recovery.HPRecoveryIncreased / 10
                            });
                            break;
                        case 9:
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeSkill,
                                Type = (byte)BCardType.CardType.Recovery,
                                SubType = (byte)AdditionalTypes.Recovery.MPRecoveryIncreased / 10
                            });
                            break;
                        case 19:
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeType,
                                Type = (byte)BCardType.CardType.SpecialisationBuffResistance,
                                SubType = (byte)AdditionalTypes.SpecialisationBuffResistance.IncreaseDamageInPVP / 10
                            });
                            break;
                        case 20:
                            bcards.Add(new BCard
                            {
                                FirstData = -skill.Skill.UpgradeType,
                                Type = (byte)BCardType.CardType.SpecialisationBuffResistance,
                                SubType = (byte)AdditionalTypes.SpecialisationBuffResistance.DecreaseDamageInPVP / 10
                            });
                            break;
                        case 21:
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeType,
                                Type = (byte)BCardType.CardType.AttackPower,
                                SubType = (byte)AdditionalTypes.AttackPower.MeleeAttacksIncreased / 10
                            });
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeType,
                                Type = (byte)BCardType.CardType.Defence,
                                SubType = (byte)AdditionalTypes.Defence.MeleeIncreased / 10
                            });
                            break;
                        case 22:
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeType,
                                Type = (byte)BCardType.CardType.Target,
                                SubType = (byte)AdditionalTypes.Target.AllHitRateIncreased / 10
                            });
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeType,
                                Type = (byte)BCardType.CardType.DodgeAndDefencePercent,
                                SubType = (byte)AdditionalTypes.DodgeAndDefencePercent.DodgeIncreased / 10
                            });
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeType,
                                Type = (byte)BCardType.CardType.Defence,
                                SubType = (byte)AdditionalTypes.Defence.RangedIncreased / 10
                            });
                            break;
                        case 23:
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeType,
                                Type = (byte)BCardType.CardType.AttackPower,
                                SubType = (byte)AdditionalTypes.AttackPower.MagicalAttacksIncreased / 10
                            });
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeType,
                                Type = (byte)BCardType.CardType.Defence,
                                SubType = (byte)AdditionalTypes.Defence.MagicalIncreased / 10
                            });
                            break;
                        case 24:
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeType,
                                Type = (byte)BCardType.CardType.MaxHPMP,
                                SubType = (byte)AdditionalTypes.MaxHPMP.MaximumHPIncreased / 10
                            });
                            break;
                        case 25:
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeType,
                                Type = (byte)BCardType.CardType.MaxHPMP,
                                SubType = (byte)AdditionalTypes.MaxHPMP.MaximumMPIncreased / 10
                            });
                            break;
                        case 26:
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeType,
                                Type = (byte)BCardType.CardType.Defence,
                                SubType = (byte)AdditionalTypes.Defence.AllIncreased / 10
                            });
                            break;
                        case 27:
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeType,
                                Type = (byte)BCardType.CardType.AttackPower,
                                SubType = (byte)AdditionalTypes.AttackPower.AllAttacksIncreased / 10
                            });
                            break;
                        case 28:
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeType,
                                Type = (byte)BCardType.CardType.ElementResistance,
                                SubType = (byte)AdditionalTypes.ElementResistance.AllIncreased / 10
                            });
                            break;
                        case 29:
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeType,
                                Type = (byte)BCardType.CardType.Item,
                                SubType = (byte)AdditionalTypes.Item.EXPIncreased / 10
                            });
                            break;
                        case 30:
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeType,
                                Type = (byte)BCardType.CardType.Item,
                                SubType = (byte)AdditionalTypes.Item.IncreaseEarnedGold / 10
                            });
                            break;
                    }
                }
            }
            
            return bcards;
        }

        #region Singleton

        private static PassiveSkillHelper _instance;

        public static PassiveSkillHelper Instance => _instance ?? (_instance = new PassiveSkillHelper());

        #endregion
    }
}