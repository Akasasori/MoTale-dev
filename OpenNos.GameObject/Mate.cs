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
using OpenNos.GameObject.Helpers;
using OpenNos.GameObject.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using OpenNos.PathFinder;
using static OpenNos.Domain.BCardType;
using OpenNos.GameObject.Battle;
using System.IO;
using System.Threading.Tasks;

namespace OpenNos.GameObject
{
    public class Mate : MateDTO
    {
        #region Members

        private NpcMonster _monster;
        private Character _owner;
        public object PVELockObject;

        #endregion

        #region Instantiation

        public Mate(MateDTO input)
        {
            PVELockObject = new object();
            Attack = input.Attack;
            CanPickUp = input.CanPickUp;
            CharacterId = input.CharacterId;
            Defence = input.Defence;
            Direction = input.Direction;
            Experience = input.Experience;
            Hp = input.Hp;
            IsSummonable = input.IsSummonable;
            IsTeamMember = input.IsTeamMember;
            Level = input.Level;
            Loyalty = input.Loyalty;
            MapX = input.MapX;
            MapY = input.MapY;
            PositionX = MapX;
            PositionY = MapY;
            MateId = input.MateId;
            MateType = input.MateType;
            Mp = input.Mp;
            Name = input.Name;
            NpcMonsterVNum = input.NpcMonsterVNum;
            Skin = input.Skin;
            Skills = new List<NpcMonsterSkill>();
            foreach (NpcMonsterSkill ski in Monster.Skills)
            {
                Skills.Add(new NpcMonsterSkill { SkillVNum = ski.SkillVNum, Rate = ski.Rate });
            }
            GenerateMateTransportId();
            IsAlive = true;
            BattleEntity = new BattleEntity(this);
            if (IsTeamMember)
            {
                AddTeamMember();
            }
            if (Monster.CriticalChance == 0 && Monster.CriticalRate == 0)
            {
                try
                {
                    StreamWriter streamWriter = new StreamWriter("MissingMateStats.txt", true)
                    {
                        AutoFlush = true
                    };
                    streamWriter.WriteLine($"{Monster.NpcMonsterVNum} is missing critical stats.");
                    streamWriter.Close();
                }
                catch (IOException)
                {
                    Logger.Warn("MissingMateStats.txt was in use, but i was able to catch this exception", null, "MissingMateStats");
                }
            }
        }

        public Mate(Character owner, NpcMonster npcMonster, byte level, MateType matetype, bool temporal = false, bool tsReward = false, bool tsProtected = false)
        {
            IsTemporalMate = temporal;
            IsTsReward = tsReward;
            IsTsProtected = tsProtected;
            PVELockObject = new object();
            NpcMonsterVNum = npcMonster.NpcMonsterVNum;
            Monster = npcMonster;
            Level = level;
            Hp = MaxHp;
            Mp = MaxMp;
          
            Name = npcMonster.Name;
            MateType = matetype;
            Loyalty = 1000;
            PositionY = (short) (owner.PositionY + 1);
            PositionX = (short) (owner.PositionX + 1);
            if (owner.MapInstance.Map.IsBlockedZone(MapX, MapY))
            {
                PositionY = owner.PositionY;
                PositionX = owner.PositionX;
            }
            MapY = PositionY;
            MapX = PositionX;
            Direction = 2;
            CharacterId = owner.CharacterId;
            Skills = new List<NpcMonsterSkill>();
            foreach (NpcMonsterSkill ski in Monster.Skills)
            {
                Skills.Add(new NpcMonsterSkill { SkillVNum = ski.SkillVNum, Rate = ski.Rate });
            }
            Owner = owner;
            GenerateMateTransportId();
            IsAlive = true;
            BattleEntity = new BattleEntity(this);
            if (IsTeamMember)
            {
                AddTeamMember();
            }
        }

        #endregion

        #region Properties
        public bool NoAttack { get; private set; }

        public bool NoMove { get; private set; }

        public ItemInstance ArmorInstance { get; set; }

        public ItemInstance BootsInstance { get; set; }

        public ThreadSafeSortedList<short, Buff> Buff => BattleEntity.Buffs;

        public new ThreadSafeSortedList<short, IDisposable> BuffObservables => BattleEntity.BuffObservables; //Fix

        public int Concentrate => ConcentrateLoad();

        public int DamageMinimum => DamageMinimumLoad();

        public int DamageMaximum => DamageMaximumLoad();

        public ItemInstance GlovesInstance { get; set; }

        public bool IsAlive { get; set; }

        public bool IsSitting { get; set; }

        public bool IsUsingSp { get; set; }

        public DateTime LastHealth { get; set; }

        public DateTime LastDefence { get; set; }

        public DateTime LastSpeedChange { get; set; }

        public DateTime LastSkillUse { get; set; }

        public DateTime LastBasicSkillUse { get; set; }

        public int MagicalDefense => MagicalDefenseLoad();

        public int MateTransportId { get; private set; }

        public double MaxHp => HpLoad();

        public double MaxMp => MpLoad();

        public int MeleeDefense => MeleeDefenseLoad();

        public int MeleeDefenseDodge => MeleeDefenseDodgeLoad();

        public NpcMonster Monster
        {
            get => _monster ?? ServerManager.GetNpcMonster(NpcMonsterVNum);

            set => _monster = value;
        }

        public Character Owner
        {
            get => _owner ?? ServerManager.Instance.GetSessionByCharacterId(CharacterId)?.Character;
            set => _owner = value;
        }

        public byte PetId { get; set; }

        public short PositionX { get; set; }

        public short PositionY { get; set; }

        public int RangeDefense => RangeDefenseLoad();

        public int RangeDefenseDodge => RangeDefenseDodgeLoad();

        public IDisposable ReviveDisposable { get; set; }

        public List<NpcMonsterSkill> Skills { get; set; }

        public byte Speed
        {
            get
            {
                byte tempSpeed = (byte)(Monster.Speed + 4);
                
                byte fixSpeed = (byte)GetBuff(CardType.Move, (byte)AdditionalTypes.Move.SetMovement)[0];
                if (fixSpeed != 0)
                {
                    return fixSpeed;
                }
                else
                {
                    tempSpeed += (byte)GetBuff(CardType.Move, (byte)AdditionalTypes.Move.MovementSpeedIncreased)[0];
                    tempSpeed = (byte)(tempSpeed * (1 + (GetBuff(CardType.Move, (byte)AdditionalTypes.Move.MoveSpeedIncreased)[0] / 100D)));
                }
                
                if (tempSpeed >= 59 || tempSpeed < 1)
                {
                    return 1;
                }

                return tempSpeed;
            }
            set
            {
                LastSpeedChange = DateTime.Now;
                Monster.Speed = value > 59 ? (byte) 59 : value;
            }
        }

        public int SpCooldown { get; set; }

        public DateTime LastSpCooldown { get; set; }

        public PartnerSp Sp { get; set; }

        public bool IsTemporalMate { get; set; }

        public bool IsTsReward { get; set; }

        public bool IsTsProtected { get; set; }

        public int TrainerHits { get; set; }

        public int TrainerDefences { get; set; }

        public ItemInstance WeaponInstance { get; set; }

        public DateTime LastMonsterAggro { get; set; }

        public Node[][] BrushFireJagged { get; set; }

        public List<EventContainer> OnDeathEvents => BattleEntity.OnDeathEvents;

        #endregion

        #region BattleEntityProperties

        public BattleEntity BattleEntity { get; set; }

        public void AddBuff(Buff indicator, BattleEntity battleEntity) => BattleEntity.AddBuff(indicator, battleEntity);

        public void RemoveBuff(short cardId) => BattleEntity.RemoveBuff(cardId);

        public int[] GetBuff(CardType type, byte subtype) => BattleEntity != null ? BattleEntity.GetBuff(type, subtype) : new[] { 0, 0 };

        public bool HasBuff(CardType type, byte subtype) => BattleEntity != null ? BattleEntity.HasBuff(type, subtype) : false;

        public void DisableBuffs(BuffType type, int level = 100) => BattleEntity.DisableBuffs(type, level);

        public void DisableBuffs(List<BuffType> types, int level = 100) => BattleEntity.DisableBuffs(types, level);

        public void DecreaseMp(int amount) => BattleEntity.DecreaseMp(amount);

        #endregion

        #region Methods

        public bool CanUseBasicSkill() => Monster != null && LastBasicSkillUse.AddMilliseconds(Monster.BasicCooldown * 100) < DateTime.Now;

        public void StartSpCooldown()
        {
            if (Sp != null)
            {
                SpCooldown = Sp.GetCooldown();

                Observable.Timer(TimeSpan.FromSeconds(SpCooldown)).Subscribe(o => Owner?.Session?.SendPacket("psd 0"));

                Owner?.Session?.SendPacket($"psd {SpCooldown}");

                LastSpCooldown = DateTime.Now;
            }
        }

        public bool CanUseSp() => LastSpCooldown.AddSeconds(SpCooldown) < DateTime.Now;

        public int GetSpRemainingCooldown() => (int)(LastSpCooldown - DateTime.Now).TotalSeconds + SpCooldown;


        public string GenerateDpski() => "dpski";

        public void RemoveSp(bool isBackToMiniland = false)
        {
            if (Owner?.Session == null || Owner.MapInstance == null)
            {
                return;
            }

            IsUsingSp = false;

            Owner.Session.SendPacket(GenerateScPacket());

            if (IsTeamMember)
            {
                Owner.MapInstance.Broadcast(GenerateCMode(-1));
                Owner.Session.SendPacket(GenerateDpski());
                Owner.Session.SendPacket(GenerateCond());
                Owner.MapInstance.Broadcast(GenerateOut());

                if (!isBackToMiniland)
                {
                    bool isAct4 = ServerManager.Instance.ChannelId == 51;

                    Parallel.ForEach(Owner.MapInstance.Sessions.Where(s => s.Character != null), s =>
                    {
                        if (!isAct4 || Owner.Faction == s.Character.Faction)
                        {
                            s.SendPacket(GenerateIn(false, isAct4));
                        }
                        else
                        {
                            s.SendPacket(GenerateIn(true, isAct4, s.Account.Authority));
                        }
                    });
                }

                Owner.Session.SendPacket(Owner.GeneratePinit());
            }
        }

        public void GenerateMateTransportId()
        {
            int nextId = ServerManager.Instance.MateIds.Count > 0 ? ServerManager.Instance.MateIds.Last() + 1 : 2000000;
            ServerManager.Instance.MateIds.Add(nextId);
            MateTransportId = nextId;
        }

        public string GenerateCMode(short morphId) => $"c_mode 2 {MateTransportId} {morphId} 0 0";

