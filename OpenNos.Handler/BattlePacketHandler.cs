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

using OpenNos.Core;
using OpenNos.Data;
using OpenNos.Domain;
using OpenNos.GameObject;
using OpenNos.GameObject.Battle;
using OpenNos.GameObject.Helpers;
using OpenNos.GameObject.Networking;
using OpenNos.Master.Library.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using static OpenNos.Domain.BCardType;
using OpenNos.GameObject.Event;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using OpenNos.GameObject.Event.GAMES;

namespace OpenNos.Handler
{
    public class BattlePacketHandler : IPacketHandler
    {
        #region Instantiation

        public BattlePacketHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        private ClientSession Session { get; }

        #endregion

        #region Methods

        /// <summary>
        /// mtlist packet
        /// </summary>
        /// <param name="multiTargetListPacket"></param>
        public void MultiTargetListHit(MultiTargetListPacket multiTargetListPacket)
        {
            if (multiTargetListPacket?.Targets == null || Session?.Character?.MapInstance == null)
            {
                return;
            }

            if (Session.Character.IsVehicled || Session.Character.MuteMessage())
            {
                Session.SendPacket(StaticPacketHelper.Cancel());
                return;
            }

            if ((DateTime.Now - Session.Character.LastTransform).TotalSeconds < 3)
            {
                Session.SendPacket(StaticPacketHelper.Cancel());
                Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("CANT_ATTACK"), 0));
                return;
            }

            if (multiTargetListPacket.TargetsAmount <= 0
                || multiTargetListPacket.TargetsAmount != multiTargetListPacket.Targets.Count)
            {
                return;
            }

            Session.Character.MTListTargetQueue.Clear();

