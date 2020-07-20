using OpenNos.Domain;
using System;
using System.Linq;
using static OpenNos.Domain.BCardType;

namespace OpenNos.GameObject.Helpers
{
    public class PartnerSkillHelper
    {
        public static double GetMultiplierBySkillLevel(byte level)
        {
            PartnerSkillLevelType levelType = (PartnerSkillLevelType)level;

            switch (levelType)
            {
                case PartnerSkillLevelType.F:
                    return 0.3D;
                case PartnerSkillLevelType.E:
                    return 0.5D;
                case PartnerSkillLevelType.D:
                    return 0.8D;
                case PartnerSkillLevelType.C:
                    return 1.0D;
                case PartnerSkillLevelType.B:
                    return 1.2D;
                case PartnerSkillLevelType.A:
                    return 1.5D;
                case PartnerSkillLevelType.S:
                    return 2.5D;
            }

            return 0;
        }

        public static Skill ConvertToNormalSkill(PartnerSkill partnerSkill)
        {
            Skill skill = new Skill(partnerSkill.Skill)
            {
                PartnerSkill = partnerSkill
            };

            double multiplier = GetMultiplierBySkillLevel(partnerSkill.Level);

            partnerSkill.Skill.BCards.ToList().ForEach(bcard =>
            {
                BCard newBCard = new BCard(bcard)
                {
                    IsPartnerSkillBCard = true
                };

                switch ((CardType)newBCard.Type)
                {
                    case CardType.DrainAndSteal:
                        {
                            if (newBCard.SubType == (byte)AdditionalTypes.DrainAndSteal.LeechEnemyHP / 10)
                            {
                                newBCard.SecondData = Convert.ToInt32(Math.Floor(multiplier * newBCard.SecondData));
                            }
                        }
                        break;

                    case CardType.Buff:
                        {
                            if (newBCard.SecondData != 7 /* Blackout */)
                            {
                                newBCard.SecondData += (partnerSkill.Level - 1);
                            }
                        }
                        break;

                    default:
                        {
                            if (newBCard.FirstData != 0 && newBCard.IsLevelScaled)
                            {
                                newBCard.FirstData = Convert.ToInt32(Math.Floor(multiplier * newBCard.FirstData));
                                newBCard.IsLevelScaled = false;
                            }
                        }
                        break;
                }

                skill.BCards.Add(newBCard);
            });

            return skill;
        }
    }
}