        public string GenerateCond() => $"cond 2 {MateTransportId} {(HasBuff(CardType.SpecialAttack, (byte)AdditionalTypes.SpecialAttack.NoAttack) || Loyalty <= 0 ? 1 : 0)} {(HasBuff(CardType.Move, (byte)AdditionalTypes.Move.MovementImpossible) || Loyalty <= 0 ? 1 : 0)} {Speed}";

        public string GeneratePst() => $"pst 2 {MateTransportId} {(int)MateType} {(int)(Hp / (float)MaxHp * 100)} {(int)(Mp / (float)MaxMp * 100)} {Hp} {Mp} 0 0 0";

        public string GenerateEInfo() =>
            $"e_info 10 " +
            $"{NpcMonsterVNum} " +
            $"{Level} " +
            $"{(MateType == MateType.Pet ? Monster.Element : /*SP Element*/0)} " +
            $"{Monster.AttackClass} " +
            $"{Monster.ElementRate} " +
            $"{Attack + (WeaponInstance?.Upgrade ?? 0)} " +
            $"{DamageMinimum + (WeaponInstance?.Item.DamageMinimum ?? 0)} " +
            $"{DamageMaximum + (WeaponInstance?.Item.DamageMaximum ?? 0)} " +
            $"{Concentrate + (WeaponInstance?.Item.HitRate ?? 0)} " +
            $"{Monster.CriticalChance + (WeaponInstance?.Item.CriticalLuckRate ?? 0)} " +
            $"{Monster.CriticalRate + (WeaponInstance?.Item.CriticalRate ?? 0)} " +
            $"{Defence + (ArmorInstance?.Upgrade ?? 0)} " +
            $"{MeleeDefense + (ArmorInstance?.Item.CloseDefence ?? 0) + (GlovesInstance?.Item.CloseDefence ?? 0) + (BootsInstance?.Item.CloseDefence ?? 0)} " +
            $"{MeleeDefenseDodge + (ArmorInstance?.Item.DefenceDodge ?? 0) + (GlovesInstance?.Item.DefenceDodge ?? 0) + (BootsInstance?.Item.DefenceDodge ?? 0)} " +
            $"{RangeDefense + (ArmorInstance?.Item.DistanceDefence ?? 0) + (GlovesInstance?.Item.DistanceDefence ?? 0) + (BootsInstance?.Item.DistanceDefence ?? 0)} " +
            $"{RangeDefenseDodge + (ArmorInstance?.Item.DistanceDefenceDodge ?? 0) + (GlovesInstance?.Item.DistanceDefenceDodge ?? 0) + (BootsInstance?.Item.DistanceDefenceDodge ?? 0)} " +
            $"{MagicalDefense + (ArmorInstance?.Item.MagicDefence ?? 0) + (GlovesInstance?.Item.MagicDefence ?? 0) + (BootsInstance?.Item.MagicDefence ?? 0)} " +
            $"{EquipmentFireResistance + Monster.FireResistance + (GlovesInstance?.FireResistance ?? 0) + (GlovesInstance?.Item.FireResistance ?? 0) + (BootsInstance?.FireResistance ?? 0) + (BootsInstance?.Item.FireResistance ?? 0)} " +
            $"{EquipmentWaterResistance + Monster.WaterResistance + (GlovesInstance?.WaterResistance ?? 0) + (GlovesInstance?.Item.WaterResistance ?? 0) + (BootsInstance?.WaterResistance ?? 0) + (BootsInstance?.Item.WaterResistance ?? 0)} " +
            $"{EquipmentLightResistance + Monster.LightResistance + (GlovesInstance?.LightResistance ?? 0) + (GlovesInstance?.Item.LightResistance ?? 0) + (BootsInstance?.LightResistance ?? 0) + (BootsInstance?.Item.LightResistance ?? 0)} " +
            $"{EquipmentDarkResistance + Monster.DarkResistance + (GlovesInstance?.DarkResistance ?? 0) + (GlovesInstance?.Item.DarkResistance ?? 0) + (BootsInstance?.DarkResistance ?? 0) + (BootsInstance?.Item.DarkResistance ?? 0)} " +
            $"{MaxHp} " +
            $"{MaxMp} " +
            $"-1 {Name.Replace(' ', '^')}";

        public string GenerateIn(bool hideNickname = false, bool isAct4 = false, AuthorityType receiverAuthority = AuthorityType.User)
        {
            if (!IsTemporalMate && (Owner.Invisible || Owner.InvisibleGm || Owner.IsVehicled || Owner.IsSeal || !IsAlive) && Owner.MapInstance.Map.MapId != 20001)
            {
                return "";
            }

            string name = IsUsingSp ? Sp.GetName() : Name.Replace(' ', '^');

            if (receiverAuthority >= AuthorityType.TMOD)
            {
                hideNickname = false;
                name = $"[{Owner.Faction}]{name}";
            }

            if (hideNickname)
            {
                name = "!§$%&/()=?*+~#";
            }

            int faction = isAct4 ? (byte)Owner.Faction + 2 : 0;

            return $"in 2 {NpcMonsterVNum} {MateTransportId} {PositionX} {PositionY} {Direction} {(int)(Hp / MaxHp * 100)} {(int)(Mp / MaxMp * 100)} 0 {faction} 3 {CharacterId} 1 0 {(IsUsingSp && Sp != null ? Sp.Instance.Item.Morph : (Skin != 0 ? Skin : -1))} {name} {(Sp != null ? 1 : 0)} {(IsUsingSp ? 1 : 0)} {(IsUsingSp ? 1 : 0)}{(IsUsingSp ? Sp.GenerateSkills(false) : " 0 0 0")} 0 0 0 0";
        }

        public string GenerateOut() => $"out 2 {MateTransportId}";

        public string GenerateRest(bool ownerSit)
        {
            IsSitting = ownerSit ? Owner.IsSitting : !IsSitting;
            return $"rest 2 {MateTransportId} {(IsSitting ? 1 : 0)}";
        }

        /// <summary>
        /// Get Stuff Buffs Useful for Stats for example
        /// </summary>
        /// <param name="type"></param>
        /// <param name="subtype"></param>
        /// <returns></returns>
        public int[] GetStuffBuff(CardType type, byte subtype)
        {
            List<BCard> EquipmentBCards = new List<BCard>();
            if (WeaponInstance != null)
            {
                EquipmentBCards.AddRange(WeaponInstance.Item.BCards);
            }
            if (ArmorInstance != null)
            {
                EquipmentBCards.AddRange(ArmorInstance.Item.BCards);
            }

            int value1 = 0;
            int value2 = 0;
            foreach (BCard entry in EquipmentBCards.Where(s => s.Type.Equals((byte)type) && s.SubType.Equals((byte)(subtype / 10)) && s.FirstData > 0))
            {
                if (entry.IsLevelScaled)
                {
                    value1 += entry.FirstData * Level;
                }
                else
                {
                    value1 += entry.FirstData;
                }
                value2 += entry.SecondData;
            }

            return new[] { value1, value2 };
        }

        int EquipmentFireResistance => GetStuffBuff(CardType.ElementResistance, (byte)AdditionalTypes.ElementResistance.FireIncreased)[0] + GetStuffBuff(CardType.ElementResistance, (byte)AdditionalTypes.ElementResistance.AllIncreased)[0];

        int EquipmentWaterResistance => GetStuffBuff(CardType.ElementResistance, (byte)AdditionalTypes.ElementResistance.WaterIncreased)[0] + GetStuffBuff(CardType.ElementResistance, (byte)AdditionalTypes.ElementResistance.AllIncreased)[0];

        int EquipmentLightResistance => GetStuffBuff(CardType.ElementResistance, (byte)AdditionalTypes.ElementResistance.LightIncreased)[0] + GetStuffBuff(CardType.ElementResistance, (byte)AdditionalTypes.ElementResistance.AllIncreased)[0];

        int EquipmentDarkResistance => GetStuffBuff(CardType.ElementResistance, (byte)AdditionalTypes.ElementResistance.DarkIncreased)[0] + GetStuffBuff(CardType.ElementResistance, (byte)AdditionalTypes.ElementResistance.AllIncreased)[0];
        
