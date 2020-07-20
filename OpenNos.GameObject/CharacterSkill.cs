/*
 * This file is part of the OpenNos Emulator Project. See AUTHORS file for Copyright information
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 */

using OpenNos.Data;
using System;
using OpenNos.GameObject.Networking;
using System.Collections.Generic;
using OpenNos.GameObject.Helpers;
using System.Linq;
using OpenNos.Domain;
using static OpenNos.Domain.BCardType;

namespace OpenNos.GameObject
{
    public class CharacterSkill : CharacterSkillDTO
    {
        #region Members

        private short? _firstCastId;

        private Skill _skill;

        #endregion

        #region Instantiation

        public CharacterSkill()
        {
            LastUse = DateTime.Now.AddHours(-1);
            Hit = 0;
        }

        public CharacterSkill(CharacterSkillDTO input) : this()
        {
            CharacterId = input.CharacterId;
            Id = input.Id;
            SkillVNum = input.SkillVNum;
        }

        #endregion

        #region Properties

        public short? FirstCastId
        {
            get => _firstCastId ?? (_firstCastId = Skill.CastId);
            set => _firstCastId = value;
        }

        public short Hit { get; set; }
        
        public DateTime LastUse { get; set; }

        public Skill Skill => _skill ?? (_skill = ServerManager.GetSkill(SkillVNum));

        #endregion

        #region Methods

        public bool CanBeUsed(bool force = false)
        {
            bool canContinue = true;
            var online = ServerManager.Instance.GetSessionByCharacterId(CharacterId);

            if (!(online is ClientSession session))
            {
                return false;
            }

            if (session.Character.Morph == (short) BrawlerMorphType.Normal)
            {
                if (Skill.SkillVNum < 1577 || Skill.SkillVNum > 1585)
                {
                    canContinue = false;
                }
            }
            else if (session.Character.Morph == (short) BrawlerMorphType.Dragon)
            {
                if (Skill.SkillVNum < 1586 || Skill.SkillVNum > 1594)
                {
                    canContinue = false;
                }
            }

            if (force)
            {
                canContinue = true;
            }

            if (!canContinue)
            {
                return false;
            }

            return Skill != null && LastUse.AddMilliseconds(Skill.Cooldown * 100) < DateTime.Now;
        }

        public List<BCard> GetSkillBCards()
        {
            List<BCard> SkillBCards = new List<BCard>();
            SkillBCards.AddRange(Skill.BCards);
            if (ServerManager.Instance.GetSessionByCharacterId(CharacterId) is ClientSession Session)
            {
                List<CharacterSkill> skills = Session.Character.GetSkills();

                //Upgrade Skills
                List<CharacterSkill> upgradeSkills = skills.FindAll(s => s.Skill?.UpgradeSkill == SkillVNum);
                if (upgradeSkills?.Count > 0)
                {
                    foreach (CharacterSkill upgradeSkill in upgradeSkills)
                    {
                        SkillBCards.AddRange(upgradeSkill.Skill.BCards);
                    }
                    if (upgradeSkills.OrderByDescending(s => s.SkillVNum).FirstOrDefault() is CharacterSkill LastUpgradeSkill)
                    {
                        if (LastUpgradeSkill.Skill.BCards.Any(s => s.Type == 25 && s.SubType == 1))
                        {
                            SkillBCards.Where(s => s.Type == 25 && s.SubType == 1 && s.SkillVNum != LastUpgradeSkill.SkillVNum).ToList().ForEach(s => SkillBCards.Remove(s)); // Only buffs of last upgrade skill
                        }
                    }
                }
                //Passive Skills
                SkillBCards.AddRange(PassiveSkillHelper.Instance.PassiveSkillToBCards(Session.Character.Skills?.Where(s => s.Skill.SkillType == 0)));

                if (Skill.SkillVNum == 1123)
                {
                    foreach (BCard ambushBCard in Session.Character.Buff.GetAllItems().SelectMany(s => s.Card.BCards.Where(b => b.Type == (byte)CardType.FearSkill && b.SubType == (byte)AdditionalTypes.FearSkill.ProduceWhenAmbushe / 10)))
                    {
                        SkillBCards.Add(ambushBCard);
                    }
                }
                else if (Skill.SkillVNum == 1124)
                {
                    foreach (BCard sniperAttackBCard in Session.Character.Buff.GetAllItems().SelectMany(s => s.Card.BCards.Where(b => b.Type == (byte)CardType.SniperAttack && b.SubType == (byte)AdditionalTypes.SniperAttack.ChanceCausing / 10)))
                    {
                        SkillBCards.Add(sniperAttackBCard);
                    }
                }
                foreach (BCard ambushAttackBCard in Session.Character.Buff.GetAllItems().SelectMany(s => s.Card.BCards.Where(b => b.Type == (byte)CardType.SniperAttack && b.SubType == (byte)AdditionalTypes.SniperAttack.ProduceChance / 10)))
                {
                    SkillBCards.Add(ambushAttackBCard);
                }
            }
            return SkillBCards.ToList();
        }

        public int GetSkillRange()
        {
            int skillRange = Skill.Range;
            if (ServerManager.Instance.GetSessionByCharacterId(CharacterId) is ClientSession Session)
            {
                skillRange += Session.Character.GetBuff(CardType.FearSkill, (byte)AdditionalTypes.FearSkill.AttackRangedIncreased)[0];
            }
            return skillRange;
        }

        public short MpCost()
        {
            short mpCost = Skill.MpCost;
            if (ServerManager.Instance.GetSessionByCharacterId(CharacterId) is ClientSession Session)
            {
                List<CharacterSkill> skills = Session.Character.GetSkills();

                //Upgrade Skills
                List<CharacterSkill> upgradeSkills = skills.FindAll(s => s.Skill?.UpgradeSkill == SkillVNum);
                if (upgradeSkills?.Count > 0)
                {
                    foreach (CharacterSkill upgradeSkill in upgradeSkills)
                    {
                        mpCost += upgradeSkill.Skill.MpCost;
                    }
                }
            }
            return mpCost;
        }

        public byte TargetRange()
        {
            byte targetRange = Skill.TargetRange;

            if (Skill.HitType != 0)
            {
                if (ServerManager.Instance.GetSessionByCharacterId(CharacterId) is ClientSession Session)
                {
                    Session.Character.Buff.GetAllItems().SelectMany(s => s.Card.BCards).Where(s => s.Type == (byte)BCardType.CardType.FireCannoneerRangeBuff
                        && s.SubType == (byte)AdditionalTypes.FireCannoneerRangeBuff.AOEIncreased / 10).ToList()
                    .ForEach(s => targetRange += (byte)s.FirstData);
                }
            }

            return targetRange;
        }

        #endregion
    }
}