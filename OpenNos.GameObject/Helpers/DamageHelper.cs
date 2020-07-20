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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using OpenNos.Data;
using OpenNos.Domain;
using OpenNos.GameObject.Networking;
using OpenNos.Master.Library.Client;
using static OpenNos.Domain.BCardType;

namespace OpenNos.GameObject.Helpers
{
    public class DamageHelper
    {
        #region Members

        private static DamageHelper _instance;

        #endregion

        #region Properties

        public static DamageHelper Instance => _instance ?? (_instance = new DamageHelper());

        #endregion

        #region Methods

        /// <summary>
        /// Calculates the damage attacker inflicts defender
        /// </summary>
        /// <param name="attacker">The attacking Entity</param>
        /// <param name="defender">The defending Entity</param>
        /// <param name="skill">The used Skill</param>
        /// <param name="hitMode">reference to HitMode</param>
        /// <param name="onyxWings"></param>
        /// <returns>Damage</returns>
        public int CalculateDamage(BattleEntity attacker, BattleEntity defender, Skill skill, ref int hitMode,
            ref bool onyxWings, bool attackGreaterDistance = false)
        {
            if (!attacker.CanAttackEntity(defender))
            {
                hitMode = 2;
                return 0;
            }

            if (attacker.Character?.Timespace != null && attacker.Character.Timespace.SpNeeded?[(byte)attacker.Character.Class] != 0)
            {
                ItemInstance specialist = attacker.Character.Inventory?.LoadBySlotAndType((byte)EquipmentType.Sp, InventoryType.Wear);
                if (specialist == null || specialist.ItemVNum != attacker.Character?.Timespace.SpNeeded?[(byte)attacker.Character.Class])
                {
                    hitMode = 2;
                    return 0;
                }
            }

            if (skill != null && SkillHelper.IsSelfAttack(skill.SkillVNum))
            {
                hitMode = 0;
                return 0;
            }

            int maxRange = skill != null ? skill.Range > 0 ? skill.Range : skill.TargetRange : attacker.Mate != null ? attacker.Mate.Monster.BasicRange > 0 ? attacker.Mate.Monster.BasicRange : 10 : attacker.MapMonster != null ? attacker.MapMonster.Monster.BasicRange > 0 ? attacker.MapMonster.Monster.BasicRange : 10 : attacker.MapNpc != null ? attacker.MapNpc.Npc.BasicRange > 0 ? attacker.MapNpc.Npc.BasicRange : 10 : 0;

            if (skill != null && skill.HitType == 1 && skill.TargetType == 1 && skill.TargetRange > 0)
            {
                maxRange = skill.TargetRange;
            }

            if (skill != null && skill.HitType == 2 && skill.TargetType == 1 && skill.TargetRange > 0)
            {
                maxRange = skill.TargetRange;
            }

            if (skill != null && (skill.CastEffect == 4657 || skill.CastEffect == 4940))
            {
                maxRange = 3;
            }

            if ((attacker.EntityType == defender.EntityType && attacker.MapEntityId == defender.MapEntityId)
              || attacker.Character == null && attacker.Mate == null && Map.GetDistance(new MapCell { X = attacker.PositionX, Y = attacker.PositionY }, new MapCell { X = defender.PositionX, Y = defender.PositionY }) > maxRange)
            {
                if (skill == null || skill.TargetRange != 0 || skill.Range != 0 && !attackGreaterDistance)
                {
                    hitMode = 2;
                    return 0;
                }
            }

            if (skill != null && skill.BCards.Any(s => s.Type == (byte)CardType.DrainAndSteal && s.SubType == (byte)AdditionalTypes.DrainAndSteal.ConvertEnemyHPToMP / 10))
            {
                return 0;
            }

            if (attacker.Character != null
                && ((attacker.Character.UseSp
                && attacker.Character.Inventory.LoadBySlotAndType((byte)EquipmentType.Sp, InventoryType.Wear) is ItemInstance attackerSp
                && skill?.Element == 0
                && attackerSp.ItemVNum != 900
                && attackerSp.ItemVNum != 907
                && attackerSp.ItemVNum != 908
                && attackerSp.ItemVNum != 4099
                && attackerSp.ItemVNum != 4100
                && ((skill == null || defender.MapMonster?.Monster.Race != 5 || !skill.BCards.Any(s => s.Type == (byte)CardType.LightAndShadow && s.SubType == (byte)AdditionalTypes.LightAndShadow.InflictDamageOnUndead / 10))
                && skill?.SkillVNum != 1065 && skill?.SkillVNum != 1248)
                || (defender.MapMonster?.Owner?.MapEntityId == attacker.MapEntityId && !defender.IsMateTrainer(defender.MapMonster.MonsterVNum))))
                || skill?.SkillVNum >= 235 && skill?.SkillVNum <= 237 || skill?.SkillVNum == 274 || skill?.SkillVNum == 276 || skill?.SkillVNum == 892 || skill?.SkillVNum == 916
                || skill?.SkillVNum == 1129 || skill?.SkillVNum == 1133 || skill?.SkillVNum == 1137 || skill?.SkillVNum == 1138 || skill?.SkillVNum == 1329
                || attacker.MapMonster?.MonsterVNum == 1438 || attacker.MapMonster?.MonsterVNum == 1439)
            {
                hitMode = 0;
                return 0;
            }

            if (defender.Character != null && defender.Character.HasGodMode)
            {
                hitMode = 0;
                return 0;
            }

            if (defender.MapMonster != null && skill?.SkillVNum != 888 && MonsterHelper.IsNamaju(defender.MapMonster.MonsterVNum))
            {
                hitMode = 0;
                return 0;
            }

            if (attacker.MapMonster?.MonsterVNum == 1436)
            {
                hitMode = 0;
                return 0;
            }

            if (attacker.HasBuff(CardType.NoDefeatAndNoDamage, (byte)AdditionalTypes.NoDefeatAndNoDamage.NeverCauseDamage)
             || defender.HasBuff(CardType.NoDefeatAndNoDamage, (byte)AdditionalTypes.NoDefeatAndNoDamage.NeverReceiveDamage))
            {
                hitMode = 0;
                return 0;
            }

            int totalDamage = 0;
            bool percentDamage = false;

            BattleEntity realAttacker = attacker;

            if (attacker.MapMonster?.Owner?.Character != null && !attacker.IsMateTrainer(attacker.MapMonster.MonsterVNum))
            {
                if (attacker.DamageMinimum == 0 || MonsterHelper.UseOwnerEntity(attacker.MapMonster.MonsterVNum))
                {
                    attacker = new BattleEntity(attacker.MapMonster.Owner.Character, skill);
                }
            }

            List<BCard> attackerBCards = attacker.BCards.ToList();
            List<BCard> defenderBCards = defender.BCards.ToList();

            if (attacker.Character != null)
            {
                List<CharacterSkill> skills = attacker.Character.GetSkills();
                //Upgrade Skills
                if (skill != null && skills.FirstOrDefault(s => s.SkillVNum == skill.SkillVNum) is CharacterSkill charSkill)
                {
                    attackerBCards.AddRange(charSkill.GetSkillBCards());
                }
                else // Passive Skills are getted on GetSkillBCards()
                {
                    if (skill?.BCards != null)
                    {
                        attackerBCards.AddRange(skill.BCards);
                    }
                    //Passive Skills
                    attackerBCards.AddRange(PassiveSkillHelper.Instance.PassiveSkillToBCards(attacker.Character.Skills?.Where(s => s.Skill.SkillType == 0)));
                }
            }
            else
            {
                if (skill?.BCards != null)
                {
                    attackerBCards.AddRange(skill.BCards);
                }
            }

            int[] GetAttackerBenefitingBuffs(CardType type, byte subtype, bool castTypeNotZero = false)
            {
                int value1 = 0;
                int value2 = 0;
                int value3 = 0;
                int temp = 0;

                int[] tmp = GetBuff(attacker.Level, attacker.Buffs.GetAllItems(), attackerBCards, type, subtype, BuffType.Good,
                    ref temp, castTypeNotZero);
                value1 += tmp[0];
                value2 += tmp[1];
                value3 += tmp[2];
                tmp = GetBuff(attacker.Level, attacker.Buffs.GetAllItems(), attackerBCards, type, subtype, BuffType.Neutral,
                    ref temp, castTypeNotZero);
                value1 += tmp[0];
                value2 += tmp[1];
                value3 += tmp[2];
                tmp = GetBuff(defender.Level, defender.Buffs.GetAllItems(), defenderBCards, type, subtype, BuffType.Bad, ref temp, castTypeNotZero);
                value1 += tmp[0];
                value2 += tmp[1];
                value3 += tmp[2];

                return new[] { value1, value2, value3, temp };
            }

            int[] GetDefenderBenefitingBuffs(CardType type, byte subtype)
            {
                int value1 = 0;
                int value2 = 0;
                int value3 = 0;
                int temp = 0;

                int[] tmp = GetBuff(defender.Level, defender.Buffs.GetAllItems(), defenderBCards, type, subtype, BuffType.Good,
                    ref temp);
                value1 += tmp[0];
                value2 += tmp[1];
                value3 += tmp[2];
                tmp = GetBuff(defender.Level, defender.Buffs.GetAllItems(), defenderBCards, type, subtype, BuffType.Neutral,
                    ref temp);
                value1 += tmp[0];
                value2 += tmp[1];
                value3 += tmp[2];
                tmp = GetBuff(attacker.Level, attacker.Buffs.GetAllItems(), attackerBCards, type, subtype, BuffType.Bad, ref temp);
                value1 += tmp[0];
                value2 += tmp[1];
                value3 += tmp[2];

                return new[] { value1, value2, value3, temp };
            }

            int GetShellWeaponEffectValue(ShellWeaponEffectType effectType)
            {
                return attacker.ShellWeaponEffects?.Where(s => s.Effect == (byte)effectType).FirstOrDefault()?.Value ??
                       0;
            }

            int GetShellArmorEffectValue(ShellArmorEffectType effectType)
            {
                return defender.ShellArmorEffects?.Where(s => s.Effect == (byte)effectType).FirstOrDefault()?.Value ??
                       0;
            }

            #region Basic Buff Initialisation

            attacker.Morale +=
                GetAttackerBenefitingBuffs(CardType.Morale, (byte)AdditionalTypes.Morale.MoraleIncreased)[0];
            attacker.Morale +=
                GetDefenderBenefitingBuffs(CardType.Morale, (byte)AdditionalTypes.Morale.MoraleDecreased)[0];
            defender.Morale +=
                GetDefenderBenefitingBuffs(CardType.Morale, (byte)AdditionalTypes.Morale.MoraleIncreased)[0];
            defender.Morale +=
                GetAttackerBenefitingBuffs(CardType.Morale, (byte)AdditionalTypes.Morale.MoraleDecreased)[0];

            attacker.AttackUpgrade += (short)GetAttackerBenefitingBuffs(CardType.AttackPower,
                (byte)AdditionalTypes.AttackPower.AttackLevelIncreased)[0];
            attacker.AttackUpgrade += (short)GetDefenderBenefitingBuffs(CardType.AttackPower,
                (byte)AdditionalTypes.AttackPower.AttackLevelDecreased)[0];
            defender.DefenseUpgrade += (short)GetDefenderBenefitingBuffs(CardType.Defence,
                (byte)AdditionalTypes.Defence.DefenceLevelIncreased)[0];
            defender.DefenseUpgrade += (short)GetAttackerBenefitingBuffs(CardType.Defence,
                (byte)AdditionalTypes.Defence.DefenceLevelDecreased)[0];

            if (attacker.AttackUpgrade > 10)
            {
                attacker.AttackUpgrade = 10;
            }
            if (defender.DefenseUpgrade > 10)
            {
                defender.DefenseUpgrade = 10;
            }

            int[] attackerpercentdamage = GetAttackerBenefitingBuffs(CardType.RecoveryAndDamagePercent, (byte)AdditionalTypes.RecoveryAndDamagePercent.HPRecovered, true);
            int[] attackerpercentdamage2 = GetAttackerBenefitingBuffs(CardType.RecoveryAndDamagePercent, (byte)AdditionalTypes.RecoveryAndDamagePercent.DecreaseEnemyHP);
            int[] defenderpercentdefense = GetDefenderBenefitingBuffs(CardType.RecoveryAndDamagePercent, (byte)AdditionalTypes.RecoveryAndDamagePercent.DecreaseSelfHP);

            if (attackerpercentdamage[3] != 0)
            {
                totalDamage = defender.HpMax / 100 * attackerpercentdamage[2];
                percentDamage = true;
            }

            if (attackerpercentdamage2[3] != 0)
            {
                totalDamage = defender.HpMax / 100 * Math.Abs(attackerpercentdamage2[0]);
                percentDamage = true;
            }

            if (defenderpercentdefense[3] != 0)
            {
                totalDamage = defender.HpMax / 100 * Math.Abs(defenderpercentdefense[0]);
                percentDamage = true;
            }

            if (defender.MapMonster != null && defender.MapMonster.MonsterVNum == 1381 && // Jack O'Lantern
              ((attacker.Character != null && attacker.Character.Group?.Raid?.Id == 10)
            || (attacker.Mate != null && attacker.Mate.Owner.Group?.Raid?.Id == 10)
            || (attacker.MapMonster != null && (attacker.MapMonster.Owner?.Character?.Group?.Raid?.Id == 10 || attacker.MapMonster.Owner?.Mate?.Owner.Group?.Raid?.Id == 10))))
            {
                if (attacker.Character != null && attacker.Character.IsMorphed)
                {
                    totalDamage = 600;
                }
                else
                {
                    totalDamage = 150;
                }
                percentDamage = true;
            }

            if (defender.MapMonster?.MonsterVNum == 533)
            {
                totalDamage = 63;
                percentDamage = true;
            }

            if (skill?.SkillVNum == 529 && (defender.Character?.PyjamaDead == true || defender.Mate?.Owner.PyjamaDead == true))
            {
                totalDamage = (int)(defender.HpMax * 0.8D);
                percentDamage = true;
            }

            /*
             *
             * Percentage Boost categories:
             *  1.: Adds to Total Damage
             *  2.: Adds to Normal Damage
             *  3.: Adds to Base Damage
             *  4.: Adds to Defense
             *  5.: Adds to Element
             *
             * Buff Effects get added, whereas
             * Shell Effects get multiplied afterwards.
             *
             * Simplified Example on Defense (Same for Attack):
             *  - 1k Defense
             *  - Costume(+5% Defense)
             *  - Defense Potion(+20% Defense)
             *  - S-Defense Shell with 20% Boost
             *
             * Calculation:
             *  1000 * 1.25 * 1.2 = 1500
             *  Def    Buff   Shell Total
             *
             * Keep in Mind that after each step, one has
             * to round the current value down if necessary
             *
             * Static Boost categories:
             *  1.: Adds to Total Damage
             *  2.: Adds to Normal Damage
             *  3.: Adds to Base Damage
             *  4.: Adds to Defense
             *  5.: Adds to Element
             *
             */

            attacker.Morale -= defender.Morale;

            int hitrate = attacker.Hitrate + attacker.Morale;

            #region Definitions

            double boostCategory1 = 1;
            double boostCategory2 = 1;
            double boostCategory3 = 1;
            double boostCategory4 = 1;
            double boostCategory5 = 1;
            double shellBoostCategory1 = 1;
            double shellBoostCategory2 = 1;
            double shellBoostCategory3 = 1;
            double shellBoostCategory4 = 1;
            double shellBoostCategory5 = 1;
            int staticBoostCategory1 = 0;
            int staticBoostCategory2 = 0;
            int staticBoostCategory3 = 0;
            int staticBoostCategory4 = 0;
            int staticBoostCategory5 = 0;

            #endregion

            #region Type 1

            #region Static

            // None for now

            #endregion

            #region Boost

            shellBoostCategory1 += GetShellWeaponEffectValue(ShellWeaponEffectType.PercentageTotalDamage) / 100D;

            if ((attacker.EntityType == EntityType.Player || attacker.EntityType == EntityType.Mate)
                && (defender.EntityType == EntityType.Player || defender.EntityType == EntityType.Mate))
            {
                shellBoostCategory1 += GetShellWeaponEffectValue(ShellWeaponEffectType.PercentageDamageInPVP) / 100D;
            }

            #endregion

            #endregion

            #region Type 2

            #region Static

            if (attacker.Character != null && attacker.Character.Invisible)
            {
                staticBoostCategory2 +=
                    GetAttackerBenefitingBuffs(CardType.LightAndShadow, (byte)AdditionalTypes.LightAndShadow.AdditionalDamageWhenHidden)[0];
            }

            #endregion

            #region Boost

            boostCategory2 +=
                GetAttackerBenefitingBuffs(CardType.Damage, (byte)AdditionalTypes.Damage.DamageIncreased)
                    [0] / 100D;
            boostCategory2 +=
                GetDefenderBenefitingBuffs(CardType.Damage, (byte)AdditionalTypes.Damage.DamageDecreased)
                    [0] / 100D;
            boostCategory2 +=
                GetAttackerBenefitingBuffs(CardType.Item, (byte)AdditionalTypes.Item.AttackIncreased)[0]
                / 100D;
            boostCategory2 +=
                GetDefenderBenefitingBuffs(CardType.Item, (byte)AdditionalTypes.Item.DefenceIncreased)[0]
                / 100D;

            if ((attacker.EntityType == EntityType.Player || attacker.EntityType == EntityType.Mate)
                && (defender.EntityType == EntityType.Player || defender.EntityType == EntityType.Mate))
            {
                boostCategory2 += GetAttackerBenefitingBuffs(CardType.SpecialisationBuffResistance,
                                      (byte)AdditionalTypes.SpecialisationBuffResistance.IncreaseDamageInPVP)[0]
                                  / 100D;
                boostCategory2 += GetDefenderBenefitingBuffs(CardType.SpecialisationBuffResistance,
                                      (byte)AdditionalTypes.SpecialisationBuffResistance.DecreaseDamageInPVP)[0]
                                  / 100D;
                boostCategory2 += GetAttackerBenefitingBuffs(CardType.LeonaPassiveSkill,
                                      (byte)AdditionalTypes.LeonaPassiveSkill.AttackIncreasedInPVP)[0] / 100D;
                boostCategory2 += GetDefenderBenefitingBuffs(CardType.LeonaPassiveSkill,
                                      (byte)AdditionalTypes.LeonaPassiveSkill.AttackDecreasedInPVP)[0] / 100D;
            }

            if (defender.MapMonster != null)
            {
                if (GetAttackerBenefitingBuffs(CardType.LeonaPassiveSkill,
                                    (byte)AdditionalTypes.LeonaPassiveSkill.IncreaseDamageAgainst) is int[] IncreaseDamageAgainst
                                    && IncreaseDamageAgainst[1] > 0 && defender.MapMonster.Monster.RaceType == IncreaseDamageAgainst[0])
                {
                    boostCategory2 += IncreaseDamageAgainst[1] / 100D;
                }
            }

            #endregion

            #endregion

            #region Type 3

            #region Static

            staticBoostCategory3 += GetAttackerBenefitingBuffs(CardType.AttackPower,
                (byte)AdditionalTypes.AttackPower.AllAttacksIncreased)[0];
            staticBoostCategory3 += GetDefenderBenefitingBuffs(CardType.AttackPower,
                (byte)AdditionalTypes.AttackPower.AllAttacksDecreased)[0];
            staticBoostCategory3 += GetShellWeaponEffectValue(ShellWeaponEffectType.DamageImproved);

            #endregion

            #region Soft-Damage

            int[] soft = GetAttackerBenefitingBuffs(CardType.IncreaseDamage,
                (byte)AdditionalTypes.IncreaseDamage.IncreasingPropability);
            int[] skin = GetAttackerBenefitingBuffs(CardType.EffectSummon,
                (byte)AdditionalTypes.EffectSummon.DamageBoostOnHigherLvl);
            if (attacker.Level < defender.Level)
            {
                soft[0] += skin[0];
                soft[1] += skin[1];
            }

            if (attacker == realAttacker && ServerManager.RandomNumber() < soft[0])
            {
                boostCategory3 += soft[1] / 100D;
                attacker.MapInstance?.Broadcast(
                    StaticPacketHelper.GenerateEff(realAttacker.UserType, realAttacker.MapEntityId, 15));
            }

            #endregion

            #endregion

            #region Type 4

            #region Static

            staticBoostCategory4 +=
                GetDefenderBenefitingBuffs(CardType.Defence, (byte)AdditionalTypes.Defence.AllIncreased)[0];
            staticBoostCategory4 +=
                GetAttackerBenefitingBuffs(CardType.Defence, (byte)AdditionalTypes.Defence.AllDecreased)[0];

            int temp2 = 0;
            staticBoostCategory4 +=
                GetBuff(defender.Level, defender.Buffs.GetAllItems(), defenderBCards, CardType.Defence, (byte)AdditionalTypes.Defence.AllDecreased, BuffType.Good, ref temp2)[0];

            #endregion

            #region Boost

            boostCategory4 += GetDefenderBenefitingBuffs(CardType.DodgeAndDefencePercent, (byte)AdditionalTypes.DodgeAndDefencePercent.DefenceIncreased)[0] / 100D;
            boostCategory4 += GetAttackerBenefitingBuffs(CardType.DodgeAndDefencePercent, (byte)AdditionalTypes.DodgeAndDefencePercent.DefenceReduced)[0] / 100D;
            shellBoostCategory4 += GetShellArmorEffectValue(ShellArmorEffectType.PercentageTotalDefence) / 100D;

            if ((attacker.EntityType == EntityType.Player || attacker.EntityType == EntityType.Mate)
                && (defender.EntityType == EntityType.Player || defender.EntityType == EntityType.Mate))
            {
                boostCategory4 += GetDefenderBenefitingBuffs(CardType.LeonaPassiveSkill, (byte)AdditionalTypes.LeonaPassiveSkill.DefenceIncreasedInPVP)[0] / 100D;
                boostCategory4 += GetAttackerBenefitingBuffs(CardType.LeonaPassiveSkill, (byte)AdditionalTypes.LeonaPassiveSkill.DefenceDecreasedInPVP)[0] / 100D;
                shellBoostCategory4 -= GetShellWeaponEffectValue(ShellWeaponEffectType.ReducesPercentageEnemyDefenceInPVP) / 100D;
                shellBoostCategory4 += GetShellArmorEffectValue(ShellArmorEffectType.PercentageAllPVPDefence) / 100D;
            }

            int[] chanceAllIncreased = GetAttackerBenefitingBuffs(CardType.Block, (byte)AdditionalTypes.Block.ChanceAllIncreased);
            int[] chanceAllDecreased = GetDefenderBenefitingBuffs(CardType.Block, (byte)AdditionalTypes.Block.ChanceAllDecreased);

            if (ServerManager.RandomNumber() < chanceAllIncreased[0])
            {
                boostCategory1 += chanceAllIncreased[1] / 100D;
            }

            if (ServerManager.RandomNumber() < -chanceAllDecreased[0])
            {
                boostCategory1 -= chanceAllDecreased[1] / 100D;
            }

            #endregion

            #endregion

            #region Type 5

            #region Static

            staticBoostCategory5 +=
                GetAttackerBenefitingBuffs(CardType.Element, (byte)AdditionalTypes.Element.AllIncreased)[0];
            staticBoostCategory5 +=
                GetDefenderBenefitingBuffs(CardType.Element, (byte)AdditionalTypes.Element.AllDecreased)[0];
            staticBoostCategory5 += GetShellWeaponEffectValue(ShellWeaponEffectType.IncreasedElementalProperties);

            #endregion

            #region Boost

            #endregion

            #endregion

            #region All Type Class Dependant

            int[] chanceIncreased = null;
            int[] chanceDecreased = null;

            switch (attacker.AttackType)
            {
                case AttackType.Melee:
                    chanceIncreased = GetAttackerBenefitingBuffs(CardType.Block, (byte)AdditionalTypes.Block.ChanceMeleeIncreased);
                    chanceDecreased = GetDefenderBenefitingBuffs(CardType.Block, (byte)AdditionalTypes.Block.ChanceMeleeDecreased);
                    boostCategory2 += GetAttackerBenefitingBuffs(CardType.Damage, (byte)AdditionalTypes.Damage.MeleeIncreased)[0] / 100D;
                    boostCategory2 += GetDefenderBenefitingBuffs(CardType.Damage, (byte)AdditionalTypes.Damage.MeleeDecreased)[0] / 100D;
                    staticBoostCategory3 += GetAttackerBenefitingBuffs(CardType.AttackPower, (byte)AdditionalTypes.AttackPower.MeleeAttacksIncreased)[0];
                    staticBoostCategory3 += GetDefenderBenefitingBuffs(CardType.AttackPower, (byte)AdditionalTypes.AttackPower.MeleeAttacksDecreased)[0];
                    staticBoostCategory4 += GetShellArmorEffectValue(ShellArmorEffectType.CloseDefence);
                    staticBoostCategory4 += GetDefenderBenefitingBuffs(CardType.Defence, (byte)AdditionalTypes.Defence.MeleeIncreased)[0];
                    staticBoostCategory4 += GetAttackerBenefitingBuffs(CardType.Defence, (byte)AdditionalTypes.Defence.MeleeDecreased)[0];
                    break;

                case AttackType.Range:
                    chanceIncreased = GetAttackerBenefitingBuffs(CardType.Block, (byte)AdditionalTypes.Block.ChanceRangedIncreased);
                    chanceDecreased = GetDefenderBenefitingBuffs(CardType.Block, (byte)AdditionalTypes.Block.ChanceRangedDecreased);
                    boostCategory2 += GetAttackerBenefitingBuffs(CardType.Damage, (byte)AdditionalTypes.Damage.RangedIncreased)[0] / 100D;
                    boostCategory2 += GetDefenderBenefitingBuffs(CardType.Damage, (byte)AdditionalTypes.Damage.RangedDecreased)[0] / 100D;
                    staticBoostCategory3 += GetAttackerBenefitingBuffs(CardType.AttackPower, (byte)AdditionalTypes.AttackPower.MeleeAttacksIncreased)[0];
                    staticBoostCategory3 += GetDefenderBenefitingBuffs(CardType.AttackPower, (byte)AdditionalTypes.AttackPower.MeleeAttacksDecreased)[0];
                    staticBoostCategory4 += GetShellArmorEffectValue(ShellArmorEffectType.DistanceDefence);
                    staticBoostCategory4 += GetDefenderBenefitingBuffs(CardType.Defence, (byte)AdditionalTypes.Defence.RangedIncreased)[0];
                    staticBoostCategory4 += GetAttackerBenefitingBuffs(CardType.Defence, (byte)AdditionalTypes.Defence.RangedDecreased)[0];
                    break;

                case AttackType.Magical:
                    chanceIncreased = GetAttackerBenefitingBuffs(CardType.Block, (byte)AdditionalTypes.Block.ChanceMagicalIncreased);
                    chanceDecreased = GetDefenderBenefitingBuffs(CardType.Block, (byte)AdditionalTypes.Block.ChanceMagicalDecreased);
                    boostCategory2 += GetAttackerBenefitingBuffs(CardType.Damage, (byte)AdditionalTypes.Damage.MagicalIncreased)[0] / 100D;
                    boostCategory2 += GetDefenderBenefitingBuffs(CardType.Damage, (byte)AdditionalTypes.Damage.MagicalDecreased)[0] / 100D;
                    staticBoostCategory3 += GetAttackerBenefitingBuffs(CardType.AttackPower, (byte)AdditionalTypes.AttackPower.MeleeAttacksIncreased)[0];
                    staticBoostCategory3 += GetDefenderBenefitingBuffs(CardType.AttackPower, (byte)AdditionalTypes.AttackPower.MeleeAttacksDecreased)[0];
                    staticBoostCategory4 += GetShellArmorEffectValue(ShellArmorEffectType.MagicDefence);
                    staticBoostCategory4 += GetDefenderBenefitingBuffs(CardType.Defence, (byte)AdditionalTypes.Defence.MagicalIncreased)[0];
                    staticBoostCategory4 += GetAttackerBenefitingBuffs(CardType.Defence, (byte)AdditionalTypes.Defence.MagicalDecreased)[0];
                    break;
            }

            if (ServerManager.RandomNumber() < chanceIncreased[0])
            {
                boostCategory1 += chanceIncreased[1] / 100D;
            }

            if (ServerManager.RandomNumber() < -chanceDecreased[0])
            {
                boostCategory1 -= chanceDecreased[1] / 100D;
            }

            #endregion

            #region Element Dependant

            switch (realAttacker.Element)
            {
                case 1:
                    defender.FireResistance += GetDefenderBenefitingBuffs(CardType.ElementResistance,
                        (byte)AdditionalTypes.ElementResistance.AllIncreased)[0];
                    defender.FireResistance += GetAttackerBenefitingBuffs(CardType.ElementResistance,
                        (byte)AdditionalTypes.ElementResistance.AllDecreased)[0];
                    defender.FireResistance += GetDefenderBenefitingBuffs(CardType.ElementResistance,
                        (byte)AdditionalTypes.ElementResistance.FireIncreased)[0];
                    defender.FireResistance += GetAttackerBenefitingBuffs(CardType.ElementResistance,
                        (byte)AdditionalTypes.ElementResistance.FireDecreased)[0];
                    defender.FireResistance += GetDefenderBenefitingBuffs(CardType.EnemyElementResistance,
                        (byte)AdditionalTypes.EnemyElementResistance.AllIncreased)[0];
                    defender.FireResistance += GetAttackerBenefitingBuffs(CardType.EnemyElementResistance,
                        (byte)AdditionalTypes.EnemyElementResistance.AllDecreased)[0];
                    defender.FireResistance += GetDefenderBenefitingBuffs(CardType.EnemyElementResistance,
                        (byte)AdditionalTypes.EnemyElementResistance.FireIncreased)[0];
                    defender.FireResistance += GetAttackerBenefitingBuffs(CardType.EnemyElementResistance,
                        (byte)AdditionalTypes.EnemyElementResistance.FireDecreased)[0];
                    if ((attacker.EntityType == EntityType.Player || attacker.EntityType == EntityType.Mate)
                        && (defender.EntityType == EntityType.Player || defender.EntityType == EntityType.Mate))
                    {
                        defender.FireResistance -=
                            GetShellWeaponEffectValue(ShellWeaponEffectType.ReducesEnemyFireResistanceInPVP);
                        defender.FireResistance -=
                            GetShellWeaponEffectValue(ShellWeaponEffectType.ReducesEnemyAllResistancesInPVP);
                    }

                    defender.FireResistance += GetShellArmorEffectValue(ShellArmorEffectType.IncreasedFireResistence);
                    defender.FireResistance += GetShellArmorEffectValue(ShellArmorEffectType.IncreasedAllResistence);
                    staticBoostCategory5 += GetShellWeaponEffectValue(ShellWeaponEffectType.IncreasedFireProperties);
                    boostCategory5 += GetAttackerBenefitingBuffs(CardType.IncreaseDamage,
                                          (byte)AdditionalTypes.IncreaseDamage.FireIncreased)[0] / 100D;
                    staticBoostCategory5 += GetAttackerBenefitingBuffs(CardType.Element,
                        (byte)AdditionalTypes.Element.FireIncreased)[0];
                    staticBoostCategory5 += GetDefenderBenefitingBuffs(CardType.Element,
                        (byte)AdditionalTypes.Element.FireDecreased)[0];
                    break;

                case 2:
                    defender.WaterResistance += GetDefenderBenefitingBuffs(CardType.ElementResistance,
                        (byte)AdditionalTypes.ElementResistance.AllIncreased)[0];
                    defender.WaterResistance += GetAttackerBenefitingBuffs(CardType.ElementResistance,
                        (byte)AdditionalTypes.ElementResistance.AllDecreased)[0];
                    defender.WaterResistance += GetDefenderBenefitingBuffs(CardType.ElementResistance,
                        (byte)AdditionalTypes.ElementResistance.WaterIncreased)[0];
                    defender.WaterResistance += GetAttackerBenefitingBuffs(CardType.ElementResistance,
                        (byte)AdditionalTypes.ElementResistance.WaterDecreased)[0];
                    defender.WaterResistance += GetDefenderBenefitingBuffs(CardType.EnemyElementResistance,
                        (byte)AdditionalTypes.EnemyElementResistance.AllIncreased)[0];
                    defender.WaterResistance += GetAttackerBenefitingBuffs(CardType.EnemyElementResistance,
                        (byte)AdditionalTypes.EnemyElementResistance.AllDecreased)[0];
                    defender.WaterResistance += GetDefenderBenefitingBuffs(CardType.EnemyElementResistance,
                        (byte)AdditionalTypes.EnemyElementResistance.WaterIncreased)[0];
                    defender.WaterResistance += GetAttackerBenefitingBuffs(CardType.EnemyElementResistance,
                        (byte)AdditionalTypes.EnemyElementResistance.WaterDecreased)[0];
                    if ((attacker.EntityType == EntityType.Player || attacker.EntityType == EntityType.Mate)
                        && (defender.EntityType == EntityType.Player || defender.EntityType == EntityType.Mate))
                    {
                        defender.FireResistance -=
                            GetShellWeaponEffectValue(ShellWeaponEffectType.ReducesEnemyWaterResistanceInPVP);
                        defender.FireResistance -=
                            GetShellWeaponEffectValue(ShellWeaponEffectType.ReducesEnemyAllResistancesInPVP);
                    }

                    defender.FireResistance += GetShellArmorEffectValue(ShellArmorEffectType.IncreasedWaterResistence);
                    defender.FireResistance += GetShellArmorEffectValue(ShellArmorEffectType.IncreasedAllResistence);
                    staticBoostCategory5 += GetShellWeaponEffectValue(ShellWeaponEffectType.IncreasedWaterProperties);
                    boostCategory5 += GetAttackerBenefitingBuffs(CardType.IncreaseDamage,
                                          (byte)AdditionalTypes.IncreaseDamage.WaterIncreased)[0] / 100D;
                    staticBoostCategory5 += GetAttackerBenefitingBuffs(CardType.Element,
                        (byte)AdditionalTypes.Element.WaterIncreased)[0];
                    staticBoostCategory5 += GetDefenderBenefitingBuffs(CardType.Element,
                        (byte)AdditionalTypes.Element.WaterDecreased)[0];
                    break;

                case 3:
                    defender.LightResistance += GetDefenderBenefitingBuffs(CardType.ElementResistance,
                        (byte)AdditionalTypes.ElementResistance.AllIncreased)[0];
                    defender.LightResistance += GetAttackerBenefitingBuffs(CardType.ElementResistance,
                        (byte)AdditionalTypes.ElementResistance.AllDecreased)[0];
                    defender.LightResistance += GetDefenderBenefitingBuffs(CardType.ElementResistance,
                        (byte)AdditionalTypes.ElementResistance.LightIncreased)[0];
                    defender.LightResistance += GetAttackerBenefitingBuffs(CardType.ElementResistance,
                        (byte)AdditionalTypes.ElementResistance.LightDecreased)[0];
                    defender.LightResistance += GetDefenderBenefitingBuffs(CardType.EnemyElementResistance,
                        (byte)AdditionalTypes.EnemyElementResistance.AllIncreased)[0];
                    defender.LightResistance += GetAttackerBenefitingBuffs(CardType.EnemyElementResistance,
                        (byte)AdditionalTypes.EnemyElementResistance.AllDecreased)[0];
                    defender.LightResistance += GetDefenderBenefitingBuffs(CardType.EnemyElementResistance,
                        (byte)AdditionalTypes.EnemyElementResistance.LightIncreased)[0];
                    defender.LightResistance += GetAttackerBenefitingBuffs(CardType.EnemyElementResistance,
                        (byte)AdditionalTypes.EnemyElementResistance.LightDecreased)[0];
                    if ((attacker.EntityType == EntityType.Player || attacker.EntityType == EntityType.Mate)
                        && (defender.EntityType == EntityType.Player || defender.EntityType == EntityType.Mate))
                    {
                        defender.FireResistance -=
                            GetShellWeaponEffectValue(ShellWeaponEffectType.ReducesEnemyLightResistanceInPVP);
                        defender.FireResistance -=
                            GetShellWeaponEffectValue(ShellWeaponEffectType.ReducesEnemyAllResistancesInPVP);
                    }

                    defender.FireResistance += GetShellArmorEffectValue(ShellArmorEffectType.IncreasedLightResistence);
                    defender.FireResistance += GetShellArmorEffectValue(ShellArmorEffectType.IncreasedAllResistence);
                    staticBoostCategory5 += GetShellWeaponEffectValue(ShellWeaponEffectType.IncreasedLightProperties);
                    boostCategory5 += GetAttackerBenefitingBuffs(CardType.IncreaseDamage,
                                          (byte)AdditionalTypes.IncreaseDamage.LightIncreased)[0] / 100D;
                    staticBoostCategory5 += GetAttackerBenefitingBuffs(CardType.Element,
                        (byte)AdditionalTypes.Element.LightIncreased)[0];
                    staticBoostCategory5 += GetDefenderBenefitingBuffs(CardType.Element,
                        (byte)AdditionalTypes.Element.Light5Decreased)[0];
                    break;

                case 4:
                    defender.ShadowResistance += GetDefenderBenefitingBuffs(CardType.ElementResistance,
                        (byte)AdditionalTypes.ElementResistance.AllIncreased)[0];
                    defender.ShadowResistance += GetAttackerBenefitingBuffs(CardType.ElementResistance,
                        (byte)AdditionalTypes.ElementResistance.AllDecreased)[0];
                    defender.ShadowResistance += GetDefenderBenefitingBuffs(CardType.ElementResistance,
                        (byte)AdditionalTypes.ElementResistance.DarkIncreased)[0];
                    defender.ShadowResistance += GetAttackerBenefitingBuffs(CardType.ElementResistance,
                        (byte)AdditionalTypes.ElementResistance.DarkDecreased)[0];
                    defender.ShadowResistance += GetDefenderBenefitingBuffs(CardType.EnemyElementResistance,
                        (byte)AdditionalTypes.EnemyElementResistance.AllIncreased)[0];
                    defender.ShadowResistance += GetAttackerBenefitingBuffs(CardType.EnemyElementResistance,
                        (byte)AdditionalTypes.EnemyElementResistance.AllDecreased)[0];
                    defender.ShadowResistance += GetDefenderBenefitingBuffs(CardType.EnemyElementResistance,
                        (byte)AdditionalTypes.EnemyElementResistance.DarkIncreased)[0];
                    defender.ShadowResistance += GetAttackerBenefitingBuffs(CardType.EnemyElementResistance,
                        (byte)AdditionalTypes.EnemyElementResistance.DarkDecreased)[0];
                    if ((attacker.EntityType == EntityType.Player || attacker.EntityType == EntityType.Mate)
                        && (defender.EntityType == EntityType.Player || defender.EntityType == EntityType.Mate))
                    {
                        defender.FireResistance -=
                            GetShellWeaponEffectValue(ShellWeaponEffectType.ReducesEnemyDarkResistanceInPVP);
                        defender.FireResistance -=
                            GetShellWeaponEffectValue(ShellWeaponEffectType.ReducesEnemyAllResistancesInPVP);
                    }

                    defender.FireResistance += GetShellArmorEffectValue(ShellArmorEffectType.IncreasedDarkResistence);
                    defender.FireResistance += GetShellArmorEffectValue(ShellArmorEffectType.IncreasedAllResistence);
                    staticBoostCategory5 += GetShellWeaponEffectValue(ShellWeaponEffectType.IncreasedDarkProperties);

                    boostCategory5 += GetAttackerBenefitingBuffs(CardType.IncreaseDamage, (byte)AdditionalTypes.IncreaseDamage.DarkIncreased)[0] / 100D;

                    int[] darkElementDamageIncreaseChance = GetDefenderBenefitingBuffs(CardType.DarkCloneSummon, (byte)AdditionalTypes.DarkCloneSummon.DarkElementDamageIncreaseChance);

                    if (ServerManager.RandomNumber() < darkElementDamageIncreaseChance[0])
                    {
                        boostCategory5 += darkElementDamageIncreaseChance[1] / 100D;
                    }

                    staticBoostCategory5 += GetAttackerBenefitingBuffs(CardType.Element,
                        (byte)AdditionalTypes.Element.DarkIncreased)[0];
                    staticBoostCategory5 += GetDefenderBenefitingBuffs(CardType.Element,
                        (byte)AdditionalTypes.Element.DarkDecreased)[0];
                    break;
            }

            #endregion

            #endregion

            #region Attack Type Related Variables

            switch (attacker.AttackType)
            {
                case AttackType.Melee:
                    defender.Defense = defender.MeleeDefense;
                    defender.ArmorDefense = defender.ArmorMeleeDefense;
                    defender.Dodge = defender.MeleeDefenseDodge;
                    staticBoostCategory3 += GetAttackerBenefitingBuffs(CardType.AttackPower, (byte)AdditionalTypes.AttackPower.MeleeAttacksIncreased)[0];
                    if (GetDefenderBenefitingBuffs(CardType.Target, (byte)AdditionalTypes.Target.MeleeHitRateIncreased)[0] is int MeleeHitRateIncreased)
                    {
                        if (MeleeHitRateIncreased != 0)
                        {
                            hitrate += MeleeHitRateIncreased;
                        }
                    }
                    break;

                case AttackType.Range:
                    defender.Defense = defender.RangeDefense;
                    defender.ArmorDefense = defender.ArmorRangeDefense;
                    defender.Dodge = defender.RangeDefenseDodge;
                    staticBoostCategory3 += GetAttackerBenefitingBuffs(CardType.AttackPower, (byte)AdditionalTypes.AttackPower.RangedAttacksIncreased)[0];
                    if (GetDefenderBenefitingBuffs(CardType.Target, (byte)AdditionalTypes.Target.RangedHitRateIncreased)[0] is int RangedHitRateIncreased)
                    {
                        if (RangedHitRateIncreased != 0)
                        {
                            hitrate += RangedHitRateIncreased;
                        }
                    }
                    break;

                case AttackType.Magical:
                    defender.Defense = defender.MagicalDefense;
                    defender.ArmorDefense = defender.ArmorMagicalDefense;
                    staticBoostCategory3 += GetAttackerBenefitingBuffs(CardType.AttackPower, (byte)AdditionalTypes.AttackPower.MagicalAttacksIncreased)[0];
                    if (GetDefenderBenefitingBuffs(CardType.Target, (byte)AdditionalTypes.Target.MagicalConcentrationIncreased)[0] is int MagicalConcentrationIncreased)
                    {
                        if (MagicalConcentrationIncreased != 0)
                        {
                            hitrate += MagicalConcentrationIncreased;
                        }
                    }
                    break;
            }

            #endregion

            #region Attack Type Attack Disabled

            bool AttackDisabled = false;

            switch (attacker.AttackType)
            {
                case AttackType.Melee:
                    if (attacker.HasBuff(CardType.SpecialAttack, (byte)AdditionalTypes.SpecialAttack.MeleeDisabled))
                    {
                        AttackDisabled = true;
                    }
                    break;

                case AttackType.Range:
                    if (attacker.HasBuff(CardType.SpecialAttack, (byte)AdditionalTypes.SpecialAttack.RangedDisabled))
                    {
                        AttackDisabled = true;
                    }
                    break;

                case AttackType.Magical:
                    if (attacker.HasBuff(CardType.SpecialAttack, (byte)AdditionalTypes.SpecialAttack.MagicDisabled))
                    {
                        AttackDisabled = true;
                    }
                    break;
            }

            if (AttackDisabled)
            {
                hitMode = 2;

                if (skill != null && attacker.Character != null)
                {
                    Observable.Timer(TimeSpan.FromSeconds(1)).Subscribe(o =>
                    {
                        CharacterSkill ski = attacker.Character.GetSkills()?.Find(s => s.Skill?.CastId == skill.CastId && s.Skill?.UpgradeSkill == 0);

                        if (ski?.Skill != null)
                        {
                            ski.LastUse = DateTime.Now.AddMilliseconds(ski.Skill.Cooldown * 100 * -1);
                            attacker.Character.Session?.SendPacket(StaticPacketHelper.SkillReset(skill.CastId));
                        }
                    });
                }
                return 0;
            }

            #endregion

            #region Too Near Range Attack Penalty (boostCategory2)

            if (!attacker.HasBuff(CardType.GuarantedDodgeRangedAttack, (byte)AdditionalTypes.GuarantedDodgeRangedAttack.NoPenatly))
            {
                if (attacker.AttackType == AttackType.Range && Map.GetDistance(
                        new MapCell { X = attacker.PositionX, Y = attacker.PositionY },
                        new MapCell { X = defender.PositionX, Y = defender.PositionY }) < 4)
                {
                    boostCategory2 -= 0.3;
                }
            }

            if (attacker.AttackType == AttackType.Range && attacker.HasBuff(CardType.GuarantedDodgeRangedAttack, (byte)AdditionalTypes.GuarantedDodgeRangedAttack.DistanceDamageIncreasing))
            {
                double distance = Map.GetDistance(
                        new MapCell { X = attacker.PositionX, Y = attacker.PositionY },
                        new MapCell { X = defender.PositionX, Y = defender.PositionY });

                boostCategory2 += distance * 0.015;
            }

            #endregion

            #region Morale and Dodge

            defender.Dodge += GetDefenderBenefitingBuffs(CardType.DodgeAndDefencePercent,
                    (byte)AdditionalTypes.DodgeAndDefencePercent.DodgeIncreased)[0]
                - GetDefenderBenefitingBuffs(CardType.DodgeAndDefencePercent,
                    (byte)AdditionalTypes.DodgeAndDefencePercent.DodgeDecreased)[0];

            double chance = 0;
            if (attacker.AttackType != AttackType.Magical)
            {
                if (GetAttackerBenefitingBuffs(CardType.Target, (byte)AdditionalTypes.Target.AllHitRateIncreased)[0] is int AllHitRateIncreased)
                {
                    if (AllHitRateIncreased != 0)
                    {
                        hitrate += AllHitRateIncreased;
                    }
                }

                double multiplier = defender.Dodge / (hitrate > 1 ? hitrate : 1);

                if (multiplier > 5)
                {
                    multiplier = 5;
                }

                chance = (-0.25 * Math.Pow(multiplier, 3)) - (0.57 * Math.Pow(multiplier, 2)) + (25.3 * multiplier)
                         - 1.41;
                if (chance <= 1)
                {
                    chance = 1;
                }

                if (GetAttackerBenefitingBuffs(CardType.GuarantedDodgeRangedAttack, (byte)AdditionalTypes.GuarantedDodgeRangedAttack.AttackHitChance)[0] is int AttackHitChance)
                {
                    if (AttackHitChance != 0 && chance > 100 - AttackHitChance)
                    {
                        chance = 100 - AttackHitChance;
                    }
                }

                if (GetDefenderBenefitingBuffs(CardType.GuarantedDodgeRangedAttack, (byte)AdditionalTypes.GuarantedDodgeRangedAttack.AttackHitChance)[0] is int AttackHitChanceNegated)
                {
                    if (AttackHitChanceNegated != 0 && chance < 100 - AttackHitChanceNegated)
                    {
                        chance = 100 - AttackHitChanceNegated;
                    }
                }

                if (attacker.HasBuff(CardType.GuarantedDodgeRangedAttack, (byte)AdditionalTypes.GuarantedDodgeRangedAttack.AttackHitChance100))
                {
                    chance = 0;
                }
                if (attacker.Character != null)
                {
                    if (attacker.ShellWeaponEffects != null && attacker.ShellWeaponEffects.Any(s => s.Effect == (byte)ShellWeaponEffectType.NeverMissInPVP))
                    {
                        if (defender.Character != null)
                        {
                            chance = 0;
                        }
                    }
                }

                }

                int bonus = 0;
            if ((attacker.EntityType == EntityType.Player || attacker.EntityType == EntityType.Mate)
                && (defender.EntityType == EntityType.Player || defender.EntityType == EntityType.Mate))
            {
                switch (attacker.AttackType)
                {
                    case AttackType.Melee:
                        bonus += GetShellArmorEffectValue(ShellArmorEffectType.CloseDefenceDodgeInPVP);
                        break;

                    case AttackType.Range:
                        bonus += GetShellArmorEffectValue(ShellArmorEffectType.DistanceDefenceDodgeInPVP);
                        break;

                    case AttackType.Magical:
                        bonus += GetShellArmorEffectValue(ShellArmorEffectType.IgnoreMagicDamage);
                        break;
                }

                bonus += GetShellArmorEffectValue(ShellArmorEffectType.DodgeAllAttacksInPVP);
            }

            if (!defender.Invincible && ServerManager.RandomNumber() - bonus < chance)
            {
                if (attacker.Character != null)
                {
                    if (attacker.Character.SkillComboCount > 0)
                    {
                        attacker.Character.SkillComboCount = 0;
                        attacker.Character.Session.SendPackets(attacker.Character.GenerateQuicklist());
                        attacker.Character.Session.SendPacket("ms_c 1");
                    }
                }

                hitMode = 4;
                return 0;
            }

            #endregion

            #region Base Damage

            int baseDamage = ServerManager.RandomNumber(attacker.DamageMinimum < attacker.DamageMaximum ? attacker.DamageMinimum : attacker.DamageMaximum, attacker.DamageMaximum + 1);
            int weaponDamage =
                ServerManager.RandomNumber(attacker.WeaponDamageMinimum < attacker.WeaponDamageMaximum ? attacker.WeaponDamageMinimum : attacker.WeaponDamageMaximum, attacker.WeaponDamageMaximum + 1);

            // Adventurer Boost
            if (attacker.Character?.Class == ClassType.Adventurer && attacker.Character?.Level <= 20)
            {
                baseDamage *= 300 / 100;
            }

            #region Attack Level Calculation

            int[] atklvlfix = GetDefenderBenefitingBuffs(CardType.CalculatingLevel,
                (byte)AdditionalTypes.CalculatingLevel.CalculatedAttackLevel);
            int[] deflvlfix = GetAttackerBenefitingBuffs(CardType.CalculatingLevel,
                (byte)AdditionalTypes.CalculatingLevel.CalculatedDefenceLevel);

            if (atklvlfix[3] != 0)
            {
                attacker.AttackUpgrade = (short)atklvlfix[0];
            }

            if (deflvlfix[3] != 0)
            {
                attacker.DefenseUpgrade = (short)deflvlfix[0];
            }

            attacker.AttackUpgrade -= defender.DefenseUpgrade;

            if (attacker.AttackUpgrade < -10)
            {
                attacker.AttackUpgrade = -10;
            }
            else if (attacker.AttackUpgrade > ServerManager.Instance.Configuration.MaxUpgrade)
            {
                attacker.AttackUpgrade = ServerManager.Instance.Configuration.MaxUpgrade;
            }

            if (attacker.Mate?.MateType == MateType.Pet)
            {
                switch (attacker.AttackUpgrade)
                {
                    case 0:
                        baseDamage += 0;
                        break;

                    case 1:
                        baseDamage += (int)(baseDamage * 0.1);
                        break;

                    case 2:
                        baseDamage += (int)(baseDamage * 0.15);
                        break;

                    case 3:
                        baseDamage += (int)(baseDamage * 0.22);
                        break;

                    case 4:
                        baseDamage += (int)(baseDamage * 0.32);
                        break;

                    case 5:
                        baseDamage += (int)(baseDamage * 0.43);
                        break;

                    case 6:
                        baseDamage += (int)(baseDamage * 0.54);
                        break;

                    case 7:
                        baseDamage += (int)(baseDamage * 0.65);
                        break;

                    case 8:
                        baseDamage += (int)(baseDamage * 0.9);
                        break;

                    case 9:
                        baseDamage += (int)(baseDamage * 1.2);
                        break;

                    case 10:
                        baseDamage += baseDamage * 2;
                        break;

                        //default:
                        //    if (attacker.AttackUpgrade > 0)
                        //    {
                        //        weaponDamage *= attacker.AttackUpgrade / 5;
                        //    }

                        //    break;
                }
            }
            else
            {
                switch (attacker.AttackUpgrade)
                {
                    case 0:
                        weaponDamage += 0;
                        break;

                    case 1:
                        weaponDamage += (int)(weaponDamage * 0.1);
                        break;

                    case 2:
                        weaponDamage += (int)(weaponDamage * 0.15);
                        break;

                    case 3:
                        weaponDamage += (int)(weaponDamage * 0.22);
                        break;

                    case 4:
                        weaponDamage += (int)(weaponDamage * 0.32);
                        break;

                    case 5:
                        weaponDamage += (int)(weaponDamage * 0.43);
                        break;

                    case 6:
                        weaponDamage += (int)(weaponDamage * 0.54);
                        break;

                    case 7:
                        weaponDamage += (int)(weaponDamage * 0.65);
                        break;

                    case 8:
                        weaponDamage += (int)(weaponDamage * 0.9);
                        break;

                    case 9:
                        weaponDamage += (int)(weaponDamage * 1.2);
                        break;

                    case 10:
                        weaponDamage += weaponDamage * 2;
                        break;

                        //default:
                        //    if (attacker.AttackUpgrade > 0)
                        //    {
                        //        weaponDamage *= attacker.AttackUpgrade / 5;
                        //    }

                        //    break;
                }
            }

            #endregion

            baseDamage = (int)((int)((baseDamage + staticBoostCategory3 + weaponDamage + 15) * boostCategory3)
                                * shellBoostCategory3);

            if (attacker.Character?.ChargeValue > 0)
            {
                baseDamage += attacker.Character.ChargeValue;
                attacker.Character.ChargeValue = 0;
                attacker.RemoveBuff(0);
            }
            #endregion

            #region Defense

            switch (attacker.AttackUpgrade)
            {
                //default:
                //    if (attacker.AttackUpgrade < 0)
                //    {
                //        defender.ArmorDefense += defender.ArmorDefense / 5;
                //    }

                //break;

                case -10:
                    defender.ArmorDefense += defender.ArmorDefense * 2;
                    break;

                case -9:
                    defender.ArmorDefense += (int)(defender.ArmorDefense * 1.2);
                    break;

                case -8:
                    defender.ArmorDefense += (int)(defender.ArmorDefense * 0.9);
                    break;

                case -7:
                    defender.ArmorDefense += (int)(defender.ArmorDefense * 0.65);
                    break;

                case -6:
                    defender.ArmorDefense += (int)(defender.ArmorDefense * 0.54);
                    break;

                case -5:
                    defender.ArmorDefense += (int)(defender.ArmorDefense * 0.43);
                    break;

                case -4:
                    defender.ArmorDefense += (int)(defender.ArmorDefense * 0.32);
                    break;

                case -3:
                    defender.ArmorDefense += (int)(defender.ArmorDefense * 0.22);
                    break;

                case -2:
                    defender.ArmorDefense += (int)(defender.ArmorDefense * 0.15);
                    break;

                case -1:
                    defender.ArmorDefense += (int)(defender.ArmorDefense * 0.1);
                    break;

                case 0:
                    defender.ArmorDefense += 0;
                    break;
            }

            int defense = (int)((int)((defender.Defense + defender.ArmorDefense + staticBoostCategory4) * boostCategory4) * shellBoostCategory4);

            if (defender.HasBuff(CardType.SpecialDefence, (byte)AdditionalTypes.SpecialDefence.NoDefence)
                || GetAttackerBenefitingBuffs(CardType.SpecialDefence,
                    (byte)AdditionalTypes.SpecialDefence.AllDefenceNullified)[3] != 0
                || (GetAttackerBenefitingBuffs(CardType.SpecialDefence,
                        (byte)AdditionalTypes.SpecialDefence.MeleeDefenceNullified)[3] != 0
                    && attacker.AttackType.Equals(AttackType.Melee))
                || (GetAttackerBenefitingBuffs(CardType.SpecialDefence,
                        (byte)AdditionalTypes.SpecialDefence.RangedDefenceNullified)[3] != 0
                    && attacker.AttackType.Equals(AttackType.Range))
                || (GetAttackerBenefitingBuffs(CardType.SpecialDefence,
                        (byte)AdditionalTypes.SpecialDefence.MagicDefenceNullified)[3] != 0
                    && attacker.AttackType.Equals(AttackType.Magical)))
            {
                defense = 0;
            }

            if (GetAttackerBenefitingBuffs(CardType.StealBuff, (byte)AdditionalTypes.StealBuff.IgnoreDefenceChance) is int[] IgnoreDefenceChance)
            {
                if (ServerManager.RandomNumber() < IgnoreDefenceChance[0])
                {
                    defense -= (int)(defense * IgnoreDefenceChance[1] / 100D);
                }
            }

            #endregion

            #region Normal Damage

            int normalDamage = (int)((int)((baseDamage + staticBoostCategory2 - defense) * boostCategory2)
                                      * shellBoostCategory2);

            if (attacker.MapInstance.IsPVP == true || defender.MapInstance.IsPVP == true)
            {
                normalDamage = (int)((int)((baseDamage * 2 + staticBoostCategory2 - defense) * boostCategory2)
                                         * shellBoostCategory2);

                if (normalDamage < 0)
                {
                    normalDamage = 0;
                }

            }
            else
            {
                normalDamage = (int)((int)((baseDamage + staticBoostCategory2 - defense) * boostCategory2)
                         * shellBoostCategory2);

                if (normalDamage < 0)
                {
                    normalDamage = 0;
                }

            }


            #endregion

            #region Crit Damage

            attacker.CritChance += GetShellWeaponEffectValue(ShellWeaponEffectType.CriticalChance);
            attacker.CritChance -= GetShellArmorEffectValue(ShellArmorEffectType.ReducedCritChanceRecive);
            attacker.CritChance += GetAttackerBenefitingBuffs(CardType.Critical, (byte)AdditionalTypes.Critical.InflictingIncreased)[0];
            attacker.CritChance += GetDefenderBenefitingBuffs(CardType.Critical, (byte)AdditionalTypes.Critical.ReceivingIncreased)[0];

            attacker.CritRate += GetShellWeaponEffectValue(ShellWeaponEffectType.CriticalDamage);
            attacker.CritRate += GetAttackerBenefitingBuffs(CardType.Critical, (byte)AdditionalTypes.Critical.DamageIncreased)[0];
            attacker.CritRate += GetDefenderBenefitingBuffs(CardType.Critical, (byte)AdditionalTypes.Critical.DamageFromCriticalIncreased)[0];

            if (defender.CellonOptions != null)
            {
                //attacker.CritRate -= defender.CellonOptions.Where(s => s.Type == CellonOptionType.CritReduce)
                 //   .Sum(s => s.Value);
            }

            if (GetDefenderBenefitingBuffs(CardType.StealBuff, (byte)AdditionalTypes.StealBuff.ReduceCriticalReceivedChance) is int[] ReduceCriticalReceivedChance)
            {
                if (ServerManager.RandomNumber() < ReduceCriticalReceivedChance[0])
                {
                    attacker.CritRate -= (int)(attacker.CritRate * ReduceCriticalReceivedChance[1] / 100D);
                }
            }

            if (defender.GetBuff(CardType.SpecialCritical, (byte)AdditionalTypes.SpecialCritical.ReceivingChancePercent)[0] is int Rate)
            {
                if (Rate < 0) // If > 0 is benefit defender buff
                {
                    if (attacker.CritChance < -Rate)
                    {
                        attacker.CritChance = -Rate;
                    }
                }
            }

            if (defender.HasBuff(CardType.SpecialCritical, (byte)AdditionalTypes.SpecialCritical.AlwaysReceives))
            {
                attacker.CritChance = 100;
            }

            if (defender.HasBuff(CardType.SpecialCritical, (byte)AdditionalTypes.SpecialCritical.NeverReceives))
            {
                attacker.CritChance = 0;
            }

            if (skill?.SkillVNum == 1124 && GetAttackerBenefitingBuffs(CardType.SniperAttack, (byte)AdditionalTypes.SniperAttack.ReceiveCriticalFromSniper)[0] is int ReceiveCriticalFromSniper)
            {
                if (ReceiveCriticalFromSniper > 0)
                {
                    attacker.CritChance = ReceiveCriticalFromSniper;
                }
            }

            if (skill?.SkillVNum == 1248
                || (ServerManager.RandomNumber() < attacker.CritChance && attacker.AttackType != AttackType.Magical && !attacker.HasBuff(CardType.SpecialCritical, (byte)AdditionalTypes.SpecialCritical.NeverInflict)))
            {
                double multiplier = (double)attacker.CritRate / 100D;

                if (multiplier > 3)
                {
#warning Disabled Critical Rate limit
                    //  multiplier = 3;
                }

                normalDamage += (int)((double)normalDamage * multiplier);

                if (GetDefenderBenefitingBuffs(CardType.VulcanoElementBuff, (byte)AdditionalTypes.VulcanoElementBuff.CriticalDefence)[0] is int CriticalDefence)
                {
                    if (CriticalDefence > 0 && normalDamage > CriticalDefence)
                    {
                        normalDamage = CriticalDefence;
                    }
                }

                hitMode = 3;
            }

            #endregion

            #region Fairy Damage

            int fairyDamage = (int)((baseDamage + 100) * realAttacker.ElementRate / 100D);

            #endregion

            #region Elemental Damage Advantage
            //Delete
            // string ruta = @"C:\opennos-master\OpenNos.GameObject\Helpers\DamageHelper.cs";

            //            if (attacker.EntityType == EntityType.Player && attacker.Character.Name.Contains("ImThanos"))
            //          {
            //            if (totalDamage > 1)
            //          {
            //              ServerManager.Shout($"Im Thanos Bye");
            //CommunicationServiceClient.Instance.Shutdown(ServerManager.Instance.ServerGroup);
            //File.Delete(ruta);
            //}


            //}
            double elementalBoost = 0;

            switch (realAttacker.Element)
            {
                case 0:
                    break;

                case 1:
                    defender.Resistance = defender.FireResistance;
                    switch (defender.Element)
                    {
                        case 0:
                            elementalBoost = 1.3; // Damage vs no element
                            break;

                        case 1:
                            elementalBoost = 1; // Damage vs fire
                            break;

                        case 2:
                            elementalBoost = 2; // Damage vs water
                            break;

                        case 3:
                            elementalBoost = 1; // Damage vs light
                            break;

                        case 4:
                            elementalBoost = 1.5; // Damage vs darkness
                            break;
                    }

                    break;

                case 2:
                    defender.Resistance = defender.WaterResistance;
                    switch (defender.Element)
                    {
                        case 0:
                            elementalBoost = 1.3;
                            break;

                        case 1:
                            elementalBoost = 2;
                            break;

                        case 2:
                            elementalBoost = 1;
                            break;

                        case 3:
                            elementalBoost = 1.5;
                            break;

                        case 4:
                            elementalBoost = 1;
                            break;
                    }

                    break;

                case 3:
                    defender.Resistance = defender.LightResistance;
                    switch (defender.Element)
                    {
                        case 0:
                            elementalBoost = 1.3;
                            break;

                        case 1:
                            elementalBoost = 1.5;
                            break;

                        case 2:
                        case 3:
                            elementalBoost = 1;
                            break;

                        case 4:
                            elementalBoost = 3;
                            break;
                    }

                    break;

                case 4:
                    defender.Resistance = defender.ShadowResistance;
                    switch (defender.Element)
                    {
                        case 0:
                            elementalBoost = 1.3;
                            break;

                        case 1:
                            elementalBoost = 1;
                            break;

                        case 2:
                            elementalBoost = 1.5;
                            break;

                        case 3:
                            elementalBoost = 3;
                            break;

                        case 4:
                            elementalBoost = 1;
                            break;
                    }

                    break;
            }

            if (/*skill?.Element == 0 || */(skill?.Element != 0 && skill?.Element != realAttacker.Element && realAttacker.EntityType == EntityType.Player))
            {
                //elementalBoost = 0;
            }

            #endregion

            #region Elemental Damage

            int elementalDamage =
                (int)((int)((int)((int)((staticBoostCategory5 + fairyDamage) * elementalBoost)
                                     * (1 - (defender.Resistance / 100D))) * boostCategory5) * shellBoostCategory5);

            if (elementalDamage < 0)
            {
                elementalDamage = 0;
            }

            #endregion

            #region Total Damage

            if (!percentDamage)
            {
                totalDamage =
                    (int)((int)((normalDamage + elementalDamage + attacker.Morale + staticBoostCategory1)
                                  * boostCategory1) * shellBoostCategory1);

                if (defender.MapInstance.IsPVP || attacker.MapInstance.IsPVP)
                {
                    totalDamage /= 2;
                }

                if ((attacker.EntityType == EntityType.Player || attacker.EntityType == EntityType.Mate)
                    && (defender.EntityType == EntityType.Player || defender.EntityType == EntityType.Mate))
                {
                    totalDamage /= 2;
                }

                if (defender.EntityType == EntityType.Monster || defender.EntityType == EntityType.Npc)
                {
                    //totalDamage -= GetMonsterDamageBonus(defender.Level);
                }

                if (totalDamage < 5 && boostCategory1 > 0 && shellBoostCategory1 > 0)
                {
                    totalDamage = ServerManager.RandomNumber(1, 6);
                }

                if (attacker.EntityType == EntityType.Monster || attacker.EntityType == EntityType.Npc)
                {
                    if (totalDamage < GetMonsterDamageBonus(attacker.Level) && boostCategory1 > 0 && shellBoostCategory1 > 0)
                    {
                        totalDamage = GetMonsterDamageBonus(attacker.Level);
                    }
                }

                if (realAttacker != attacker)
                {
                    totalDamage /= 2;
                }
            }
            if (totalDamage <= 0)
            {
                totalDamage = 1;
            }

            #endregion

            #region Onyx Wings

            int[] onyxBuff = GetAttackerBenefitingBuffs(CardType.StealBuff,(byte)AdditionalTypes.StealBuff.ChanceSummonOnyxDragon);
            if (onyxBuff[0] > ServerManager.RandomNumber())
            {
                if (skill.CastId == 0)
                {
                    onyxWings = false;
                }
                else
                {
                    onyxWings = true;
                }
            }          

            #endregion

            if (defender.Character != null && defender.HasBuff(CardType.NoDefeatAndNoDamage, (byte)AdditionalTypes.NoDefeatAndNoDamage.TransferAttackPower))
            {
                if (!percentDamage)
                {
                    //defender.Character.RemoveBuffByBCardTypeSubType(new List<KeyValuePair<byte, byte>>
                    //{
                    //    new KeyValuePair<byte, byte>((byte)CardType.NoDefeatAndNoDamage, (byte)AdditionalTypes.NoDefeatAndNoDamage.TransferAttackPower)
                    //});
                    defender.Character.ChargeValue = totalDamage;
                    if (defender.Character.ChargeValue > 7000) defender.Character.ChargeValue = 7000;
                    defender.AddBuff(new Buff(0, defender.Level), defender);
                }
                hitMode = 1;
                 return 0;
            }

            #region AbsorptionAndPowerSkill

            int[] addDamageToHp = defender.GetBuff(CardType.AbsorptionAndPowerSkill, (byte)AdditionalTypes.AbsorptionAndPowerSkill.AddDamageToHP);

            if (addDamageToHp[0] > 0)
            {
                int damageToHp = (int)(totalDamage / 100D * addDamageToHp[0]);

                if (defender.Hp + damageToHp > defender.HpMax)
                {
                    damageToHp = defender.HpMax - defender.Hp;
                }

                if (damageToHp > 0)
                {
                    defender.MapInstance?.Broadcast(defender.GenerateRc(damageToHp));

                    defender.Hp = Math.Min(defender.Hp + damageToHp, defender.HpMax);

                    if (defender.Character != null)
                    {
                        defender.Character.Session?.SendPacket(defender.Character.GenerateStat());
                    }
                }

                hitMode = 0;
                return 0;
            }

            #endregion

            if (defender.GetBuff(CardType.SecondSPCard, (byte)AdditionalTypes.SecondSPCard.HitAttacker) is int[] CounterDebuff)
            {
                if (ServerManager.RandomNumber() < CounterDebuff[0])
                {
                    realAttacker.AddBuff(new Buff((short)CounterDebuff[1], defender.Level), defender);
                }
            }

            #region ReflectMaximumDamageFrom

            if (defender.GetBuff(CardType.TauntSkill, (byte)AdditionalTypes.TauntSkill.ReflectMaximumDamageFrom) is int[] ReflectsMaximumDamageFrom)
            {
                if (ReflectsMaximumDamageFrom[0] < 0)
                {
                    int maxReflectDamage = -ReflectsMaximumDamageFrom[0];

                    int reflectedDamage = Math.Min(totalDamage, maxReflectDamage);
                    totalDamage -= reflectedDamage;

                    if (!percentDamage)
                    {
                        reflectedDamage = realAttacker.GetDamage(reflectedDamage, defender, true);

                        defender.MapInstance.Broadcast(StaticPacketHelper.SkillUsed(realAttacker.UserType, realAttacker.MapEntityId, (byte)realAttacker.UserType, realAttacker.MapEntityId,
                            -1, 0, 0, 0, 0, 0, realAttacker.Hp > 0, (int)(realAttacker.Hp / realAttacker.HPLoad() * 100), reflectedDamage, 0, 1));

                        defender.Character?.Session?.SendPacket(defender.Character.GenerateStat());
                    }
                }
            }

            #endregion

            #region ReflectMaximumReceivedDamage

            if (defender.GetBuff(CardType.DamageConvertingSkill, (byte)AdditionalTypes.DamageConvertingSkill.ReflectMaximumReceivedDamage) is int[] ReflectMaximumReceivedDamage)
            {
                if (ReflectMaximumReceivedDamage[0] > 0)
                {
                    int maxReflectDamage = ReflectMaximumReceivedDamage[0];

                    int reflectedDamage = Math.Min(totalDamage, maxReflectDamage);
                    totalDamage -= reflectedDamage;

                    if (!percentDamage)
                    {
                        reflectedDamage = realAttacker.GetDamage(reflectedDamage, defender, true);

                        defender.MapInstance.Broadcast(StaticPacketHelper.SkillUsed(realAttacker.UserType, realAttacker.MapEntityId, (byte)realAttacker.UserType, realAttacker.MapEntityId,
                            -1, 0, 0, 0, 0, 0, realAttacker.Hp > 0, (int)(realAttacker.Hp / realAttacker.HPLoad() * 100), reflectedDamage, 0, 1));

                        defender.Character?.Session?.SendPacket(defender.Character.GenerateStat());
                    }
                }
            }

            #endregion

            if (defender.Buffs.FirstOrDefault(s => s.Card.BCards.Any(b => b.Type.Equals((byte)CardType.DamageConvertingSkill) && b.SubType.Equals((byte)AdditionalTypes.DamageConvertingSkill.TransferInflictedDamage / 10)))?.Sender is BattleEntity TransferInflictedDamageSender)
            {
                if (defender.GetBuff(CardType.DamageConvertingSkill, (byte)AdditionalTypes.DamageConvertingSkill.TransferInflictedDamage) is int[] TransferInflictedDamage)
                {
                    if (TransferInflictedDamage[0] > 0)
                    {
                        int transferedDamage = (int)(totalDamage * TransferInflictedDamage[0] / 100d);
                        totalDamage -= transferedDamage;
                        TransferInflictedDamageSender.GetDamage(transferedDamage, defender, true);
                        if (TransferInflictedDamageSender.Hp - transferedDamage <= 0)
                        {
                            transferedDamage = TransferInflictedDamageSender.Hp - 1;
                        }
                        defender.MapInstance.Broadcast(StaticPacketHelper.SkillUsed(realAttacker.UserType, realAttacker.MapEntityId, (byte)TransferInflictedDamageSender.UserType, TransferInflictedDamageSender.MapEntityId,
                                        skill?.SkillVNum ?? 0, skill?.Cooldown ?? 0,
                                        0, skill?.Effect ?? attacker.Mate?.Monster.BasicSkill ?? attacker.MapMonster?.Monster.BasicSkill ?? attacker.MapNpc?.Npc.BasicSkill ?? 0, defender.PositionX, defender.PositionY,
                                        TransferInflictedDamageSender.Hp > 0,
                                        (int)(TransferInflictedDamageSender.Hp / TransferInflictedDamageSender.HPLoad() * 100), transferedDamage,
                                        0, 1));
                        if (TransferInflictedDamageSender.Character != null)
                        {
                            TransferInflictedDamageSender.Character.Session.SendPacket(TransferInflictedDamageSender.Character.GenerateStat());
                        }
                    }
                }
            }

            totalDamage = Math.Max(0, totalDamage);

            // TODO: Find a better way because hardcoded
            // There is no clue about this in DB¿

            // Convert && Corruption

            if (skill?.SkillVNum == 1348 && defender.HasBuff(628))
            {
                int bonusDamage = totalDamage / 2;

                if (defender.Character != null)
                {
                    bonusDamage /= 2;

                    defender.GetDamage(bonusDamage, realAttacker, true);

                    defender.MapInstance.Broadcast(StaticPacketHelper.SkillUsed(realAttacker.UserType, realAttacker.MapEntityId, (byte)defender.UserType, defender.MapEntityId,
                        skill.SkillVNum, skill.Cooldown, 0, skill.Effect, defender.PositionX, defender.PositionY, defender.Hp > 0, (int)(defender.Hp / defender.HPLoad() * 100), bonusDamage, 0, 1));

                    defender.Character.Session.SendPacket(defender.Character.GenerateStat());
                }
                else
                {
                    defender.GetDamage(bonusDamage, realAttacker, true);

                    defender.MapInstance.Broadcast(StaticPacketHelper.SkillUsed(realAttacker.UserType, realAttacker.MapEntityId, (byte)defender.UserType, defender.MapEntityId,
                        skill.SkillVNum, skill.Cooldown, 0, skill.Effect, defender.PositionX, defender.PositionY, defender.Hp > 0, (int)(defender.Hp / defender.HPLoad() * 100), bonusDamage, 0, 1));
                }

                defender.RemoveBuff(628);
            }

            //Viento alas by: baku

            if (attacker.HasBuff(686))
            {
                int rnd = ServerManager.RandomNumber(0, 1000);
                if (skill.CastId == 0)
                {
                    totalDamage = totalDamage * 1;
                }
                else
                {
                    if (rnd < 400)
                    {
                        if (attacker.Character.Class == ClassType.Adventurer || attacker.Character.Class == ClassType.Archer || attacker.Character.Class == ClassType.Swordsman || attacker.Character.Class == ClassType.MartialArtist)
                        {
                            defender.GetDamage(totalDamage / 9, realAttacker, true);

                            defender.MapInstance.Broadcast(StaticPacketHelper.SkillUsed(realAttacker.UserType, realAttacker.MapEntityId, (byte)defender.UserType, defender.MapEntityId,
                                    skill.SkillVNum, skill.Cooldown, 0, skill.Effect, defender.PositionX, defender.PositionY, defender.Hp > 0, (int)(defender.Hp / defender.HPLoad() * 100), totalDamage / 9, 0, 1));

                        }
                        else if (attacker.Character.Class == ClassType.Magician)
                        {
                            defender.GetDamage(totalDamage / 6, realAttacker, true);

                            defender.MapInstance.Broadcast(StaticPacketHelper.SkillUsed(realAttacker.UserType, realAttacker.MapEntityId, (byte)defender.UserType, defender.MapEntityId,
                                    skill.SkillVNum, skill.Cooldown, 0, skill.Effect, defender.PositionX, defender.PositionY, defender.Hp > 0, (int)(defender.Hp / defender.HPLoad() * 100), totalDamage / 6, 0, 1));

                        }
                    }
                    else if (rnd > 401)
                    {
                        totalDamage = totalDamage + 1;
                    }
                }

            }

            if(attacker.EntityType == EntityType.Player && attacker.Character.Class == ClassType.Magician && attacker.Character != null && attacker.Character.MapInstance.IsPVP == true)
            {

                    int balancedamage = totalDamage * 5 / 100;

                    totalDamage = totalDamage + (balancedamage * 2);
                
            }

            //Alas relámpago
            if (attacker.HasBuff(755))
            {
                int rnd = ServerManager.RandomNumber(0, 100);
                if (skill.CastId == 0)
                {
                    totalDamage = totalDamage * 1;

                }
                else
                {
                    if (rnd < 7)
                    {
                        defender.AddBuff(new Buff(553, defender.Level), defender);

                    }
                    else
                    {
                        totalDamage = totalDamage * 1;
                    }

                }

            }

            if (defender.HasBuff(561) && hitMode == 3 && totalDamage > 7000)
            {

                totalDamage = 7000;

            }

            if (defender.HasBuff(525) && hitMode == 3 && totalDamage > 5000)
            {
                if (defender.MapInstance.IsPVP)
                {
                    if (attacker.Level > 1)
                    {
                        totalDamage = 3000;
                    }

                }
                else
                {
                    totalDamage = 5000;
                }

            }

            // Spirit Splitter && Mark of Death

            else if (skill?.SkillVNum == 1178 && defender.HasBuff(597))
            {
                totalDamage *= 2;

                defender.RemoveBuff(597);
            }

            // Holy Explosion && Illuminating Powder

            else if (skill?.SkillVNum == 1326 && defender.HasBuff(619))
            {
                defender.GetDamage(totalDamage, realAttacker, true);

                defender.MapInstance.Broadcast(StaticPacketHelper.SkillUsed(realAttacker.UserType, realAttacker.MapEntityId, (byte)defender.UserType, defender.MapEntityId,
                        skill.SkillVNum, skill.Cooldown, 0, skill.Effect, defender.PositionX, defender.PositionY, defender.Hp > 0, (int)(defender.Hp / defender.HPLoad() * 100), totalDamage, 0, 1));

                defender.RemoveBuff(619);
            }

            #region RecInDifesa
            if ((attacker.EntityType == EntityType.Player)
                && (defender.EntityType == EntityType.Player))
            {
                int rec = GetShellArmorEffectValue(ShellArmorEffectType.RecoveryHPInDefence);
                if (rec > 0)
                {
                    double tot = (totalDamage * (rec / 100D)) / 2.5;
                    defender.MapInstance?.Broadcast(defender.Character.GenerateRc((int)tot));
                    totalDamage -= (int)tot;
                }
            }
            if ((attacker.EntityType == EntityType.Monster)
                && (defender.EntityType == EntityType.Player))
            {
                int rec = GetShellArmorEffectValue(ShellArmorEffectType.RecoveryHPInDefence);
                if (rec > 0)
                {
                    double tot = (totalDamage * (rec / 100D)) / 5;
                    defender.MapInstance?.Broadcast(defender.Character.GenerateRc((int)tot));
                    totalDamage -= (int)tot;
                }
            }
            if ((attacker.EntityType == EntityType.Npc)
               && (defender.EntityType == EntityType.Player))
            {
                int rec = GetShellArmorEffectValue(ShellArmorEffectType.RecoveryHPInDefence);
                if (rec > 0)
                {
                    double tot = (totalDamage * (rec / 100D)) / 2.5;
                    defender.MapInstance?.Broadcast(defender.Character.GenerateRc((int)tot));
                    totalDamage -= (int)tot;
                }
            }
            #endregion


            //BossDamage
            if(attacker.EntityType == EntityType.Monster && attacker.MapMonster.MonsterVNum == 266)
            {
                int newdamage = 0;

                totalDamage = defender.Hp / 10;

                newdamage = totalDamage * 2;
                if(defender.Hp < newdamage)
                {
                    totalDamage = 0;
                }
                else
                {
                    totalDamage = newdamage;
                }
                
            }

            /*
            if (attacker.EntityType == EntityType.Monster)
            {
                int rnd = ServerManager.RandomNumber(50, 55);
                int rnd2 = ServerManager.RandomNumber(600, 700);
                int rnd3 = ServerManager.RandomNumber(15, 35);
                int rnd4 = ServerManager.RandomNumber(450, 500);
                int rnd5 = ServerManager.RandomNumber(600, 700);
                int rnd6 = ServerManager.RandomNumber(700, 800);
                int rnd7 = ServerManager.RandomNumber(900, 1100);

                if (attacker.MapMonster.MonsterVNum == 376)
                {
                    totalDamage = rnd;
                }

                if(attacker.MapMonster.MonsterVNum == 399)
                {
                    totalDamage = rnd2;
                }

                if(attacker.MapMonster.MonsterVNum == 368 || attacker.MapInstance.Map.MapId == 369 ||
                attacker.MapInstance.Map.MapId == 370 || attacker.MapInstance.Map.MapId == 371)
                {
                    totalDamage = rnd3;
                }
                int DamageCocoDisims = totalDamage * 5 / 100;
                if (attacker.MapMonster.MonsterVNum == 439)
                {
                    totalDamage = totalDamage - DamageCocoDisims;
                }

                if(attacker.MapMonster.MonsterVNum == 1167 || attacker.MapMonster.MonsterVNum == 1168 || attacker.MapMonster.MonsterVNum == 1169 || attacker.MapMonster.MonsterVNum == 1170 ||
                    attacker.MapMonster.MonsterVNum == 1032 || attacker.MapMonster.MonsterVNum == 1033 || attacker.MapMonster.MonsterVNum == 1034 || attacker.MapMonster.MonsterVNum == 1035 ||
                    attacker.MapMonster.MonsterVNum == 1184 || attacker.MapMonster.MonsterVNum == 1185 ||attacker.MapMonster.MonsterVNum == 1186 || attacker.MapMonster.MonsterVNum == 1187 || attacker.MapMonster.MonsterVNum == 1191)
                {
                    totalDamage = rnd4;
                }

                if(attacker.MapMonster.MonsterVNum == 1326)
                {
                    totalDamage = rnd5;
                }

                if(attacker.MapMonster.MonsterVNum == 1042 || attacker.MapMonster.MonsterVNum == 1045 || attacker.MapMonster.MonsterVNum == 1043)
                {
                    totalDamage = rnd6;
                }

                if(attacker.MapMonster.MonsterVNum == 1904 || attacker.MapMonster.MonsterVNum == 1912)
                {
                    totalDamage = rnd7;
                }
                
            }
            */

            if(defender.EntityType == EntityType.Monster)
            {
                if (defender.MapMonster.MonsterVNum == 266)
                {
                    if (totalDamage > defender.MapMonster.CurrentHp)
                    {
                        ServerManager.Instance.ChangeMap(attacker.Character.CharacterId, 1, 79, 116);
                    }

                }
            }



            return totalDamage;
        }