        public string GenerateScPacket()
        {
            if (IsTemporalMate)
            {
                return "";
            }

            double xp = XpLoad();

            if (xp > int.MaxValue)
            {
                xp = (int) (xp / 100);
            }

            switch (MateType)
            {
                case MateType.Partner:
                    return
                        $"sc_n " +
                        $"{PetId} " +
                        $"{NpcMonsterVNum} " +
                        $"{MateTransportId} " +
                        $"{Level} " +
                        $"{Loyalty} " +
                        $"{Experience} " +
                        $"{(WeaponInstance != null ? $"{WeaponInstance.ItemVNum}.{WeaponInstance.Rare}.{WeaponInstance.Upgrade}" : "-1")} " +
                        $"{(ArmorInstance != null ? $"{ArmorInstance.ItemVNum}.{ArmorInstance.Rare}.{ArmorInstance.Upgrade}" : "-1")} " +
                        $"{(GlovesInstance != null ? $"{GlovesInstance.ItemVNum}.0.0" : "-1")} " +
                        $"{(BootsInstance != null ? $"{BootsInstance.ItemVNum}.0.0" : "-1")} " +
                        $"0 0 1 " +
                        $"{WeaponInstance?.Upgrade ?? 0} " +
                        $"{DamageMinimum + (WeaponInstance?.Item.DamageMinimum ?? 0)} " +
                        $"{DamageMaximum + (WeaponInstance?.Item.DamageMaximum ?? 0)} " +
                        $"{Concentrate + (WeaponInstance?.Item.HitRate ?? 0)} " +
                        $"{Monster.CriticalChance + (WeaponInstance?.Item.CriticalLuckRate ?? 0)} " +
                        $"{Monster.CriticalRate + (WeaponInstance?.Item.CriticalRate ?? 0)} " +
                        $"{ArmorInstance?.Upgrade ?? 0} {Monster.CloseDefence + MeleeDefense + (ArmorInstance?.Item.CloseDefence ?? 0) + (GlovesInstance?.Item.CloseDefence ?? 0) + (BootsInstance?.Item.CloseDefence ?? 0)} " +
                        $"{MeleeDefenseDodge + (ArmorInstance?.Item.DefenceDodge ?? 0) + (GlovesInstance?.Item.DefenceDodge ?? 0) + (BootsInstance?.Item.DefenceDodge ?? 0)} " +
                        $"{RangeDefense + (ArmorInstance?.Item.DistanceDefence ?? 0) + (GlovesInstance?.Item.DistanceDefence ?? 0) + (BootsInstance?.Item.DistanceDefence ?? 0)} " +
                        $"{RangeDefenseDodge + (ArmorInstance?.Item.DistanceDefenceDodge ?? 0) + (GlovesInstance?.Item.DistanceDefenceDodge ?? 0) + (BootsInstance?.Item.DistanceDefenceDodge ?? 0)} " +
                        $"{MagicalDefense + (ArmorInstance?.Item.MagicDefence ?? 0) + (GlovesInstance?.Item.MagicDefence ?? 0) + (BootsInstance?.Item.MagicDefence ?? 0)} " +
                        $"{(IsUsingSp ? Sp.Instance.Item.Element : 0)} " +
                        $"{EquipmentFireResistance + Monster.FireResistance + (GlovesInstance?.FireResistance ?? 0) + (GlovesInstance?.Item.FireResistance ?? 0) + (BootsInstance?.FireResistance ?? 0) + (BootsInstance?.Item.FireResistance ?? 0)} " +
                        $"{EquipmentWaterResistance + Monster.WaterResistance + (GlovesInstance?.WaterResistance ?? 0) + (GlovesInstance?.Item.WaterResistance ?? 0) + (BootsInstance?.WaterResistance ?? 0) + (BootsInstance?.Item.WaterResistance ?? 0)} " +
                        $"{EquipmentLightResistance + Monster.LightResistance + (GlovesInstance?.LightResistance ?? 0) + (GlovesInstance?.Item.LightResistance ?? 0) + (BootsInstance?.LightResistance ?? 0) + (BootsInstance?.Item.LightResistance ?? 0)} " +
                        $"{EquipmentDarkResistance + Monster.DarkResistance + (GlovesInstance?.DarkResistance ?? 0) + (GlovesInstance?.Item.DarkResistance ?? 0) + (BootsInstance?.DarkResistance ?? 0) + (BootsInstance?.Item.DarkResistance ?? 0)} " +
                        $"{Hp} " +
                        $"{MaxHp} " +
                        $"{Mp} " +
                        $"{MaxMp} " +
                        $"0 " +
                        $"{xp} " +
                        $"{(IsUsingSp ? Sp.GetName() : Name.Replace(' ', '^'))} " +
                        $"{(IsUsingSp && Sp != null ? Sp.Instance.Item.Morph : Skin != 0 ? Skin : -1)} " +
                        $"{(IsSummonable ? 1 : 0)} " +
                        $"{(Sp != null ? $"{Sp.Instance.ItemVNum}.{Sp.GetXpPercent()}" : "-1")}" +
                        $"{(Sp != null ? Sp.GenerateSkills() : " -1 -1 -1")}";

                case MateType.Pet:
                    return
                        $"sc_p " +
                        $"{PetId} " +
                        $"{NpcMonsterVNum} " +
                        $"{MateTransportId} " +
                        $"{Level} " +
                        $"{Loyalty} " +
                        $"{Experience} " +
                        $"0 " +
                        $"{Attack} " +
                        $"{DamageMinimum} " +
                        $"{DamageMaximum} " +
                        $"{Concentrate} " +
                        $"{Monster.CriticalChance} " +
                        $"{Monster.CriticalRate} " +
                        $"{Defence} " +
                        $"{MeleeDefense} " +
                        $"{MeleeDefenseDodge} " +
                        $"{RangeDefense} " +
                        $"{RangeDefenseDodge} " +
                        $"{MagicalDefense} " +
                        $"{Monster.Element} " +
                        $"{Monster.FireResistance} " +
                        $"{Monster.WaterResistance} " +
                        $"{Monster.LightResistance} " +
                        $"{Monster.DarkResistance} " +
                        $"{Hp} " +
                        $"{MaxHp} " +
                        $"{Mp} " +
                        $"{MaxMp} " +
                        $"0 " +
                        $"{xp} " +
                        $"{(CanPickUp ? 1 : 0)} " +
                        $"{Name.Replace(' ', '^')} " +
                        $"{(IsSummonable ? 1 : 0)}";
            }

            return "";
        }

        public string GenerateStatInfo() => $"st 2 {MateTransportId} {Level} 0 {(int) (Hp / MaxHp * 100)} {(int) (Mp / MaxMp * 100)} {Hp} {Mp}{Buff.GetAllItems().Aggregate("", (current, buff) => current + $" {buff.Card.CardId}.{buff.Level}")}";

        public string GenerateBf(short cardId, int remainingTime, short level) => $"bf 2 {MateTransportId} 0.{cardId}.{remainingTime} {level}";

