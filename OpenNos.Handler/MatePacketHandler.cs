using OpenNos.Core;
using OpenNos.Data;
using OpenNos.Domain;
using OpenNos.GameObject;
using OpenNos.GameObject.Helpers;
using OpenNos.GameObject.Networking;
using OpenNos.GameObject.Packets.ClientPackets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using static OpenNos.Domain.BCardType;

namespace OpenNos.Handler
{
    public class MatePacketHandler : IPacketHandler
    {
        #region Properties

        public MatePacketHandler(ClientSession session) => Session = session;

        private ClientSession Session { get; }

        #endregion

        #region Methods

        /// <summary>
        /// ps_op packet
        /// </summary>
        /// <param name="partnerSkillOpenPacket"></param>
        public void PartnerSkillOpen(PartnerSkillOpenPacket partnerSkillOpenPacket)
        {
            if (partnerSkillOpenPacket == null
                || partnerSkillOpenPacket.CastId < 0
                || partnerSkillOpenPacket.CastId > 2)
            {
                return;
            }

            Mate mate = Session?.Character?.Mates?.ToList().FirstOrDefault(s => s.IsTeamMember && s.MateType == MateType.Partner && s.PetId == partnerSkillOpenPacket.PetId);

            if (mate?.Sp == null || mate.IsUsingSp)
            {
                return;
            }

            if (!mate.Sp.CanLearnSkill())
            {
                return;
            }

            PartnerSkill partnerSkill = mate.Sp.GetSkill(partnerSkillOpenPacket.CastId);

            if (partnerSkill != null)
            {
                return;
            }

            if (partnerSkillOpenPacket.JustDoIt)
            {
                if (mate.Sp.AddSkill(partnerSkillOpenPacket.CastId))
                {
                    Session.SendPacket(UserInterfaceHelper.GenerateModal(Language.Instance.GetMessageFromKey("PSP_SKILL_LEARNED"), 1));
                    mate.Sp.ResetXp();
                }

                Session.SendPacket(mate.GenerateScPacket());
            }
            else
            {
                if (Session.Account.Authority >= AuthorityType.DEV)
                {
                    if (mate.Sp.AddSkill(partnerSkillOpenPacket.CastId))
                    {
                        Session.SendPacket(UserInterfaceHelper.GenerateModal(Language.Instance.GetMessageFromKey("PSP_SKILL_LEARNED"), 1));
                        mate.Sp.FullXp();
                    }

                    Session.SendPacket(mate.GenerateScPacket());
                    return;
                }
                Session.SendPacket($"pdelay 3000 12 #ps_op^{partnerSkillOpenPacket.PetId}^{partnerSkillOpenPacket.CastId}^1");
                Session.CurrentMapInstance?.Broadcast(UserInterfaceHelper.GenerateGuri(2, 2, mate.MateTransportId), mate.PositionX, mate.PositionY);
            }
        }

        /// <summary>
        /// u_ps packet
        /// </summary>
        /// <param name="usePartnerSkillPacket"></param>
        public void UseSkill(UsePartnerSkillPacket usePartnerSkillPacket)
        {
            #region Invalid packet

            if (usePartnerSkillPacket == null)
            {
                return;
            }

            #endregion

            #region Mate not found (or invalid)

            Mate mate = Session?.Character?.Mates?.ToList().FirstOrDefault(s => s.IsTeamMember && s.MateType == MateType.Partner && s.MateTransportId == usePartnerSkillPacket.TransportId);

            if (mate?.Monster == null)
            {
                return;
            }

            #endregion

            #region Not using PSP

            if (mate.Sp == null || !mate.IsUsingSp)
            {
                return;
            }

            #endregion

            #region Skill not found

            PartnerSkill partnerSkill = mate.Sp.GetSkill(usePartnerSkillPacket.CastId);

            if (partnerSkill == null)
            {
                return;
            }

            #endregion

            #region Convert PartnerSkill to Skill

            Skill skill = PartnerSkillHelper.ConvertToNormalSkill(partnerSkill);

            #endregion

            #region Battle entities

            BattleEntity battleEntityAttacker = mate.BattleEntity;
            BattleEntity battleEntityDefender = null;

            switch (usePartnerSkillPacket.TargetType)
            {
                case UserType.Player:
                    {
                        Character target = Session.Character.MapInstance?.GetCharacterById(usePartnerSkillPacket.TargetId);
                        battleEntityDefender = target?.BattleEntity;
                    }
                    break;

                case UserType.Npc:
                    {
                        Mate target = Session.Character.MapInstance?.GetMate(usePartnerSkillPacket.TargetId);
                        battleEntityDefender = target?.BattleEntity;
                    }
                    break;

                case UserType.Monster:
                    {
                        MapMonster target = Session.Character.MapInstance?.GetMonsterById(usePartnerSkillPacket.TargetId);
                        battleEntityDefender = target?.BattleEntity;
                    }
                    break;
            }

            #endregion

            #region Attack

            PartnerSkillTargetHit(battleEntityAttacker, battleEntityDefender, skill);

            #endregion
        }