            foreach (MultiTargetListSubPacket subPacket in multiTargetListPacket.Targets)
            {
                Session.Character.MTListTargetQueue.Push(new MTListHitTarget(subPacket.TargetType, subPacket.TargetId, TargetHitType.AOETargetHit));
            }
        }

        /// <summary>
        /// u_s packet
        /// </summary>
        /// <param name="useSkillPacket"></param>
        public void UseSkill(UseSkillPacket useSkillPacket)
        {
            if (Session.Character.MapInstance == null)
            {
                Session.SendPacket(StaticPacketHelper.Cancel());
                return;
            }

            Session.Character.WalkDisposable?.Dispose();
            Session.Character.Direction = Session.Character.BeforeDirection;

            switch (useSkillPacket.UserType)
            {
                case UserType.Npc:
                    {
                        MapNpc target = Session.Character.MapInstance.GetNpc(useSkillPacket.MapMonsterId);

                        if (target != null)
                        {
                            if ((Session.Character.Morph == 1000099 /* Hamster */ && target.NpcVNum == 2329 /* Cheese Chunk */)
                                || (Session.Character.Morph == 1000156 /* Bushtail */ && target.NpcVNum == 2330 /* Grass Clump /!\ VEGAN DETECTED /!\ */))
                            {
                                Session.SendPacket(StaticPacketHelper.Cancel(2));
                                Session.SendPacket($"delay 1000 13 #guri^513^2^{target.MapNpcId}");
                                Session.Character.MapInstance.Broadcast(UserInterfaceHelper.GenerateGuri(2, 1, Session.Character.CharacterId), ReceiverType.AllExceptMe);
                                return;
                            }
                            if (target.NpcVNum == 922)
                            {
                                Session.SendPacket(StaticPacketHelper.Cancel(2));
                                Session.SendPacket($"delay 1000 13 #guri^513^2^{target.MapNpcId}");
                                target.Score++;
                                return;
                            }
                            if (Session.CurrentMapInstance.Map.MapId == 2628 && target.NpcVNum == 2306)
                            {
                                Session.SendPacket(StaticPacketHelper.Cancel(2));
                              //  Session.SendPacket($"delay 1000 13 #guri^513^2^{target.MapNpcId}");
                                ServerManager.Instance.ChangeMapInstance(Session.Character.CharacterId, ShopShipev.ShopMapInstance.MapInstanceId, 15, 7);
                                return;
                            }

                            if (Session.CurrentMapInstance.Map.MapId == 5404 && target.NpcVNum == 2306)
                            {
                                Session.SendPacket(StaticPacketHelper.Cancel(2));
                             //   Session.SendPacket($"delay 1000 13 #guri^513^2^{target.MapNpcId}");
                                ServerManager.Instance.ChangeMap(Session.Character.CharacterId, 2628, 75, 69);
                                return;
                            }
                        }
                    }
                    break;
            }

            if (!Session.Character.CanAttack())
            {
                Session.SendPacket(StaticPacketHelper.Cancel());
                return;
            }

            switch (useSkillPacket.UserType)
            {
                case UserType.Player:
                    {
                        Character target = ServerManager.Instance.GetSessionByCharacterId(useSkillPacket.MapMonsterId)?.Character;
                        if(target.Session.Character.HasGodMode == true)
                        {
                            Session.SendPacket(StaticPacketHelper.Cancel(2));
                            return;
                        }
                        if (target.Session.Character.LastPVPRevive > DateTime.Now.AddSeconds(-5)
                          || Session.Character.LastPVPRevive > DateTime.Now.AddSeconds(-5))
                        {
                            Session.SendPacket(StaticPacketHelper.Cancel(2, target.Session.Character.CharacterId));
                            return;
                        }
                            if (target != null && target.CharacterId != Session.Character.CharacterId)
                        {
                            if (target.HasBuff(CardType.FrozenDebuff, (byte)AdditionalTypes.FrozenDebuff.EternalIce))
                            {
                                Session.SendPacket(StaticPacketHelper.Cancel(2));
                                Session.SendPacket($"delay 2000 5 #guri^502^1^{target.CharacterId}");
                                Session.Character.MapInstance.Broadcast(UserInterfaceHelper.GenerateGuri(2, 1, Session.Character.CharacterId), ReceiverType.AllExceptMe);
                                return;
                            }
                        }
                    }
                    break;
            }

            if (Session.Character.IsLaurenaMorph())
            {
                Session.SendPacket(StaticPacketHelper.Cancel());
                return;
            }

            if (Session.Character.CanFight && useSkillPacket != null && !Session.Character.IsSeal)
            {
                if (useSkillPacket.UserType == UserType.Monster)
                {
                    MapMonster monsterToAttack = Session.Character.MapInstance.GetMonsterById(useSkillPacket.MapMonsterId);
                    if (monsterToAttack != null)
                    {
                        if (Session.Character.Quests.Any(q => q.Quest.QuestType == (int)QuestType.Required && q.Quest.QuestObjectives.Any(s => s.Data == monsterToAttack.MonsterVNum)))
                        {
                            Session.Character.IncrementQuests(QuestType.Required, monsterToAttack.MonsterVNum);
                            Session.SendPacket(StaticPacketHelper.Cancel());
                            return;
                        }
                    } 
                }

                List<CharacterSkill> skills = Session.Character.GetSkills();

                if (skills != null)
                {
                    CharacterSkill ski = skills.FirstOrDefault(s => s?.Skill?.CastId == useSkillPacket.CastId && (s.Skill?.UpgradeSkill == 0 || s.Skill?.SkillType == 1));

                    if (ski != null)
                    {
                        if (ski.GetSkillBCards().ToList().Any(s => s.Type.Equals((byte)CardType.MeditationSkill)
                            && s.SubType.Equals((byte)AdditionalTypes.MeditationSkill.Sacrifice / 10)))
                        {
                            if (Session.Character.MapInstance.BattleEntities.ToList().FirstOrDefault(s => s.UserType == useSkillPacket.UserType && s.MapEntityId == useSkillPacket.MapMonsterId) is BattleEntity targetEntity)
                            {
                                if (Session.Character.BattleEntity.CanAttackEntity(targetEntity))
                                {
                                    Session.SendPacket(StaticPacketHelper.Cancel());
                                    return;
                                }
                            }
                        }
                        else if (!(ski.Skill.TargetType == 1 && ski.Skill.HitType != 1)
                            && !(ski.Skill.TargetType == 2 && ski.Skill.HitType == 0)
                            && !(ski.Skill.TargetType == 1 && ski.Skill.HitType == 1))
                        {
                            if (Session.Character.MapInstance.BattleEntities.ToList().FirstOrDefault(s => s.UserType == useSkillPacket.UserType && s.MapEntityId == useSkillPacket.MapMonsterId) is BattleEntity targetEntity)
                            {
                                if (!Session.Character.BattleEntity.CanAttackEntity(targetEntity))
                                {
                                    Session.SendPacket(StaticPacketHelper.Cancel());
                                    return;
                                }
                            }
                        }
                    }
                }

                Session.Character.RemoveBuff(614);
                Session.Character.RemoveBuff(615);
                Session.Character.RemoveBuff(616);

                bool isMuted = Session.Character.MuteMessage();

                if (isMuted || Session.Character.IsVehicled)
                {
                    Session.SendPacket(StaticPacketHelper.Cancel());
                    return;
                }

                if (Session.Character.Authority != AuthorityType.Owner && Session.Character.InvisibleGm)
                {
                    Session.SendPacket(StaticPacketHelper.Cancel());
                    return;
                }

                if (useSkillPacket.MapX.HasValue && useSkillPacket.MapY.HasValue)
                {
                    Session.Character.PositionX = useSkillPacket.MapX.Value;
                    Session.Character.PositionY = useSkillPacket.MapY.Value;
                }

                if (Session.Character.IsSitting)
                {
                    Session.Character.Rest();
                }
                
                switch (useSkillPacket.UserType)
                {
                    case UserType.Npc:
                    case UserType.Monster:
                        if (Session.Character.Hp > 0)
                        {
                            TargetHit(useSkillPacket.CastId, useSkillPacket.UserType, useSkillPacket.MapMonsterId);
                        }

                        break;

                    case UserType.Player:
                        if (Session.Character.Hp > 0)
                        {
                            if (useSkillPacket.MapMonsterId != Session.Character.CharacterId)
                            {
                                TargetHit(useSkillPacket.CastId, useSkillPacket.UserType, useSkillPacket.MapMonsterId, true);
                            }
                            else
                            {
                                TargetHit(useSkillPacket.CastId, useSkillPacket.UserType, useSkillPacket.MapMonsterId);
                            }
                        }
                        else
                        {
                            Session.SendPacket(StaticPacketHelper.Cancel(2));
                        }

                        break;

                    default:
                        Session.SendPacket(StaticPacketHelper.Cancel(2));
                        return;
                }

                if (skills != null)
                {
                    CharacterSkill ski = skills.FirstOrDefault(s => s?.Skill?.CastId == useSkillPacket.CastId);

                    if (ski == null || !(ski.Skill.TargetType == 1 && ski.Skill.HitType == 1))
                    {
                        if (Session.Character.MapInstance.BattleEntities.FirstOrDefault(s => s.MapEntityId == useSkillPacket.MapMonsterId) is BattleEntity target)
                        {
                            if (target.Hp <= 0)
                            {
                                Session.SendPacket(StaticPacketHelper.Cancel(2));
                            }
                        }
                        else
                        {
                            Session.SendPacket(StaticPacketHelper.Cancel(2));
                        }
                    }
                }
            }
            else
            {
                Session.SendPacket(StaticPacketHelper.Cancel(2));
            }
        }

        /// <summary>
        /// u_as packet
        /// </summary>
        /// <param name="useAoeSkillPacket"></param>
        public void UseZonesSkill(UseAOESkillPacket useAoeSkillPacket)
        {
            Session.Character.Direction = Session.Character.BeforeDirection;

            bool isMuted = Session.Character.MuteMessage();
            if (isMuted || Session.Character.IsVehicled)
            {
                Session.SendPacket(StaticPacketHelper.Cancel());
            }
            else
            {
                if (Session.Character.LastTransform.AddSeconds(3) > DateTime.Now)
                {
                    Session.SendPacket(StaticPacketHelper.Cancel());
                    Session.SendPacket(
                        UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("CANT_ATTACK"), 0));
                    return;
                }

                if (Session.Character.CanFight && Session.Character.Hp > 0)
                {
                    ZoneHit(useAoeSkillPacket.CastId, useAoeSkillPacket.MapX, useAoeSkillPacket.MapY);
                }
            }
        }

        /// <summary>
        /// ob_a packet
        /// </summary>
        /// <param name="useIconFalconSkillPacket"></param>
        public void UseIconFalconSkill(UseIconFalconSkillPacket useIconFalconSkillPacket)
        {
            if (Session.Character.BattleEntity.FalconFocusedEntityId > 0)
            {
                HitRequest iconSkillHitRequest = new HitRequest(TargetHitType.SingleTargetHit, Session, ServerManager.GetSkill(1248), 4283);
                if (Session.CurrentMapInstance.BattleEntities.FirstOrDefault(s => s.MapEntityId == Session.Character.BattleEntity.FalconFocusedEntityId) is BattleEntity FalconFocusedEntity)
                {
                    Session.SendPacket("ob_ar");
                    switch (FalconFocusedEntity.EntityType)
                    {
                        case EntityType.Player:
                            PvpHit(iconSkillHitRequest, FalconFocusedEntity.Character.Session);
                            break;

                        case EntityType.Monster:
                            FalconFocusedEntity.MapMonster.HitQueue.Enqueue(iconSkillHitRequest);
                            break;

                        case EntityType.Mate:
                            FalconFocusedEntity.Mate.HitRequest(iconSkillHitRequest);
                            break;
                    }
                    Session.CurrentMapInstance.Broadcast(Session, $"eff_ob  {(byte)FalconFocusedEntity.UserType} {FalconFocusedEntity.MapEntityId} 0 4269", ReceiverType.AllExceptMe);
                }
            }
        }

        private void PvpHit(HitRequest hitRequest, ClientSession target)
        {
            if (target?.Character.Hp > 0 && hitRequest?.Session.Character.Hp > 0)
            {
                if (target.Character.IsSitting)
                {
                    target.Character.Rest();
                }

                double cooldownReduction = Session.Character.GetBuff(CardType.Morale, (byte)AdditionalTypes.Morale.SkillCooldownDecreased)[0];

                int[] increaseEnemyCooldownChance = Session.Character.GetBuff(CardType.DarkCloneSummon, (byte)AdditionalTypes.DarkCloneSummon.IncreaseEnemyCooldownChance);

                if (ServerManager.RandomNumber() < increaseEnemyCooldownChance[0])
                {
                    cooldownReduction -= increaseEnemyCooldownChance[1];
                }
                
                int hitmode = 0;
                bool onyxWings = false;
                BattleEntity battleEntity = new BattleEntity(hitRequest.Session.Character, hitRequest.Skill);
                BattleEntity battleEntityDefense = new BattleEntity(target.Character, null);
                int damage = DamageHelper.Instance.CalculateDamage(battleEntity, battleEntityDefense, hitRequest.Skill,
                    ref hitmode, ref onyxWings);
                if (target.Character.HasGodMode)
                {
                    hitRequest?.Session.SendPacket(StaticPacketHelper.Cancel(2, target.Character.CharacterId));
                    return;
                    damage = 0;
                    hitmode = 4;
                }
                else if (target.Character.LastPVPRevive > DateTime.Now.AddSeconds(-5)
                         || hitRequest.Session.Character.LastPVPRevive > DateTime.Now.AddSeconds(-5))
                {
                    hitRequest?.Session.SendPacket(StaticPacketHelper.Cancel(2, target.Character.CharacterId));
                    return;
                    damage = 0;
                    hitmode = 4;
                    hitRequest?.Session.SendPacket(StaticPacketHelper.Cancel(2, target.Character.CharacterId));
                }

                if (ServerManager.RandomNumber() < target.Character.GetBuff(CardType.DarkCloneSummon,
                    (byte)AdditionalTypes.DarkCloneSummon.ConvertDamageToHPChance)[0])
                {
                    int amount = damage / 2;

                    target.Character.ConvertedDamageToHP += amount;
                    target.Character.MapInstance?.Broadcast(target.Character.GenerateRc(amount));
                    target.Character.Hp += amount;

                    if (target.Character.Hp > target.Character.HPLoad())
                    {
                        target.Character.Hp = (int)target.Character.HPLoad();
                    }

                    target.SendPacket(target.Character.GenerateStat());

                    damage = 0;
                }

                if (hitmode != 4 && hitmode != 2 && damage > 0)
                {
                    Session.Character.RemoveBuffByBCardTypeSubType(new List<KeyValuePair<byte, byte>>
                    {
                        new KeyValuePair<byte, byte>((byte)CardType.SpecialActions, (byte)AdditionalTypes.SpecialActions.Hide)
                    });
                    target.Character.RemoveBuffByBCardTypeSubType(new List<KeyValuePair<byte, byte>>
                    {
                        new KeyValuePair<byte, byte>((byte)CardType.SpecialActions, (byte)AdditionalTypes.SpecialActions.Hide)
                    });
                    target.Character.RemoveBuff(36);
                    target.Character.RemoveBuff(548);
                }
                if (damage == 0 && Session.Character.LastSkillComboUse < DateTime.Now)
                {
                    Session.Character.SkillComboCount = 0;
                    Session.Character.LastSkillComboUse = DateTime.Now.AddSeconds(3);
                }
                if (Session.Character.Buff.FirstOrDefault(s => s.Card.BCards.Any(b => b.Type == (byte)BCardType.CardType.FalconSkill && b.SubType.Equals((byte)AdditionalTypes.FalconSkill.Hide / 10))) is Buff FalconHideBuff)
                {
                    Session.Character.RemoveBuff(FalconHideBuff.Card.CardId);
                    Session.Character.AddBuff(new Buff(560, Session.Character.Level), Session.Character.BattleEntity);
                }

                int[] manaShield = target.Character.GetBuff(CardType.LightAndShadow,
                    (byte) AdditionalTypes.LightAndShadow.InflictDamageToMP);
                if (manaShield[0] != 0 && hitmode != 4)
                {
                    int reduce = damage / 100 * manaShield[0];
                    if (target.Character.Mp < reduce)
                    {
                        reduce = target.Character.Mp;
                        target.Character.Mp = 0;
                    }
                    else
                    {
                        target.Character.DecreaseMp(reduce);
                    }
                    damage -= reduce;
                }

                if (onyxWings && hitmode != 4 && hitmode != 2)
                {
                    short onyxX = (short) (hitRequest.Session.Character.PositionX + 2);
                    short onyxY = (short) (hitRequest.Session.Character.PositionY + 2);
                    int onyxId = target.CurrentMapInstance.GetNextMonsterId();
                    MapMonster onyx = new MapMonster
                    {
                        MonsterVNum = 2371,
                        MapX = onyxX,
                        MapY = onyxY,
                        MapMonsterId = onyxId,
                        IsHostile = false,
                        IsMoving = false,
                        ShouldRespawn = false
                    };
                    target.CurrentMapInstance.Broadcast(UserInterfaceHelper.GenerateGuri(31, 1,
                        hitRequest.Session.Character.CharacterId, onyxX, onyxY));
                    onyx.Initialize(target.CurrentMapInstance);
                    target.CurrentMapInstance.AddMonster(onyx);
                    target.CurrentMapInstance.Broadcast(onyx.GenerateIn());
                    target.Character.GetDamage((int)(damage / 2D), battleEntity);
                    Observable.Timer(TimeSpan.FromMilliseconds(350)).Subscribe(o =>
                    {
                        target.CurrentMapInstance.Broadcast(StaticPacketHelper.SkillUsed(UserType.Monster, onyxId, 1,
                            target.Character.CharacterId, -1, 0, -1, hitRequest.Skill.Effect, -1, -1, true, 92,
                            (int)(damage / 2D), 0, 0));
                        target.CurrentMapInstance.RemoveMonster(onyx);
                        target.CurrentMapInstance.Broadcast(StaticPacketHelper.Out(UserType.Monster,
                            onyx.MapMonsterId));
                    });
                }

                #region C45 equipe buffs

                ItemInstance itemInUse = null;

                //A bit hardcoded but works properly
                if (Session.Character.Buff.ContainsKey(413) || Session.Character.Buff.ContainsKey(416) || Session.Character.Buff.ContainsKey(414))
                {
                    if (Session.Character.Buff.ContainsKey(413) || Session.Character.Buff.ContainsKey(414))
                        Session.Character.ConvertedDamageToHP = (int)(damage * 8 / 100D);
                    else
                    {
                        Session.Character.ConvertedDamageToHP = (int)(damage * 15 / 100D);
                        Session.Character.Mp += Session.Character.ConvertedDamageToHP;
                    }
                    Session.CurrentMapInstance?.Broadcast(Session.Character.GenerateRc(Session.Character.ConvertedDamageToHP));
                    Session.Character.Hp += Session.Character.ConvertedDamageToHP;
                    Session.Character.Session.SendPacket(Session.Character.GenerateStat());
                }

                //Main weapons
                itemInUse = Session.Character.Inventory.LoadBySlotAndType(0, InventoryType.Wear);

                //Sword
                if (itemInUse != null && itemInUse.Item.VNum == 4981 && ServerManager.RandomNumber() <= 8)
                {
                    Session.Character.ConvertedDamageToHP = (int)(damage * 8 / 100D);
                    Session.Character.AddBuff(new Buff(413, Session.Character.Level), Session.Character.BattleEntity);
                }

                //Staff
                if (itemInUse != null && itemInUse.Item.VNum == 4982 && ServerManager.RandomNumber() <= 10 && !Session.Character.Buff.ContainsKey(588))
                {
                    if(Session.Character.Buff.ContainsKey(588))
                    {
                        Session.Character.RemoveBuff(416);
                    }
                    Session.Character.ConvertedDamageToHP = (int)(damage * 15 / 100D);
                    Session.Character.AddBuff(new Buff(416, Session.Character.Level), Session.Character.BattleEntity);
                }

                //Bow
                if (itemInUse != null && itemInUse.Item.VNum == 4983 && ServerManager.RandomNumber() <= 4)
                {
                    target.Character.AddBuff(new Buff(415, Session.Character.Level), Session.Character.BattleEntity);
                }

                if (target.Character.Buff.ContainsKey(415) && ServerManager.RandomNumber() <= 50)
                    foreach (Buff b in target.Character.Buff.Where(b => b.Card.BuffType == BuffType.Good && b.Card.Level < 4))
                        target.Character.RemoveBuff(b.Card.CardId);

                //Punch
                if(itemInUse != null && itemInUse.Item.VNum == 4736 && ServerManager.RandomNumber() <= 7)
                {
                    target.Character.AddBuff(new Buff(672, Session.Character.Level), Session.Character.BattleEntity);
                }

                //Secondary weapons
                itemInUse = Session.Character.Inventory.LoadBySlotAndType(5, InventoryType.Wear);

                //Crossbow
                if (itemInUse != null && itemInUse.Item.VNum == 4978 && ServerManager.RandomNumber() <= 5)
                {
                    target.Character.AddBuff(new Buff(417, Session.Character.Level), Session.Character.BattleEntity);
                }

                //Dagger
                if (itemInUse != null && itemInUse.Item.VNum == 4980 && ServerManager.RandomNumber() <= 8)
                {
                    Session.Character.ConvertedDamageToHP = (int)(damage * 8 / 100D);
                    Session.Character.AddBuff(new Buff(414, Session.Character.Level), Session.Character.BattleEntity);
                }

                //Gun
                if (itemInUse != null && itemInUse.Item.VNum == 4979 && ServerManager.RandomNumber() <= 5)
                {
                    target.Character.AddBuff(new Buff(418, Session.Character.Level), Session.Character.BattleEntity);
                }

                //NO c45 secondary weap for MA 

                //Armors
                itemInUse = Session.Character.Inventory.LoadBySlotAndType(1, InventoryType.Wear);

                //Swordsman
                if (itemInUse != null && itemInUse.Item.VNum == 4984 && ServerManager.RandomNumber() <= 2)
                {
                    Session.Character.AddBuff(new Buff(419, Session.Character.Level), Session.Character.BattleEntity);
                }

                //Mage
                if (itemInUse != null && itemInUse.Item.VNum == 4985 && ServerManager.RandomNumber() <= 2)
                {
                    Session.Character.AddBuff(new Buff(421, Session.Character.Level), Session.Character.BattleEntity);
                }

                //Archer
                if (itemInUse != null && itemInUse.Item.VNum == 4986 && ServerManager.RandomNumber() <= 2)
                {
                    Session.Character.AddBuff(new Buff(420, Session.Character.Level), Session.Character.BattleEntity);
                }

                //Martial Artist
                if(itemInUse != null && itemInUse.Item.VNum == 4754 && ServerManager.RandomNumber() <= 2)
                {
                    Session.Character.AddBuff(new Buff(673, Session.Character.Level), Session.Character.BattleEntity);
                }

                #endregion

                target.Character.GetDamage(damage / 2, battleEntity);
                target.SendPacket(target.Character.GenerateStat());

                // Magical Fetters

                if (damage > 0)
                {
                    if (target.Character.HasMagicalFetters)
                    {
                        // Magic Spell

                        target.Character.AddBuff(new Buff(617, target.Character.Level), target.Character.BattleEntity);

                        int castId = 10 + Session.Character.Element;

                        if (castId == 10)
                        {
                            castId += 5; // No element
                        }

                        target.Character.LastComboCastId = castId;
                        target.SendPacket($"mslot {castId} -1");
                    }
                }

                bool isAlive = target.Character.Hp > 0;
                if (!isAlive && target.HasCurrentMapInstance)
                {
                    if (target.Character.IsVehicled)
                    {
                        target.Character.RemoveVehicle();
                    }

                    if (hitRequest.Session.Character != null && hitRequest.SkillBCards.FirstOrDefault(s => s.Type == (byte)CardType.TauntSkill && s.SubType == (byte)AdditionalTypes.TauntSkill.EffectOnKill / 10) is BCard EffectOnKill)
                    {
                        if (ServerManager.RandomNumber() < EffectOnKill.FirstData)
                        {
                            hitRequest.Session.Character.AddBuff(new Buff((short)EffectOnKill.SecondData, hitRequest.Session.Character.Level), hitRequest.Session.Character.BattleEntity);
                        }
                    }
                    
                    target.Character.LastPvPKiller = Session;
                    if (target.CurrentMapInstance.Map?.MapTypes.Any(s => s.MapTypeId == (short) MapTypeEnum.Act4)
                        == true)
                    {
                        if (ServerManager.Instance.ChannelId == 51 && ServerManager.Instance.Act4DemonStat.Mode == 0
                                                                   && ServerManager.Instance.Act4AngelStat.Mode == 0)
                        {
                            switch (Session.Character.Faction)
                            {
                                case FactionType.Angel:
                                    ServerManager.Instance.Act4AngelStat.Percentage += 5000 * ServerManager.Instance.Configuration.Act4Rate;
                                    break;

                                case FactionType.Demon:
                                    ServerManager.Instance.Act4DemonStat.Percentage += 5000 * ServerManager.Instance.Configuration.Act4Rate;
                                    break;
                            }
                        }

                        hitRequest.Session.Character.Act4Kill++;
                        target.Character.Act4Dead++;
                        target.Character.GetAct4Points(-1);
                        if (target.Character.Level + 10 >= hitRequest.Session.Character.Level
                            && hitRequest.Session.Character.Level <= target.Character.Level - 10)
                        {
                            hitRequest.Session.Character.GetAct4Points(2);
                        }

                        if (target.Character.Session.CleanIpAddress != hitRequest.Session.CleanIpAddress)
                        {
                            int levelDifference = target.Character.Level - hitRequest.Session.Character.Level;

                            if (levelDifference < 30)
                            {
                                int ReputationValue = 0;

                                if (levelDifference >= 0)
                                {
                                    ReputationValue = 500 + (levelDifference * 100);
                                }
                                else if (levelDifference > -20)
                                {
                                    ReputationValue = 500 - (levelDifference * 25);
                                }
                                else
                                {
                                    ReputationValue -= 150 + (-levelDifference * 10);
                                }

                                ReputationValue *= ServerManager.Instance.Configuration.RateReputation;

                                if (ReputationValue > 0)
                                {
                                    hitRequest.Session.Character.Reputation += ReputationValue;
                                    hitRequest.Session.SendPacket(hitRequest.Session.Character.GenerateSay(
                                        string.Format(Language.Instance.GetMessageFromKey("WIN_REPUT"),
                                            (short)ReputationValue), 12));
                                    hitRequest.Session.Character.GiftAdd(2361, 1);

                                    int act4RaidPenalty =
                                        ((target.Character.Faction == FactionType.Angel && ServerManager.Instance.Act4DemonStat.Mode == 3)
                                        || (target.Character.Faction == FactionType.Demon && ServerManager.Instance.Act4AngelStat.Mode == 3))
                                        ? 2 : 1;

                                    target.Character.Reputation -= ReputationValue/* * act4RaidPenalty*/;
                                    target.SendPacket(target.Character.GenerateSay(
                                        string.Format(Language.Instance.GetMessageFromKey("LOSE_REP"),
                                            (short)ReputationValue/* * act4RaidPenalty*/), 11));
                                }
                                else
                                {
                                    hitRequest.Session.Character.Reputation -= ReputationValue;
                                    hitRequest.Session.SendPacket(hitRequest.Session.Character.GenerateSay(
                                        string.Format(Language.Instance.GetMessageFromKey("LOSE_REP"),
                                            (short)ReputationValue), 11));
                                }
                                hitRequest.Session.SendPacket(hitRequest.Session.Character.GenerateLev());
                            }
                            else
                            {
                                hitRequest.Session.SendPacket(hitRequest.Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("TOO_LEVEL_DIFFERENCE"), 11));
                            }
                        }
                        else
                        {
                            hitRequest.Session.SendPacket(hitRequest.Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("TARGET_SAME_IP"), 11));
                        }

                        foreach (ClientSession sess in ServerManager.Instance.Sessions.Where(
                            s => s.HasSelectedCharacter))
                        {
                            if (sess.Character.Faction == Session.Character.Faction)
                            {
                                sess.SendPacket(sess.Character.GenerateSay(
                                    string.Format(
                                        Language.Instance.GetMessageFromKey(
                                            $"ACT4_PVP_KILL{(int) target.Character.Faction}"), Session.Character.Name),
                                    12));
                            }
                            else if (sess.Character.Faction == target.Character.Faction)
                            {
                                sess.SendPacket(sess.Character.GenerateSay(
                                    string.Format(
                                        Language.Instance.GetMessageFromKey(
                                            $"ACT4_PVP_DEATH{(int) target.Character.Faction}"), target.Character.Name),
                                    11));
                            }
                        }

                        target.SendPacket(target.Character.GenerateFd());
                        target.CurrentMapInstance?.Broadcast(target, target.Character.GenerateIn(InEffect: 1), ReceiverType.AllExceptMe);
                        target.CurrentMapInstance?.Broadcast(target, target.Character.GenerateGidx(), ReceiverType.AllExceptMe);
                        hitRequest.Session.SendPacket(hitRequest.Session.Character.GenerateFd());
                        hitRequest.Session.CurrentMapInstance?.Broadcast(hitRequest.Session, hitRequest.Session.Character.GenerateIn(InEffect: 1), ReceiverType.AllExceptMe);
                        hitRequest.Session.CurrentMapInstance?.Broadcast(hitRequest.Session, hitRequest.Session.Character.GenerateGidx(), ReceiverType.AllExceptMe);
                        target.Character.DisableBuffs(BuffType.All);

                        if (target.Character.MapInstance == CaligorRaid.CaligorMapInstance)
                        {
                            ServerManager.Instance.AskRevive(target.Character.CharacterId);
                        }
                        else
                        {
                            target.SendPacket(
                                target.Character.GenerateSay(Language.Instance.GetMessageFromKey("ACT4_PVP_DIE"), 11));
                            target.SendPacket(
                                UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("ACT4_PVP_DIE"), 0));

                            Observable.Timer(TimeSpan.FromMilliseconds(2000)).Subscribe(o => target.Character.SetSeal());
                        }
                    }
                    else if (target.CurrentMapInstance.MapInstanceType == MapInstanceType.IceBreakerInstance)
                    {
                        if (IceBreaker.AlreadyFrozenPlayers.Contains(target))
                        {
                            IceBreaker.AlreadyFrozenPlayers.Remove(target);
                            target.CurrentMapInstance?.Broadcast(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("ICEBREAKER_PLAYER_OUT"), target?.Character?.Name), 0));
                            target.Character.Hp = 1;
                            target.Character.Mp = 1;
                            var respawn = target?.Character?.Respawn;
                            ServerManager.Instance.ChangeMap(target.Character.CharacterId, respawn.DefaultMapId);
                            Session.SendPacket(StaticPacketHelper.Cancel(2, target.Character.CharacterId));
                        }
                        else
                        {
                            isAlive = true;
                            IceBreaker.FrozenPlayers.Add(target);
                            target.CurrentMapInstance?.Broadcast(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("ICEBREAKER_PLAYER_FROZEN"), target?.Character?.Name), 0));
                            Task.Run(() =>
                            {
                                target.Character.Hp = (int)target.Character.HPLoad();
                                target.Character.Mp = (int)target.Character.MPLoad();
                                target.SendPacket(target?.Character?.GenerateStat());
                                target.Character.NoMove = true;
                                target.Character.NoAttack = true;
                                target.SendPacket(target?.Character?.GenerateCond());
                                while (IceBreaker.FrozenPlayers.Contains(target))
                                {
                                    target?.CurrentMapInstance?.Broadcast(target?.Character?.GenerateEff(35));
                                    Thread.Sleep(1000);
                                }
                            });
                        }
                    }
                    else
                    {                      
                        if (Session.CurrentMapInstance?.MapInstanceType != MapInstanceType.TalentArenaMapInstance)
                        {
                            hitRequest.Session.Character.BattleEntity.ApplyScoreArena(target.Character.BattleEntity);
                            Session.CurrentMapInstance?.Broadcast(Session.Character.GenerateSay(
                            string.Format(Language.Instance.GetMessageFromKey("PVP_KILL"),
                               Session.Character.Name, target.Character.Name), 10));     
                            Session.CurrentMapInstance?.Broadcast(UserInterfaceHelper.GenerateMsg(
                          string.Format(Language.Instance.GetMessageFromKey("PVP_KILL"),
                             Session.Character.Name, target.Character.Name), 0));
                            Observable.Timer(TimeSpan.FromMilliseconds(3000)).Subscribe(o =>
                                ServerManager.Instance.AskPvpRevive(target.Character.CharacterId));
                        }
                        else
                        {
                            Session.CurrentMapInstance?.Broadcast(Session.Character.GenerateSay(
                            string.Format(Language.Instance.GetMessageFromKey("ADTPVP_KILL"),
                                Session.Character.Name, target.Character.Name), 10));
                            Observable.Timer(TimeSpan.FromMilliseconds(1000)).Subscribe(o =>
                               ServerManager.Instance.AskPvpRevive(target.Character.CharacterId));
                        }
                        if (target.Character.IsVehicled)
                        {
                            target.Character.RemoveVehicle();
                        }                     
                    }
                }

                battleEntity.BCards.Where(s => s.CastType == 1).ForEach(s =>
                {
                    if (s.Type != (byte)CardType.Buff)
                    {
                        s.ApplyBCards(target.Character.BattleEntity, Session.Character.BattleEntity);
                    }
                });

                hitRequest.SkillBCards.Where(s => !s.Type.Equals((byte)CardType.Buff) && !s.Type.Equals((byte)CardType.Capture) && s.CardId == null).ToList()
                    .ForEach(s => s.ApplyBCards(target.Character.BattleEntity, Session.Character.BattleEntity));

                if (hitmode != 4 && hitmode != 2)
                {
                    battleEntity.BCards.Where(s => s.CastType == 1).ForEach(s =>
                    {
                        if (s.Type == (byte)CardType.Buff)
                        {
                            Buff b = new Buff((short)s.SecondData, battleEntity.Level);
                            if (b.Card != null)
                            {
                                switch (b.Card?.BuffType)
                                {
                                    case BuffType.Bad:
                                        s.ApplyBCards(target.Character.BattleEntity, Session.Character.BattleEntity);
                                        break;

                                    case BuffType.Good:
                                    case BuffType.Neutral:
                                        s.ApplyBCards(Session.Character.BattleEntity, Session.Character.BattleEntity);
                                        break;
                                }
                            }
                        }
                    });
                    
                    battleEntityDefense.BCards.Where(s => s.CastType == 0).ForEach(s =>
                    {
                        if (s.Type == (byte)CardType.Buff)
                        {
                            Buff b = new Buff((short)s.SecondData, battleEntityDefense.Level);
                            if (b.Card != null)
                            {
                                switch (b.Card?.BuffType)
                                {
                                    case BuffType.Bad:
                                        s.ApplyBCards(Session.Character.BattleEntity , target.Character.BattleEntity);
                                        break;

                                    case BuffType.Good:
                                    case BuffType.Neutral:
                                        s.ApplyBCards(target.Character.BattleEntity, target.Character.BattleEntity);
                                        break;
                                }
                            }
                        }
                    });

                    hitRequest.SkillBCards.Where(s => s.Type.Equals((byte)CardType.Buff) && new Buff((short)s.SecondData, Session.Character.Level).Card?.BuffType == BuffType.Bad).ToList()
                        .ForEach(s => s.ApplyBCards(target.Character.BattleEntity, Session.Character.BattleEntity));

                    hitRequest.SkillBCards.Where(s => s.Type.Equals((byte)CardType.SniperAttack)).ToList()
                        .ForEach(s => s.ApplyBCards(target.Character.BattleEntity, Session.Character.BattleEntity));

                    if (battleEntity?.ShellWeaponEffects != null)
                    {
                        foreach (ShellEffectDTO shell in battleEntity.ShellWeaponEffects)
                        {
                            switch (shell.Effect)
                            {
                                case (byte) ShellWeaponEffectType.Blackout:
                                {
                                    Buff buff = new Buff(7, battleEntity.Level);
                                    if (ServerManager.RandomNumber() < shell.Value
                                        - (shell.Value
                                           * (battleEntityDefense.ShellArmorEffects?.Find(s =>
                                                  s.Effect == (byte) ShellArmorEffectType.ReducedStun)?.Value
                                              + battleEntityDefense.ShellArmorEffects?.Find(s =>
                                                  s.Effect == (byte) ShellArmorEffectType.ReducedAllStun)?.Value
                                              + battleEntityDefense.ShellArmorEffects?.Find(s =>
                                                      s.Effect == (byte) ShellArmorEffectType.ReducedAllNegativeEffect)
                                                  ?.Value) / 100D))
                                    {
                                        target.Character.AddBuff(buff, battleEntity);
                                    }

                                    break;
                                }
                                case (byte) ShellWeaponEffectType.DeadlyBlackout:
                                {
                                    Buff buff = new Buff(66, battleEntity.Level);
                                    if (ServerManager.RandomNumber() < shell.Value
                                        - (shell.Value
                                           * (battleEntityDefense.ShellArmorEffects?.Find(s =>
                                                  s.Effect == (byte) ShellArmorEffectType.ReducedAllStun)?.Value
                                              + battleEntityDefense.ShellArmorEffects?.Find(s =>
                                                      s.Effect == (byte) ShellArmorEffectType.ReducedAllNegativeEffect)
                                                  ?.Value) / 100D))
                                    {
                                        target.Character.AddBuff(buff, battleEntity);
                                    }

                                    break;
                                }
                                case (byte) ShellWeaponEffectType.MinorBleeding:
                                {
                                    Buff buff = new Buff(1, battleEntity.Level);
                                    if (ServerManager.RandomNumber() < shell.Value
                                        - (shell.Value * (battleEntityDefense.ShellArmorEffects?.Find(s =>
                                                              s.Effect == (byte) ShellArmorEffectType
                                                                  .ReducedMinorBleeding)?.Value
                                                          + battleEntityDefense.ShellArmorEffects?.Find(s =>
                                                              s.Effect == (byte) ShellArmorEffectType
                                                                  .ReducedBleedingAndMinorBleeding)?.Value
                                                          + battleEntityDefense.ShellArmorEffects?.Find(s =>
                                                              s.Effect == (byte) ShellArmorEffectType
                                                                  .ReducedAllBleedingType)?.Value
                                                          + battleEntityDefense.ShellArmorEffects?.Find(s =>
                                                              s.Effect == (byte) ShellArmorEffectType
                                                                  .ReducedAllNegativeEffect)?.Value) / 100D))
                                    {
                                        target.Character.AddBuff(buff, battleEntity);
                                    }

                                    break;
                                }
                                case (byte) ShellWeaponEffectType.Bleeding:
                                {
                                    Buff buff = new Buff(21, battleEntity.Level);
                                    if (ServerManager.RandomNumber() < shell.Value
                                        - (shell.Value * (battleEntityDefense.ShellArmorEffects?.Find(s =>
                                                              s.Effect == (byte) ShellArmorEffectType
                                                                  .ReducedBleedingAndMinorBleeding)?.Value
                                                          + battleEntityDefense.ShellArmorEffects?.Find(s =>
                                                              s.Effect == (byte) ShellArmorEffectType
                                                                  .ReducedAllBleedingType)?.Value
                                                          + battleEntityDefense.ShellArmorEffects?.Find(s =>
                                                              s.Effect == (byte) ShellArmorEffectType
                                                                  .ReducedAllNegativeEffect)?.Value) / 100D))
                                    {
                                        target.Character.AddBuff(buff, battleEntity);
                                    }

                                    break;
                                }
                                case (byte) ShellWeaponEffectType.HeavyBleeding:
                                {
                                    Buff buff = new Buff(42, battleEntity.Level);
                                    if (ServerManager.RandomNumber() < shell.Value
                                        - (shell.Value * (battleEntityDefense.ShellArmorEffects?.Find(s =>
                                                              s.Effect == (byte) ShellArmorEffectType
                                                                  .ReducedAllBleedingType)?.Value
                                                          + battleEntityDefense.ShellArmorEffects?.Find(s =>
                                                              s.Effect == (byte) ShellArmorEffectType
                                                                  .ReducedAllNegativeEffect)?.Value) / 100D))
                                    {
                                        target.Character.AddBuff(buff, battleEntity);
                                    }

                                    break;
                                }
                                case (byte) ShellWeaponEffectType.Freeze:
                                {
                                    Buff buff = new Buff(27, battleEntity.Level);
                                    if (ServerManager.RandomNumber() < shell.Value - (shell.Value
                                                                                      * (battleEntityDefense
                                                                                             .ShellArmorEffects?.Find(
                                                                                                 s =>
                                                                                                     s.Effect ==
                                                                                                     (byte)
                                                                                                     ShellArmorEffectType
                                                                                                         .ReducedFreeze)
                                                                                             ?.Value
                                                                                         + battleEntityDefense
                                                                                             .ShellArmorEffects?.Find(
                                                                                                 s =>
                                                                                                     s.Effect ==
                                                                                                     (byte)
                                                                                                     ShellArmorEffectType
                                                                                                         .ReducedAllNegativeEffect)
                                                                                             ?.Value) / 100D))
                                    {
                                        target.Character.AddBuff(buff, battleEntity);
                                    }

                                    break;
                                }
                            }
                        }
                    }
                }

                if (hitmode != 2)
                {
                    switch (hitRequest.TargetHitType)
                    {
                        case TargetHitType.SingleTargetHit:
                            hitRequest.Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.SkillUsed(UserType.Player,
                                hitRequest.Session.Character.CharacterId, 1, target.Character.CharacterId,
                                hitRequest.Skill.SkillVNum, (short)(hitRequest.Skill.Cooldown + hitRequest.Skill.Cooldown * cooldownReduction / 100D), hitRequest.Skill.AttackAnimation,
                                hitRequest.SkillEffect, hitRequest.Session.Character.PositionX,
                                hitRequest.Session.Character.PositionY, isAlive,
                                (int)(target.Character.Hp / (float)target.Character.HPLoad() * 100), damage, hitmode,
                                (byte)(hitRequest.Skill.SkillType - 1)));
                            break;

                        case TargetHitType.SingleTargetHitCombo:
                            hitRequest.Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.SkillUsed(UserType.Player,
                                hitRequest.Session.Character.CharacterId, 1, target.Character.CharacterId,
                                hitRequest.Skill.SkillVNum, (short)(hitRequest.Skill.Cooldown + hitRequest.Skill.Cooldown * cooldownReduction / 100D), hitRequest.SkillCombo.Animation,
                                hitRequest.SkillCombo.Effect, hitRequest.Session.Character.PositionX,
                                hitRequest.Session.Character.PositionY, isAlive,
                                (int)(target.Character.Hp / (float)target.Character.HPLoad() * 100), damage, hitmode,
                                (byte)(hitRequest.Skill.SkillType - 1)));
                            break;

                        case TargetHitType.SingleAOETargetHit:
                            if (hitRequest.ShowTargetHitAnimation)
                            {
                                if (hitRequest.Skill.SkillVNum == 1085 || hitRequest.Skill.SkillVNum == 1091 || hitRequest.Skill.SkillVNum == 1060)
                                {
                                    hitRequest.Session.Character.PositionX = target.Character.PositionX;
                                    hitRequest.Session.Character.PositionY = target.Character.PositionY;
                                    hitRequest.Session.CurrentMapInstance.Broadcast(hitRequest.Session.Character.GenerateTp());
                                }
                                hitRequest.Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.SkillUsed(
                                    UserType.Player, hitRequest.Session.Character.CharacterId, 1, target.Character.CharacterId, 
                                    hitRequest.Skill.SkillVNum, (short)(hitRequest.Skill.Cooldown + hitRequest.Skill.Cooldown * cooldownReduction / 100D),
                                    hitRequest.Skill.AttackAnimation, hitRequest.SkillEffect, 
                                    hitRequest.Session.Character.PositionX, hitRequest.Session.Character.PositionY, isAlive,
                                    (int)(target.Character.Hp / (float)target.Character.HPLoad() * 100), damage, hitmode,
                                    (byte)(hitRequest.Skill.SkillType - 1)));
                            }
                            else
                            {
                                switch (hitmode)
                                {
                                    case 1:
                                    case 4:
                                        hitmode = 7;
                                        break;

                                    case 2:
                                        hitmode = 2;
                                        break;

                                    case 3:
                                        hitmode = 6;
                                        break;

                                    default:
                                        hitmode = 5;
                                        break;
                                }

                                hitRequest.Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.SkillUsed(
                                    UserType.Player, hitRequest.Session.Character.CharacterId, 1, target.Character.CharacterId, 
                                    -1, (short)(hitRequest.Skill.Cooldown + hitRequest.Skill.Cooldown * cooldownReduction / 100D),
                                    hitRequest.Skill.AttackAnimation, hitRequest.SkillEffect, 
                                    hitRequest.Session.Character.PositionX, hitRequest.Session.Character.PositionY, isAlive,
                                    (int)(target.Character.Hp / (float)target.Character.HPLoad() * 100), damage, hitmode,
                                    (byte)(hitRequest.Skill.SkillType - 1)));
                            }
                            break;

                        case TargetHitType.AOETargetHit:
                            switch (hitmode)
                            {
                                case 1:
                                case 4:
                                    hitmode = 7;
                                    break;

                                case 2:
                                    hitmode = 2;
                                    break;

                                case 3:
                                    hitmode = 6;
                                    break;

                                default:
                                    hitmode = 5;
                                    break;
                            }

                            hitRequest.Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.SkillUsed(UserType.Player,
                                hitRequest.Session.Character.CharacterId, 1, target.Character.CharacterId,
                                hitRequest.Skill.SkillVNum, (short)(hitRequest.Skill.Cooldown + hitRequest.Skill.Cooldown * cooldownReduction / 100D), hitRequest.Skill.AttackAnimation,
                                hitRequest.SkillEffect, hitRequest.Session.Character.PositionX,
                                hitRequest.Session.Character.PositionY, isAlive,
                                (int)(target.Character.Hp / (float)target.Character.HPLoad() * 100), damage, hitmode,
                                (byte)(hitRequest.Skill.SkillType - 1)));
                            break;

                        case TargetHitType.ZoneHit:
                            hitRequest.Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.SkillUsed(UserType.Player,
                                hitRequest.Session.Character.CharacterId, 1, target.Character.CharacterId,
                                hitRequest.Skill.SkillVNum, (short)(hitRequest.Skill.Cooldown + hitRequest.Skill.Cooldown * cooldownReduction / 100D), hitRequest.Skill.AttackAnimation,
                                hitRequest.SkillEffect, hitRequest.MapX, hitRequest.MapY, isAlive,
                                (int)(target.Character.Hp / (float)target.Character.HPLoad() * 100), damage, hitmode,
                                (byte)(hitRequest.Skill.SkillType - 1)));
                            break;

                        case TargetHitType.SpecialZoneHit:
                            hitRequest.Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.SkillUsed(UserType.Player,
                                hitRequest.Session.Character.CharacterId, 1, target.Character.CharacterId,
                                hitRequest.Skill.SkillVNum, (short)(hitRequest.Skill.Cooldown + hitRequest.Skill.Cooldown * cooldownReduction / 100D), hitRequest.Skill.AttackAnimation,
                                hitRequest.SkillEffect, hitRequest.Session.Character.PositionX,
                                hitRequest.Session.Character.PositionY, isAlive,
                                (int)(target.Character.Hp / target.Character.HPLoad() * 100), damage, hitmode,
                                (byte)(hitRequest.Skill.SkillType - 1)));
                            break;

                        default:
                            Logger.Warn("Not Implemented TargetHitType Handling!");
                            break;
                    }
                }
                else
                {
                    if (target != null)
                    {
                        hitRequest?.Session.SendPacket(StaticPacketHelper.Cancel(2, target.Character.CharacterId));
                    }
                }

                short Tit = Session.Character.EffTit;
                if (Tit == 9378 && target.Character.Faction == FactionType.Angel)
                {
                    int daño = damage / 20;
                    damage += daño;
                }
                else if (Tit == 9379 && target.Character.Faction == FactionType.Demon)
                {
                    int daño = damage / 20;
                    damage += daño;
                }
                else if (Tit == 9339)
                {
                    int daño = damage / 10;
                    damage += daño;
                }
                else if (Tit == 9337)
                {
                    int daño = damage / 20;
                    damage += daño;
                }
                else if (Tit == 9338)
                {
                    int daño = damage / 10;
                    damage += daño;
                }
                else if (Tit == 9336)
                {
                    int daño = (damage / 10) + (damage / 20);
                    damage += daño;
                }
                else if (Tit == 9400)
                {
                    int daño = damage / 10;
                    damage += daño;
                }
            }
            else
            {
                // monster already has been killed, send cancel
                if (target != null)
                {
                    hitRequest?.Session.SendPacket(StaticPacketHelper.Cancel(2, target.Character.CharacterId));
                }
            }
        }

        private void TargetHit(int castingId, UserType targetType, int targetId, bool isPvp = false)
        {
            // O gods of software development and operations, I have sinned. 

            bool shouldCancel = true;
            bool isSacrificeSkill = false;
            
            if ((DateTime.Now - Session.Character.LastTransform).TotalSeconds < 3)
            {
                Session.SendPacket(StaticPacketHelper.Cancel());
                Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("CANT_ATTACK"),
                    0));
                return;
            }

            List<CharacterSkill> skills = Session.Character.GetSkills();

            if (skills != null)
            {
                CharacterSkill ski = skills.FirstOrDefault(s => s.Skill?.CastId == castingId && (s.Skill?.UpgradeSkill == 0 || s.Skill?.SkillType == 1));

                if (castingId != 0)
                {
                    Session.SendPacket("ms_c 0");

                    foreach (string qslot in Session.Character.GenerateQuicklist())
                    {
                        Session.SendPacket(qslot);
                    }
                }

                if (ski != null)
                {
                    if (!Session.Character.WeaponLoaded(ski) || !ski.CanBeUsed())
                    {
                        Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                        return;
                    }

                    //TODO: Clean it up issou
                    if (ski.SkillVNum == 656)
                    {
                        Session.Character.RemoveUltimatePoints(2000);
                    }
                    else if (ski.SkillVNum == 657)
                    {
                        Session.Character.RemoveUltimatePoints(1000);
                    }
                    else if (ski.SkillVNum == 658 || ski.SkillVNum == 659)
                    {
                        Session.Character.RemoveUltimatePoints(3000);
                    }


                    if (Session.Character.LastSkillComboUse > DateTime.Now
                        && ski.SkillVNum != SkillHelper.GetOriginalSkill(ski.Skill)?.SkillVNum)
                    {
                        Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                        return;
                    }

                    BattleEntity targetEntity = null;

                    switch (targetType)
                    {
                        case UserType.Player:
                            {
                                targetEntity = ServerManager.Instance.GetSessionByCharacterId(targetId)?.Character?.BattleEntity;
                            }
                            break;

                        case UserType.Npc:
                            {
                                targetEntity = Session.Character.MapInstance?.Npcs?.ToList().FirstOrDefault(n => n.MapNpcId == targetId)?.BattleEntity
                                    ?? Session.Character.MapInstance?.Sessions?.Where(s => s?.Character?.Mates != null).SelectMany(s => s.Character.Mates).FirstOrDefault(m => m.MateTransportId == targetId)?.BattleEntity;
                            }
                            break;

                        case UserType.Monster:
                            {
                                targetEntity = Session.Character.MapInstance?.Monsters?.ToList().FirstOrDefault(m => m.Owner?.Character == null && m.MapMonsterId == targetId)?.BattleEntity;
                            }
                            break;
                    }

                    if (targetEntity == null)
                    {
                        Session.SendPacket(StaticPacketHelper.Cancel(2));
                        return;
                    }

                    foreach (BCard bc in ski.GetSkillBCards().ToList().Where(s => s.Type.Equals((byte)CardType.MeditationSkill)
                        && (!s.SubType.Equals((byte)AdditionalTypes.MeditationSkill.CausingChance / 10) || SkillHelper.IsCausingChance(ski.SkillVNum))))
                    {
                        shouldCancel = false;

                        if (bc.SubType.Equals((byte)AdditionalTypes.MeditationSkill.Sacrifice / 10))
                        {
                            isSacrificeSkill = true;
                            if (targetEntity == Session.Character.BattleEntity || targetEntity.MapMonster != null || targetEntity.MapNpc != null)
                            {
                                Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("INVALID_TARGET"), 0));
                                Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                                return;
                            }
                        }

                        bc.ApplyBCards(Session.Character.BattleEntity, Session.Character.BattleEntity);
                    }

                    if (ski.Skill.SkillVNum == 1098 && ski.GetSkillBCards().FirstOrDefault(s => s.Type.Equals((byte)CardType.SpecialisationBuffResistance) && s.SubType.Equals((byte)AdditionalTypes.SpecialisationBuffResistance.RemoveBadEffects / 10)) is BCard RemoveBadEffectsBcard)
                    {
                        if (Session.Character.BattleEntity.BCardDisposables[RemoveBadEffectsBcard.BCardId] != null)
                        {
                            Session.SendPacket(StaticPacketHelper.SkillResetWithCoolDown(castingId, 300));
                            ski.LastUse = DateTime.Now.AddSeconds(29);
                            Observable.Timer(TimeSpan.FromSeconds(30)).Subscribe(o =>
                            {
                                CharacterSkill
                                    skill = Session.Character.GetSkills().Find(s =>
                                        s.Skill?.CastId
                                        == castingId && (s.Skill?.UpgradeSkill == 0 || s.Skill?.SkillType == 1));
                                if (skill != null && skill.LastUse <= DateTime.Now)
                                {
                                    Session.SendPacket(StaticPacketHelper.SkillReset(castingId));
                                }
                            });
                            RemoveBadEffectsBcard.ApplyBCards(Session.Character.BattleEntity, Session.Character.BattleEntity);
                            Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                            return;
                        }
                    }

                    double cooldownReduction = Session.Character.GetBuff(CardType.Morale, (byte)AdditionalTypes.Morale.SkillCooldownDecreased)[0];

                    int[] increaseEnemyCooldownChance = Session.Character.GetBuff(CardType.DarkCloneSummon, (byte)AdditionalTypes.DarkCloneSummon.IncreaseEnemyCooldownChance);

                    if (ServerManager.RandomNumber() < increaseEnemyCooldownChance[0])
                    {
                        cooldownReduction -= increaseEnemyCooldownChance[1];
                    }
                    
                    short mpCost = ski.MpCost();
                    short hpCost = 0;

                    mpCost = (short)(mpCost * ((100 - Session.Character.CellonOptions.Where(s => s.Type == CellonOptionType.MPUsage).Sum(s => s.Value)) / 100D));

                    if (Session.Character.GetBuff(CardType.HealingBurningAndCasting, (byte)AdditionalTypes.HealingBurningAndCasting.HPDecreasedByConsumingMP)[0] is int HPDecreasedByConsumingMP)
                    {
                        if (HPDecreasedByConsumingMP < 0)
                        {
                            int amountDecreased = -(ski.MpCost() * HPDecreasedByConsumingMP / 100);
                            hpCost = (short)amountDecreased;
                            mpCost -= (short)amountDecreased;
                        }
                    }

                    if (Session.Character.Mp >= mpCost && Session.Character.Hp > hpCost && Session.HasCurrentMapInstance)
                    {
                        if (!Session.Character.HasGodMode)
                        {
                            Session.Character.DecreaseMp(ski.MpCost());
                        }

                        ski.LastUse = DateTime.Now;
                        Session.Character.PyjamaDead = ski.SkillVNum == 801;

                        // Area on attacker
                        if (ski.Skill.TargetType == 1 && ski.Skill.HitType == 1)
                        {
                            if (Session.Character.MapInstance.MapInstanceType == MapInstanceType.TalentArenaMapInstance && !Session.Character.MapInstance.IsPVP)
                            {
                                Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                                return;
                            }

                            if (Session.Character.UseSp && ski.Skill.CastEffect != -1)
                            {
                                Session.SendPackets(Session.Character.GenerateQuicklist());
                            }

                            Session.SendPacket(Session.Character.GenerateStat());
                            CharacterSkill skillinfo = Session.Character.Skills.FirstOrDefault(s =>
                                s.Skill.UpgradeSkill == ski.Skill.SkillVNum && s.Skill.Effect > 0
                                                                            && s.Skill.SkillType == 2);

                            Session.CurrentMapInstance.Broadcast(StaticPacketHelper.CastOnTarget(UserType.Player,
                                Session.Character.CharacterId, targetType, targetId,
                                ski.Skill.CastAnimation, skillinfo?.Skill.CastEffect ?? ski.Skill.CastEffect,
                                ski.Skill.SkillVNum));

                            short skillEffect = skillinfo?.Skill.Effect ?? ski.Skill.Effect;

                            if (Session.Character.BattleEntity.HasBuff(CardType.FireCannoneerRangeBuff, (byte)AdditionalTypes.FireCannoneerRangeBuff.AOEIncreased) && ski.Skill.Effect == 4569)
                            {
                                skillEffect = 4572;
                            }

                            byte targetRange = ski.TargetRange();

                            if (targetRange != 0)
                            {
                                ski.GetSkillBCards().Where(s => (s.Type.Equals((byte)CardType.Buff) && new Buff((short)s.SecondData, Session.Character.Level).Card?.BuffType == BuffType.Good)
                                    || (s.Type.Equals((byte)CardType.SpecialEffects2) && s.SubType.Equals((byte)AdditionalTypes.SpecialEffects2.TeleportInRadius / 10))).ToList()
                                    .ForEach(s => s.ApplyBCards(Session.Character.BattleEntity, Session.Character.BattleEntity));
                            }

                            Session.CurrentMapInstance.Broadcast(StaticPacketHelper.SkillUsed(UserType.Player,
                            Session.Character.CharacterId, 1, Session.Character.CharacterId, ski.Skill.SkillVNum,
                            (short)(ski.Skill.Cooldown + ski.Skill.Cooldown * cooldownReduction / 100D), ski.Skill.AttackAnimation,
                            skillEffect, Session.Character.PositionX,
                            Session.Character.PositionY, true,
                            (int)(Session.Character.Hp / Session.Character.HPLoad() * 100), 0, -2,
                            (byte)(ski.Skill.SkillType - 1)));

                            if (targetRange != 0)
                            {
                                foreach (ClientSession character in ServerManager.Instance.Sessions.Where(s =>
                                    s.CurrentMapInstance == Session.CurrentMapInstance
                                    && s.Character.CharacterId != Session.Character.CharacterId
                                    && s.Character.IsInRange(Session.Character.PositionX, Session.Character.PositionY,
                                        ski.TargetRange() + 5)))
                                {
                                    if (Session.Character.BattleEntity.CanAttackEntity(character.Character.BattleEntity))
                                    {
                                        PvpHit(new HitRequest(TargetHitType.AOETargetHit, Session, ski.Skill, skillBCards: ski.GetSkillBCards()),
                                            character);
                                    }
                                }

                                foreach (MapMonster mon in Session.CurrentMapInstance
                                    .GetMonsterInRangeList(Session.Character.PositionX, Session.Character.PositionY,
                                        ski.TargetRange()).Where(s => Session.Character.BattleEntity.CanAttackEntity(s.BattleEntity)))
                                {
                                    lock (mon._onHitLockObject)
                                    {
                                        mon.OnReceiveHit(new HitRequest(TargetHitType.AOETargetHit, Session, ski.Skill,
                                            skillinfo?.Skill.Effect ?? ski.Skill.Effect));
                                    }
                                }

                                foreach (Mate mate in Session.CurrentMapInstance
                                    .GetListMateInRange(Session.Character.PositionX, Session.Character.PositionY,
                                        ski.TargetRange()).Where(s => Session.Character.BattleEntity.CanAttackEntity(s.BattleEntity)))
                                {
                                    mate.HitRequest(new HitRequest(TargetHitType.AOETargetHit, Session, ski.Skill,
                                        skillinfo?.Skill.Effect ?? ski.Skill.Effect, skillBCards: ski.GetSkillBCards()));
                                }
                            }
                        }
                        else if (ski.Skill.TargetType == 2 && ski.Skill.HitType == 0 || isSacrificeSkill)
                        {
                            ConcurrentBag<ArenaTeamMember> team = null;
                            if (Session.Character.MapInstance.MapInstanceType == MapInstanceType.TalentArenaMapInstance)
                            {
                                team = ServerManager.Instance.ArenaTeams.ToList().FirstOrDefault(s => s.Any(o => o.Session == Session));
                            }

                            if (Session.Character.BattleEntity.CanAttackEntity(targetEntity)
                             || (team != null && team.FirstOrDefault(s => s.Session == Session)?.ArenaTeamType != team.FirstOrDefault(s => s.Session == targetEntity.Character.Session)?.ArenaTeamType))
                            {
                                targetEntity = Session.Character.BattleEntity;
                            }
                            if (Session.Character.MapInstance == ServerManager.Instance.ArenaInstance && targetEntity.Mate?.Owner != Session.Character && targetEntity != Session.Character.BattleEntity && (Session.Character.Group == null || !Session.Character.Group.IsMemberOfGroup(targetEntity.MapEntityId)))
                            {
                                targetEntity = Session.Character.BattleEntity;
                            }
                            if (Session.Character.MapInstance == ServerManager.Instance.FamilyArenaInstance && targetEntity.Mate?.Owner != Session.Character && targetEntity != Session.Character.BattleEntity && Session.Character.Family != (targetEntity.Character?.Family ?? targetEntity.Mate?.Owner.Family ?? targetEntity.MapMonster?.Owner?.Character?.Family))
                            {
                                targetEntity = Session.Character.BattleEntity;
                            }

                            if (targetEntity.Character != null && targetEntity.Character.IsSitting)
                            {
                                targetEntity.Character.IsSitting = false;
                                Session.CurrentMapInstance?.Broadcast(targetEntity.Character.GenerateRest());
                            }

                            if (targetEntity.Mate != null && targetEntity.Mate.IsSitting)
                            {
                                Session.CurrentMapInstance?.Broadcast(targetEntity.Mate.GenerateRest(false));
                            }

                            ski.GetSkillBCards().ToList().Where(s => !s.Type.Equals((byte)CardType.MeditationSkill)).ToList()
                                .ForEach(s => s.ApplyBCards(targetEntity, Session.Character.BattleEntity));

                            targetEntity.MapInstance.Broadcast(StaticPacketHelper.CastOnTarget(UserType.Player,
                                Session.Character.CharacterId, targetEntity.UserType, targetEntity.MapEntityId,
                                ski.Skill.CastAnimation, ski.Skill.CastEffect, ski.Skill.SkillVNum));
                            targetEntity.MapInstance.Broadcast(StaticPacketHelper.SkillUsed(UserType.Player,
                                Session.Character.CharacterId, (byte)targetEntity.UserType, targetEntity.MapEntityId, ski.Skill.SkillVNum, (short)(ski.Skill.Cooldown + ski.Skill.Cooldown * cooldownReduction / 100D),
                                ski.Skill.AttackAnimation, ski.Skill.Effect, targetEntity.PositionX,
                                targetEntity.PositionY, true,
                                (int)(targetEntity.Hp / targetEntity.HPLoad() * 100), 0, -1,
                                (byte)(ski.Skill.SkillType - 1)));
                        }
                        else if (ski.Skill.TargetType == 1 && ski.Skill.HitType != 1)
                        {
                            Session.CurrentMapInstance.Broadcast(StaticPacketHelper.CastOnTarget(UserType.Player,
                                Session.Character.CharacterId, UserType.Player, Session.Character.CharacterId,
                                ski.Skill.CastAnimation, ski.Skill.CastEffect, ski.Skill.SkillVNum));

                            if (ski.Skill.CastEffect != 0)
                            {
                                Thread.Sleep(ski.Skill.CastTime * 100);
                            }

                            Session.CurrentMapInstance.Broadcast(StaticPacketHelper.SkillUsed(UserType.Player,
                                Session.Character.CharacterId, 1, Session.Character.CharacterId, ski.Skill.SkillVNum,
                                (short)(ski.Skill.Cooldown + ski.Skill.Cooldown * cooldownReduction / 100D), ski.Skill.AttackAnimation, ski.Skill.Effect,
                                Session.Character.PositionX, Session.Character.PositionY, true,
                                (int)(Session.Character.Hp / Session.Character.HPLoad() * 100), 0, -1,
                                (byte)(ski.Skill.SkillType - 1)));

                            if (ski.SkillVNum != 1330)
                            {
                                switch (ski.Skill.HitType)
                                {
                                    case 0:
                                    case 4:
                                        if (Session.Character.Buff.FirstOrDefault(s => s.Card.BCards.Any(b => b.Type == (byte)BCardType.CardType.FalconSkill && b.SubType.Equals((byte)AdditionalTypes.FalconSkill.Hide / 10))) is Buff FalconHideBuff)
                                        {
                                            Session.Character.RemoveBuff(FalconHideBuff.Card.CardId);
                                            Session.Character.AddBuff(new Buff(560, Session.Character.Level), Session.Character.BattleEntity);
                                        }
                                        break;

                                    case 2:
                                        ConcurrentBag<ArenaTeamMember> team = null;
                                        if (Session.Character.MapInstance.MapInstanceType == MapInstanceType.TalentArenaMapInstance)
                                        {
                                            team = ServerManager.Instance.ArenaTeams.ToList().FirstOrDefault(s => s.Any(o => o.Session == Session));
                                        }

                                        IEnumerable<ClientSession> clientSessions =
                                            Session.CurrentMapInstance.Sessions?.Where(s => s.Character.CharacterId != Session.Character.CharacterId &&
                                                s.Character.IsInRange(Session.Character.PositionX,
                                                    Session.Character.PositionY, ski.TargetRange()));
                                        if (clientSessions != null)
                                        {
                                            foreach (ClientSession target in clientSessions)
                                            {
                                                if (!Session.Character.BattleEntity.CanAttackEntity(target.Character.BattleEntity)
                                                  && (team == null || team.FirstOrDefault(s => s.Session == Session)?.ArenaTeamType == team.FirstOrDefault(s => s.Session == target.Character.Session)?.ArenaTeamType))
                                                {
                                                    if (Session.Character.MapInstance == ServerManager.Instance.ArenaInstance && (Session.Character.Group == null || !Session.Character.Group.IsMemberOfGroup(target.Character.CharacterId)))
                                                    {
                                                        continue;
                                                    }
                                                    if (Session.Character.MapInstance == ServerManager.Instance.FamilyArenaInstance && Session.Character.Family != target.Character.Family)
                                                    {
                                                        continue;
                                                    }

                                                    ski.GetSkillBCards().ToList().Where(s => !s.Type.Equals((byte)CardType.MeditationSkill))
                                                    .ToList().ForEach(s =>
                                                        s.ApplyBCards(target.Character.BattleEntity, Session.Character.BattleEntity));

                                                    Session.CurrentMapInstance.Broadcast(StaticPacketHelper.SkillUsed(UserType.Player,
                                                        Session.Character.CharacterId, 1, target.Character.CharacterId, ski.Skill.SkillVNum,
                                                        (short)(ski.Skill.Cooldown + ski.Skill.Cooldown * cooldownReduction / 100D), ski.Skill.AttackAnimation, ski.Skill.Effect,
                                                        target.Character.PositionX, target.Character.PositionY, true,
                                                        (int)(target.Character.Hp / target.Character.HPLoad() * 100), 0, -1,
                                                        (byte)(ski.Skill.SkillType - 1)));
                                                }
                                            }
                                        }

                                        IEnumerable<Mate> mates = Session.CurrentMapInstance.GetListMateInRange(Session.Character.PositionX, Session.Character.PositionY, ski.TargetRange());
                                        if (mates != null)
                                        {
                                            foreach (Mate target in mates)
                                            {
                                                if (!Session.Character.BattleEntity.CanAttackEntity(target.BattleEntity))
                                                {
                                                    if (Session.Character.MapInstance == ServerManager.Instance.ArenaInstance && (Session.Character.Group == null || !Session.Character.Group.IsMemberOfGroup(target.Owner.CharacterId)))
                                                    {
                                                        continue;
                                                    }
                                                    if (Session.Character.MapInstance == ServerManager.Instance.FamilyArenaInstance && Session.Character.Family != target.Owner.Family)
                                                    {
                                                        continue;
                                                    }

                                                    ski.GetSkillBCards().ToList().Where(s => !s.Type.Equals((byte)CardType.MeditationSkill))
                                                    .ToList().ForEach(s =>
                                                        s.ApplyBCards(target.BattleEntity, Session.Character.BattleEntity));

                                                    Session.CurrentMapInstance.Broadcast(StaticPacketHelper.SkillUsed(UserType.Player,
                                                        Session.Character.CharacterId, (byte)target.BattleEntity.UserType, target.MateTransportId, ski.Skill.SkillVNum,
                                                        (short)(ski.Skill.Cooldown + ski.Skill.Cooldown * cooldownReduction / 100D), ski.Skill.AttackAnimation, ski.Skill.Effect,
                                                        target.PositionX, target.PositionY, true,
                                                        (int)(target.Hp / target.HpLoad() * 100), 0, -1,
                                                        (byte)(ski.Skill.SkillType - 1)));
                                                }
                                            }
                                        }

                                        IEnumerable<MapMonster> monsters = Session.CurrentMapInstance.GetMonsterInRangeList(Session.Character.PositionX, Session.Character.PositionY, ski.TargetRange());
                                        if (monsters != null)
                                        {
                                            foreach (MapMonster target in monsters)
                                            {
                                                if (!Session.Character.BattleEntity.CanAttackEntity(target.BattleEntity))
                                                {
                                                    if (target.Owner != null)
                                                    {
                                                        if (target.Owner.Character != null)
                                                        {
                                                            continue;
                                                        }
                                                        if (Session.Character.MapInstance == ServerManager.Instance.ArenaInstance && (Session.Character.Group == null || !Session.Character.Group.IsMemberOfGroup(target.Owner.MapEntityId)))
                                                        {
                                                            continue;
                                                        }
                                                        if (Session.Character.MapInstance == ServerManager.Instance.FamilyArenaInstance && Session.Character.Family != target.Owner.Character?.Family)
                                                        {
                                                            continue;
                                                        }
                                                    }

                                                    ski.GetSkillBCards().ToList().Where(s => !s.Type.Equals((byte)CardType.MeditationSkill))
                                                    .ToList().ForEach(s =>
                                                        s.ApplyBCards(target.BattleEntity, Session.Character.BattleEntity));

                                                    Session.CurrentMapInstance.Broadcast(StaticPacketHelper.SkillUsed(UserType.Player,
                                                        Session.Character.CharacterId, (byte)target.BattleEntity.UserType, target.MapMonsterId, ski.Skill.SkillVNum,
                                                        (short)(ski.Skill.Cooldown + ski.Skill.Cooldown * cooldownReduction / 100D), ski.Skill.AttackAnimation, ski.Skill.Effect,
                                                        target.MapX, target.MapY, true,
                                                        (int)(target.CurrentHp / target.MaxHp * 100), 0, -1,
                                                        (byte)(ski.Skill.SkillType - 1)));
                                                }
                                            }
                                        }

                                        IEnumerable<MapNpc> npcs = Session.CurrentMapInstance.GetListNpcInRange(Session.Character.PositionX, Session.Character.PositionY, ski.TargetRange());
                                        if (npcs != null)
                                        {
                                            foreach (MapNpc target in npcs)
                                            {
                                                if (!Session.Character.BattleEntity.CanAttackEntity(target.BattleEntity))
                                                {
                                                    if (target.Owner != null)
                                                    {
                                                        if (Session.Character.MapInstance == ServerManager.Instance.ArenaInstance && (Session.Character.Group == null || !Session.Character.Group.IsMemberOfGroup(target.Owner.MapEntityId)))
                                                        {
                                                            continue;
                                                        }
                                                        if (Session.Character.MapInstance == ServerManager.Instance.FamilyArenaInstance && Session.Character.Family != target.Owner.Character?.Family)
                                                        {
                                                            continue;
                                                        }
                                                    }

                                                    ski.GetSkillBCards().ToList().Where(s => !s.Type.Equals((byte)CardType.MeditationSkill))
                                                    .ToList().ForEach(s =>
                                                        s.ApplyBCards(target.BattleEntity, Session.Character.BattleEntity));

                                                    Session.CurrentMapInstance.Broadcast(StaticPacketHelper.SkillUsed(UserType.Player,
                                                        Session.Character.CharacterId, (byte)target.BattleEntity.UserType, target.MapNpcId, ski.Skill.SkillVNum,
                                                        (short)(ski.Skill.Cooldown + ski.Skill.Cooldown * cooldownReduction / 100D), ski.Skill.AttackAnimation, ski.Skill.Effect,
                                                        target.MapX, target.MapY, true,
                                                        (int)(target.CurrentHp / target.MaxHp * 100), 0, -1,
                                                        (byte)(ski.Skill.SkillType - 1)));
                                                }
                                            }
                                        }

                                        break;


                                }
                            }

                            ski.GetSkillBCards().ToList().Where(s => !s.Type.Equals((byte)CardType.MeditationSkill)).ToList().ForEach(s => s.ApplyBCards(Session.Character.BattleEntity, Session.Character.BattleEntity));
                        }
                        else if (ski.Skill.TargetType == 0)
                        {
                            if (Session.Character.MapInstance.MapInstanceType == MapInstanceType.TalentArenaMapInstance && !Session.Character.MapInstance.IsPVP)
                            {
                                Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                                return;
                            }

                            if (isPvp)
                            {
                                //ClientSession playerToAttack = ServerManager.Instance.GetSessionByCharacterId(targetId);
                                ClientSession playerToAttack = targetEntity.Character?.Session;

                                if (playerToAttack != null && !IceBreaker.FrozenPlayers.Contains(playerToAttack))
                                {
                                    if (Map.GetDistance(
                                            new MapCell
                                            {
                                                X = Session.Character.PositionX,
                                                Y = Session.Character.PositionY
                                            },
                                            new MapCell
                                            {
                                                X = playerToAttack.Character.PositionX,
                                                Y = playerToAttack.Character.PositionY
                                            }) <= ski.Skill.Range + 5)
                                    {
                                        if (Session.Character.UseSp && ski.Skill.CastEffect != -1)
                                        {
                                            Session.SendPackets(Session.Character.GenerateQuicklist());
                                        }

                                        if (ski.SkillVNum == 1061)
                                        {
                                            Session.CurrentMapInstance.Broadcast($"eff 1 {targetId} 4968");
                                            Session.CurrentMapInstance.Broadcast($"eff 1 {Session.Character.CharacterId} 4968");
                                        }

                                        Session.SendPacket(Session.Character.GenerateStat());
                                        CharacterSkill characterSkillInfo = Session.Character.Skills.FirstOrDefault(s =>
                                            s.Skill.UpgradeSkill == ski.Skill.SkillVNum && s.Skill.Effect > 0
                                                                                        && s.Skill.SkillType == 2);
                                        Session.CurrentMapInstance.Broadcast(
                                            StaticPacketHelper.CastOnTarget(UserType.Player,
                                                Session.Character.CharacterId, UserType.Player, targetId, ski.Skill.CastAnimation,
                                                characterSkillInfo?.Skill.CastEffect ?? ski.Skill.CastEffect,
                                                ski.Skill.SkillVNum));
                                        Session.Character.Skills.Where(s => s.Id != ski.Id).ForEach(i => i.Hit = 0);

                                        // Generate scp
                                        if ((DateTime.Now - ski.LastUse).TotalSeconds > 3)
                                        {
                                            ski.Hit = 0;
                                        }
                                        else
                                        {
                                            ski.Hit++;
                                        }

                                        ski.LastUse = DateTime.Now;

                                        if (ski.Skill.CastEffect != 0)
                                        {
                                            Thread.Sleep(ski.Skill.CastTime * 100);
                                        }

                                        if (ski.Skill.HitType == 3)
                                        {
                                            int count = 0;
                                            if (playerToAttack.CurrentMapInstance == Session.CurrentMapInstance
                                                && playerToAttack.Character.CharacterId !=
                                                Session.Character.CharacterId)
                                            {
                                                if (Session.Character.BattleEntity.CanAttackEntity(playerToAttack.Character.BattleEntity))
                                                {
                                                    count++;
                                                    PvpHit(
                                                        new HitRequest(TargetHitType.SingleAOETargetHit, Session,
                                                            ski.Skill, skillBCards: ski.GetSkillBCards(), showTargetAnimation: true), playerToAttack);
                                                }
                                                else
                                                {
                                                    Session.SendPacket(
                                                        StaticPacketHelper.Cancel(2, targetId));
                                                }
                                            }

                                            //foreach (long id in Session.Character.MTListTargetQueue.Where(s => s.EntityType == UserType.Player).Select(s => s.TargetId))
                                            foreach (long id in Session.Character.GetMTListTargetQueue_QuickFix(ski, UserType.Player))
                                            {
                                                ClientSession character = ServerManager.Instance.GetSessionByCharacterId(id);

                                                if (character != null
                                                    && character.CurrentMapInstance == Session.CurrentMapInstance
                                                    && character.Character.CharacterId != Session.Character.CharacterId
                                                    && character != playerToAttack)
                                                {
                                                    if (Session.Character.BattleEntity.CanAttackEntity(character.Character.BattleEntity))
                                                    {
                                                        count++;
                                                        PvpHit(new HitRequest(TargetHitType.SingleAOETargetHit, Session, ski.Skill, showTargetAnimation: count == 1, skillBCards: ski.GetSkillBCards()), character);
                                                    }
                                                }
                                            }

                                            if (count == 0)
                                            {
                                                Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                                            }
                                        }
                                        else
                                        {
                                            // check if we will hit mutltiple targets
                                            if (ski.TargetRange() != 0)
                                            {
                                                ComboDTO skillCombo = ski.Skill.Combos.Find(s => ski.Hit == s.Hit);
                                                if (skillCombo != null)
                                                {
                                                    if (ski.Skill.Combos.OrderByDescending(s => s.Hit).First().Hit
                                                        == ski.Hit)
                                                    {
                                                        ski.Hit = 0;
                                                    }

                                                    IEnumerable<ClientSession> playersInAoeRange =
                                                        ServerManager.Instance.Sessions.Where(s =>
                                                            s.CurrentMapInstance == Session.CurrentMapInstance
                                                            && s.Character.CharacterId != Session.Character.CharacterId
                                                            && s != playerToAttack
                                                            && s.Character.IsInRange(playerToAttack.Character.PositionX,
                                                                playerToAttack.Character.PositionY, ski.TargetRange()));
                                                    int count = 0;
                                                    if (Session.Character.BattleEntity.CanAttackEntity(playerToAttack.Character.BattleEntity))
                                                    {
                                                        count++;
                                                        PvpHit(
                                                            new HitRequest(TargetHitType.SingleTargetHitCombo,
                                                                Session, ski.Skill, skillCombo: skillCombo, skillBCards: ski.GetSkillBCards()),
                                                            playerToAttack);
                                                    }
                                                    else
                                                    {
                                                        Session.SendPacket(
                                                            StaticPacketHelper.Cancel(2, targetId));
                                                    }

                                                    foreach (ClientSession character in playersInAoeRange)
                                                    {
                                                        if (Session.Character.BattleEntity.CanAttackEntity(character.Character.BattleEntity))
                                                        {
                                                            count++;
                                                            PvpHit(
                                                                new HitRequest(TargetHitType.SingleTargetHitCombo,
                                                                    Session, ski.Skill, skillCombo: skillCombo, showTargetAnimation: count == 1, skillBCards: ski.GetSkillBCards()),
                                                                character);
                                                        }
                                                    }

                                                    if (playerToAttack.Character.Hp <= 0 || count == 0)
                                                    {
                                                        Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                                                    }
                                                }
                                                else
                                                {
                                                    IEnumerable<ClientSession> playersInAoeRange =
                                                        ServerManager.Instance.Sessions.Where(s =>
                                                            s.CurrentMapInstance == Session.CurrentMapInstance
                                                            && s.Character.CharacterId != Session.Character.CharacterId
                                                            && s != playerToAttack
                                                            && s.Character.IsInRange(playerToAttack.Character.PositionX,
                                                                playerToAttack.Character.PositionY, ski.TargetRange()));

                                                    int count = 0;
                                                    // hit the targetted player
                                                    if (Session.Character.BattleEntity.CanAttackEntity(playerToAttack.Character.BattleEntity))
                                                    {
                                                        count++;
                                                        PvpHit(
                                                            new HitRequest(TargetHitType.SingleAOETargetHit,
                                                                Session, ski.Skill, showTargetAnimation: true, skillBCards: ski.GetSkillBCards()), playerToAttack);
                                                    }
                                                    else
                                                    {
                                                        Session.SendPacket(
                                                            StaticPacketHelper.Cancel(2, targetId));
                                                    }

                                                    //hit all other players
                                                    foreach (ClientSession character in playersInAoeRange)
                                                    {
                                                        count++;
                                                        if (Session.Character.BattleEntity.CanAttackEntity(character.Character.BattleEntity))
                                                        {
                                                            PvpHit(
                                                                new HitRequest(TargetHitType.SingleAOETargetHit,
                                                                    Session, ski.Skill, showTargetAnimation: count == 1, skillBCards: ski.GetSkillBCards()), character);
                                                        }
                                                    }

                                                    if (playerToAttack.Character.Hp <= 0)
                                                    {
                                                        Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                ComboDTO skillCombo = ski.Skill.Combos.Find(s => ski.Hit == s.Hit);
                                                if (skillCombo != null)
                                                {
                                                    if (ski.Skill.Combos.OrderByDescending(s => s.Hit).First().Hit
                                                        == ski.Hit)
                                                    {
                                                        ski.Hit = 0;
                                                    }

                                                    if (Session.Character.BattleEntity.CanAttackEntity(playerToAttack.Character.BattleEntity))
                                                    {
                                                        PvpHit(
                                                            new HitRequest(TargetHitType.SingleTargetHitCombo,
                                                                Session, ski.Skill, skillCombo: skillCombo, skillBCards: ski.GetSkillBCards()),
                                                            playerToAttack);
                                                    }
                                                    else
                                                    {
                                                        Session.SendPacket(
                                                            StaticPacketHelper.Cancel(2, targetId));
                                                    }
                                                }
                                                else
                                                {
                                                    if (Session.Character.BattleEntity.CanAttackEntity(playerToAttack.Character.BattleEntity))
                                                    {
                                                        PvpHit(
                                                            new HitRequest(TargetHitType.SingleTargetHit,
                                                                Session, ski.Skill, showTargetAnimation: true, skillBCards: ski.GetSkillBCards()), playerToAttack);
                                                    }
                                                    else
                                                    {
                                                        Session.SendPacket(
                                                            StaticPacketHelper.Cancel(2, targetId));
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                                    }
                                }
                                else if (IceBreaker.FrozenPlayers.Contains(playerToAttack))
                                {
                                    Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                                    if (playerToAttack.Character.LastPvPKiller == null
                                        || playerToAttack.Character.LastPvPKiller != Session)
                                    {
                                        Session.SendPacket($"delay 2000 5 #guri^502^1^{playerToAttack.Character.CharacterId}");
                                    }
                                }
                                else
                                {
                                    Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                                }
                            }
                            else
                            {
                                MapMonster monsterToAttack = targetEntity.MapMonster;

                                if (monsterToAttack != null)
                                {
                                    if (Map.GetDistance(new MapCell { X = Session.Character.PositionX, Y = Session.Character.PositionY },
                                        new MapCell { X = monsterToAttack.MapX, Y = monsterToAttack.MapY }) <= ski.Skill.Range + 5 + monsterToAttack.Monster.BasicArea)
                                    {
                                        if (Session.Character.UseSp && ski.Skill.CastEffect != -1)
                                        {
                                            Session.SendPackets(Session.Character.GenerateQuicklist());
                                        }

                                        #region Taunt

                                        if (ski.SkillVNum == 1061)
                                        {
                                            Session.CurrentMapInstance.Broadcast($"eff 3 {targetId} 4968");
                                            Session.CurrentMapInstance.Broadcast($"eff 1 {Session.Character.CharacterId} 4968");
                                        }

                                        #endregion

                                        ski.GetSkillBCards().ToList().Where(s => s.CastType == 1).ToList()
                                            .ForEach(s => s.ApplyBCards(monsterToAttack.BattleEntity, Session.Character.BattleEntity));

                                        Session.SendPacket(Session.Character.GenerateStat());

                                        CharacterSkill ski2 = Session.Character.Skills.FirstOrDefault(s => s.Skill.UpgradeSkill == ski.Skill.SkillVNum
                                            && s.Skill.Effect > 0 && s.Skill.SkillType == 2);

                                        Session.CurrentMapInstance.Broadcast(StaticPacketHelper.CastOnTarget(UserType.Player, Session.Character.CharacterId, UserType.Monster, monsterToAttack.MapMonsterId,
                                            ski.Skill.CastAnimation, ski2?.Skill.CastEffect ?? ski.Skill.CastEffect, ski.Skill.SkillVNum));

                                        Session.Character.Skills.Where(x => x.Id != ski.Id).ForEach(x => x.Hit = 0);

                                        #region Generate scp

                                        if ((DateTime.Now - ski.LastUse).TotalSeconds > 3)
                                        {
                                            ski.Hit = 0;
                                        }
                                        else
                                        {
                                            ski.Hit++;
                                        }

                                        #endregion

                                        ski.LastUse = DateTime.Now;

                                        if (ski.Skill.CastEffect != 0)
                                        {
                                            Thread.Sleep(ski.Skill.CastTime * 100);
                                        }

                                        if (ski.Skill.HitType == 3)
                                        {
                                            monsterToAttack.HitQueue.Enqueue(new HitRequest(TargetHitType.SingleAOETargetHit, Session,
                                                ski.Skill, ski2?.Skill.Effect ?? ski.Skill.Effect, showTargetAnimation: true, skillBCards: ski.GetSkillBCards()));

                                            //foreach (long id in Session.Character.MTListTargetQueue.Where(s => s.EntityType == UserType.Monster).Select(s => s.TargetId))
                                            foreach (long id in Session.Character.GetMTListTargetQueue_QuickFix(ski, UserType.Monster))
                                            {
                                                MapMonster mon = Session.CurrentMapInstance.GetMonsterById(id);

                                                if (mon?.CurrentHp > 0)
                                                {
                                                    mon.HitQueue.Enqueue(new HitRequest(TargetHitType.SingleAOETargetHit, Session,
                                                        ski.Skill, ski2?.Skill.Effect ?? ski.Skill.Effect, skillBCards: ski.GetSkillBCards()));
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (ski.TargetRange() != 0 || ski.Skill.HitType == 1)
                                            {
                                                ComboDTO skillCombo = ski.Skill.Combos.Find(s => ski.Hit == s.Hit);

                                                List<MapMonster> monstersInAoeRange = Session.CurrentMapInstance?.GetMonsterInRangeList(monsterToAttack.MapX, monsterToAttack.MapY, ski.TargetRange())?
                                                        .Where(m => Session.Character.BattleEntity.CanAttackEntity(m.BattleEntity)).ToList();

                                                if (skillCombo != null)
                                                {
                                                    if (ski.Skill.Combos.OrderByDescending(s => s.Hit).First().Hit == ski.Hit)
                                                    {
                                                        ski.Hit = 0;
                                                    }

                                                    if (monsterToAttack.IsAlive && monstersInAoeRange?.Count != 0)
                                                    {
                                                        foreach (MapMonster mon in monstersInAoeRange)
                                                        {
                                                            mon.HitQueue.Enqueue(new HitRequest(TargetHitType.SingleTargetHitCombo, Session,
                                                                ski.Skill, skillCombo: skillCombo, skillBCards: ski.GetSkillBCards()));
                                                        }
                                                    }
                                                    else
                                                    {
                                                        Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                                                    }
                                                }
                                                else
                                                {
                                                    monsterToAttack.HitQueue.Enqueue(new HitRequest(TargetHitType.SingleAOETargetHit, Session,
                                                            ski.Skill, ski2?.Skill.Effect ?? ski.Skill.Effect, showTargetAnimation: true, skillBCards: ski.GetSkillBCards()));

                                                    if (monsterToAttack.IsAlive && monstersInAoeRange?.Count != 0)
                                                    {
                                                        foreach (MapMonster mon in monstersInAoeRange.Where(m => m.MapMonsterId != monsterToAttack.MapMonsterId))
                                                        {
                                                            mon.HitQueue.Enqueue(new HitRequest(TargetHitType.SingleAOETargetHit, Session, ski.Skill, ski2?.Skill.Effect ?? ski.Skill.Effect, skillBCards: ski.GetSkillBCards()));
                                                        }
                                                    }
                                                    else
                                                    {
                                                        Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                ComboDTO skillCombo = ski.Skill.Combos.Find(s => ski.Hit == s.Hit);

                                                if (skillCombo != null)
                                                {
                                                    if (ski.Skill.Combos.OrderByDescending(s => s.Hit).First().Hit == ski.Hit)
                                                    {
                                                        ski.Hit = 0;
                                                    }

                                                    monsterToAttack.HitQueue.Enqueue(new HitRequest(TargetHitType.SingleTargetHitCombo, Session,
                                                        ski.Skill, skillCombo: skillCombo, skillBCards: ski.GetSkillBCards()));
                                                }
                                                else
                                                {
                                                    monsterToAttack.HitQueue.Enqueue(new HitRequest(TargetHitType.SingleTargetHit, Session,
                                                        ski.Skill, skillBCards: ski.GetSkillBCards()));
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                                    }
                                }
                                else if (targetEntity.Mate is Mate mateToAttack)
                                {
                                    if (!Session.Character.BattleEntity.CanAttackEntity(mateToAttack.BattleEntity))
                                    {
                                        Session.Character.Session.SendPacket(StaticPacketHelper.Cancel(2, mateToAttack.BattleEntity.MapEntityId));
                                        return;
                                    }

                                    if (Map.GetDistance(
                                            new MapCell
                                            {
                                                X = Session.Character.PositionX,
                                                Y = Session.Character.PositionY
                                            },
                                            new MapCell { X = mateToAttack.PositionX, Y = mateToAttack.PositionY })
                                        <= ski.Skill.Range + 5 + mateToAttack.Monster.BasicArea)
                                    {
                                        if (Session.Character.UseSp && ski.Skill.CastEffect != -1)
                                        {
                                            Session.SendPackets(Session.Character.GenerateQuicklist());
                                        }

                                        if (ski.SkillVNum == 1061)
                                        {
                                            Session.CurrentMapInstance.Broadcast($"eff 2 {targetId} 4968");
                                            Session.CurrentMapInstance.Broadcast($"eff 1 {Session.Character.CharacterId} 4968");
                                        }

                                        ski.GetSkillBCards().ToList().Where(s => s.CastType == 1).ToList().ForEach(s => s.ApplyBCards(mateToAttack.BattleEntity, Session.Character.BattleEntity));

                                        Session.SendPacket(Session.Character.GenerateStat());
                                        CharacterSkill characterSkillInfo = Session.Character.Skills.FirstOrDefault(s =>
                                            s.Skill.UpgradeSkill == ski.Skill.SkillVNum && s.Skill.Effect > 0
                                                                                        && s.Skill.SkillType == 2);

                                        Session.CurrentMapInstance.Broadcast(StaticPacketHelper.CastOnTarget(
                                            UserType.Player, Session.Character.CharacterId, UserType.Npc,
                                            mateToAttack.MateTransportId, ski.Skill.CastAnimation,
                                            characterSkillInfo?.Skill.CastEffect ?? ski.Skill.CastEffect,
                                            ski.Skill.SkillVNum));
                                        Session.Character.Skills.Where(s => s.Id != ski.Id).ForEach(i => i.Hit = 0);

                                        // Generate scp
                                        if ((DateTime.Now - ski.LastUse).TotalSeconds > 3)
                                        {
                                            ski.Hit = 0;
                                        }
                                        else
                                        {
                                            ski.Hit++;
                                        }

                                        ski.LastUse = DateTime.Now;
                                        if (ski.Skill.CastEffect != 0)
                                        {
                                            Thread.Sleep(ski.Skill.CastTime * 100);
                                        }

                                        if (ski.Skill.HitType == 3)
                                        {
                                            mateToAttack.HitRequest(new HitRequest(
                                                TargetHitType.SingleAOETargetHit, Session, ski.Skill,
                                                characterSkillInfo?.Skill.Effect ?? ski.Skill.Effect,
                                                showTargetAnimation: true, skillBCards: ski.GetSkillBCards()));

                                            //foreach (long id in Session.Character.MTListTargetQueue.Where(s => s.EntityType == UserType.Monster).Select(s => s.TargetId))
                                            foreach (long id in Session.Character.GetMTListTargetQueue_QuickFix(ski, UserType.Monster))
                                            {
                                                Mate mate = Session.CurrentMapInstance.GetMate(id);
                                                if (mate != null && mate.Hp > 0 && Session.Character.BattleEntity.CanAttackEntity(mate.BattleEntity))
                                                {
                                                    mate.HitRequest(new HitRequest(
                                                        TargetHitType.SingleAOETargetHit, Session, ski.Skill,
                                                        characterSkillInfo?.Skill.Effect ?? ski.Skill.Effect, skillBCards: ski.GetSkillBCards()));
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (ski.TargetRange() != 0 || ski.Skill.HitType == 1) // check if we will hit mutltiple targets
                                            {
                                                ComboDTO skillCombo = ski.Skill.Combos.Find(s => ski.Hit == s.Hit);
                                                if (skillCombo != null)
                                                {
                                                    if (ski.Skill.Combos.OrderByDescending(s => s.Hit).First().Hit
                                                        == ski.Hit)
                                                    {
                                                        ski.Hit = 0;
                                                    }

                                                    List<Mate> monstersInAoeRange = Session.CurrentMapInstance?
                                                        .GetListMateInRange(mateToAttack.MapX,
                                                            mateToAttack.MapY, ski.TargetRange()).Where(m => Session.Character.BattleEntity.CanAttackEntity(m.BattleEntity)).ToList();
                                                    if (monstersInAoeRange.Count != 0)
                                                    {
                                                        foreach (Mate mate in monstersInAoeRange)
                                                        {
                                                            mate.HitRequest(
                                                                new HitRequest(TargetHitType.SingleTargetHitCombo,
                                                                    Session, ski.Skill, skillCombo: skillCombo, skillBCards: ski.GetSkillBCards()));
                                                        }
                                                    }
                                                    else
                                                    {
                                                        Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                                                    }

                                                    if (!mateToAttack.IsAlive)
                                                    {
                                                        Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                                                    }
                                                }
                                                else
                                                {
                                                    List<Mate> matesInAoeRange = Session.CurrentMapInstance?
                                                                                              .GetListMateInRange(
                                                                                                  mateToAttack.MapX,
                                                                                                  mateToAttack.MapY,
                                                                                                  ski.TargetRange())
                                                                                              ?.Where(m => Session.Character.BattleEntity.CanAttackEntity(m.BattleEntity)).ToList();

                                                    //hit the targetted mate
                                                    mateToAttack.HitRequest(
                                                        new HitRequest(TargetHitType.SingleAOETargetHit, Session,
                                                            ski.Skill,
                                                            characterSkillInfo?.Skill.Effect ?? ski.Skill.Effect,
                                                            showTargetAnimation: true, skillBCards: ski.GetSkillBCards()));

                                                    //hit all other mates
                                                    if (matesInAoeRange != null && matesInAoeRange.Count != 0)
                                                    {
                                                        foreach (Mate mate in matesInAoeRange.Where(m =>
                                                            m.MateTransportId != mateToAttack.MateTransportId)
                                                        ) //exclude targetted mates
                                                        {
                                                            mate.HitRequest(
                                                                new HitRequest(TargetHitType.SingleAOETargetHit,
                                                                    Session, ski.Skill,
                                                                    characterSkillInfo?.Skill.Effect ??
                                                                    ski.Skill.Effect, skillBCards: ski.GetSkillBCards()));
                                                        }
                                                    }
                                                    else
                                                    {
                                                        Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                                                    }

                                                    if (!mateToAttack.IsAlive)
                                                    {
                                                        Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                ComboDTO skillCombo = ski.Skill.Combos.Find(s => ski.Hit == s.Hit);
                                                if (skillCombo != null)
                                                {
                                                    if (ski.Skill.Combos.OrderByDescending(s => s.Hit).First().Hit
                                                        == ski.Hit)
                                                    {
                                                        ski.Hit = 0;
                                                    }

                                                    mateToAttack.HitRequest(
                                                        new HitRequest(TargetHitType.SingleTargetHitCombo, Session,
                                                            ski.Skill, skillCombo: skillCombo, skillBCards: ski.GetSkillBCards()));
                                                }
                                                else
                                                {
                                                    mateToAttack.HitRequest(
                                                        new HitRequest(TargetHitType.SingleTargetHit, Session,
                                                            ski.Skill, skillBCards: ski.GetSkillBCards()));
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                                    }
                                }
                                else
                                {
                                    Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                                }
                            }

                            if (ski.Skill.HitType == 3)
                            {
                                Session.Character.MTListTargetQueue.Clear();
                            }

                            ski.GetSkillBCards().Where(s =>
                               (s.Type.Equals((byte)CardType.Buff) && new Buff((short)s.SecondData, Session.Character.Level).Card?.BuffType == BuffType.Good)).ToList()
                                .ForEach(s => s.ApplyBCards(Session.Character.BattleEntity, Session.Character.BattleEntity));
                        }
                        else
                        {
                            Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                        }

                        //if (ski.Skill.UpgradeSkill == 3 && ski.Skill.SkillType == 1)
                        if (ski.Skill.SkillVNum != 1098 && ski.Skill.SkillVNum != 1330)
                        {
                            Session.SendPacket(StaticPacketHelper.SkillResetWithCoolDown(castingId, (short)(ski.Skill.Cooldown + ski.Skill.Cooldown * cooldownReduction / 100D)));
                        }

                        int cdResetMilliseconds = (int)((ski.Skill.Cooldown + ski.Skill.Cooldown * cooldownReduction / 100D) * 100);
                        Observable.Timer(TimeSpan.FromMilliseconds(cdResetMilliseconds))
                            .Subscribe(o =>
                            {
                                sendSkillReset();
                                if (cdResetMilliseconds <= 500) Observable.Timer(TimeSpan.FromMilliseconds(500)).Subscribe(obs => sendSkillReset());
                                void sendSkillReset()
                                {
                                    List<CharacterSkill> charSkills = Session.Character.GetSkills();

                                    CharacterSkill skill = charSkills.Find(s => s.Skill?.CastId == castingId && (s.Skill?.UpgradeSkill == 0 || s.Skill?.SkillType == 1));

                                    if (skill != null && skill.LastUse.AddMilliseconds((short)(skill.Skill.Cooldown + ski.Skill.Cooldown * cooldownReduction / 100D) * 100 - 100) <= DateTime.Now)
                                    {
                                        if (cooldownReduction < 0)
                                        {
                                            skill.LastUse = DateTime.Now.AddMilliseconds(skill.Skill.Cooldown * 100 * -1);
                                        }

                                        Session.SendPacket(StaticPacketHelper.SkillReset(castingId));
                                    }
                                }
                            });

                        int[] fairyWings = Session.Character.GetBuff(CardType.EffectSummon, 11);
                        int random = ServerManager.RandomNumber();
                        if (fairyWings[0] > random)
                        {
                            Observable.Timer(TimeSpan.FromSeconds(1)).Subscribe(o =>
                            {
                                if (ski != null)
                                {
                                    ski.LastUse = DateTime.Now.AddMilliseconds(ski.Skill.Cooldown * 100 * -1);
                                    Session.SendPacket(StaticPacketHelper.SkillReset(ski.Skill.CastId));
                                }
                            });
                        }
                    }
                    else
                    {
                        Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                        Session.SendPacket(
                            Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NOT_ENOUGH_MP"), 10));
                    }
                }
            }
            else
            {
                Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
            }

            if ((castingId != 0 && castingId < 11 && shouldCancel) || Session.Character.SkillComboCount > 7)
            {
                Session.SendPackets(Session.Character.GenerateQuicklist());

                if (!Session.Character.HasMagicSpellCombo
                    && Session.Character.SkillComboCount > 7)
                {
                    Session.SendPacket("ms_c 1");
                }
            }

            Session.Character.LastSkillUse = DateTime.Now;
        }

        private void ZoneHit(int castingId, short x, short y)
        {
            CharacterSkill characterSkill = Session.Character.GetSkills()?.Find(s => s.Skill?.CastId == castingId);
            if (characterSkill == null || !Session.Character.WeaponLoaded(characterSkill)
                                       || !Session.HasCurrentMapInstance
                                       || ((x != 0 || y != 0) && !Session.Character.IsInRange(x, y, characterSkill.GetSkillRange() + 1)))
            {
                Session.SendPacket(StaticPacketHelper.Cancel(2));
                return;
            }

            if (characterSkill.CanBeUsed())
            {
                short mpCost = characterSkill.MpCost();
                short hpCost = 0;

                mpCost = (short)(mpCost * ((100 - Session.Character.CellonOptions.Where(s => s.Type == CellonOptionType.MPUsage).Sum(s => s.Value)) / 100D));

                if (Session.Character.GetBuff(CardType.HealingBurningAndCasting, (byte)AdditionalTypes.HealingBurningAndCasting.HPDecreasedByConsumingMP)[0] is int HPDecreasedByConsumingMP)
                {
                    if (HPDecreasedByConsumingMP < 0)
                    {
                        int amountDecreased = -(characterSkill.MpCost() * HPDecreasedByConsumingMP / 100);
                        hpCost = (short)amountDecreased;
                        mpCost -= (short)amountDecreased;
                    }
                }

                if (Session.Character.Mp >= mpCost && Session.Character.Hp > hpCost && Session.HasCurrentMapInstance)
                {
                    Session.Character.LastSkillUse = DateTime.Now;

                    double cooldownReduction = Session.Character.GetBuff(CardType.Morale, (byte)AdditionalTypes.Morale.SkillCooldownDecreased)[0];

                    int[] increaseEnemyCooldownChance = Session.Character.GetBuff(CardType.DarkCloneSummon, (byte)AdditionalTypes.DarkCloneSummon.IncreaseEnemyCooldownChance);

                    if (ServerManager.RandomNumber() < increaseEnemyCooldownChance[0])
                    {
                        cooldownReduction -= increaseEnemyCooldownChance[1];
                    }
                    
                    Session.CurrentMapInstance.Broadcast(
                        $"ct_n 1 {Session.Character.CharacterId} 3 -1 {characterSkill.Skill.CastAnimation}" +
                        $" {characterSkill.Skill.CastEffect} {characterSkill.Skill.SkillVNum}");
                    characterSkill.LastUse = DateTime.Now;
                    if (!Session.Character.HasGodMode)
                    {
                        Session.Character.DecreaseMp(characterSkill.MpCost());
                    }

                    characterSkill.LastUse = DateTime.Now;
                    Observable.Timer(TimeSpan.FromMilliseconds(characterSkill.Skill.CastTime * 100)).Subscribe(o =>
                    {
                        Session.CurrentMapInstance.Broadcast(
                            $"bs 1 {Session.Character.CharacterId} {x} {y} {characterSkill.Skill.SkillVNum}" +
                            $" {(short)(characterSkill.Skill.Cooldown + characterSkill.Skill.Cooldown * cooldownReduction / 100D)} {characterSkill.Skill.AttackAnimation}" +
                            $" {characterSkill.Skill.Effect} 0 0 1 1 0 0 0");

                        byte Range = characterSkill.TargetRange();
                        if (characterSkill.GetSkillBCards().Any(s => s.Type == (byte)CardType.FalconSkill && s.SubType == (byte)AdditionalTypes.FalconSkill.FalconFocusLowestHP / 10))
                        {
                            if (Session.CurrentMapInstance.BattleEntities.Where(s => s.IsInRange(x, y, Range) 
                                && Session.Character.BattleEntity.CanAttackEntity(s)).OrderBy(s => s.Hp).FirstOrDefault() is BattleEntity lowestHPEntity)
                            {
                                Session.Character.MTListTargetQueue.Push(new MTListHitTarget(lowestHPEntity.UserType, lowestHPEntity.MapEntityId, (TargetHitType)characterSkill.Skill.HitType));
                            }
                        }
                        else if (Session.Character.MTListTargetQueue.Count == 0)
                        {
                            Session.CurrentMapInstance.BattleEntities
                            .Where(s => s.IsInRange(x, y, Range) && Session.Character.BattleEntity.CanAttackEntity(s))
                            .ToList().ForEach(s => Session.Character.MTListTargetQueue.Push(new MTListHitTarget(s.UserType, s.MapEntityId, (TargetHitType)characterSkill.Skill.HitType)));
                        }

                        int count = 0;

                        //foreach (long id in Session.Character.MTListTargetQueue.Where(s => s.EntityType == UserType.Monster).Select(s => s.TargetId))
                        foreach (long id in Session.Character.GetMTListTargetQueue_QuickFix(characterSkill, UserType.Monster))
                        {
                            MapMonster mon = Session.CurrentMapInstance.GetMonsterById(id);
                            if (mon?.CurrentHp > 0 && mon?.Owner?.MapEntityId != Session.Character.CharacterId)
                            {
                                count++;
                                mon.HitQueue.Enqueue(new HitRequest(TargetHitType.SingleAOETargetHit, Session,
                                    characterSkill.Skill, characterSkill.Skill.Effect, x, y, showTargetAnimation: count == 0, skillBCards: characterSkill.GetSkillBCards()));
                            }
                        }

                        //foreach (long id in Session.Character.MTListTargetQueue.Where(s => s.EntityType == UserType.Player).Select(s => s.TargetId))
                        foreach (long id in Session.Character.GetMTListTargetQueue_QuickFix(characterSkill, UserType.Player))
                        {
                            ClientSession character = ServerManager.Instance.GetSessionByCharacterId(id);
                            if (character != null && character.CurrentMapInstance == Session.CurrentMapInstance
                                                  && character.Character.CharacterId != Session.Character.CharacterId)
                            {
                                if (Session.Character.BattleEntity.CanAttackEntity(character.Character.BattleEntity))
                                {
                                    count++;
                                    PvpHit(
                                        new HitRequest(TargetHitType.SingleAOETargetHit, Session, characterSkill.Skill, characterSkill.Skill.Effect, x, y, showTargetAnimation: count == 0, skillBCards: characterSkill.GetSkillBCards()),
                                        character);
                                }
                            }
                        }

                        characterSkill.GetSkillBCards().ToList().Where(s => 
                           (s.Type.Equals((byte)CardType.Buff) && new Buff((short)s.SecondData, Session.Character.Level).Card.BuffType.Equals(BuffType.Good))
                        || (s.Type.Equals((byte)CardType.FalconSkill) && s.SubType.Equals((byte)AdditionalTypes.FalconSkill.CausingChanceLocation / 10))
                        || (s.Type.Equals((byte)CardType.FearSkill) && s.SubType.Equals((byte)AdditionalTypes.FearSkill.ProduceWhenAmbushe / 10))).ToList()
                        .ForEach(s => s.ApplyBCards(Session.Character.BattleEntity, Session.Character.BattleEntity, x, y));

                        Session.Character.MTListTargetQueue.Clear();
                    });

                    Observable.Timer(TimeSpan.FromMilliseconds((short)(characterSkill.Skill.Cooldown + characterSkill.Skill.Cooldown * cooldownReduction / 100D) * 100))
                        .Subscribe(o =>
                        {
                            CharacterSkill
                                skill = Session.Character.GetSkills().Find(s =>
                                    s.Skill?.CastId
                                    == castingId && (s.Skill?.UpgradeSkill == 0 || s.Skill?.SkillType == 1));
                            if (skill != null && skill.LastUse.AddMilliseconds((short)(characterSkill.Skill.Cooldown + characterSkill.Skill.Cooldown * cooldownReduction / 100D) * 100 - 100) <= DateTime.Now)
                            {
                                if (cooldownReduction < 0)
                                {
                                    skill.LastUse = DateTime.Now.AddMilliseconds(skill.Skill.Cooldown * 100 * -1);
                                    skill.LastUse.AddSeconds(cooldownReduction);
                                }

                                Session.SendPacket(StaticPacketHelper.SkillReset(castingId));
                            }
                        });
                }
                else
                {
                    Session.SendPacket(
                        Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NOT_ENOUGH_MP"), 10));
                    Session.SendPacket(StaticPacketHelper.Cancel(2));
                }
            }
            else
            {
                Session.SendPacket(StaticPacketHelper.Cancel(2));
            }
        }

        #endregion
    }
}