        public void AddBuff2(Buff indicator)
        {
            if (indicator?.Card != null)
            {
                Buff[indicator.Card.CardId] = indicator;
                indicator.RemainingTime = indicator.Card.Duration;
                indicator.Start = DateTime.UtcNow;

                indicator.Card.BCards.ForEach(c => c.ApplyBCards(BattleEntity, BattleEntity));
                Observable.Timer(TimeSpan.FromMilliseconds(indicator.Card.Duration * 100)).Subscribe(o =>
                {
                    RemoveBuff(indicator.Card.CardId);
                    if (indicator.Card.TimeoutBuff != 0
                        && ServerManager.RandomNumber() < indicator.Card.TimeoutBuffChance)
                    {
                        AddBuff2(new Buff(indicator.Card.TimeoutBuff, Monster.Level));
                    }
                });
                NoAttack |= indicator.Card.BCards.Any(s =>
                    s.Type == (byte)CardType.SpecialAttack
                    && s.SubType.Equals((byte)AdditionalTypes.SpecialAttack.NoAttack / 10));
                NoMove |= indicator.Card.BCards.Any(s =>
                    s.Type == (byte)CardType.Move
                    && s.SubType.Equals((byte)AdditionalTypes.Move.MovementImpossible / 10));
                Owner?.Session.SendPacket(GenerateBf(indicator.Card.CardId, indicator.RemainingTime, (short)(indicator.Level <= 0 ? (ValueType)Owner?.Level : indicator.Level)));
            }
        }
        public void GenerateXp(int xp)
        {
            if (Level < ServerManager.Instance.Configuration.MaxLevel)
            {
                Experience += (int)(xp * (1 + (Owner.Buff.ContainsKey(122) ? 0.5 : 0)));
                if (Experience >= XpLoad())
                {
                    if (Level + 1 < Owner.Level)
                    {
                        Experience = (long) (Experience - XpLoad());
                        Level++;
                        Hp = MaxHp;
                        Mp = MaxMp;
                        Owner.MapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Npc, MateTransportId, 6),
                            PositionX, PositionY);
                        Owner.MapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Npc, MateTransportId, 198),
                            PositionX, PositionY);
                        RefreshStats();
                    }
                }
            }

            Owner.Session.SendPacket(GenerateScPacket());
        }

        public List<ItemInstance> GetInventory()
        {
            return MateType == MateType.Pet ? new List<ItemInstance>() : Owner.Inventory.Where(s => s.Type == (InventoryType)(13 + PetId));
        }

        #region Stats Load

        public int HealthHpLoad()
        {
            int regen = GetBuff(CardType.Recovery, (byte)AdditionalTypes.Recovery.HPRecoveryIncreased)[0];
                //- GetBuff(CardType.Recovery, (byte)AdditionalTypes.Recovery.HPRecoveryDecreased)[0];
            return IsSitting ? regen + 50 :
                (DateTime.Now - LastDefence).TotalSeconds > 4 ? regen + 20 : 0;
        }

        public int HealthMpLoad()
        {
            int regen = GetBuff(CardType.Recovery, (byte)AdditionalTypes.Recovery.MPRecoveryIncreased)[0];
                //- GetBuff(CardType.Recovery, (byte)AdditionalTypes.Recovery.MPRecoveryDecreased)[0];
            return IsSitting ? regen + 50 :
                (DateTime.Now - LastDefence).TotalSeconds > 4 ? regen + 20 : 0;
        }

        public int HpLoad()
        {
            if (IsTemporalMate)
            {
                return Monster.MaxHP;
            }

            double multiplicator = 1.0;
            int hp = 0;

            multiplicator += GetBuff(CardType.BearSpirit, (byte)AdditionalTypes.BearSpirit.IncreaseMaximumHP)[0]
                             / 100D;
            multiplicator += GetBuff(CardType.MaxHPMP, (byte)AdditionalTypes.MaxHPMP.IncreasesMaximumHP)[0] / 100D;
            hp += GetBuff(CardType.MaxHPMP, (byte)AdditionalTypes.MaxHPMP.MaximumHPIncreased)[0];
            hp += GetBuff(CardType.MaxHPMP, (byte)AdditionalTypes.MaxHPMP.MaximumHPMPIncreased)[0];
            
            return (int)((MateHelper.Instance.HpData[GetMateType(), Level] + hp) * multiplicator);
        }

        public int MpLoad()
        {
            if (IsTemporalMate)
            {
                return Monster.MaxMP;
            }

            int mp = 0;
            double multiplicator = 1.0;
            multiplicator += GetBuff(CardType.BearSpirit, (byte)AdditionalTypes.BearSpirit.IncreaseMaximumMP)[0]
                             / 100D;
            multiplicator += GetBuff(CardType.MaxHPMP, (byte)AdditionalTypes.MaxHPMP.IncreasesMaximumMP)[0] / 100D;
            mp += GetBuff(CardType.MaxHPMP, (byte)AdditionalTypes.MaxHPMP.MaximumMPIncreased)[0];
            mp += GetBuff(CardType.MaxHPMP, (byte)AdditionalTypes.MaxHPMP.MaximumHPMPIncreased)[0];

            return (int)((MateHelper.Instance.MpData[GetMateType(), Level] + mp) * multiplicator);
        }

        public int ConcentrateLoad()
        {
            if (IsTemporalMate)
            {
                return Monster.Concentrate;
            }

            return MateHelper.Instance.Concentrate[GetMateType(), Level] / (MateType == MateType.Partner ? 3 : 1);
        }
        public int DamageMinimumLoad()
        {
            if (IsTemporalMate)
            {
                return Monster.DamageMinimum;
            }

            return MateHelper.Instance.MinDamageData[GetMateType(), Level] / (MateType == MateType.Partner ? 3 : 1);
        }
        public int DamageMaximumLoad()
        {
            if (IsTemporalMate)
            {
                return Monster.DamageMaximum;
            }

            return MateHelper.Instance.MaxDamageData[GetMateType(), Level] / (MateType == MateType.Partner ? 3 : 1);
        }
        public int MeleeDefenseLoad()
        {
            if (IsTemporalMate)
            {
                return Monster.CloseDefence;
            }

            return MateHelper.Instance.MeleeDefenseData[GetMateType(), Level] / (MateType == MateType.Partner ? 3 : 1);
        }
        public int MeleeDefenseDodgeLoad()
        {
            if (IsTemporalMate)
            {
                return Monster.DefenceDodge;
            }

            return MateHelper.Instance.MeleeDefenseDodgeData[GetMateType(), Level] / (MateType == MateType.Partner ? 3 : 1);
        }
        public int RangeDefenseLoad()
        {
            if (IsTemporalMate)
            {
                return Monster.DistanceDefence;
            }

            return MateHelper.Instance.RangeDefenseData[GetMateType(), Level] / (MateType == MateType.Partner ? 3 : 1);
        }
        public int RangeDefenseDodgeLoad()
        {
            if (IsTemporalMate)
            {
                return Monster.DistanceDefenceDodge;
            }

            return MateHelper.Instance.RangeDefenseDodgeData[GetMateType(), Level] / (MateType == MateType.Partner ? 3 : 1);
        }
        public int MagicalDefenseLoad()
        {
            if (IsTemporalMate)
            {
                return Monster.MagicDefence;
            }

            return MateHelper.Instance.MagicDefenseData[GetMateType(), Level] / (MateType == MateType.Partner ? 3 : 1);
        }

        #endregion

        public string GenerateRc(int characterHealth) => $"rc 2 {MateTransportId} {characterHealth} 0";

        /// <summary>
        /// Checks if the current character is in range of the given position
        /// </summary>
        /// <param name="xCoordinate">The x coordinate of the object to check.</param>
        /// <param name="yCoordinate">The y coordinate of the object to check.</param>
        /// <param name="range">The range of the coordinates to be maximal distanced.</param>
        /// <returns>True if the object is in Range, False if not.</returns>
        public bool IsInRange(int xCoordinate, int yCoordinate, int range) =>
            Math.Abs(PositionX - xCoordinate) <= range && Math.Abs(PositionY - yCoordinate) <= range;

        public void LoadInventory()
        {
            List<ItemInstance> inv = GetInventory();

            if (inv.Count == 0)
            {
                return;
            }

            WeaponInstance = inv.Find(s => s.Item.EquipmentSlot == EquipmentType.MainWeapon);
            ArmorInstance = inv.Find(s => s.Item.EquipmentSlot == EquipmentType.Armor);
            GlovesInstance = inv.Find(s => s.Item.EquipmentSlot == EquipmentType.Gloves);
            BootsInstance = inv.Find(s => s.Item.EquipmentSlot == EquipmentType.Boots);

            ItemInstance partnerSpInstance = inv.Find(s => s.Item.EquipmentSlot == EquipmentType.Sp);

            if (partnerSpInstance != null)
            {
                Sp = new PartnerSp(partnerSpInstance);
            }
        }

        public void BackToMiniland()
        {
            RemoveSp(true);
            ReviveDisposable?.Dispose();
            IsAlive = true;
            RemoveTeamMember();
            Owner.Session.SendPacket(Owner.GeneratePinit());
            Owner.MapInstance.Broadcast(GenerateOut());
        }

        public void GenerateRevive()
        {
            if (Owner == null || IsAlive)
            {
                return;
            }

            Owner.MapInstance?.Broadcast(GenerateOut());
            IsAlive = true;
            Hp = MaxHp / 2;
            Mp = MaxMp / 2;
            PositionY = (short)(Owner.PositionY + 1);
            PositionX = (short)(Owner.PositionX + 1);
            if (Owner.MapInstance.Map.IsBlockedZone(PositionX, PositionY))
            {
                PositionY = Owner.PositionY;
                PositionX = Owner.PositionX;
            }
            Parallel.ForEach(Owner.Session.CurrentMapInstance.Sessions.Where(s => s.Character != null), s =>
            {
                if (ServerManager.Instance.ChannelId != 51 || Owner.Session.Character.Faction == s.Character.Faction)
                {
                    s.SendPacket(GenerateIn(false, ServerManager.Instance.ChannelId == 51));
                }
                else
                {
                    s.SendPacket(GenerateIn(true, ServerManager.Instance.ChannelId == 51, s.Account.Authority));
                }
            });
            //Owner.Session.SendPacket(GenerateCond());
            Owner.Session.SendPacket(Owner.GeneratePinit());
            Owner.Session.SendPackets(Owner.GeneratePst());
            if (Loyalty <= 100)
            {
                Owner.Session.SendPacket(StaticPacketHelper.GenerateEff(UserType.Npc, MateTransportId, 5003));
            }
        }

        public void GenerateDeath(BattleEntity killer)
        {
            if (Hp > 0 || !IsAlive)
            {
                return;
            }

            IsAlive = false;
            BattleEntity.RemoveOwnedMonsters();
            DisableBuffs(BuffType.All);

            if (IsTemporalMate && Owner.Timespace != null)
            {
                return;
            }

            Owner.Session.SendPacket(GenerateScPacket());
            if (Loyalty > 0 && Loyalty - 50 <= 0)
            {
                Owner.Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("MATE_LOYALTY_ZERO"), 0));
                Owner.Session.SendPacket(Owner.GenerateSay(Language.Instance.GetMessageFromKey("MATE_LOYALTY_ZERO"), 11));
                Owner.Session.SendPacket(Owner.GenerateSay(Language.Instance.GetMessageFromKey("MATE_NEED_FEED"), 11));
            }
            if (MateType == MateType.Pet) Loyalty -= 50;
            if (Loyalty <= 0) Loyalty = 0;

            Owner.Session.SendPacket(GenerateScPacket());

            if (Owner.Session.CurrentMapInstance == Owner.Miniland)
            {
                GenerateRevive();
                return;
            }
            
            if (MateType == MateType.Pet ? Owner.IsPetAutoRelive : Owner.IsPartnerAutoRelive)
            {
                if (MateType == MateType.Pet)
                {
                    if (Owner.Inventory.CountItem(2089) > 0)
                    {
                        Owner.Inventory.RemoveItemAmount(2089, 1);
                        GenerateRevive();
                        return;
                    }
                    if (Owner.Inventory.CountItem(10016) > 0)
                    {
                        Owner.Inventory.RemoveItemAmount(10016, 1);
                        GenerateRevive();
                        return;
                    }
                }
                if (MateType == MateType.Partner)
                {
                    if (Owner.Inventory.CountItem(2329) > 0)
                    {
                        Owner.Inventory.RemoveItemAmount(2329, 1);
                        GenerateRevive();
                        return;
                    }
                    if (Owner.Inventory.CountItem(10050) > 0)
                    {
                        Owner.Inventory.RemoveItemAmount(10050, 1);
                        GenerateRevive();
                        return;
                    }
                }
                if (Owner.Inventory.CountItem(1012) < 5)
                {
                    Owner.Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                        string.Format(Language.Instance.GetMessageFromKey("NO_ITEM_REQUIRED"),
                            ServerManager.GetItem(1012).Name), 0));
                    if (MateType == MateType.Pet)
                    {
                        Owner.IsPetAutoRelive = false;
                    }
                    else
                    {
                        Owner.IsPartnerAutoRelive = false;
                    }
                }
                else
                {
                    Owner.Inventory.RemoveItemAmount(1012, 5);
                    ReviveDisposable = Observable.Timer(TimeSpan.FromMinutes(3)).Subscribe(s => GenerateRevive());
                    Owner.Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                        string.Format(Language.Instance.GetMessageFromKey("WILL_BE_BACK"), MateType), 0));
                    return;
                }
            }

            Owner.Session.SendPacket(
                UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("BACK_TO_MINILAND"), 0));
            BackToMiniland();
        }

        public void RefreshStats()
        {
            MateHelper.Instance.LoadConcentrate();
            MateHelper.Instance.LoadHpData();
            MateHelper.Instance.LoadMinDamageData();
            MateHelper.Instance.LoadMaxDamageData();
            MateHelper.Instance.LoadMpData();
            MateHelper.Instance.LoadXpData();
            MateHelper.Instance.LoadDefences();
        }

        public void AddTeamMember()
        {
            if (Owner.Mates.Any(m => m.IsTeamMember && m.MateType == MateType))
            {
                return;
            }
            IsTeamMember = true;
            IsAlive = true;
            Hp = MaxHp;
            Mp = MaxMp;
            Observable.Timer(TimeSpan.FromSeconds(2)).Subscribe(s =>
            {
                if (IsTeamMember)
                {
                    // Add Pet Buff
                    int cardId = -1;
                    if (MateHelper.Instance.MateBuffs.TryGetValue(NpcMonsterVNum, out cardId) &&
                        Owner.Buff.All(b => b.Card.CardId != cardId))
                    {
                        Owner.AddBuff(new Buff((short)cardId, Level, isPermaBuff: true), BattleEntity);
                    }
                }
            });
            foreach (NpcMonsterSkill skill in Monster.Skills.Where(sk => MateHelper.Instance.PetSkills.Contains(sk.SkillVNum)))
            {
                Owner.Session.SendPacket(Owner.GeneratePetskill(skill.SkillVNum));
            }
        }

        public void RemoveTeamMember(bool maxHpMp = true)
        {
            IsTeamMember = false;
            if (BattleEntity.MapInstance != Owner.Miniland)
            {
                Owner.Session.CurrentMapInstance.Broadcast(GenerateOut());
            }
            if (maxHpMp)
            {
                Hp = MaxHp;
                Mp = MaxMp;
            }
            // Remove Pet Buffs
            foreach (Buff mateBuff in Owner.BattleEntity.Buffs.Where(b =>
                MateHelper.Instance.MateBuffs.Values.Any(v => v == b.Card.CardId)))
            {
                Owner.RemoveBuff(mateBuff.Card.CardId, true);
            }
            Owner.Session.SendPacket(Owner.GeneratePetskill());
        }

        private double XpLoad()
        {
            try
            {
                return MateHelper.Instance.XpData[Level - 1];
            }
            catch
            {
                return 0;
            }
        }

        #endregion

        public void UpdateBushFire() => BattleEntity.UpdateBushFire();

        public void GetDamage(int damage, BattleEntity damager) => BattleEntity.GetDamage(damage, damager);

        private byte GetMateType(bool simple = false)
        {
            return Monster.AttackClass;
        }

        public string GenerateTp() => $"tp 2 {MateTransportId} {PositionX} {PositionY} 0";

        public void HitRequest(HitRequest hitRequest)
        {
            if (IsAlive && (hitRequest.Session?.Character == null || hitRequest.Session.Character.Hp > 0) && (hitRequest.Mate == null || hitRequest.Mate.Hp > 0))
            {
                double cooldownReduction = 0;

                if (hitRequest.Session?.Character != null)
                {
                    cooldownReduction = hitRequest.Session.Character.GetBuff(CardType.Morale, (byte)AdditionalTypes.Morale.SkillCooldownDecreased)[0];

                    int[] increaseEnemyCooldownChance = hitRequest.Session.Character.GetBuff(CardType.DarkCloneSummon, (byte)AdditionalTypes.DarkCloneSummon.IncreaseEnemyCooldownChance);

                    if (ServerManager.RandomNumber() < increaseEnemyCooldownChance[0])
                    {
                        cooldownReduction -= increaseEnemyCooldownChance[1];
                    }
                }

                int hitmode = 0;

                // calculate damage
                bool onyxWings = false;
                BattleEntity attackerBattleEntity = hitRequest.Mate != null
                    ? new BattleEntity(hitRequest.Mate)
                    : hitRequest.Session?.Character != null
                    ? new BattleEntity(hitRequest.Session.Character, hitRequest.Skill)
                    :  hitRequest.Monster != null 
                    ? new BattleEntity(hitRequest.Monster)
                    : null;

                if (attackerBattleEntity == null)
                {
                    return;
                }

                bool attackGreaterDistance = false;
                if (hitRequest.Skill != null && hitRequest.Skill.TargetType == 1 && hitRequest.Skill.HitType == 1 && hitRequest.Skill.TargetRange == 0 && hitRequest.Skill.Range > 0)
                {
                    attackGreaterDistance = true;
                }

                int damage = DamageHelper.Instance.CalculateDamage(attackerBattleEntity, new BattleEntity(this),
                hitRequest.Skill, ref hitmode, ref onyxWings, attackGreaterDistance);
                if (Monster.BCards.Find(s =>
                    s.Type == (byte)CardType.LightAndShadow &&
                    s.SubType == (byte)AdditionalTypes.LightAndShadow.InflictDamageToMP) is BCard card)
                {
                    int reduce = damage / 100 * card.FirstData;
                    if (Hp < reduce)
                    {
                        reduce = (int)Mp;
                        Mp = 0;
                    }
                    else
                    {
                        DecreaseMp(reduce);
                    }
                    damage -= reduce;
                }

                if (damage >= Hp &&
                    Monster.BCards.Any(s => s.Type == (byte)CardType.NoDefeatAndNoDamage && s.SubType == (byte)AdditionalTypes.NoDefeatAndNoDamage.DecreaseHPNoDeath / 10 && s.FirstData == -1))
                {
                    damage = (int)Hp - 1;
                }
                else if (onyxWings)
                {
                    short onyxX = (short)(attackerBattleEntity.PositionX + 2);
                    short onyxY = (short)(attackerBattleEntity.PositionY + 2);
                    int onyxId = BattleEntity.MapInstance.GetNextMonsterId();
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
                    BattleEntity.MapInstance.Broadcast(UserInterfaceHelper.GenerateGuri(31, 1,
                        attackerBattleEntity.MapEntityId, onyxX, onyxY));
                    onyx.Initialize(BattleEntity.MapInstance);
                    BattleEntity.MapInstance.AddMonster(onyx);
                    BattleEntity.MapInstance.Broadcast(onyx.GenerateIn());
                    BattleEntity.GetDamage(damage / 2, attackerBattleEntity);
                    var request = hitRequest;
                    var damage1 = damage;
                    Observable.Timer(TimeSpan.FromMilliseconds(350)).Subscribe(o =>
                    {
                        BattleEntity.MapInstance.Broadcast(StaticPacketHelper.SkillUsed(UserType.Monster, onyxId, (byte)BattleEntity.UserType,
                            BattleEntity.MapEntityId, -1, 0, -1, request.Skill?.Effect ?? 0, -1, -1, IsAlive, (int)(Hp / MaxHp * 100), damage1 / 2, 0,
                            0));
                        BattleEntity.MapInstance.RemoveMonster(onyx);
                        BattleEntity.MapInstance.Broadcast(StaticPacketHelper.Out(UserType.Monster, onyx.MapMonsterId));
                    });
                }

                attackerBattleEntity.BCards.Where(s => s.CastType == 1).ForEach(s =>
                {
                    if (s.Type != (byte)CardType.Buff && (hitRequest.TargetHitType != TargetHitType.AOETargetHit || s.Type != (byte)CardType.Summons && s.Type != (byte)CardType.SummonSkill))
                    {
                        s.ApplyBCards(BattleEntity, attackerBattleEntity);
                    }
                });

                hitRequest.SkillBCards.Where(s => !s.Type.Equals((byte)CardType.Buff) && (hitRequest.TargetHitType != TargetHitType.AOETargetHit || s.Type != (byte)CardType.Summons && s.Type != (byte)CardType.SummonSkill) && !s.Type.Equals((byte)CardType.Capture) && s.CardId == null).ToList()
                    .ForEach(s => s.ApplyBCards(BattleEntity, attackerBattleEntity));

                if (hitmode != 4 && hitmode != 2)
                {
                    if (damage > 0)
                    {
                        RemoveBuff(36);
                        RemoveBuff(548);
                    }

                    attackerBattleEntity.BCards.Where(s => s.CastType == 1).ForEach(s =>
                    {
                        if (s.Type == (byte)CardType.Buff)
                        {
                            Buff b = new Buff((short)s.SecondData, Monster.Level);
                            if (b.Card != null)
                            {
                                switch (b.Card?.BuffType)
                                {
                                    case BuffType.Bad:
                                        s.ApplyBCards(BattleEntity, attackerBattleEntity);
                                        break;

                                    case BuffType.Good:
                                    case BuffType.Neutral:
                                        s.ApplyBCards(attackerBattleEntity, attackerBattleEntity);
                                        break;
                                }
                            }
                        }
                    });

                    BattleEntity.BCards.Where(s => s.CastType == 0).ForEach(s =>
                    {
                        if (s.Type == (byte)CardType.Buff)
                        {
                            Buff b = new Buff((short)s.SecondData, BattleEntity.Level);
                            if (b.Card != null)
                            {
                                switch (b.Card?.BuffType)
                                {
                                    case BuffType.Bad:
                                        s.ApplyBCards(attackerBattleEntity, BattleEntity);
                                        break;

                                    case BuffType.Good:
                                    case BuffType.Neutral:
                                        s.ApplyBCards(BattleEntity, BattleEntity);
                                        break;
                                }
                            }
                        }
                    });

                    hitRequest.SkillBCards.Where(s => s.Type.Equals((byte)CardType.Buff) && new Buff((short)s.SecondData, attackerBattleEntity.Level).Card?.BuffType == BuffType.Bad).ToList()
                        .ForEach(s => s.ApplyBCards(BattleEntity, attackerBattleEntity));
                    
                    hitRequest.SkillBCards.Where(s => s.Type.Equals((byte)CardType.SniperAttack)).ToList()
                        .ForEach(s => s.ApplyBCards(BattleEntity, attackerBattleEntity));

                    if (attackerBattleEntity?.ShellWeaponEffects != null)
                    {
                        foreach (ShellEffectDTO shell in attackerBattleEntity.ShellWeaponEffects)
                        {
                            switch (shell.Effect)
                            {
                                case (byte)ShellWeaponEffectType.Blackout:
                                    {
                                        Buff buff = new Buff(7, attackerBattleEntity.Level);
                                        if (ServerManager.RandomNumber() < shell.Value)
                                        {
                                            AddBuff(buff, attackerBattleEntity);
                                        }

                                        break;
                                    }
                                case (byte)ShellWeaponEffectType.DeadlyBlackout:
                                    {
                                        Buff buff = new Buff(66, attackerBattleEntity.Level);
                                        if (ServerManager.RandomNumber() < shell.Value)
                                        {
                                            AddBuff(buff, attackerBattleEntity);
                                        }

                                        break;
                                    }
                                case (byte)ShellWeaponEffectType.MinorBleeding:
                                    {
                                        Buff buff = new Buff(1, attackerBattleEntity.Level);
                                        if (ServerManager.RandomNumber() < shell.Value)
                                        {
                                            AddBuff(buff, attackerBattleEntity);
                                        }

                                        break;
                                    }
                                case (byte)ShellWeaponEffectType.Bleeding:
                                    {
                                        Buff buff = new Buff(21, attackerBattleEntity.Level);
                                        if (ServerManager.RandomNumber() < shell.Value)
                                        {
                                            AddBuff(buff, attackerBattleEntity);
                                        }

                                        break;
                                    }
                                case (byte)ShellWeaponEffectType.HeavyBleeding:
                                    {
                                        Buff buff = new Buff(42, attackerBattleEntity.Level);
                                        if (ServerManager.RandomNumber() < shell.Value)
                                        {
                                            AddBuff(buff, attackerBattleEntity);
                                        }

                                        break;
                                    }
                                case (byte)ShellWeaponEffectType.Freeze:
                                    {
                                        Buff buff = new Buff(27, attackerBattleEntity.Level);
                                        if (ServerManager.RandomNumber() < shell.Value)
                                        {
                                            AddBuff(buff, attackerBattleEntity);
                                        }

                                        break;
                                    }
                            }
                        }
                    }
                }

                BattleEntity.GetDamage(damage, attackerBattleEntity);

                if (IsSitting)
                {
                    Owner.MapInstance.Broadcast(GenerateRest(false));
                }

                if (attackerBattleEntity.MapMonster?.DamageList != null)
                {
                    lock (attackerBattleEntity.MapMonster)
                    {
                        lock (attackerBattleEntity.MapMonster.DamageList)
                        {
                            if (!attackerBattleEntity.MapMonster.DamageList.Any(s => s.Key.MapEntityId == BattleEntity.MapEntityId))
                            {
                                attackerBattleEntity.MapMonster.AddToAggroList(BattleEntity);
                            }
                        }
                    }
                }

                if (hitmode != 2)
                {
                    if (hitRequest.Skill != null)
                    {
                        switch (hitRequest.TargetHitType)
                        {
                            case TargetHitType.SingleTargetHit:
                                BattleEntity.MapInstance?.Broadcast(StaticPacketHelper.SkillUsed(attackerBattleEntity.UserType,
                                    attackerBattleEntity.MapEntityId, (byte)BattleEntity.UserType, BattleEntity.MapEntityId,
                                    hitRequest.Skill.SkillVNum, (short)(hitRequest.Skill.Cooldown + hitRequest.Skill.Cooldown * cooldownReduction / 100D),
                                    hitRequest.Skill.AttackAnimation, hitRequest.SkillEffect,
                                    attackerBattleEntity.PositionX, attackerBattleEntity.PositionY,
                                    IsAlive, (int)(Hp / MaxHp * 100), damage, hitmode,
                                    (byte)(hitRequest.Skill.SkillType - 1)));
                                break;

                            case TargetHitType.SingleTargetHitCombo:
                                BattleEntity.MapInstance?.Broadcast(StaticPacketHelper.SkillUsed(attackerBattleEntity.UserType,
                                    attackerBattleEntity.MapEntityId, (byte)BattleEntity.UserType, BattleEntity.MapEntityId,
                                    hitRequest.Skill.SkillVNum, (short)(hitRequest.Skill.Cooldown + hitRequest.Skill.Cooldown * cooldownReduction / 100D),
                                    hitRequest.SkillCombo.Animation, hitRequest.SkillCombo.Effect,
                                    attackerBattleEntity.PositionX, attackerBattleEntity.PositionY,
                                    IsAlive, (int)(Hp / MaxHp * 100), damage, hitmode,
                                    (byte)(hitRequest.Skill.SkillType - 1)));
                                break;

                            case TargetHitType.SingleAOETargetHit:
                                if (hitRequest.ShowTargetHitAnimation)
                                {
                                    if (hitRequest.Session?.Character != null)
                                    {
                                        if (hitRequest.Skill.SkillVNum == 1085 || hitRequest.Skill.SkillVNum == 1091 || hitRequest.Skill.SkillVNum == 1060)
                                        {
                                            attackerBattleEntity.PositionX = PositionX;
                                            attackerBattleEntity.PositionY = PositionY;
                                            attackerBattleEntity.MapInstance?.Broadcast(hitRequest.Session.Character.GenerateTp());
                                        }
                                    }
                                    BattleEntity.MapInstance?.Broadcast(StaticPacketHelper.SkillUsed(attackerBattleEntity.UserType,
                                        attackerBattleEntity.MapEntityId, (byte)BattleEntity.UserType, BattleEntity.MapEntityId,
                                        hitRequest.Skill.SkillVNum, (short)(hitRequest.Skill.Cooldown + hitRequest.Skill.Cooldown * cooldownReduction / 100D),
                                        hitRequest.Skill.AttackAnimation, hitRequest.SkillEffect,
                                        attackerBattleEntity.PositionX, attackerBattleEntity.PositionY,
                                        IsAlive, (int)(Hp / MaxHp * 100), damage, hitmode,
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

                                    BattleEntity.MapInstance?.Broadcast(StaticPacketHelper.SkillUsed(attackerBattleEntity.UserType,
                                        attackerBattleEntity.MapEntityId, (byte)BattleEntity.UserType, BattleEntity.MapEntityId,
                                        -1, (short)(hitRequest.Skill.Cooldown + hitRequest.Skill.Cooldown * cooldownReduction / 100D),
                                        hitRequest.Skill.AttackAnimation, hitRequest.SkillEffect,
                                        attackerBattleEntity.PositionX, attackerBattleEntity.PositionY,
                                        IsAlive, (int)(Hp / MaxHp * 100), damage, hitmode,
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

                                BattleEntity.MapInstance?.Broadcast(StaticPacketHelper.SkillUsed(attackerBattleEntity.UserType,
                                    attackerBattleEntity.MapEntityId, (byte)BattleEntity.UserType, BattleEntity.MapEntityId,
                                    hitRequest.Skill.SkillVNum, (short)(hitRequest.Skill.Cooldown + hitRequest.Skill.Cooldown * cooldownReduction / 100D),
                                    hitRequest.Skill.AttackAnimation, hitRequest.SkillEffect,
                                    attackerBattleEntity.PositionX, attackerBattleEntity.PositionY,
                                    IsAlive, (int)(Hp / MaxHp * 100), damage, hitmode,
                                    (byte)(hitRequest.Skill.SkillType - 1)));
                                break;

                            case TargetHitType.ZoneHit:
                                BattleEntity.MapInstance?.Broadcast(StaticPacketHelper.SkillUsed(attackerBattleEntity.UserType,
                                    attackerBattleEntity.MapEntityId, (byte)BattleEntity.UserType, BattleEntity.MapEntityId,
                                    hitRequest.Skill.SkillVNum, (short)(hitRequest.Skill.Cooldown + hitRequest.Skill.Cooldown * cooldownReduction / 100D),
                                    hitRequest.Skill.AttackAnimation, hitRequest.SkillEffect, hitRequest.MapX,
                                    hitRequest.MapY, IsAlive, (int)(Hp / MaxHp * 100), damage, hitmode,
                                    (byte)(hitRequest.Skill.SkillType - 1)));
                                break;

                            case TargetHitType.SpecialZoneHit:
                                BattleEntity.MapInstance?.Broadcast(StaticPacketHelper.SkillUsed(attackerBattleEntity.UserType,
                                    attackerBattleEntity.MapEntityId, (byte)BattleEntity.UserType, BattleEntity.MapEntityId,
                                    hitRequest.Skill.SkillVNum, (short)(hitRequest.Skill.Cooldown + hitRequest.Skill.Cooldown * cooldownReduction / 100D),
                                    hitRequest.Skill.AttackAnimation, hitRequest.SkillEffect,
                                    attackerBattleEntity.PositionX, attackerBattleEntity.PositionY,
                                    IsAlive, (int)(Hp / MaxHp * 100), damage, hitmode,
                                    (byte)(hitRequest.Skill.SkillType - 1)));
                                break;
                        }
                    }
                    else
                    {
                        BattleEntity.MapInstance?.Broadcast(StaticPacketHelper.SkillUsed(attackerBattleEntity.UserType,
                            attackerBattleEntity.MapEntityId, (byte)BattleEntity.UserType, BattleEntity.MapEntityId,
                            0, (short)(hitRequest.Mate != null ? hitRequest.Mate.Monster.BasicCooldown : 12), 11, (short)(hitRequest.Mate != null ? hitRequest.Mate.Monster.BasicSkill : 12), 0, 0, IsAlive,
                            (int)(Hp / MaxHp * 100), damage, hitmode, 0));
                    }
                }
                else
                {
                    hitRequest.Session?.SendPacket(StaticPacketHelper.Cancel(2, BattleEntity.MapEntityId));
                }

                if (attackerBattleEntity.Character != null)
                {
                    if (hitmode != 4 && hitmode != 2 && damage > 0)
                    {
                        attackerBattleEntity.Character.RemoveBuffByBCardTypeSubType(new List<KeyValuePair<byte, byte>>
                        {
                            new KeyValuePair<byte, byte>((byte)CardType.SpecialActions, (byte)AdditionalTypes.SpecialActions.Hide)
                        });
                    }

                    if (attackerBattleEntity.HasBuff(CardType.FalconSkill, (byte)AdditionalTypes.FalconSkill.Hide))
                    {
                        attackerBattleEntity.Character.RemoveBuffByBCardTypeSubType(new List<KeyValuePair<byte, byte>>
                        {
                            new KeyValuePair<byte, byte>((byte)CardType.FalconSkill, (byte)AdditionalTypes.FalconSkill.Hide)
                        });
                        attackerBattleEntity.AddBuff(new Buff(560, attackerBattleEntity.Level), attackerBattleEntity);
                    }
                    if (Hp <= 0)
                    {
                        if (hitRequest.SkillBCards.FirstOrDefault(s => s.Type == (byte)CardType.TauntSkill && s.SubType == (byte)AdditionalTypes.TauntSkill.EffectOnKill / 10) is BCard EffectOnKill)
                        {
                            if (ServerManager.RandomNumber() < EffectOnKill.FirstData)
                            {
                                attackerBattleEntity.AddBuff(new Buff((short)EffectOnKill.SecondData, attackerBattleEntity.Level), attackerBattleEntity);
                            }
                        }
                    }
                }
            }
            else
            {
                hitRequest.Session?.SendPacket(StaticPacketHelper.Cancel(2, BattleEntity.MapEntityId));
            }
        }

        /// <summary>
        /// Hit the Target.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="npcMonsterSkill"></param>
        public void TargetHit(BattleEntity target, NpcMonsterSkill npcMonsterSkill)
        {
            if (Monster != null && !HasBuff(CardType.SpecialAttack, (byte)AdditionalTypes.SpecialAttack.NoAttack) && BattleEntity.CanAttackEntity(target))
            {
                if (npcMonsterSkill != null)
                {
                    if (BattleEntity.Mp < npcMonsterSkill.Skill.MpCost)
                    {
                        return;
                    }

                    if (!npcMonsterSkill.CanBeUsed())
                    {
                        return;
                    }

                    if (npcMonsterSkill.Skill.TargetType == 0 && BattleEntity.GetDistance(target) > npcMonsterSkill.Skill.Range)
                    {
                        return;
                    }

                    npcMonsterSkill.LastSkillUse = DateTime.Now;

                    DecreaseMp(npcMonsterSkill.Skill.MpCost);

                    BattleEntity.MapInstance?.Broadcast(StaticPacketHelper.CastOnTarget(BattleEntity.UserType, BattleEntity.MapEntityId, target.UserType, target.MapEntityId,
                        npcMonsterSkill.Skill.CastAnimation, npcMonsterSkill.Skill.CastEffect,
                        npcMonsterSkill.Skill.SkillVNum));
                }
                else
                {
                    if (!CanUseBasicSkill())
                    {
                        return;
                    }

                    if (BattleEntity.GetDistance(target) > Monster.BasicRange)
                    {
                        return;
                    }

                    LastBasicSkillUse = DateTime.Now;
                }

                Owner.Session.SendPacket(StaticPacketHelper.GenerateEff(UserType.Npc, MateTransportId, 5005));

                LastSkillUse = DateTime.Now;

                int hitmode = 0;
                bool onyxWings = false;
                BattleEntity targetEntity = null;
                switch (target.EntityType)
                {
                    case EntityType.Player:
                        targetEntity = new BattleEntity(target.Character, null);
                        break;
                    case EntityType.Mate:
                        targetEntity = new BattleEntity(target.Mate);
                        break;
                    case EntityType.Monster:
                        targetEntity = new BattleEntity(target.MapMonster);
                        break;
                    case EntityType.Npc:
                        targetEntity = new BattleEntity(target.MapNpc);
                        break;
                }

                int damage = DamageHelper.Instance.CalculateDamage(new BattleEntity(this),
                    targetEntity, npcMonsterSkill?.Skill, ref hitmode,
                    ref onyxWings);

                // deal 0 damage to GM with GodMode
                if (target.Character != null && target.Character.HasGodMode || target.Mate != null && target.Mate.Owner.HasGodMode)
                {
                    damage = 0;
                }

                if (target.Character != null)
                {
                    if (ServerManager.RandomNumber() < target.Character.GetBuff(CardType.DarkCloneSummon,
                        (byte)AdditionalTypes.DarkCloneSummon.ConvertDamageToHPChance)[0])
                    {
                        int amount = damage;

                        target.Character.ConvertedDamageToHP += amount;
                        target.Character.MapInstance?.Broadcast(target.Character.GenerateRc(amount));
                        target.Character.Hp += amount;

                        if (target.Character.Hp > target.Character.HPLoad())
                        {
                            target.Character.Hp = (int)target.Character.HPLoad();
                        }

                        target.Character.Session?.SendPacket(target.Character.GenerateStat());

                        damage = 0;
                    }
                }

                int[] manaShield = target.GetBuff(CardType.LightAndShadow,
                    (byte)AdditionalTypes.LightAndShadow.InflictDamageToMP);
                if (manaShield[0] != 0 && hitmode != 4)
                {
                    int reduce = damage / 100 * manaShield[0];
                    if (target.Mp < reduce)
                    {
                        reduce = target.Mp;
                        target.Mp = 0;
                    }
                    else
                    {
                        target.DecreaseMp(reduce);
                    }
                    damage -= reduce;
                }

                if (target.Character != null && target.Character.IsSitting)
                {
                    target.Character.IsSitting = false;
                    BattleEntity.MapInstance?.Broadcast(target.Character.GenerateRest());
                }

                int castTime = 0;
                if (npcMonsterSkill != null && npcMonsterSkill.Skill.CastEffect != 0)
                {
                    BattleEntity.MapInstance?.Broadcast(
                        StaticPacketHelper.GenerateEff(BattleEntity.UserType, BattleEntity.MapEntityId,
                            npcMonsterSkill.Skill.CastEffect), MapX, MapY);
                    castTime = npcMonsterSkill.Skill.CastTime * 100;
                }

                Observable.Timer(TimeSpan.FromMilliseconds(castTime)).Subscribe(o =>
                {
                    if (target.Hp > 0)
                    {
                        TargetHit2(target, npcMonsterSkill, damage, hitmode);
                    }
                });
            }
        }

        public void TargetHit2(BattleEntity target, NpcMonsterSkill npcMonsterSkill, int damage, int hitmode)
        {
            List<BCard> bCards = new List<BCard>();
            bCards.AddRange(Monster.BCards.ToList());
            if (npcMonsterSkill != null)
            {
                bCards.AddRange(npcMonsterSkill.Skill.BCards.ToList());
            }

            lock (target.PVELockObject)
            {
                if (target.Hp > 0 && target.MapInstance == BattleEntity.MapInstance)
                {
                    if (target.MapMonster != null)
                    {
                        target.MapMonster.HitQueue.Enqueue(
                            new HitRequest(TargetHitType.SingleTargetHit, Owner.Session, this, npcMonsterSkill));
                    }
                    else if (target.Mate != null)
                    {
                        target.Mate.HitRequest(new HitRequest(TargetHitType.SingleTargetHit, Owner.Session, this, npcMonsterSkill));
                    }
                    else
                    {
                        if (damage >= target.Hp &&
                            Monster.BCards.Any(s => s.Type == (byte)CardType.NoDefeatAndNoDamage && s.SubType == (byte)AdditionalTypes.NoDefeatAndNoDamage.DecreaseHPNoKill / 10 && s.FirstData == 1))
                        {
                            damage = target.Hp - 1;
                        }

                        target.GetDamage(damage, BattleEntity);

                        if (target.Character != null)
                        {
                            BattleEntity.MapInstance.Broadcast(null, target.Character.GenerateStat(), ReceiverType.OnlySomeone,
                                "", target.MapEntityId);
                        }
                        if (target.Mate != null)
                        {
                            target.Mate.Owner.Session.SendPacket(target.Mate.Owner.GeneratePst().FirstOrDefault(s => s.Contains(target.Mate.MateTransportId.ToString())));
                        }
                        if (target.MapMonster != null && Owner != null)
                        {
                            target.MapMonster.AddToDamageList(Owner.BattleEntity, damage);
                        }
                        BattleEntity.MapInstance.Broadcast(npcMonsterSkill != null
                            ? StaticPacketHelper.SkillUsed(BattleEntity.UserType, BattleEntity.MapEntityId, (byte)target.UserType, target.MapEntityId,
                                npcMonsterSkill.SkillVNum, npcMonsterSkill.Skill.Cooldown,
                                npcMonsterSkill.Skill.AttackAnimation, npcMonsterSkill.Skill.Effect, MapX, MapY,
                                target.Hp > 0,
                                (int)(target.Hp / target.HPLoad() * 100), damage,
                                hitmode, 0)
                            : StaticPacketHelper.SkillUsed(BattleEntity.UserType, BattleEntity.MapEntityId, (byte)target.UserType, target.MapEntityId, 0,
                                Monster.BasicCooldown, 11, Monster.BasicSkill, 0, 0, target.Hp > 0,
                                (int)(target.Hp / target.HPLoad() * 100), damage,
                                hitmode, 0));

                        if (hitmode != 4 && hitmode != 2)
                        {
                            bCards.Where(s => s.CastType == 1 || s.SkillVNum != null).ToList().ForEach(s =>
                            {
                                if (s.Type != (byte)CardType.Buff)
                                {
                                    s.ApplyBCards(target, BattleEntity);
                                }
                            });

                            bCards.Where(s => s.CastType == 1 || s.SkillVNum != null).ToList().ForEach(s =>
                            {
                                if (s.Type == (byte)CardType.Buff)
                                {
                                    Buff b = new Buff((short)s.SecondData, Monster.Level);
                                    if (b.Card != null)
                                    {
                                        switch (b.Card?.BuffType)
                                        {
                                            case BuffType.Bad:
                                                s.ApplyBCards(target, BattleEntity);
                                                break;

                                            case BuffType.Good:
                                            case BuffType.Neutral:
                                                s.ApplyBCards(BattleEntity, BattleEntity);
                                                break;
                                        }
                                    }
                                }
                            });

                            target.BCards.Where(s => s.CastType == 0).ForEach(s =>
                            {
                                if (s.Type == (byte)CardType.Buff)
                                {
                                    Buff b = new Buff((short)s.SecondData, BattleEntity.Level);
                                    if (b.Card != null)
                                    {
                                        switch (b.Card?.BuffType)
                                        {
                                            case BuffType.Bad:
                                                s.ApplyBCards(BattleEntity, target);
                                                break;

                                            case BuffType.Good:
                                            case BuffType.Neutral:
                                                s.ApplyBCards(target, target);
                                                break;
                                        }
                                    }
                                }
                            });

                            if (damage > 0)
                            {
                                target.Character?.RemoveBuffByBCardTypeSubType(new List<KeyValuePair<byte, byte>>
                                {
                                    new KeyValuePair<byte, byte>((byte)CardType.SpecialActions, (byte)AdditionalTypes.SpecialActions.Hide)
                                });
                                target.RemoveBuff(36);
                                target.RemoveBuff(548);
                            }
                        }
                        if (target.Hp <= 0)
                        {
                            if (target.Character != null)
                            {
                                if (target.Character.IsVehicled)
                                {
                                    target.Character.RemoveVehicle();
                                }
                                Owner.BattleEntity.ApplyScoreArena(target);
                                Owner.MapInstance?.Broadcast(Owner.GenerateSay(
                                    string.Format(Language.Instance.GetMessageFromKey("PVP_KILL"),
                                        Owner.Name, target.Character.Name), 10));
                                Observable.Timer(TimeSpan.FromMilliseconds(1000)).Subscribe(o =>
                                    ServerManager.Instance.AskPvpRevive((long)target.Character?.CharacterId));
                            }
                            else if (target.MapNpc != null)
                            {
                                target.MapNpc.RunDeathEvent();
                            }
                        }
                    }
                }
            }

            // In range entities

            short RangeBaseX = target.PositionX;
            short RangeBaseY = target.PositionY;

            if (npcMonsterSkill != null && npcMonsterSkill.Skill.HitType == 1 && npcMonsterSkill.Skill.TargetType == 1)
            {
                RangeBaseX = MapX;
                RangeBaseY = MapY;
            }

            if (npcMonsterSkill != null && (npcMonsterSkill.Skill.Range > 0 || npcMonsterSkill.Skill.TargetRange > 0))
            {
                bool onyxWings = false;
                foreach (Character characterInRange in BattleEntity.MapInstance
                    .GetCharactersInRange(
                        npcMonsterSkill.Skill.TargetRange == 0 ? MapX : RangeBaseX,
                        npcMonsterSkill.Skill.TargetRange == 0 ? MapY : RangeBaseY,
                        npcMonsterSkill.Skill.TargetRange).Where(s => s.CharacterId != target.MapEntityId))
                {
                    if (!BattleEntity.CanAttackEntity(characterInRange.BattleEntity))
                    {
                        npcMonsterSkill.Skill.BCards.Where(s => s.Type == (byte)CardType.Buff).ToList().ForEach(s =>
                        {
                            if (new Buff((short)s.SecondData, Monster.Level) is Buff b)
                            {
                                switch (b.Card?.BuffType)
                                {
                                    case BuffType.Good:
                                    case BuffType.Neutral:
                                        s.ApplyBCards(characterInRange.BattleEntity, BattleEntity);
                                        break;
                                }
                            }
                        });
                    }
                    else
                    {
                        if (characterInRange.IsSitting)
                        {
                            characterInRange.IsSitting = false;
                            BattleEntity.MapInstance.Broadcast(characterInRange.GenerateRest());
                        }

                        if (characterInRange.HasGodMode)
                        {
                            damage = 0;
                            hitmode = 4;
                        }

                        if (characterInRange.Hp > 0)
                        {
                            int dmg = DamageHelper.Instance.CalculateDamage(BattleEntity, characterInRange.BattleEntity, npcMonsterSkill.Skill, ref hitmode, ref onyxWings);
                            if (dmg >= characterInRange.Hp &&
                                Monster.BCards.Any(s => s.Type == (byte)CardType.NoDefeatAndNoDamage && s.SubType == (byte)AdditionalTypes.NoDefeatAndNoDamage.DecreaseHPNoKill / 10 && s.FirstData == 1))
                            {
                                dmg = characterInRange.Hp - 1;
                            }

                            if (hitmode != 4 && hitmode != 2)
                            {
                                bCards.Where(s => s.CastType == 1 || s.SkillVNum != null).ToList().ForEach(s =>
                                {
                                    if (s.Type != (byte)CardType.Buff)
                                    {
                                        s.ApplyBCards(characterInRange.BattleEntity, BattleEntity);
                                    }
                                });

                                if (dmg > 0)
                                {
                                    characterInRange.RemoveBuff(36);
                                    characterInRange.RemoveBuff(548);
                                }

                                bCards.Where(s => s.CastType == 1 || s.SkillVNum != null).ToList().ForEach(s =>
                                {
                                    if (s.Type == (byte)CardType.Buff)
                                    {
                                        Buff b = new Buff((short)s.SecondData, Monster.Level);
                                        if (b.Card != null)
                                        {
                                            switch (b.Card?.BuffType)
                                            {
                                                case BuffType.Bad:
                                                    s.ApplyBCards(characterInRange.BattleEntity, BattleEntity);
                                                    break;

                                                case BuffType.Good:
                                                case BuffType.Neutral:
                                                    s.ApplyBCards(BattleEntity, BattleEntity);
                                                    break;
                                            }
                                        }
                                    }
                                });

                                characterInRange.BattleEntity.BCards.Where(s => s.CastType == 0).ForEach(s =>
                                {
                                    if (s.Type == (byte)CardType.Buff)
                                    {
                                        Buff b = new Buff((short)s.SecondData, BattleEntity.Level);
                                        if (b.Card != null)
                                        {
                                            switch (b.Card?.BuffType)
                                            {
                                                case BuffType.Bad:
                                                    s.ApplyBCards(BattleEntity, characterInRange.BattleEntity);
                                                    break;

                                                case BuffType.Good:
                                                case BuffType.Neutral:
                                                    s.ApplyBCards(characterInRange.BattleEntity, characterInRange.BattleEntity);
                                                    break;
                                            }
                                        }
                                    }
                                });
                            }

                            characterInRange.GetDamage(dmg, BattleEntity);
                            BattleEntity.MapInstance.Broadcast(null, characterInRange.GenerateStat(), ReceiverType.OnlySomeone,
                                "", characterInRange.CharacterId);

                            BattleEntity.MapInstance.Broadcast(npcMonsterSkill != null
                                ? StaticPacketHelper.SkillUsed(BattleEntity.UserType, BattleEntity.MapEntityId, (byte)UserType.Player, characterInRange.CharacterId,
                                    npcMonsterSkill.SkillVNum, npcMonsterSkill.Skill.Cooldown,
                                    npcMonsterSkill.Skill.AttackAnimation, npcMonsterSkill.Skill.Effect, MapX, MapY,
                                    characterInRange.Hp > 0,
                                    (int)(characterInRange.Hp / characterInRange.HPLoad() * 100), dmg,
                                    hitmode, 0)
                                : StaticPacketHelper.SkillUsed(BattleEntity.UserType, BattleEntity.MapEntityId, (byte)UserType.Player, characterInRange.CharacterId, 0,
                                    Monster.BasicCooldown, 11, Monster.BasicSkill, 0, 0, characterInRange.Hp > 0,
                                    (int)(characterInRange.Hp / characterInRange.HPLoad() * 100), dmg,
                                    hitmode, 0));

                            if (hitmode != 4 && hitmode != 2)
                            {
                                characterInRange.RemoveBuffByBCardTypeSubType(new List<KeyValuePair<byte, byte>>
                            {
                                new KeyValuePair<byte, byte>((byte)CardType.SpecialActions, (byte)AdditionalTypes.SpecialActions.Hide)
                            });
                            }
                            if (characterInRange.Hp <= 0)
                            {
                                if (characterInRange.IsVehicled)
                                {
                                    characterInRange.RemoveVehicle();
                                }
                                if (Owner.Session.CurrentMapInstance?.MapInstanceType != MapInstanceType.TalentArenaMapInstance)
                                {
                                    Owner.BattleEntity.ApplyScoreArena(characterInRange.BattleEntity);
                                    Owner.Session.CurrentMapInstance?.Broadcast(Owner.Session.Character.GenerateSay(
                                    string.Format(Language.Instance.GetMessageFromKey("PVP_KILL"),
                                        Owner.Session.Character.Name, target.Character.Name), 10));
                                    Observable.Timer(TimeSpan.FromMilliseconds(1000)).Subscribe(o =>
                                        ServerManager.Instance.AskPvpRevive(target.Character.CharacterId));
                                }
                                else
                                {
                                    Owner.Session.CurrentMapInstance?.Broadcast(Owner.Session.Character.GenerateSay(
                                    string.Format(Language.Instance.GetMessageFromKey("ADTPVP_KILL"),
                                        Owner.Session.Character.Name, target.Character.Name), 10));
                                    Observable.Timer(TimeSpan.FromMilliseconds(1000)).Subscribe(o =>
                                       ServerManager.Instance.AskPvpRevive(target.Character.CharacterId));
                                }                                                              
                            }
                        }
                    }

                    foreach (Mate mateInRange in BattleEntity.MapInstance
                        .GetListMateInRange(npcMonsterSkill.Skill.TargetRange == 0 ? BattleEntity.PositionX : RangeBaseX, npcMonsterSkill.Skill.TargetRange == 0 ? BattleEntity.PositionY : RangeBaseY,
                            npcMonsterSkill.Skill.TargetRange))
                    {
                        if (!BattleEntity.CanAttackEntity(mateInRange.BattleEntity))
                        {
                            npcMonsterSkill.Skill.BCards.Where(s => s.Type == (byte)CardType.Buff).ToList().ForEach(s =>
                            {
                                if (new Buff((short)s.SecondData, Monster.Level) is Buff b)
                                {
                                    switch (b.Card?.BuffType)
                                    {
                                        case BuffType.Good:
                                        case BuffType.Neutral:
                                            s.ApplyBCards(mateInRange.BattleEntity, BattleEntity);
                                            break;
                                    }
                                }
                            });
                        }
                        else
                        {
                            mateInRange.HitRequest(new HitRequest(TargetHitType.AOETargetHit, Owner.Session, this, npcMonsterSkill));
                        }
                    }

                    foreach (MapMonster monsterInRange in BattleEntity.MapInstance
                        .GetMonsterInRangeList(
                            npcMonsterSkill.Skill.TargetRange == 0 ? BattleEntity.PositionX : RangeBaseX,
                            npcMonsterSkill.Skill.TargetRange == 0 ? BattleEntity.PositionY : RangeBaseY,
                            npcMonsterSkill.Skill.TargetRange).Where(s => s.MapMonsterId != target.MapEntityId))
                    {
                        if (!BattleEntity.CanAttackEntity(monsterInRange.BattleEntity))
                        {
                            npcMonsterSkill.Skill.BCards.Where(s => s.Type == (byte)CardType.Buff).ToList().ForEach(s =>
                            {
                                if (new Buff((short)s.SecondData, Monster.Level) is Buff b)
                                {
                                    switch (b.Card?.BuffType)
                                    {
                                        case BuffType.Good:
                                        case BuffType.Neutral:
                                            s.ApplyBCards(monsterInRange.BattleEntity, BattleEntity);
                                            break;
                                    }
                                }
                            });
                        }
                        else
                        {
                            monsterInRange.HitQueue.Enqueue(new HitRequest(TargetHitType.AOETargetHit, Owner.Session, this, npcMonsterSkill));
                        }
                    }
                }
            }
        }

        public void HitTrainer(int trainerVnum, int amount = 1)
        {
            bool canDown = trainerVnum != 636 && trainerVnum != 971;

            TrainerHits += amount;
            if (TrainerHits >= MateHelper.Instance.TrainerUpgradeHits[Attack])
            {
                TrainerHits = 0;

                int UpRate = MateHelper.Instance.TrainerUpRate[Attack];
                int DownRate = MateHelper.Instance.TrainerDownRate[Attack];

                int rnd = ServerManager.RandomNumber();

                if (DownRate < UpRate)
                {
                    if (rnd < DownRate && canDown)
                    {
                        DownAttack();
                    }
                    else if (rnd < UpRate)
                    {
                        UpAttack();
                    }
                    else
                    {
                        EqualAttack();
                    }
                }
                else
                {
                    if (rnd < UpRate)
                    {
                        UpAttack();
                    }
                    else if (rnd < DownRate && canDown)
                    {
                        DownAttack();
                    }
                    else
                    {
                        EqualAttack();
                    }
                }

                void UpAttack()
                {
                    if (Attack < 10)
                    {
                        Attack++;
                        BattleEntity.AttackUpgrade++;
                        Owner.Session.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("MATE_ATTACK_CHANGED"), Attack), 0));
                        Owner.Session.SendPacket(Owner.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("MATE_ATTACK_CHANGED"), Attack), 12));
                    }
                    else
                    {
                        Owner.Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("MATE_MAX_ATTACK"), 0));
                    }
                }
                void DownAttack()
                {
                    if (Attack > 0)
                    {
                        Attack--;
                        BattleEntity.AttackUpgrade--;
                        Owner.Session.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("MATE_ATTACK_CHANGED"), Attack), 0));
                        Owner.Session.SendPacket(Owner.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("MATE_ATTACK_CHANGED"), Attack), 12));
                    }
                    else
                    {
                        Owner.Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("MATE_MIN_ATTACK"), 0));
                    }
                }
                void EqualAttack()
                {
                    Owner.Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("MATE_ATTACK_EQUAL"), 0));
                    Owner.Session.SendPacket(Owner.GenerateSay(Language.Instance.GetMessageFromKey("MATE_ATTACK_EQUAL"), 12));
                }

                Owner.Session.SendPacket(UserInterfaceHelper.GeneratePClear());
                Owner.Session.SendPackets(Owner.GenerateScP());
                Owner.Session.SendPackets(Owner.GenerateScN());
            }
        }

        public void DefendTrainer(int trainerVnum, int amount = 1)
        {
            bool canDown = trainerVnum != 636 && trainerVnum != 971;

            TrainerDefences += amount;
            if (TrainerDefences >= MateHelper.Instance.TrainerUpgradeHits[Defence])
            {
                TrainerDefences = 0;
                int UpRate = MateHelper.Instance.TrainerUpRate[Defence];
                int DownRate = MateHelper.Instance.TrainerDownRate[Defence];

                int rnd = ServerManager.RandomNumber();

                if (DownRate < UpRate)
                {
                    if (rnd < DownRate && canDown)
                    {
                        DownDefence();
                    }
                    else if (rnd < UpRate)
                    {
                        UpDefence();
                    }
                    else
                    {
                        EqualDefence();
                    }
                }
                else
                {
                    if (rnd < UpRate)
                    {
                        UpDefence();
                    }
                    else if (rnd < DownRate && canDown)
                    {
                        DownDefence();
                    }
                    else
                    {
                        EqualDefence();
                    }
                }

                void UpDefence()
                {
                    if (Defence < 10)
                    {
                        Defence++;
                        BattleEntity.DefenseUpgrade++;
                        Owner.Session.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("MATE_DEFENCE_CHANGED"), Defence), 0));
                        Owner.Session.SendPacket(Owner.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("MATE_DEFENCE_CHANGED"), Defence), 12));
                    }
                    else
                    {
                        Owner.Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("MATE_MAX_DEFENCE"), 0));
                    }
                }
                void DownDefence()
                {
                    if (Defence > 0)
                    {
                        Defence--;
                        BattleEntity.DefenseUpgrade--;
                        Owner.Session.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("MATE_DEFENCE_CHANGED"), Defence), 0));
                        Owner.Session.SendPacket(Owner.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("MATE_DEFENCE_CHANGED"), Defence), 12));
                    }
                    else
                    {
                        Owner.Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("MATE_MIN_DEFENCE"), 0));
                    }
                }
                void EqualDefence()
                {
                    Owner.Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("MATE_DEFENCE_EQUAL"), 0));
                    Owner.Session.SendPacket(Owner.GenerateSay(Language.Instance.GetMessageFromKey("MATE_DEFENCE_EQUAL"), 12));
                }

                Owner.Session.SendPacket(UserInterfaceHelper.GeneratePClear());
                Owner.Session.SendPackets(Owner.GenerateScP());
                Owner.Session.SendPackets(Owner.GenerateScN());
            }
        }
    }
}