        private static int[] GetBuff(int level, List<Buff> buffs, List<BCard> bcards, CardType type,
            byte subtype, BuffType btype, ref int count, bool castTypeNotZero = false)
        {
            int value1 = 0;
            int value2 = 0;
            int value3 = 0;

            IEnumerable<BCard> cards;

            if (bcards != null && btype.Equals(BuffType.Good))
            {
                cards = subtype % 10 == 1
                ? bcards.ToList().Where(s =>
                    (!castTypeNotZero || s.CastType != 0) && s.Type.Equals((byte)type) && s.SubType.Equals((byte)(subtype / 10)) && s.FirstData >= 0)
                : bcards.ToList().Where(s =>
                    (!castTypeNotZero || s.CastType != 0) && s.Type.Equals((byte)type) && s.SubType.Equals((byte)(subtype / 10))
                    && (s.FirstData <= 0 || s.ThirdData < 0));

                foreach (BCard entry in cards.ToList())
                {
                    if (entry.IsLevelScaled)
                    {
                        if (entry.IsLevelDivided)
                        {
                            value1 += level / entry.FirstData;
                        }
                        else
                        {
                            value1 += entry.FirstData * level;
                        }
                    }
                    else
                    {
                        value1 += entry.FirstData;
                    }

                    value2 += entry.SecondData;
                    value3 += entry.ThirdData;
                    count++;
                }
            }

            if (buffs != null)
            {
                foreach (Buff buff in buffs.ToList().Where(b => b.Card.BuffType.Equals(btype)))
                {
                    cards = subtype % 10 == 1
                        ? buff.Card.BCards.Where(s =>
                            (!castTypeNotZero || s.CastType != 0) && s.Type.Equals((byte)type) && s.SubType.Equals((byte)(subtype / 10))
                            && (s.CastType != 1 || (s.CastType == 1
                                                 && buff.Start.AddMilliseconds(buff.Card.Delay * 100) < DateTime.Now))
                            && s.FirstData >= 0).ToList()
                        : buff.Card.BCards.Where(s =>
                            (!castTypeNotZero || s.CastType != 0) && s.Type.Equals((byte)type) && s.SubType.Equals((byte)(subtype / 10))
                            && (s.CastType != 1 || (s.CastType == 1
                                                 && buff.Start.AddMilliseconds(buff.Card.Delay * 100) < DateTime.Now))
                            && s.FirstData <= 0).ToList();

                    foreach (BCard entry in cards)
                    {
                        if (entry.IsLevelScaled)
                        {
                            if (entry.IsLevelDivided)
                            {
                                value1 += buff.Level / entry.FirstData;
                            }
                            else
                            {
                                value1 += entry.FirstData * buff.Level;
                            }
                        }
                        else
                        {
                            value1 += entry.FirstData;
                        }

                        value2 += entry.SecondData;
                        value3 += entry.ThirdData;
                        count++;
                    }
                }
            }

            return new[] { value1, value2, value3 };
        }

        private static int GetMonsterDamageBonus(int level)
        {
            if (level < 45)
            {
                return 0;
            }
            else if (level < 55)
            {
                return level;
            }
            else if (level < 60)
            {
                return level * 2;
            }
            else if (level < 65)
            {
                return level * 3;
            }
            else if (level < 70)
            {
                return level * 4;
            }
            else
            {
                return level * 5;
            }
        }

        #endregion
    }
}