        /// <summary>
        ///     u_pet packet
        /// </summary>
        /// <param name="upetPacket"></param>
        public void SpecialSkill(UpetPacket upetPacket)
        {
            if (upetPacket == null)
            {
                return;
            }

            PenaltyLogDTO penalty = Session.Account.PenaltyLogs.OrderByDescending(s => s.DateEnd).FirstOrDefault();
            if (Session.Character.IsMuted() && penalty != null)
            {
                if (Session.Character.Gender == GenderType.Female)
                {
                    Session.CurrentMapInstance?.Broadcast(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("MUTED_FEMALE"), 1));
                    Session.SendPacket(Session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("MUTE_TIME"), (penalty.DateEnd - DateTime.Now).ToString("hh\\:mm\\:ss")), 11));
                }
                else
                {
                    Session.CurrentMapInstance?.Broadcast(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("MUTED_MALE"), 1));
                    Session.SendPacket(Session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("MUTE_TIME"), (penalty.DateEnd - DateTime.Now).ToString("hh\\:mm\\:ss")), 11));
                }

                return;
            }

            Mate attacker = Session.Character.Mates.FirstOrDefault(x => x.MateTransportId == upetPacket.MateTransportId);
            if (attacker == null)
            {
                return;
            }

            NpcMonsterSkill mateSkill = null;
            if (attacker.Monster.Skills.Any())
            {
                mateSkill = attacker.Monster.Skills.FirstOrDefault(x => x.Rate == 0);
            }

            if (mateSkill == null)
            {
                mateSkill = new NpcMonsterSkill
                {
                    SkillVNum = 200
                };
            }

            if (attacker.IsSitting)
            {
                return;
            }

            switch (upetPacket.TargetType)
            {
                case UserType.Monster:
                    if (attacker.Hp > 0)
                    {
                        MapMonster target = Session?.CurrentMapInstance?.GetMonsterById(upetPacket.TargetId);
                        attacker.TargetHit(target.BattleEntity, mateSkill);
                    }

                    return;

                case UserType.Npc:
                    return;

                case UserType.Player:
                    if (attacker.Hp > 0)
                    {
                        Character target = Session?.CurrentMapInstance?.GetSessionByCharacterId(upetPacket.TargetId).Character;
                        attacker.TargetHit(target.BattleEntity, mateSkill);
                    }
                    return;

                case UserType.Object:
                    return;

                default:
                    return;
            }
        }

        /// <summary>
        /// suctl packet
        /// </summary>
        /// <param name="suctlPacket"></param>
        public void Attack(SuctlPacket suctlPacket)
        {
            if (suctlPacket == null)
            {
                return;
            }

            if (suctlPacket.TargetType != UserType.Npc
                && !Session.Account.VerifiedLock)
            {
                Session.SendPacket(UserInterfaceHelper.GenerateMsg("You cant do this because your account is blocked. Use $Unlock", 0));
                return;
            }

            PenaltyLogDTO penalty = Session.Account.PenaltyLogs.OrderByDescending(s => s.DateEnd).FirstOrDefault();
            if (Session.Character.IsMuted() && penalty != null)
            {
                if (Session.Character.Gender == GenderType.Female)
                {
                    Session.CurrentMapInstance?.Broadcast(
                        Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("MUTED_FEMALE"), 1));
                    Session.SendPacket(Session.Character.GenerateSay(
                        string.Format(Language.Instance.GetMessageFromKey("MUTE_TIME"),
                            (penalty.DateEnd - DateTime.Now).ToString("hh\\:mm\\:ss")), 11));
                }
                else
                {
                    Session.CurrentMapInstance?.Broadcast(
                        Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("MUTED_MALE"), 1));
                    Session.SendPacket(Session.Character.GenerateSay(
                        string.Format(Language.Instance.GetMessageFromKey("MUTE_TIME"),
                            (penalty.DateEnd - DateTime.Now).ToString("hh\\:mm\\:ss")), 11));
                }

                return;
            }

            Mate attacker = Session.Character.Mates.Find(x => x.MateTransportId == suctlPacket.MateTransportId);

            if (attacker != null && !attacker.HasBuff(CardType.SpecialAttack, (byte)AdditionalTypes.SpecialAttack.NoAttack))
            {
                IEnumerable<NpcMonsterSkill> mateSkills = attacker.Skills;

                if (mateSkills != null)
                {
                    NpcMonsterSkill skill = null;

                    List<NpcMonsterSkill> PossibleSkills = mateSkills.Where(s => (DateTime.Now - s.LastSkillUse).TotalMilliseconds >= 1000 * s.Skill.Cooldown || s.Rate == 0).ToList();

                    foreach (NpcMonsterSkill ski in PossibleSkills.OrderBy(rnd => ServerManager.RandomNumber()))
                    {
                        if (ski.Rate == 0)
                        {
                            skill = ski;
                        }
                        else if (ServerManager.RandomNumber() < ski.Rate)
                        {
                            skill = ski;
                            break;
                        }
                    }
                    
                    switch (suctlPacket.TargetType)
                    {
                        case UserType.Monster:
                            if (attacker.Hp > 0)
                            {
                                MapMonster target = Session.CurrentMapInstance?.GetMonsterById(suctlPacket.TargetId);
                                if (target != null)
                                {
                                    if (attacker.BattleEntity.CanAttackEntity(target.BattleEntity))
                                    {
                                        attacker.TargetHit(target.BattleEntity, skill);
                                    }
                                }
                            }

                            return;

                        case UserType.Npc:
                            if (attacker.Hp > 0)
                            {
                                Mate target = Session.CurrentMapInstance?.GetMate(suctlPacket.TargetId);
                                if (target != null)
                                {
                                    if (attacker.Owner.BattleEntity.CanAttackEntity(target.BattleEntity))
                                    {
                                        attacker.TargetHit(target.BattleEntity, skill);
                                    }
                                    else
                                    {
                                        Session.SendPacket(StaticPacketHelper.Cancel(2, target.CharacterId));
                                    }
                                }
                            }
                            return;

                        case UserType.Player:
                            if (attacker.Hp > 0)
                            {
                                Character target = Session.CurrentMapInstance?.GetSessionByCharacterId(suctlPacket.TargetId) ?.Character;
                                if (target != null)
                                {
                                    if (attacker.Owner.BattleEntity.CanAttackEntity(target.BattleEntity))
                                    {
                                        attacker.TargetHit(target.BattleEntity, skill);
                                    }
                                    else
                                    {
                                        Session.SendPacket(StaticPacketHelper.Cancel(2, target.CharacterId));
                                    }
                                }
                            }

                            return;

                        case UserType.Object:
                            return;
                    }
                }
            }
        }

        /// <summary>
        /// psl packet
        /// </summary>
        /// <param name="pslPacket"></param>
        public void Psl(PslPacket pslPacket)
        {
            Mate mate = Session?.Character?.Mates?.ToList().Find(s => s.IsTeamMember && s.MateType == MateType.Partner);

            if (mate == null)
            {
                return;
            }

            if (mate.Sp == null)
            {
                Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NO_PSP"), 0));
                return;
            }

            if (!mate.IsUsingSp && !mate.CanUseSp())
            {
                int spRemainingCooldown = mate.GetSpRemainingCooldown();
                Session.SendPacket(Session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("STAY_TIME"), spRemainingCooldown), 11));
                Session.SendPacket($"psd {spRemainingCooldown}");
                return;
            }

            if (pslPacket.Type == 0)
            {
                if (mate.IsUsingSp)
                {
                    mate.RemoveSp();
                    mate.StartSpCooldown();
                }
                else
                {
                    Session.SendPacket("pdelay 5000 3 #psl^1");
                    Session.CurrentMapInstance?.Broadcast(UserInterfaceHelper.GenerateGuri(2, 2, mate.MateTransportId), mate.PositionX, mate.PositionY);
                }
            }
            else
            {
                mate.IsUsingSp = true;

                Session.SendPacket(mate.GenerateCond());
                Session.Character.MapInstance.Broadcast(mate.GenerateCMode(mate.Sp.Instance.Item.Morph));
                Session.SendPacket(mate.Sp.GeneratePski());
                Session.SendPacket(mate.GenerateScPacket());
                Session.Character.MapInstance.Broadcast(mate.GenerateOut());

                bool isAct4 = ServerManager.Instance.ChannelId == 51;

                Parallel.ForEach(Session.CurrentMapInstance.Sessions.Where(s => s.Character != null), s =>
                {
                    if (!isAct4 || Session.Character.Faction == s.Character.Faction)
                    {
                        s.SendPacket(mate.GenerateIn(false, isAct4));
                    }
                    else
                    {
                        s.SendPacket(mate.GenerateIn(true, isAct4, s.Account.Authority));
                    }
                });

                Session.SendPacket(Session.Character.GeneratePinit());
                Session.Character.MapInstance.Broadcast(StaticPacketHelper.GenerateEff(UserType.Npc, mate.MateTransportId, 196));
            }
        }

        private void PartnerSkillTargetHit(BattleEntity battleEntityAttacker, BattleEntity battleEntityDefender, Skill skill, bool isRecursiveCall = false)
        {
            #region Invalid entities

            if (battleEntityAttacker?.MapInstance == null
                || battleEntityAttacker.Mate?.Owner?.BattleEntity == null
                || battleEntityAttacker.Mate.Monster == null)
            {
                return;
            }

            if (battleEntityDefender?.MapInstance == null)
            {
                return;
            }

            #endregion

            #region Maps NOT matching

            if (battleEntityAttacker.MapInstance != battleEntityDefender.MapInstance)
            {
                return;
            }

            #endregion

            #region Invalid skill

            if (skill == null)
            {
                return;
            }

            #endregion

            #region Invalid state

            if (battleEntityAttacker.Hp < 1 || battleEntityAttacker.Mate.IsSitting)
            {
                return;
            }

            if (battleEntityDefender.Hp < 1)
            {
                return;
            }

            #endregion

            #region Can NOT attack

            if (((skill.TargetType != 1 || !battleEntityDefender.Equals(battleEntityAttacker)) && !battleEntityAttacker.CanAttackEntity(battleEntityDefender))
                || battleEntityAttacker.HasBuff(CardType.SpecialAttack, (byte)AdditionalTypes.SpecialAttack.NoAttack))
            {
                return;
            }

            #endregion

            #region Cooldown

            if (!isRecursiveCall && skill.PartnerSkill != null && !skill.PartnerSkill.CanBeUsed())
            {
                return;
            }

            #endregion

            #region Enemy too far

            if (skill.TargetType == 0 && battleEntityAttacker.GetDistance(battleEntityDefender) > skill.Range)
            {
                return;
            }

            #endregion

            #region Mp NOT enough

            if (!isRecursiveCall && battleEntityAttacker.Mp < skill.MpCost)
            {
                return;
            }

            #endregion

            lock (battleEntityDefender.PVELockObject)
            {
                if (!isRecursiveCall)
                {
                    #region Update skill LastUse

                    if (skill.PartnerSkill != null)
                    {
                        skill.PartnerSkill.LastUse = DateTime.Now;
                    }

                    #endregion

                    #region Decrease MP

                    battleEntityAttacker.DecreaseMp(skill.MpCost);

                    #endregion

                    #region Cast on target

                    battleEntityAttacker.MapInstance.Broadcast(StaticPacketHelper.CastOnTarget(
                        battleEntityAttacker.UserType, battleEntityAttacker.MapEntityId,
                        battleEntityDefender.UserType, battleEntityDefender.MapEntityId,
                        skill.CastAnimation, skill.CastEffect,
                        skill.SkillVNum));

                    #endregion

                    #region Show icon

                    battleEntityAttacker.MapInstance.Broadcast(StaticPacketHelper.GenerateEff(battleEntityAttacker.UserType, battleEntityAttacker.MapEntityId, 5005));

                    #endregion
                }

                #region Calculate damage

                int hitMode = 0;
                bool onyxWings = false;
                bool hasAbsorbed = false;

                int damage = DamageHelper.Instance.CalculateDamage(battleEntityAttacker, battleEntityDefender,
                    skill, ref hitMode, ref onyxWings/*, ref hasAbsorbed*/);

                #endregion

                if (hitMode != 4)
                {
                    #region ConvertDamageToHPChance

                    if (battleEntityDefender.Character is Character target)
                    {
                        int[] convertDamageToHpChance = target.GetBuff(CardType.DarkCloneSummon, (byte)AdditionalTypes.DarkCloneSummon.ConvertDamageToHPChance);

                        if (ServerManager.RandomNumber() < convertDamageToHpChance[0])
                        {
                            int amount = damage;

                            if (target.Hp + amount > target.HPLoad())
                            {
                                amount = (int)target.HPLoad() - target.Hp;
                            }

                            target.Hp += amount;
                            target.ConvertedDamageToHP += amount;
                            target.MapInstance.Broadcast(target.GenerateRc(amount));
                            target.Session?.SendPacket(target.GenerateStat());

                            damage = 0;
                        }
                    }

                    #endregion

                    #region InflictDamageToMP

                    if (damage > 0)
                    {
                        int[] inflictDamageToMp = battleEntityDefender.GetBuff(CardType.LightAndShadow, (byte)AdditionalTypes.LightAndShadow.InflictDamageToMP);

                        if (inflictDamageToMp[0] != 0)
                        {
                            int amount = Math.Min((int)(damage / 100D * inflictDamageToMp[0]), battleEntityDefender.Mp);
                            battleEntityDefender.DecreaseMp(amount);

                            damage -= amount;
                        }
                    }

                    #endregion
                }

                #region Stand up

                battleEntityDefender.Character?.StandUp();

                #endregion

                #region Cast effect

                int castTime = 0;

                if (!isRecursiveCall && skill.CastEffect != 0)
                {
                    battleEntityAttacker.MapInstance.Broadcast(StaticPacketHelper.GenerateEff(battleEntityAttacker.UserType, battleEntityAttacker.MapEntityId,
                        skill.CastEffect), battleEntityAttacker.PositionX, battleEntityAttacker.PositionY);

                    castTime = skill.CastTime * 100;
                }

                #endregion

                #region Use skill

                Observable.Timer(TimeSpan.FromMilliseconds(castTime)).Subscribe(o => PartnerSkillTargetHit2(battleEntityAttacker, battleEntityDefender, skill,
                    isRecursiveCall, damage, hitMode, hasAbsorbed));

                #endregion
            }
        }

        private void PartnerSkillTargetHit2(BattleEntity battleEntityAttacker, BattleEntity battleEntityDefender, Skill skill, bool isRecursiveCall, int damage, int hitMode, bool hasAbsorbed)
        {
            #region BCards

            List<BCard> bcards = new List<BCard>();

            if (battleEntityAttacker.Mate.Monster.BCards != null)
            {
                bcards.AddRange(battleEntityAttacker.Mate.Monster.BCards.ToList());
            }

            if (skill.BCards != null)
            {
                bcards.AddRange(skill.BCards.ToList());
            }

            #endregion

            #region Owner

            Character attackerOwner = battleEntityAttacker.Mate.Owner;

            #endregion

            lock (battleEntityDefender.PVELockObject)
            {
                #region Battle logic

                if (isRecursiveCall || skill.TargetType == 0)
                {
                    battleEntityDefender.GetDamage(damage, battleEntityAttacker);

                    battleEntityAttacker.MapInstance.Broadcast(StaticPacketHelper.SkillUsed(battleEntityAttacker.UserType, battleEntityAttacker.MapEntityId,
                        battleEntityDefender.UserType, battleEntityDefender.MapEntityId, skill.SkillVNum, skill.Cooldown, skill.AttackAnimation, skill.Effect,
                        battleEntityDefender.PositionX, battleEntityDefender.PositionY, battleEntityDefender.Hp > 0, battleEntityDefender.HpPercent(),
                        damage, hitMode, skill.SkillType));

                    if (battleEntityDefender.Character != null)
                    {
                        battleEntityDefender.Character.Session?.SendPacket(battleEntityDefender.Character.GenerateStat());
                    }

                    if (battleEntityDefender.MapMonster != null && attackerOwner.BattleEntity != null)
                    {
                        battleEntityDefender.MapMonster.AddToDamageList(attackerOwner.BattleEntity, damage);
                    }

                    bcards.ForEach(bcard =>
                    {
                        if (bcard.Type == Convert.ToByte(CardType.Buff) && new Buff(Convert.ToInt16(bcard.SecondData), battleEntityAttacker.Level).Card?.BuffType != BuffType.Bad)
                        {
                            if (!isRecursiveCall)
                            {
                                bcard.ApplyBCards(battleEntityAttacker, battleEntityAttacker);
                            }
                        }
                        else if (battleEntityDefender.Hp > 0)
                        {
                            if (hitMode != 4 && !hasAbsorbed)
                            {
                                bcard.ApplyBCards(battleEntityDefender, battleEntityAttacker);
                            }
                        }
                    });

                    if (battleEntityDefender.Hp > 0 && hitMode != 4 && !hasAbsorbed)
                    {
                        battleEntityDefender.BCards?.ToList().ForEach(bcard =>
                        {
                            if (bcard.Type == Convert.ToByte(CardType.Buff))
                            {
                                if (new Buff(Convert.ToInt16(bcard.SecondData), battleEntityDefender.Level).Card?.BuffType != BuffType.Bad)
                                {
                                    bcard.ApplyBCards(battleEntityDefender, battleEntityDefender);
                                }
                                else
                                {
                                    bcard.ApplyBCards(battleEntityAttacker, battleEntityDefender);
                                }
                            }
                        });
                    }
                }
                else if (skill.HitType == 1 && skill.TargetRange > 0)
                {
                    battleEntityAttacker.MapInstance.Broadcast(StaticPacketHelper.SkillUsed(battleEntityAttacker.UserType, battleEntityAttacker.MapEntityId,
                        battleEntityAttacker.UserType, battleEntityAttacker.MapEntityId, skill.SkillVNum, skill.Cooldown, skill.AttackAnimation, skill.Effect,
                        battleEntityAttacker.PositionX, battleEntityAttacker.PositionY, battleEntityAttacker.Hp > 0, battleEntityAttacker.HpPercent(),
                        damage, hitMode, skill.SkillType));

                    if (battleEntityAttacker.Hp > 0)
                    {
                        bcards.ForEach(bcard =>
                        {
                            if (bcard.Type == Convert.ToByte(CardType.Buff) && new Buff(Convert.ToInt16(bcard.SecondData), battleEntityAttacker.Level).Card?.BuffType != BuffType.Bad)
                            {
                                bcard.ApplyBCards(battleEntityAttacker, battleEntityAttacker);
                            }
                        });
                    }

                    battleEntityAttacker.MapInstance.GetBattleEntitiesInRange(battleEntityAttacker.GetPos(), skill.TargetRange).ToList()
                        .ForEach(battleEntityInRange =>
                        {
                            if (!battleEntityInRange.Equals(battleEntityAttacker))
                            {
                                PartnerSkillTargetHit(battleEntityAttacker, battleEntityInRange, skill, true);
                            }
                        });
                }

                #endregion

                #region Skill reset

                if (!isRecursiveCall && (skill.Class == 28 || skill.Class == 29))
                {
                    Observable.Timer(TimeSpan.FromMilliseconds(skill.Cooldown * 100))
                        .Subscribe(o => attackerOwner.Session?.SendPacket($"psr {skill.CastId}"));
                }

                #endregion

                #region Hp <= 0

                if (battleEntityDefender.Hp <= 0)
                {
                    switch (battleEntityDefender.EntityType)
                    {
                        case EntityType.Player:
                            {
                                Character target = battleEntityDefender.Character;

                                if (target != null)
                                {
                                    if (target.IsVehicled)
                                    {
                                        target.RemoveVehicle();
                                    }

                                    Observable.Timer(TimeSpan.FromMilliseconds(1000))
                                        .Subscribe(o => ServerManager.Instance.AskPvpRevive(target.CharacterId));
                                }
                            }
                            break;

                        case EntityType.Mate:
                            break;

                        case EntityType.Npc:
                            battleEntityDefender.MapNpc?.RunDeathEvent();
                            break;

                        case EntityType.Monster:
                            {
                                battleEntityDefender.MapMonster?.SetDeathStatement();
                                attackerOwner.GenerateKillBonus(battleEntityDefender.MapMonster, battleEntityAttacker);
                            }
                            break;
                    }
                }

                #endregion
            }
        }

        #endregion
    }
}