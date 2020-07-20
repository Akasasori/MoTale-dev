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
using OpenNos.DAL;
using OpenNos.Data;
using OpenNos.Domain;
using OpenNos.GameObject.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using OpenNos.GameObject.Networking;
using System.Text;

namespace OpenNos.GameObject
{
    public class ItemInstance : ItemInstanceDTO
    {
        #region Members

        private Item _item;
        private long _transportId;
        private List<CellonOptionDTO> _cellonOptions;
        private List<ShellEffectDTO> _shellEffects;

        #endregion

        #region Instantiation

        public ItemInstance()
        {
            
        }

        public ItemInstance(short vNum, short amount)
        {
            ItemVNum = vNum;
            Amount = amount;
            Type = Item.Type;
        }

        public ItemInstance(ItemInstanceDTO input)
        {
            Ammo = input.Ammo;
            Amount = input.Amount;
            BoundCharacterId = input.BoundCharacterId;
            Cellon = input.Cellon;
            CharacterId = input.CharacterId;
            CloseDefence = input.CloseDefence;
            Concentrate = input.Concentrate;
            CriticalDodge = input.CriticalDodge;
            CriticalLuckRate = input.CriticalLuckRate;
            CriticalRate = input.CriticalRate;
            DamageMaximum = input.DamageMaximum;
            DamageMinimum = input.DamageMinimum;
            DarkElement = input.DarkElement;
            DarkResistance = input.DarkResistance;
            DefenceDodge = input.DefenceDodge;
            Design = input.Design;
            DistanceDefence = input.DistanceDefence;
            DistanceDefenceDodge = input.DistanceDefenceDodge;
            DurabilityPoint = input.DurabilityPoint;
            ElementRate = input.ElementRate;
            EquipmentSerialId = input.EquipmentSerialId;
            FireElement = input.FireElement;
            FireResistance = input.FireResistance;
            HitRate = input.HitRate;
            HoldingVNum = input.HoldingVNum;
            HP = input.HP;
            Id = input.Id;
            IsEmpty = input.IsEmpty;
            IsFixed = input.IsFixed;
            IsPartnerEquipment = input.IsPartnerEquipment;
            ItemDeleteTime = input.ItemDeleteTime;
            ItemVNum = input.ItemVNum;
            LightElement = input.LightElement;
            LightResistance = input.LightResistance;
            MagicDefence = input.MagicDefence;
            MaxElementRate = input.MaxElementRate;
            MP = input.MP;
            Rare = input.Rare;
            ShellRarity = input.ShellRarity;
            SlDamage = input.SlDamage;
            SlDefence = input.SlDefence;
            SlElement = input.SlElement;
            SlHP = input.SlHP;
            Slot = input.Slot;
            SpDamage = input.SpDamage;
            SpDark = input.SpDark;
            SpDefence = input.SpDefence;
            SpElement = input.SpElement;
            SpFire = input.SpFire;
            SpHP = input.SpHP;
            SpLevel = input.SpLevel;
            SpLight = input.SpLight;
            SpStoneUpgrade = input.SpStoneUpgrade;
            SpWater = input.SpWater;
            Type = input.Type;
            Upgrade = input.Upgrade;
            WaterElement = input.WaterElement;
            WaterResistance = input.WaterResistance;
            XP = input.XP;
        }

        #endregion

        #region Properties

        public bool IsBound => BoundCharacterId.HasValue && Item.ItemType != ItemType.Armor && Item.ItemType != ItemType.Weapon;

        public Item Item => _item ?? (_item = IsPartnerEquipment && HoldingVNum != 0 ? ServerManager.GetItem(HoldingVNum) : ServerManager.GetItem(ItemVNum));

        public long TransportId
        {
            get
            {
                if (_transportId == 0)
                {
                    // create transportId thru factory
                    _transportId = TransportFactory.Instance.GenerateTransportId();
                }

                return _transportId;
            }
        }

        public List<CellonOptionDTO> CellonOptions => _cellonOptions ?? (_cellonOptions = DAOFactory.CellonOptionDAO.GetOptionsByWearableInstanceId(EquipmentSerialId == Guid.Empty ? EquipmentSerialId = Guid.NewGuid() : EquipmentSerialId).ToList());

        public List<ShellEffectDTO> ShellEffects => _shellEffects ?? (_shellEffects = DAOFactory.ShellEffectDAO.LoadByEquipmentSerialId(EquipmentSerialId == Guid.Empty ? EquipmentSerialId = Guid.NewGuid() : EquipmentSerialId).ToList());


        #endregion

        #region Methods

        public ItemInstance DeepCopy() => (ItemInstance)MemberwiseClone();

        public string GenerateFStash() => $"f_stash {GenerateStashPacket()}";

        public string GenerateInventoryAdd()
        {
            switch (Type)
            {
                case InventoryType.Equipment:
                    return $"ivn 0 {Slot}.{ItemVNum}.{Rare}.{(Item.IsColored ? Design : Upgrade)}.{SpStoneUpgrade}.{Act7RuneUpgrade}";

                case InventoryType.Main:
                    return $"ivn 1 {Slot}.{ItemVNum}.{Amount}.0";

                case InventoryType.Etc:
                    return $"ivn 2 {Slot}.{ItemVNum}.{Amount}.0";

                case InventoryType.Miniland:
                    return $"ivn 3 {Slot}.{ItemVNum}.{Amount}";

                case InventoryType.Specialist:
                    return $"ivn 6 {Slot}.{ItemVNum}.{Rare}.{Upgrade}.{SpStoneUpgrade}";

                case InventoryType.Costume:
                    return $"ivn 7 {Slot}.{ItemVNum}.{Rare}.{Upgrade}.0";
            }
            return "";
        }

        public string GeneratePStash() => $"pstash {GenerateStashPacket()}";

        public string GenerateStash() => $"stash {GenerateStashPacket()}";

        public string GenerateStashPacket()
        {
            string packet = $"{Slot}.{ItemVNum}.{(byte)Item.Type}";
            switch (Item.Type)
            {
                case InventoryType.Equipment:
                    return packet + $".{Amount}.{Rare}.{Upgrade}";

                case InventoryType.Specialist:
                    return packet + $".{Upgrade}.{SpStoneUpgrade}.0";

                default:
                    return packet + $".{Amount}.0.0";
            }
        }

        public string GeneratePslInfo()
        {
            PartnerSp partnerSp = new PartnerSp(this);

            return $"pslinfo {Item.VNum} {Item.Element} {Item.ElementRate} {Item.LevelJobMinimum} {Item.Speed} {Item.FireResistance} {Item.WaterResistance} {Item.LightResistance} {Item.DarkResistance}{partnerSp.GenerateSkills()}";
        }

        public string GenerateSlInfo(ClientSession session = null)
        {
            int freepoint = CharacterHelper.SPPoint(SpLevel, Upgrade) - SlDamage - SlHP - SlElement - SlDefence;
            int slElementShell = 0;
            int slHpShell = 0;
            int slDefenceShell = 0;
            int slHitShell = 0;

            if (session != null)
            {
                ItemInstance mainWeapon = session.Character?.Inventory.LoadBySlotAndType((byte)EquipmentType.MainWeapon, InventoryType.Wear);
                ItemInstance secondaryWeapon = session.Character?.Inventory.LoadBySlotAndType((byte)EquipmentType.SecondaryWeapon, InventoryType.Wear);

                List<ShellEffectDTO> effects = new List<ShellEffectDTO>();
                if (mainWeapon?.ShellEffects != null)
                {
                    effects.AddRange(mainWeapon.ShellEffects);
                }
                if (secondaryWeapon?.ShellEffects != null)
                {
                    effects.AddRange(secondaryWeapon.ShellEffects);
                }

                int GetShellWeaponEffectValue(ShellWeaponEffectType effectType)
                {
                    return effects?.Where(s => s.Effect == (byte)effectType)?.OrderByDescending(s => s.Value)?.FirstOrDefault()?.Value ?? 0;
                }

                slElementShell = GetShellWeaponEffectValue(ShellWeaponEffectType.SLElement) + GetShellWeaponEffectValue(ShellWeaponEffectType.SLGlobal);
                slHpShell = GetShellWeaponEffectValue(ShellWeaponEffectType.SLHP) + GetShellWeaponEffectValue(ShellWeaponEffectType.SLGlobal);
                slDefenceShell = GetShellWeaponEffectValue(ShellWeaponEffectType.SLDefence) + GetShellWeaponEffectValue(ShellWeaponEffectType.SLGlobal);
                slHitShell = GetShellWeaponEffectValue(ShellWeaponEffectType.SLDamage) + GetShellWeaponEffectValue(ShellWeaponEffectType.SLGlobal);
            }

            int slElement = CharacterHelper.SlPoint(SlElement, 2);
            int slHp = CharacterHelper.SlPoint(SlHP, 3);
            int slDefence = CharacterHelper.SlPoint(SlDefence, 1);
            int slHit = CharacterHelper.SlPoint(SlDamage, 0);

            StringBuilder skills = new StringBuilder();

            List<CharacterSkill> skillsSp = new List<CharacterSkill>();

            var morphUpdate = 31;

            if (ItemVNum == 4485 || ItemVNum == 4437 || ItemVNum == 4416)
            {
                morphUpdate = 30;
            }

            foreach (Skill ski in ServerManager.GetAllSkill().Where(ski => ski.Class == Item.Morph + morphUpdate && ski.LevelMinimum <= SpLevel))
            {
                skillsSp.Add(new CharacterSkill
                {
                    SkillVNum = ski.SkillVNum,
                    CharacterId = CharacterId
                });
            }

            if (Item.ItemType == ItemType.Box)
            {
                // should! Prevent but only for boxinstances. idk if raidboxes are they but i look another packet
                if (Amount < 0)
                {
                    session.SendPacket(UserInterfaceHelper.GenerateMsg($"Im so stupid", 0));
                    Logger.Log.Info($"Cenk's Logging : A accident happened, someone tried to dupe with raidboxes or any boxes");
                }
            }

            byte spdestroyed = 0;

            if (Rare == -2)
            {
                spdestroyed = 1;
            }

            if (skillsSp.Count == 0)
            {
                skills.Append("-1");
            }
            else
            {
                short firstSkillVNum = skillsSp[0].SkillVNum;

                for (int i = 1; i < 11; i++)
                {
                    if (skillsSp.Count >= i + 1 && skillsSp[i].SkillVNum <= firstSkillVNum + 10)
                    {
                        if (skills.Length > 0)
                        {
                            skills.Append(".");
                        }

                        skills.Append($"{skillsSp[i].SkillVNum}");
                    }
                }
            }

            // 10 9 8 '0 0 0 0'<- bonusdamage bonusarmor bonuselement bonushpmp its after upgrade and
            // 3 first values are not important

            return $"slinfo {(Type == InventoryType.Wear || Type == InventoryType.Specialist || Type == InventoryType.Equipment ? "0" : "2")} {ItemVNum} {Item.Morph} {SpLevel} {Item.LevelJobMinimum} {Item.ReputationMinimum} 0 {Item.Speed} 0 0 0 0 0 {Item.SpType} {Item.FireResistance} {Item.WaterResistance} {Item.LightResistance} {Item.DarkResistance} {XP} {CharacterHelper.SPXPData[SpLevel == 0 ? 0 : SpLevel - 1]} {skills} {TransportId} {freepoint} {slHit} {slDefence} {slElement} {slHp} {Upgrade} 0 0 {spdestroyed} {slHitShell} {slDefenceShell} {slElementShell} {slHpShell} {SpStoneUpgrade} {SpDamage} {SpDefence} {SpElement} {SpHP} {SpFire} {SpWater} {SpLight} {SpDark}";
        }

        public string GenerateReqInfo()
        {
            byte type = 0;
            if (BoundCharacterId != null && BoundCharacterId != CharacterId)
            {
                type = 2;
            }
            return $"r_info {ItemVNum} {type} {0}";
        }

        public void PerfectSP(ClientSession session)
        {
            if (ServerManager.Instance.Configuration.DoublePerfectionUp == true)
            {
                short[] upsuccessevt = { 75, 60, 45, 30, 15 };

                int[] goldprice = { 5000, 10000, 20000, 50000, 100000 };
                byte[] stoneprice = { 1, 2, 3, 4, 5 };
                short stonevnum;
                byte upmode = 1;
                switch (Item.Morph)
                {
                    case 2:
                    case 6:
                    case 9:
                    case 12:
                        stonevnum = 2514;
                        break;

                    case 3:
                    case 4:
                    case 14:
                        stonevnum = 2515;
                        break;

                    case 5:
                    case 11:
                    case 15:
                        stonevnum = 2516;
                        break;

                    case 10:
                    case 13:
                    case 7:
                        stonevnum = 2517;
                        break;

                    case 17:
                    case 18:
                    case 19:
                        stonevnum = 2518;
                        break;

                    case 20:
                    case 21:
                    case 22:
                        stonevnum = 2519;
                        break;

                    case 23:
                    case 24:
                    case 25:
                        stonevnum = 2520;
                        break;

                    case 26:
                    case 27:
                    case 28:
                        stonevnum = 2521;
                        break;

                    default:
                        return;
                }
                if (SpStoneUpgrade > 99)
                {
                    return;
                }
                if (SpStoneUpgrade > 80)
                {
                    upmode = 5;
                }
                else if (SpStoneUpgrade > 60)
                {
                    upmode = 4;
                }
                else if (SpStoneUpgrade > 40)
                {
                    upmode = 3;
                }
                else if (SpStoneUpgrade > 20)
                {
                    upmode = 2;
                }

                if (IsFixed)
                {
                    return;
                }
                if (session.Character.Gold < goldprice[upmode - 1])
                {
                    return;
                }
                if (session.Character.Inventory.CountItem(stonevnum) < stoneprice[upmode - 1])
                {
                    return;
                }

                ItemInstance specialist = session.Character.Inventory.GetItemInstanceById(Id);

                int rnd = ServerManager.RandomNumber();
                if (rnd < upsuccessevt[upmode - 1])
                {
                    byte type = (byte)ServerManager.RandomNumber(0, 16), count = 1;
                    if (upmode == 4)
                    {
                        count = 2;
                    }
                    if (upmode == 5)
                    {
                        count = (byte)ServerManager.RandomNumber(3, 6);
                    }

                    session.CurrentMapInstance.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, session.Character.CharacterId, 3005), session.Character.PositionX, session.Character.PositionY);

                    if (type < 3)
                    {
                        specialist.SpDamage += count;
                        session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("PERFECTSP_SUCCESS"), Language.Instance.GetMessageFromKey("PERFECTSP_ATTACK"), count), 12));
                        session.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("PERFECTSP_SUCCESS"), Language.Instance.GetMessageFromKey("PERFECTSP_ATTACK"), count), 0));
                    }
                    else if (type < 6)
                    {
                        specialist.SpDefence += count;
                        session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("PERFECTSP_SUCCESS"), Language.Instance.GetMessageFromKey("PERFECTSP_DEFENSE"), count), 12));
                        session.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("PERFECTSP_SUCCESS"), Language.Instance.GetMessageFromKey("PERFECTSP_DEFENSE"), count), 0));
                    }
                    else if (type < 9)
                    {
                        specialist.SpElement += count;
                        session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("PERFECTSP_SUCCESS"), Language.Instance.GetMessageFromKey("PERFECTSP_ELEMENT"), count), 12));
                        session.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("PERFECTSP_SUCCESS"), Language.Instance.GetMessageFromKey("PERFECTSP_ELEMENT"), count), 0));
                    }
                    else if (type < 12)
                    {
                        specialist.SpHP += count;
                        session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("PERFECTSP_SUCCESS"), Language.Instance.GetMessageFromKey("PERFECTSP_HPMP"), count), 12));
                        session.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("PERFECTSP_SUCCESS"), Language.Instance.GetMessageFromKey("PERFECTSP_HPMP"), count), 0));
                    }
                    else if (type == 12)
                    {
                        specialist.SpFire += count;
                        session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("PERFECTSP_SUCCESS"), Language.Instance.GetMessageFromKey("PERFECTSP_FIRE"), count), 12));
                        session.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("PERFECTSP_SUCCESS"), Language.Instance.GetMessageFromKey("PERFECTSP_FIRE"), count), 0));
                    }
                    else if (type == 13)
                    {
                        specialist.SpWater += count;
                        session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("PERFECTSP_SUCCESS"), Language.Instance.GetMessageFromKey("PERFECTSP_WATER"), count), 12));
                        session.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("PERFECTSP_SUCCESS"), Language.Instance.GetMessageFromKey("PERFECTSP_WATER"), count), 0));
                    }
                    else if (type == 14)
                    {
                        specialist.SpLight += count;
                        session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("PERFECTSP_SUCCESS"), Language.Instance.GetMessageFromKey("PERFECTSP_LIGHT"), count), 12));
                        session.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("PERFECTSP_SUCCESS"), Language.Instance.GetMessageFromKey("PERFECTSP_LIGHT"), count), 0));
                    }
                    else if (type == 15)
                    {
                        specialist.SpDark += count;
                        session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("PERFECTSP_SUCCESS"), Language.Instance.GetMessageFromKey("PERFECTSP_SHADOW"), count), 12));
                        session.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("PERFECTSP_SUCCESS"), Language.Instance.GetMessageFromKey("PERFECTSP_SHADOW"), count), 0));
                    }
                    specialist.SpStoneUpgrade++;
                }
                else
                {
                    session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("PERFECTSP_FAILURE"), 11));
                    session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("PERFECTSP_FAILURE"), 0));
                }
                session.SendPacket(specialist.GenerateInventoryAdd());
                session.Character.Gold -= goldprice[upmode - 1];
                session.SendPacket(session.Character.GenerateGold());
                session.Character.Inventory.RemoveItemAmount(stonevnum, stoneprice[upmode - 1]);
                session.SendPacket("shop_end 1");
            }
            else
            {
                short[] upsuccess = { 50, 40, 30, 20, 10 };

                int[] goldprice = { 5000, 10000, 20000, 50000, 100000 };
                byte[] stoneprice = { 1, 2, 3, 4, 5 };
                short stonevnum;
                byte upmode = 1;

                switch (Item.Morph)
                {
                    case 2:
                    case 6:
                    case 9:
                    case 12:
                        stonevnum = 2514;
                        break;

                    case 3:
                    case 4:
                    case 14:
                        stonevnum = 2515;
                        break;

                    case 5:
                    case 11:
                    case 15:
                        stonevnum = 2516;
                        break;

                    case 10:
                    case 13:
                    case 7:
                        stonevnum = 2517;
                        break;

                    case 17:
                    case 18:
                    case 19:
                        stonevnum = 2518;
                        break;

                    case 20:
                    case 21:
                    case 22:
                        stonevnum = 2519;
                        break;

                    case 23:
                    case 24:
                    case 25:
                        stonevnum = 2520;
                        break;

                    case 26:
                    case 27:
                    case 28:
                        stonevnum = 2521;
                        break;

                    default:
                        return;
                }
                if (SpStoneUpgrade > 99)
                {
                    return;
                }
                if (SpStoneUpgrade > 80)
                {
                    upmode = 5;
                }
                else if (SpStoneUpgrade > 60)
                {
                    upmode = 4;
                }
                else if (SpStoneUpgrade > 40)
                {
                    upmode = 3;
                }
                else if (SpStoneUpgrade > 20)
                {
                    upmode = 2;
                }

                if (IsFixed)
                {
                    return;
                }
                if (session.Character.Gold < goldprice[upmode - 1])
                {
                    return;
                }
                if (session.Character.Inventory.CountItem(stonevnum) < stoneprice[upmode - 1])
                {
                    return;
                }

                ItemInstance specialist = session.Character.Inventory.GetItemInstanceById(Id);

                int rnd = ServerManager.RandomNumber();
                if (rnd < upsuccess[upmode - 1])
                {
                    byte type = (byte)ServerManager.RandomNumber(0, 16), count = 1;
                    if (upmode == 4)
                    {
                        count = 2;
                    }
                    if (upmode == 5)
                    {
                        count = (byte)ServerManager.RandomNumber(3, 6);
                    }

                    session.CurrentMapInstance.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, session.Character.CharacterId, 3005), session.Character.PositionX, session.Character.PositionY);

                    if (type < 3)
                    {
                        specialist.SpDamage += count;
                        session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("PERFECTSP_SUCCESS"), Language.Instance.GetMessageFromKey("PERFECTSP_ATTACK"), count), 12));
                        session.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("PERFECTSP_SUCCESS"), Language.Instance.GetMessageFromKey("PERFECTSP_ATTACK"), count), 0));
                    }
                    else if (type < 6)
                    {
                        specialist.SpDefence += count;
                        session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("PERFECTSP_SUCCESS"), Language.Instance.GetMessageFromKey("PERFECTSP_DEFENSE"), count), 12));
                        session.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("PERFECTSP_SUCCESS"), Language.Instance.GetMessageFromKey("PERFECTSP_DEFENSE"), count), 0));
                    }
                    else if (type < 9)
                    {
                        specialist.SpElement += count;
                        session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("PERFECTSP_SUCCESS"), Language.Instance.GetMessageFromKey("PERFECTSP_ELEMENT"), count), 12));
                        session.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("PERFECTSP_SUCCESS"), Language.Instance.GetMessageFromKey("PERFECTSP_ELEMENT"), count), 0));
                    }
                    else if (type < 12)
                    {
                        specialist.SpHP += count;
                        session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("PERFECTSP_SUCCESS"), Language.Instance.GetMessageFromKey("PERFECTSP_HPMP"), count), 12));
                        session.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("PERFECTSP_SUCCESS"), Language.Instance.GetMessageFromKey("PERFECTSP_HPMP"), count), 0));
                    }
                    else if (type == 12)
                    {
                        specialist.SpFire += count;
                        session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("PERFECTSP_SUCCESS"), Language.Instance.GetMessageFromKey("PERFECTSP_FIRE"), count), 12));
                        session.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("PERFECTSP_SUCCESS"), Language.Instance.GetMessageFromKey("PERFECTSP_FIRE"), count), 0));
                    }
                    else if (type == 13)
                    {
                        specialist.SpWater += count;
                        session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("PERFECTSP_SUCCESS"), Language.Instance.GetMessageFromKey("PERFECTSP_WATER"), count), 12));
                        session.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("PERFECTSP_SUCCESS"), Language.Instance.GetMessageFromKey("PERFECTSP_WATER"), count), 0));
                    }
                    else if (type == 14)
                    {
                        specialist.SpLight += count;
                        session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("PERFECTSP_SUCCESS"), Language.Instance.GetMessageFromKey("PERFECTSP_LIGHT"), count), 12));
                        session.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("PERFECTSP_SUCCESS"), Language.Instance.GetMessageFromKey("PERFECTSP_LIGHT"), count), 0));
                    }
                    else if (type == 15)
                    {
                        specialist.SpDark += count;
                        session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("PERFECTSP_SUCCESS"), Language.Instance.GetMessageFromKey("PERFECTSP_SHADOW"), count), 12));
                        session.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("PERFECTSP_SUCCESS"), Language.Instance.GetMessageFromKey("PERFECTSP_SHADOW"), count), 0));
                    }
                    specialist.SpStoneUpgrade++;
                }
                else
                {
                    session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("PERFECTSP_FAILURE"), 11));
                    session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("PERFECTSP_FAILURE"), 0));
                }
                session.SendPacket(specialist.GenerateInventoryAdd());
                session.Character.Gold -= goldprice[upmode - 1];
                session.SendPacket(session.Character.GenerateGold());
                session.Character.Inventory.RemoveItemAmount(stonevnum, stoneprice[upmode - 1]);
                session.SendPacket("shop_end 1");
            }



        }

        public void UpgradeSp(ClientSession session, UpgradeProtection protect)
        {
            if (Upgrade >= 15)
            {
                return;
            }
            
            int[] goldprice = { 200000, 200000, 200000, 200000, 200000, 500000, 500000, 500000, 500000, 500000, 1000000, 1000000, 1000000, 1000000, 1000000 };
            byte[] feather = { 3, 5, 8, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55, 60, 70 };
            byte[] fullmoon = { 1, 3, 5, 7, 10, 12, 14, 16, 18, 20, 22, 24, 26, 28, 30 };
            byte[] soul = { 2, 4, 6, 8, 10, 1, 2, 3, 4, 5, 1, 2, 3, 4, 5 };
            const short featherVnum = 2282;
            const short fullmoonVnum = 1030;
            const short limitatedFullmoonVnum = 9131;
            const short greenSoulVnum = 2283;
            const short redSoulVnum = 2284;
            const short blueSoulVnum = 2285;
            const short dragonSkinVnum = 2511;
            const short dragonBloodVnum = 2512;
            const short dragonHeartVnum = 2513;
            const short blueScrollVnum = 1363;
            const short redScrollVnum = 1364;
            SpecialistMorphType[] soulSpecialists =
            {
                SpecialistMorphType.Pajama,
                SpecialistMorphType.Warrior,
                SpecialistMorphType.Ninja,
                SpecialistMorphType.Ranger,
                SpecialistMorphType.Assassin,
                SpecialistMorphType.RedMage,
                SpecialistMorphType.HolyMage,
                SpecialistMorphType.Chicken,
                SpecialistMorphType.Jajamaru,
                SpecialistMorphType.Crusader,
                SpecialistMorphType.Berserker,
                SpecialistMorphType.BombArtificer,
                SpecialistMorphType.WildKeeper,
                SpecialistMorphType.IceMage,
                SpecialistMorphType.DarkGunner,
                SpecialistMorphType.Pirate,
                SpecialistMorphType.Drakenfer,
                SpecialistMorphType.MysticalArts,
                SpecialistMorphType.WolfMaster,
                SpecialistMorphType.NewNinja,
                SpecialistMorphType.NewIceMage,
                SpecialistMorphType.NewRanger
            };


            if (!session.HasCurrentMapInstance)
            {
                return;
            }
            short itemToRemove = 2283;
            if (protect != UpgradeProtection.Event)
            {
                if (session.Character.Inventory.CountItem(fullmoonVnum) < fullmoon[Upgrade] && session.Character.Inventory.CountItem(limitatedFullmoonVnum) < fullmoon[Upgrade])
                {
                    session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey(string.Format(Language.Instance.GetMessageFromKey("NOT_ENOUGH_ITEMS"), ServerManager.GetItem(fullmoonVnum).Name, fullmoon[Upgrade])), 10));
                    return;
                }
                if (session.Character.Inventory.CountItem(featherVnum) < feather[Upgrade])
                {
                    session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey(string.Format(Language.Instance.GetMessageFromKey("NOT_ENOUGH_ITEMS"), ServerManager.GetItem(featherVnum).Name, feather[Upgrade])), 10));
                    return;
                }
                if (session.Character.Gold < goldprice[Upgrade])
                {
                    session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NOT_ENOUGH_MONEY"), 10));
                    return;
                }

                if (Upgrade < 5)
                {
                    if (SpLevel > 20)
                    {
                        if (soulSpecialists.Any(s => s == (SpecialistMorphType)Item.Morph))
                        {
                            if (session.Character.Inventory.CountItem(greenSoulVnum) < soul[Upgrade])
                            {
                                session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey(string.Format(Language.Instance.GetMessageFromKey("NOT_ENOUGH_ITEMS"), ServerManager.GetItem(greenSoulVnum).Name, soul[Upgrade])), 10));
                                return;
                            }
                            if (protect == UpgradeProtection.Protected)
                            {
                                if (session.Character.Inventory.CountItem(blueScrollVnum) < 1)
                                {
                                    session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey(string.Format(Language.Instance.GetMessageFromKey("NOT_ENOUGH_ITEMS"), ServerManager.GetItem(blueScrollVnum).Name, 1)), 10));
                                    return;
                                }
                                session.Character.Inventory.RemoveItemAmount(blueScrollVnum);
                                session.SendPacket("shop_end 2");
                            }
                        }
                        else
                        {
                            if (session.Character.Inventory.CountItem(dragonSkinVnum) < soul[Upgrade])
                            {
                                session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey(string.Format(Language.Instance.GetMessageFromKey("NOT_ENOUGH_ITEMS"), ServerManager.GetItem(dragonSkinVnum).Name, soul[Upgrade])), 10));
                                return;
                            }
                            if (protect == UpgradeProtection.Protected)
                            {
                                if (session.Character.Inventory.CountItem(blueScrollVnum) < 1)
                                {
                                    session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey(string.Format(Language.Instance.GetMessageFromKey("NOT_ENOUGH_ITEMS"), ServerManager.GetItem(blueScrollVnum).Name, 1)), 10));
                                    return;
                                }
                                session.Character.Inventory.RemoveItemAmount(blueScrollVnum);
                                session.SendPacket("shop_end 2");
                            }
                            itemToRemove = dragonSkinVnum;
                        }
                    }
                    else
                    {
                        session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("LVL_REQUIRED"), 21), 11));
                        return;
                    }
                }
                else if (Upgrade < 10)
                {
                    if (SpLevel > 40)
                    {
                        if (soulSpecialists.Any(s => s == (SpecialistMorphType)Item.Morph))
                        {
                            if (session.Character.Inventory.CountItem(redSoulVnum) < soul[Upgrade])
                            {
                                session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey(string.Format(Language.Instance.GetMessageFromKey("NOT_ENOUGH_ITEMS"), ServerManager.GetItem(redSoulVnum).Name, soul[Upgrade])), 10));
                                return;
                            }
                            if (protect == UpgradeProtection.Protected)
                            {
                                if (session.Character.Inventory.CountItem(blueScrollVnum) < 1)
                                {
                                    session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey(string.Format(Language.Instance.GetMessageFromKey("NOT_ENOUGH_ITEMS"), ServerManager.GetItem(blueScrollVnum).Name, 1)), 10));
                                    return;
                                }
                                session.Character.Inventory.RemoveItemAmount(blueScrollVnum);
                                session.SendPacket("shop_end 2");
                            }
                            itemToRemove = redSoulVnum;
                        }
                        else
                        {
                            if (session.Character.Inventory.CountItem(dragonBloodVnum) < soul[Upgrade])
                            {
                                session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey(string.Format(Language.Instance.GetMessageFromKey("NOT_ENOUGH_ITEMS"), ServerManager.GetItem(dragonBloodVnum).Name, soul[Upgrade])), 10));
                                return;
                            }
                            if (protect == UpgradeProtection.Protected)
                            {
                                if (session.Character.Inventory.CountItem(blueScrollVnum) < 1)
                                {
                                    session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey(string.Format(Language.Instance.GetMessageFromKey("NOT_ENOUGH_ITEMS"), ServerManager.GetItem(blueScrollVnum).Name, 1)), 10));
                                    return;
                                }
                                session.Character.Inventory.RemoveItemAmount(blueScrollVnum);
                                session.SendPacket("shop_end 2");
                            }
                            itemToRemove = dragonBloodVnum;
                        }
                    }
                    else
                    {
                        session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("LVL_REQUIRED"), 41), 11));
                        return;
                    }
                }
                else if (Upgrade < 15)
                {
                    if (SpLevel > 50)
                    {
                        if (soulSpecialists.Any(s => s == (SpecialistMorphType)Item.Morph))
                        {
                            if (session.Character.Inventory.CountItem(blueSoulVnum) < soul[Upgrade])
                            {
                                session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey(string.Format(Language.Instance.GetMessageFromKey("NOT_ENOUGH_ITEMS"), ServerManager.GetItem(blueSoulVnum).Name, soul[Upgrade])), 10));
                                return;
                            }
                            if (protect == UpgradeProtection.Protected)
                            {
                                if (session.Character.Inventory.CountItem(redScrollVnum) < 1)
                                {
                                    return;
                                }
                                session.Character.Inventory.RemoveItemAmount(redScrollVnum);
                                session.SendPacket("shop_end 2");
                            }
                            itemToRemove = blueSoulVnum;
                        }
                        else
                        {
                            if (session.Character.Inventory.CountItem(dragonHeartVnum) < soul[Upgrade])
                            {
                                return;
                            }
                            if (protect == UpgradeProtection.Protected)
                            {
                                if (session.Character.Inventory.CountItem(redScrollVnum) < 1)
                                {
                                    session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey(string.Format(Language.Instance.GetMessageFromKey("NOT_ENOUGH_ITEMS"), ServerManager.GetItem(redScrollVnum).Name, 1)), 10));
                                    return;
                                }
                                session.Character.Inventory.RemoveItemAmount(redScrollVnum);
                                session.SendPacket("shop_end 2");
                            }
                            itemToRemove = dragonHeartVnum;
                        }
                    }
                    else
                    {
                        session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("LVL_REQUIRED"), 51), 11));
                        return;
                    }
                }
                session.Character.Gold -= goldprice[Upgrade];
                // remove feather and fullmoon before upgrading
                session.Character.Inventory.RemoveItemAmount(featherVnum, feather[Upgrade]);
                if (session.Character.Inventory.CountItem(limitatedFullmoonVnum) >= fullmoon[Upgrade])
                {
                    session.Character.Inventory.RemoveItemAmount(limitatedFullmoonVnum, fullmoon[Upgrade]);
                }
                else
                {
                    session.Character.Inventory.RemoveItemAmount(fullmoonVnum, fullmoon[Upgrade]);
                }
            }
            else
            {
                session.SendPacket("shop_end 2");
                itemToRemove = -1;
                short eventScrollVnum = -1;
                switch (ItemVNum)
                {
                    case 900: // Pyjama
                        eventScrollVnum = 5207;
                        break;
                    case 907: // Chicken
                        eventScrollVnum = 5107;
                        break;
                    case 4099: // Pirate
                        eventScrollVnum = 5519;
                        break;
                }
                if (eventScrollVnum < 0)
                {
                    return;
                }
                if (session.Character.Inventory.CountItem(eventScrollVnum) > 0)
                {
                    session.Character.Inventory.RemoveItemAmount(eventScrollVnum);
                }
                else
                {
                    session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey(string.Format(Language.Instance.GetMessageFromKey("NOT_ENOUGH_ITEMS"), ServerManager.GetItem(eventScrollVnum).Name, 1)), 10));
                    return;
                }
            }

            ItemInstance wearable = session.Character.Inventory.GetItemInstanceById(Id);
            ItemInstance inventory = session.Character.Inventory.GetItemInstanceById(Id);
            int rnd = ServerManager.RandomNumber();
            if (ServerManager.Instance.Configuration.DoubleSpUp == true)
            {
                if (rnd < ItemHelper.SpDestroyRate[Upgrade])
                {
                    if (protect == UpgradeProtection.Protected || protect == UpgradeProtection.Event)
                    {
                        session.CurrentMapInstance.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, session.Character.CharacterId, 3004), session.Character.PositionX, session.Character.PositionY);
                        session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("UPGRADESP_FAILED_SAVED"), 11));
                        session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("UPGRADESP_FAILED_SAVED"), 0));
                    }
                    else
                    {
                        session.Character.Inventory.RemoveItemAmount(itemToRemove, soul[Upgrade]);
                        wearable.Rare = -2;
                        session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("UPGRADESP_DESTROYED"), 11));
                        session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("UPGRADESP_DESTROYED"), 0));
                        session.SendPacket(wearable.GenerateInventoryAdd());
                    }
                }
                else if (rnd > ItemHelper.SPUpLuckRateEvent[Upgrade])
                {
                    if (protect == UpgradeProtection.Protected || protect == UpgradeProtection.Event)
                    {
                        session.CurrentMapInstance.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, session.Character.CharacterId, 3004), session.Character.PositionX, session.Character.PositionY);
                    }
                    else
                    {
                        session.Character.Inventory.RemoveItemAmount(itemToRemove, soul[Upgrade]);
                    }
                    session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("UPGRADESP_FAILED"), 11));
                    session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("UPGRADESP_FAILED"), 0));
                }
                else
                {
                    if (protect == UpgradeProtection.Protected || protect == UpgradeProtection.Event)
                    {
                        session.CurrentMapInstance.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, session.Character.CharacterId, 3004), session.Character.PositionX, session.Character.PositionY);
                    }
                    session.Character.Inventory.RemoveItemAmount(itemToRemove, soul[Upgrade]);
                    session.CurrentMapInstance.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, session.Character.CharacterId, 3005), session.Character.PositionX, session.Character.PositionY);
                    session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("UPGRADESP_SUCCESS"), 12));
                    session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("UPGRADESP_SUCCESS"), 0));
                    wearable.Upgrade++;
                    if (wearable.Upgrade > 8)
                    {
                        session.Character.Family?.InsertFamilyLog(FamilyLogType.ItemUpgraded, session.Character.Name, itemVNum: wearable.ItemVNum, upgrade: wearable.Upgrade);
                    }
                    session.SendPacket(wearable.GenerateInventoryAdd());
                }
                session.SendPacket(session.Character.GenerateGold());
                session.SendPacket(session.Character.GenerateEq());
                session.SendPacket("shop_end 1");
            }
            else
            {
                if (rnd < ItemHelper.SpDestroyRate[Upgrade])
                {
                    if (protect == UpgradeProtection.Protected || protect == UpgradeProtection.Event)
                    {
                        session.CurrentMapInstance.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, session.Character.CharacterId, 3004), session.Character.PositionX, session.Character.PositionY);
                        session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("UPGRADESP_FAILED_SAVED"), 11));
                        session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("UPGRADESP_FAILED_SAVED"), 0));
                    }
                    else
                    {
                        session.Character.Inventory.RemoveItemAmount(itemToRemove, soul[Upgrade]);
                        wearable.Rare = -2;
                        session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("UPGRADESP_DESTROYED"), 11));
                        session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("UPGRADESP_DESTROYED"), 0));
                        session.SendPacket(wearable.GenerateInventoryAdd());
                    }
                }
                else if (rnd < ItemHelper.SpUpFailRate[Upgrade])
                {
                    if (protect == UpgradeProtection.Protected || protect == UpgradeProtection.Event)
                    {
                        session.CurrentMapInstance.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, session.Character.CharacterId, 3004), session.Character.PositionX, session.Character.PositionY);
                    }
                    else
                    {
                        session.Character.Inventory.RemoveItemAmount(itemToRemove, soul[Upgrade]);
                    }
                    session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("UPGRADESP_FAILED"), 11));
                    session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("UPGRADESP_FAILED"), 0));
                }
                else
                {
                    if (protect == UpgradeProtection.Protected || protect == UpgradeProtection.Event)
                    {
                        session.CurrentMapInstance.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, session.Character.CharacterId, 3004), session.Character.PositionX, session.Character.PositionY);
                    }
                    session.Character.Inventory.RemoveItemAmount(itemToRemove, soul[Upgrade]);
                    session.CurrentMapInstance.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, session.Character.CharacterId, 3005), session.Character.PositionX, session.Character.PositionY);
                    session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("UPGRADESP_SUCCESS"), 12));
                    session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("UPGRADESP_SUCCESS"), 0));
                    wearable.Upgrade++;
                    if (wearable.Upgrade > 8)
                    {
                        session.Character.Family?.InsertFamilyLog(FamilyLogType.ItemUpgraded, session.Character.Name, itemVNum: wearable.ItemVNum, upgrade: wearable.Upgrade);
                    }
                    session.SendPacket(wearable.GenerateInventoryAdd());
                }
                session.SendPacket(session.Character.GenerateGold());
                session.SendPacket(session.Character.GenerateEq());
                session.SendPacket("shop_end 1");
            }
        }

        public string GenerateEInfo()
        {
            EquipmentType equipmentslot = Item.EquipmentSlot;
            ItemType itemType = Item.ItemType;
            byte itemClass = Item.Class;
            byte subtype = Item.ItemSubType;
            DateTime test = ItemDeleteTime ?? DateTime.Now;
            long time = ItemDeleteTime != null ? (long)test.Subtract(DateTime.Now).TotalSeconds : 0;
            long seconds = IsBound ? time : Item.ItemValidTime;
            if (seconds < 0)
            {
                seconds = 0;
            }
            if (Item == null)
                return "";
            string sShellEffects = "";
            if (ShellEffects != null && ShellEffects.Count > 0)
            {
                if (itemType == ItemType.Armor)
                {
                    sShellEffects = ShellEffects?.Aggregate(string.Empty, (result, effect) => result += $"{((byte)effect?.EffectLevel > 12 ? (byte)effect?.EffectLevel - 12 : (byte)effect?.EffectLevel)}.{(effect?.Effect > 50 ? effect?.Effect - 50 : effect?.Effect)}.{(byte)effect?.Value} ");
                }
                else
                {
                    sShellEffects = ShellEffects.Aggregate(string.Empty, (result, effect) => result += $"{(byte)effect?.EffectLevel}.{effect?.Effect}.{(byte)effect?.Value} ");
                }

            }
            switch (itemType)
            {
                case ItemType.Weapon:
                    switch (equipmentslot)
                    {
                        case EquipmentType.MainWeapon:
                            return $"e_info {(itemClass == 4 ? 1 : itemClass == 8 ? 5 : 0)} {ItemVNum} {Rare} {Upgrade} {(IsFixed ? 1 : 0)} {Item.LevelMinimum} {Item.DamageMinimum + DamageMinimum} {Item.DamageMaximum + DamageMaximum} {Item.HitRate + HitRate} {Item.CriticalLuckRate + CriticalLuckRate} {Item.CriticalRate + CriticalRate} {Ammo} {Item.MaximumAmmo} {Item.SellToNpcPrice} {(IsPartnerEquipment ? $"{HoldingVNum}" : "-1")} {(ShellRarity == null ? "0" : $"{ShellRarity}")} {BoundCharacterId ?? 0} {ShellEffects.Count} {sShellEffects}"; // Shell Rare, CharacterId, ShellEffectCount, ShellEffects

                        case EquipmentType.SecondaryWeapon:
                            return $"e_info {(itemClass <= 2 ? 1 : 0)} {ItemVNum} {Rare} {Upgrade} {(IsFixed ? 1 : 0)} {Item.LevelMinimum} {Item.DamageMinimum + DamageMinimum} {Item.DamageMaximum + DamageMaximum} {Item.HitRate + HitRate} {Item.CriticalLuckRate + CriticalLuckRate} {Item.CriticalRate + CriticalRate} {Ammo} {Item.MaximumAmmo} {Item.SellToNpcPrice} {(IsPartnerEquipment ? $"{HoldingVNum}" : "-1")} {(ShellRarity == null ? "0" : $"{ShellRarity}")} {BoundCharacterId ?? 0} {ShellEffects.Count} {sShellEffects}";
                    }
                    break;

                case ItemType.Armor:
                    return $"e_info 2 {ItemVNum} {Rare} {Upgrade} {(IsFixed ? 1 : 0)} {Item.LevelMinimum} {Item.CloseDefence + CloseDefence} {Item.DistanceDefence + DistanceDefence} {Item.MagicDefence + MagicDefence} {Item.DefenceDodge + DefenceDodge} {Item.SellToNpcPrice} {(IsPartnerEquipment ? $"{HoldingVNum}" : "-1")} {(ShellRarity == null ? "0" : $"{ShellRarity}")} {BoundCharacterId ?? 0} {ShellEffects?.Count} {sShellEffects}";

                case ItemType.Fashion:
                    switch (equipmentslot)
                    {
                        case EquipmentType.CostumeHat:
                            return $"e_info 3 {ItemVNum} {Item.LevelMinimum} {Item.CloseDefence + CloseDefence} {Item.DistanceDefence + DistanceDefence} {Item.MagicDefence + MagicDefence} {Item.DefenceDodge + DefenceDodge} {Item.FireResistance + FireResistance} {Item.WaterResistance + WaterResistance} {Item.LightResistance + LightResistance} {Item.DarkResistance + DarkResistance} {Item.SellToNpcPrice} {(Item.ItemValidTime == 0 ? -1 : 0)} 2 {(Item.ItemValidTime == 0 ? -1 : seconds / 3600)}";

                        case EquipmentType.CostumeSuit:
                            return $"e_info 2 {ItemVNum} {Rare} {Upgrade} {(IsFixed ? 1 : 0)} {Item.LevelMinimum} {Item.CloseDefence + CloseDefence} {Item.DistanceDefence + DistanceDefence} {Item.MagicDefence + MagicDefence} {Item.DefenceDodge + DefenceDodge} {Item.SellToNpcPrice} {(Item.ItemValidTime == 0 ? -1 : 0)} 1 {(Item.ItemValidTime == 0 ? -1 : seconds / 3600)}"; // 1 = IsCosmetic -1 = no shells

                        default:
                            return $"e_info 3 {ItemVNum} {Item.LevelMinimum} {Item.CloseDefence + CloseDefence} {Item.DistanceDefence + DistanceDefence} {Item.MagicDefence + MagicDefence} {Item.DefenceDodge + DefenceDodge} {Item.FireResistance + FireResistance} {Item.WaterResistance + WaterResistance} {Item.LightResistance + LightResistance} {Item.DarkResistance + DarkResistance} {Item.SellToNpcPrice} {Upgrade} 0 -1"; // after Item.Price theres TimesConnected {(Item.ItemValidTime == 0 ? -1 : Item.ItemValidTime / (3600))}
                    }

                case ItemType.Jewelery:
                    switch (equipmentslot)
                    {
                        case EquipmentType.Amulet:
                            if (DurabilityPoint > 0)
                            {
                                return $"e_info 4 {ItemVNum} {Item.LevelMinimum} {DurabilityPoint} 100 0 {Item.SellToNpcPrice}";
                            }
                            return $"e_info 4 {ItemVNum} {Item.LevelMinimum} {seconds * 10} 0 0 {Item.SellToNpcPrice}";

                        case EquipmentType.Fairy:
                            return $"e_info 4 {ItemVNum} {Item.Element} {ElementRate + Item.ElementRate} 0 0 0 0 0"; // last IsNosmall

                        default:
                            string cellon = "";
                            foreach (CellonOptionDTO option in CellonOptions)
                            {
                                cellon += $" {(byte)option.Type} {option.Level} {option.Value}";
                            }
                            return $"e_info 4 {ItemVNum} {Item.LevelMinimum} {Item.MaxCellonLvl} {Item.MaxCellon} {CellonOptions.Count} {Item.SellToNpcPrice}{cellon}";
                    }
                case ItemType.Specialist:
                    return $"e_info 8 {ItemVNum}";

                case ItemType.Box:
                    switch (subtype)
                    {
                        case 0:
                        case 1:
                            return HoldingVNum == 0 ?
                                $"e_info 7 {ItemVNum} 0" : $"e_info 7 {ItemVNum} 1 {HoldingVNum} {SpLevel} {XP} 100 {SpDamage} {SpDefence}";
                            
                        case 2:
                            Item spitem = ServerManager.GetItem(HoldingVNum);
                            return HoldingVNum == 0 ?
                                $"e_info 7 {ItemVNum} 0" :
                                $"e_info 7 {ItemVNum} 1 {HoldingVNum} {SpLevel} {XP} {CharacterHelper.SPXPData[SpLevel == 0 ? 0 : SpLevel - 1]} {Upgrade} {CharacterHelper.SlPoint(SlDamage, 0)} {CharacterHelper.SlPoint(SlDefence, 1)} {CharacterHelper.SlPoint(SlElement, 2)} {CharacterHelper.SlPoint(SlHP, 3)} {CharacterHelper.SPPoint(SpLevel, Upgrade) - SlDamage - SlHP - SlElement - SlDefence} {SpStoneUpgrade} {spitem.FireResistance} {spitem.WaterResistance} {spitem.LightResistance} {spitem.DarkResistance} {SpDamage} {SpDefence} {SpElement} {SpHP} {SpFire} {SpWater} {SpLight} {SpDark}";

                        case 4:
                            return HoldingVNum == 0 ?
                                $"e_info 11 {ItemVNum} 0" :
                                $"e_info 11 {ItemVNum} 1 {HoldingVNum}";

                        case 5:
                            Item fairyitem = ServerManager.GetItem(HoldingVNum);
                            return HoldingVNum == 0 ?
                                $"e_info 12 {ItemVNum} 0" :
                                $"e_info 12 {ItemVNum} 1 {HoldingVNum} {ElementRate + fairyitem.ElementRate}";

                        default:
                            return $"e_info 8 {ItemVNum} {Design} {Rare}";
                    }

                case ItemType.Shell:
                    return $"e_info 9 {ItemVNum} {Upgrade} {Rare} {Item.SellToNpcPrice} {ShellEffects.Count}{ShellEffects.Aggregate("", (current, option) => current + $" {((byte)option.EffectLevel > 12 ? (byte)option.EffectLevel - 12 : (byte)option.EffectLevel)}.{(option.Effect > 50 ? option.Effect - 50 : option.Effect)}.{option.Value}")}";
            }
            return "";
        }

        public void OptionItem(ClientSession session, short cellonVNum)
        {
            if (EquipmentSerialId == Guid.Empty)
            {
                EquipmentSerialId = Guid.NewGuid();
            }
            if (Item.MaxCellon <= CellonOptions.Count)
            {
                session.SendPacket($"info {Language.Instance.GetMessageFromKey("MAX_OPTIONS")}");
                session.SendPacket("shop_end 1");
                return;
            }
            if (session.Character.Inventory.CountItem(cellonVNum) > 0)
            {
                byte dataIndex = 0;
                int goldAmount = 0;
                switch (cellonVNum)
                {
                    case 1017:
                        dataIndex = 0;
                        goldAmount = 700;
                        break;

                    case 1018:
                        dataIndex = 1;
                        goldAmount = 1400;
                        break;

                    case 1019:
                        dataIndex = 2;
                        goldAmount = 3000;
                        break;

                    case 1020:
                        dataIndex = 3;
                        goldAmount = 5000;
                        break;

                    case 1021:
                        dataIndex = 4;
                        goldAmount = 10000;
                        break;

                    case 1022:
                        dataIndex = 5;
                        goldAmount = 20000;
                        break;

                    case 1023:
                        dataIndex = 6;
                        goldAmount = 32000;
                        break;

                    case 1024:
                        dataIndex = 7;
                        goldAmount = 58000;
                        break;

                    case 1025:
                        dataIndex = 8;
                        goldAmount = 95000;
                        break;

                    case 1026:
                        return;
                        //no data known, not implemented in the client at all right now
                        //dataIndex = 9;
                        //break;
                }

                if (Item.MaxCellonLvl > dataIndex && session.Character.Gold >= goldAmount)
                {
                    short[][] minimumData = {
                    new short[] { 30, 50, 5, 8, 0, 0 },             //lv1
                    new short[] { 120, 150, 14, 16, 0, 0 },         //lv2
                    new short[] { 220, 280, 22, 28, 0, 0 },         //lv3
                    new short[] { 330, 350, 30, 38, 0, 0 },         //lv4
                    new short[] { 430, 450, 40, 50, 0, 0 },         //lv5
                    new short[] { 600, 600, 55, 65, 1, 1 },         //lv6
                    new short[] { 800, 800, 75, 75, 8, 11 },        //lv7
                    new short[] { 1000, 1000, 100, 100, 13, 21 },   //lv8
                    new short[] { 1100, 1100, 110, 110, 14, 22 },   //lv9
                    new short[] { 0, 0, 0, 0, 0, 0 }                //lv10 (NOT EXISTING!)
                    };
                    short[][] maximumData = {
                    new short[] { 100, 150, 10, 15, 0, 0 },         //lv1
                    new short[] { 200, 250, 20, 25, 0, 0 },         //lv1
                    new short[] { 300, 330, 28, 35, 0, 0 },         //lv1
                    new short[] { 400, 420, 38, 45, 0, 0 },         //lv1
                    new short[] { 550, 550, 50, 60, 0, 0 },         //lv1
                    new short[] { 750, 750, 70, 80, 7, 10 },        //lv1
                    new short[] { 1000, 1000, 90,90, 12, 20 },      //lv1
                    new short[] { 1300, 1300, 120, 120, 17, 35 },   //lv1
                    new short[] { 1500, 1500, 135, 135, 21, 45 },   //lv1
                    new short[] { 0, 0, 0, 0, 0, 0 }                //lv10 (NOT EXISTING!)
                    };

                    short[] generateOption()
                    {
                        byte option = 0;
                        if (dataIndex < 5)
                        {
                            option = (byte)ServerManager.RandomNumber(0, 4);
                        }
                        else
                        {
                            option = (byte)ServerManager.RandomNumber(0, 6);
                        }

                        if (CellonOptions.Any(s => s.Type == (CellonOptionType)option))
                        {
                            return new short[] { -1, -1 };
                        }

                        return new short[] { option, (short)ServerManager.RandomNumber(minimumData[dataIndex][option], maximumData[dataIndex][option] + 1) };
                    }

                    short[] value = generateOption();
                    Logger.LogUserEvent("OPTION", session.GenerateIdentity(), $"[OptionItem]Serial: {EquipmentSerialId} Type: {value[0]} Value: {value[1]}");
                    if (value[0] != -1)
                    {
                        CellonOptionDTO cellonOptionDTO = new CellonOptionDTO
                        {
                            EquipmentSerialId = EquipmentSerialId,
                            Level = (byte)(dataIndex + 1),
                            Type = (CellonOptionType)value[0],
                            Value = value[1]
                        };

                        CellonOptions.Add(cellonOptionDTO);

                        session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("OPTION_SUCCESS"), Rare), 12));
                        session.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("OPTION_SUCCESS"), Rare), 0));
                        session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, CharacterId, 3005), session.Character.PositionX, session.Character.PositionY);
                        session.SendPacket("shop_end 1");
                    }
                    else
                    {
                        session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("OPTION_FAIL"), Rare), 11));
                        session.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("OPTION_FAIL"), Rare), 0));
                        session.SendPacket("shop_end 1");
                    }
                    session.Character.Inventory.RemoveItemAmount(cellonVNum);
                    session.Character.Gold -= goldAmount;
                    session.SendPacket(session.Character.GenerateGold());
                }
            }

            foreach (CellonOptionDTO effect in CellonOptions)
            {
                effect.EquipmentSerialId = EquipmentSerialId;
                effect.CellonOptionId = DAOFactory.CellonOptionDAO.InsertOrUpdate(effect).CellonOptionId;
            }
        }

        public void GenerateHeroicShell(RarifyProtection protection, bool forced = false)
        {
            if (protection != RarifyProtection.RandomHeroicAmulet && !forced)
            {
                return;
            }
            if (!Item.IsHeroic || Rare <= 0)
            {
                return;
            }
            byte shellType = (byte)(Item.ItemType == ItemType.Armor ? 11 : 10);
            if (shellType != 11 && shellType != 10)
            {
                return;
            }
            ShellEffects.Clear();
            int shellLevel = Item.LevelMinimum == 25 ? 101 : 106;
            ShellEffects.AddRange(ShellGeneratorHelper.Instance.GenerateShell(shellType, Rare == 8 ? 7 : Rare, shellLevel));
        }

        public void RarifyItem(ClientSession session, RarifyMode mode, RarifyProtection protection, bool isCommand = false, byte forceRare = 0)
        {
            const short goldprice = 500;
            const double reducedpricefactor = 0.5;
            const double reducedchancefactor = 1.1;
            const byte cella = 5;
            const int cellaVnum = 1014;
            const int scrollVnum = 1218;
            double rnd;
            if (ServerManager.Instance.Configuration.DoubleBet == true)
            {
                byte[] rarifyRate = new byte[ItemHelper.RarifyRateEvent.Length];
                ItemHelper.RarifyRateEvent.CopyTo(rarifyRate, 0);

                if (session?.HasCurrentMapInstance == false)
                {
                    return;
                }
                if (mode != RarifyMode.Drop || Item.ItemType == ItemType.Shell)
                {
                    rarifyRate[0] = 0;
                    rarifyRate[1] = 0;
                    rarifyRate[2] = 0;
                    rnd = ServerManager.RandomNumber(0, 80);
                }
                else
                {
                    rnd = ServerManager.RandomNumber(0, 1000) / 10D;
                }
                if (protection == RarifyProtection.RedAmulet ||
                    protection == RarifyProtection.HeroicAmulet ||
                    protection == RarifyProtection.RandomHeroicAmulet)
                {
                    for (byte i = 0; i < rarifyRate.Length; i++)
                    {
                        rarifyRate[i] = (byte)(rarifyRate[i] * reducedchancefactor);
                    }
                }
                if (session != null)
                {
                    switch (mode)
                    {
                        case RarifyMode.Free:
                            break;

                        case RarifyMode.Success:
                            if (Item.IsHeroic && Rare >= 8 || !Item.IsHeroic && Rare <= 7)
                            {
                                session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("ALREADY_MAX_RARE"), 10));
                                return;
                            }
                            Rare += 1;
                            SetRarityPoint();
                            ItemInstance inventory = session?.Character.Inventory.GetItemInstanceById(Id);
                            if (inventory != null)
                            {
                                session.SendPacket(inventory.GenerateInventoryAdd());
                            }
                            return;

                        case RarifyMode.Reduced:
                            if (session.Character.Gold < (long)(goldprice * reducedpricefactor))
                            {
                                return;
                            }
                            if (session.Character.Inventory.CountItem(cellaVnum) < cella * reducedpricefactor)
                            {
                                return;
                            }
                            session.Character.Inventory.RemoveItemAmount(cellaVnum, (int)(cella * reducedpricefactor));
                            session.Character.Gold -= (long)(goldprice * reducedpricefactor);
                            session.SendPacket(session.Character.GenerateGold());
                            break;

                        case RarifyMode.Normal:
                            if (session.Character.Gold < goldprice)
                            {
                                return;
                            }
                            if (session.Character.Inventory.CountItem(cellaVnum) < cella)
                            {
                                return;
                            }
                            if (protection == RarifyProtection.Scroll && !isCommand
                                && session.Character.Inventory.CountItem(scrollVnum) < 1)
                            {
                                return;
                            }

                            if ((protection == RarifyProtection.Scroll || protection == RarifyProtection.BlueAmulet || protection == RarifyProtection.RedAmulet) && !isCommand && Item.IsHeroic)
                            {
                                session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("ITEM_IS_HEROIC"), 0));
                                return;
                            }
                            if ((protection == RarifyProtection.HeroicAmulet ||
                                 protection == RarifyProtection.RandomHeroicAmulet) && !Item.IsHeroic)
                            {
                                session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("ITEM_NOT_HEROIC"), 0));
                                return;
                            }
                            if (Item.IsHeroic && Rare == 8)
                            {
                                session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("ALREADY_MAX_RARE"), 0));
                                return;
                            }

                            if (protection == RarifyProtection.Scroll && !isCommand)
                            {
                                session.Character.Inventory.RemoveItemAmount(scrollVnum);
                                session.SendPacket("shop_end 2");
                            }
                            session.Character.Gold -= goldprice;
                            session.Character.Inventory.RemoveItemAmount(cellaVnum, cella);
                            session.SendPacket(session.Character.GenerateGold());
                            break;

                        case RarifyMode.Drop:
                            break;

                        case RarifyMode.HeroEquipmentDowngrade:
                            {
                                rarify(7, true);
                                return;
                            }

                        default:
                            throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
                    }
                }

                void rarify(sbyte rarity, bool isHeroEquipmentDowngrade = false)
                {
                    Rare = rarity;
                    if (mode != RarifyMode.Drop)
                    {
                        Logger.LogUserEvent("GAMBLE", session.GenerateIdentity(), $"[RarifyItem]Protection: {protection.ToString()} IIId: {Id} ItemVnum: {ItemVNum} Result: Success");

                        session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey(isHeroEquipmentDowngrade ? "RARIFY_DOWNGRADE_SUCCESS" : "RARIFY_SUCCESS"), Rare), 12));
                        session.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey(isHeroEquipmentDowngrade ? "RARIFY_DOWNGRADE_SUCCESS" : "RARIFY_SUCCESS"), Rare), 0));
                        session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, CharacterId, 3005), session.Character.PositionX, session.Character.PositionY);
                        session.SendPacket("shop_end 1");
                    }
                    SetRarityPoint();

                    if (!isHeroEquipmentDowngrade)
                    {
                        GenerateHeroicShell(protection);
                    }
                }

                if (forceRare != 0)
                {
                    rarify((sbyte)forceRare);
                    return;
                }
                if (Item.IsHeroic && protection != RarifyProtection.None)
                {
                    if (rnd < rarifyRate[10])
                    {
                        rarify(8);
                        if (mode != RarifyMode.Drop && session != null)
                        {
                            ItemInstance inventory = session.Character.Inventory.GetItemInstanceById(Id);
                            if (inventory != null)
                            {
                                session.SendPacket(inventory.GenerateInventoryAdd());
                            }
                        }
                        return;
                    }
                }
                /*if (rnd < rare[10] && !(protection == RarifyProtection.Scroll && Rare >= 8))
                {
                    rarify(8);
                }*/
                if (rnd < rarifyRate[9] && !(protection == RarifyProtection.Scroll && Rare >= 7))
                {
                    rarify(7);
                }
                else if (rnd < rarifyRate[8] && !(protection == RarifyProtection.Scroll && Rare >= 6))
                {
                    rarify(6);
                }
                else if (rnd < rarifyRate[7] && !(protection == RarifyProtection.Scroll && Rare >= 5))
                {
                    rarify(5);
                }
                else if (rnd < rarifyRate[6] && !(protection == RarifyProtection.Scroll && Rare >= 4))
                {
                    rarify(4);
                }
                else if (rnd < rarifyRate[5] && !(protection == RarifyProtection.Scroll && Rare >= 3))
                {
                    rarify(3);
                }
                else if (rnd < rarifyRate[4] && !(protection == RarifyProtection.Scroll && Rare >= 2))
                {
                    rarify(2);
                }
                else if (rnd < rarifyRate[3] && !(protection == RarifyProtection.Scroll && Rare >= 1))
                {
                    rarify(1);
                }
                else if (rnd < rarifyRate[2] && !(protection == RarifyProtection.Scroll && Rare >= 0))
                {
                    rarify(0);
                }
                else if (rnd < rarifyRate[1] && !(protection == RarifyProtection.Scroll && Rare >= -1))
                {
                    rarify(-1);
                }
                else if (rnd < rarifyRate[0] && !(protection == RarifyProtection.Scroll && Rare >= -2))
                {
                    rarify(-2);
                }
                else if (Rare < 1 && Item.ItemType == ItemType.Shell)
                {
                    Rare = 1;
                }
                else if (mode != RarifyMode.Drop && session != null)
                {
                    switch (protection)
                    {
                        case RarifyProtection.BlueAmulet:
                        case RarifyProtection.RedAmulet:
                        case RarifyProtection.HeroicAmulet:
                        case RarifyProtection.RandomHeroicAmulet:
                            session.Character.RemoveBuff(62);
                            ItemInstance amulet = session.Character.Inventory.LoadBySlotAndType((short)EquipmentType.Amulet, InventoryType.Wear);
                            amulet.DurabilityPoint -= 1;
                            if (amulet.DurabilityPoint <= 0)
                            {
                                session.Character.DeleteItemByItemInstanceId(amulet.Id);
                                session.SendPacket($"info {Language.Instance.GetMessageFromKey("AMULET_DESTROYED")}");
                                session.SendPacket(session.Character.GenerateEquipment());
                            }
                            else
                            {
                                session.Character.AddBuff(new Buff(62, session.Character.Level), session.Character.BattleEntity);
                            }
                            break;
                        case RarifyProtection.None:
                            session.Character.DeleteItemByItemInstanceId(Id);
                            session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("RARIFY_FAILED"), 11));
                            session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("RARIFY_FAILED"), 0));
                            return;
                    }
                    session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("RARIFY_FAILED_ITEM_SAVED"), 11));
                    session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("RARIFY_FAILED_ITEM_SAVED"), 0));
                    session.CurrentMapInstance.Broadcast(session.Character.GenerateEff(3004), session.Character.PositionX, session.Character.PositionY);
                    return;
                }
                if (mode != RarifyMode.Drop && session != null)
                {
                    ItemInstance inventory = session.Character.Inventory.GetItemInstanceById(Id);
                    if (inventory != null)
                    {
                        session.SendPacket(inventory.GenerateInventoryAdd());
                    }
                }
            }
            else
            {
                byte[] rarifyRate = new byte[ItemHelper.RarifyRate.Length];
                ItemHelper.RarifyRate.CopyTo(rarifyRate, 0);
                if (session?.HasCurrentMapInstance == false)
                {
                    return;
                }
                if (mode != RarifyMode.Drop || Item.ItemType == ItemType.Shell)
                {
                    rarifyRate[0] = 0;
                    rarifyRate[1] = 0;
                    rarifyRate[2] = 0;
                    rnd = ServerManager.RandomNumber(0, 80);
                }
                else
                {
                    rnd = ServerManager.RandomNumber(0, 1000) / 10D;
                }
                if (protection == RarifyProtection.RedAmulet ||
                    protection == RarifyProtection.HeroicAmulet ||
                    protection == RarifyProtection.RandomHeroicAmulet)
                {
                    for (byte i = 0; i < rarifyRate.Length; i++)
                    {
                        rarifyRate[i] = (byte)(rarifyRate[i] * reducedchancefactor);
                    }
                }
                if (session != null)
                {
                    switch (mode)
                    {
                        case RarifyMode.Free:
                            break;

                        case RarifyMode.Success:
                            if (Item.IsHeroic && Rare >= 8 || !Item.IsHeroic && Rare <= 7)
                            {
                                session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("ALREADY_MAX_RARE"), 10));
                                return;
                            }
                            Rare += 1;
                            SetRarityPoint();
                            ItemInstance inventory = session?.Character.Inventory.GetItemInstanceById(Id);
                            if (inventory != null)
                            {
                                session.SendPacket(inventory.GenerateInventoryAdd());
                            }
                            return;

                        case RarifyMode.Reduced:
                            if (session.Character.Gold < (long)(goldprice * reducedpricefactor))
                            {
                                return;
                            }
                            if (session.Character.Inventory.CountItem(cellaVnum) < cella * reducedpricefactor)
                            {
                                return;
                            }
                            session.Character.Inventory.RemoveItemAmount(cellaVnum, (int)(cella * reducedpricefactor));
                            session.Character.Gold -= (long)(goldprice * reducedpricefactor);
                            session.SendPacket(session.Character.GenerateGold());
                            break;

                        case RarifyMode.Normal:
                            if (session.Character.Gold < goldprice)
                            {
                                return;
                            }
                            if (session.Character.Inventory.CountItem(cellaVnum) < cella)
                            {
                                return;
                            }
                            if (protection == RarifyProtection.Scroll && !isCommand
                                && session.Character.Inventory.CountItem(scrollVnum) < 1)
                            {
                                return;
                            }

                            if ((protection == RarifyProtection.Scroll || protection == RarifyProtection.BlueAmulet || protection == RarifyProtection.RedAmulet) && !isCommand && Item.IsHeroic)
                            {
                                session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("ITEM_IS_HEROIC"), 0));
                                return;
                            }
                            if ((protection == RarifyProtection.HeroicAmulet ||
                                 protection == RarifyProtection.RandomHeroicAmulet) && !Item.IsHeroic)
                            {
                                session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("ITEM_NOT_HEROIC"), 0));
                                return;
                            }
                            if (Item.IsHeroic && Rare == 8)
                            {
                                session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("ALREADY_MAX_RARE"), 0));
                                return;
                            }

                            if (protection == RarifyProtection.Scroll && !isCommand)
                            {
                                session.Character.Inventory.RemoveItemAmount(scrollVnum);
                                session.SendPacket("shop_end 2");
                            }
                            session.Character.Gold -= goldprice;
                            session.Character.Inventory.RemoveItemAmount(cellaVnum, cella);
                            session.SendPacket(session.Character.GenerateGold());
                            break;

                        case RarifyMode.Drop:
                            break;

                        case RarifyMode.HeroEquipmentDowngrade:
                            {
                                rarify(7, true);
                                return;
                            }

                        default:
                            throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
                    }
                }

                void rarify(sbyte rarity, bool isHeroEquipmentDowngrade = false)
                {
                    Rare = rarity;
                    if (mode != RarifyMode.Drop)
                    {
                        Logger.LogUserEvent("GAMBLE", session.GenerateIdentity(), $"[RarifyItem]Protection: {protection.ToString()} IIId: {Id} ItemVnum: {ItemVNum} Result: Success");

                        session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey(isHeroEquipmentDowngrade ? "RARIFY_DOWNGRADE_SUCCESS" : "RARIFY_SUCCESS"), Rare), 12));
                        session.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey(isHeroEquipmentDowngrade ? "RARIFY_DOWNGRADE_SUCCESS" : "RARIFY_SUCCESS"), Rare), 0));
                        session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, CharacterId, 3005), session.Character.PositionX, session.Character.PositionY);
                        session.SendPacket("shop_end 1");
                    }
                    SetRarityPoint();

                    if (!isHeroEquipmentDowngrade)
                    {
                        GenerateHeroicShell(protection);
                    }
                }

                if (forceRare != 0)
                {
                    rarify((sbyte)forceRare);
                    return;
                }
                if (Item.IsHeroic && protection != RarifyProtection.None)
                {
                    if (rnd < rarifyRate[10])
                    {
                        rarify(8);
                        if (mode != RarifyMode.Drop && session != null)
                        {
                            ItemInstance inventory = session.Character.Inventory.GetItemInstanceById(Id);
                            if (inventory != null)
                            {
                                session.SendPacket(inventory.GenerateInventoryAdd());
                            }
                        }
                        return;
                    }
                }
                /*if (rnd < rare[10] && !(protection == RarifyProtection.Scroll && Rare >= 8))
                {
                    rarify(8);
                }*/
                if (rnd < rarifyRate[9] && !(protection == RarifyProtection.Scroll && Rare >= 7))
                {
                    rarify(7);
                }
                else if (rnd < rarifyRate[8] && !(protection == RarifyProtection.Scroll && Rare >= 6))
                {
                    rarify(6);
                }
                else if (rnd < rarifyRate[7] && !(protection == RarifyProtection.Scroll && Rare >= 5))
                {
                    rarify(5);
                }
                else if (rnd < rarifyRate[6] && !(protection == RarifyProtection.Scroll && Rare >= 4))
                {
                    rarify(4);
                }
                else if (rnd < rarifyRate[5] && !(protection == RarifyProtection.Scroll && Rare >= 3))
                {
                    rarify(3);
                }
                else if (rnd < rarifyRate[4] && !(protection == RarifyProtection.Scroll && Rare >= 2))
                {
                    rarify(2);
                }
                else if (rnd < rarifyRate[3] && !(protection == RarifyProtection.Scroll && Rare >= 1))
                {
                    rarify(1);
                }
                else if (rnd < rarifyRate[2] && !(protection == RarifyProtection.Scroll && Rare >= 0))
                {
                    rarify(0);
                }
                else if (rnd < rarifyRate[1] && !(protection == RarifyProtection.Scroll && Rare >= -1))
                {
                    rarify(-1);
                }
                else if (rnd < rarifyRate[0] && !(protection == RarifyProtection.Scroll && Rare >= -2))
                {
                    rarify(-2);
                }
                else if (Rare < 1 && Item.ItemType == ItemType.Shell)
                {
                    Rare = 1;
                }
                else if (mode != RarifyMode.Drop && session != null)
                {
                    switch (protection)
                    {
                        case RarifyProtection.BlueAmulet:
                        case RarifyProtection.RedAmulet:
                        case RarifyProtection.HeroicAmulet:
                        case RarifyProtection.RandomHeroicAmulet:
                            session.Character.RemoveBuff(62);
                            ItemInstance amulet = session.Character.Inventory.LoadBySlotAndType((short)EquipmentType.Amulet, InventoryType.Wear);
                            amulet.DurabilityPoint -= 1;
                            if (amulet.DurabilityPoint <= 0)
                            {
                                session.Character.DeleteItemByItemInstanceId(amulet.Id);
                                session.SendPacket($"info {Language.Instance.GetMessageFromKey("AMULET_DESTROYED")}");
                                session.SendPacket(session.Character.GenerateEquipment());
                            }
                            else
                            {
                                session.Character.AddBuff(new Buff(62, session.Character.Level), session.Character.BattleEntity);
                            }
                            break;
                        case RarifyProtection.None:
                            session.Character.DeleteItemByItemInstanceId(Id);
                            session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("RARIFY_FAILED"), 11));
                            session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("RARIFY_FAILED"), 0));
                            return;
                    }
                    session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("RARIFY_FAILED_ITEM_SAVED"), 11));
                    session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("RARIFY_FAILED_ITEM_SAVED"), 0));
                    session.CurrentMapInstance.Broadcast(session.Character.GenerateEff(3004), session.Character.PositionX, session.Character.PositionY);
                    return;
                }
                if (mode != RarifyMode.Drop && session != null)
                {
                    ItemInstance inventory = session.Character.Inventory.GetItemInstanceById(Id);
                    if (inventory != null)
                    {
                        session.SendPacket(inventory.GenerateInventoryAdd());
                    }
                }
            }

        }

        public void SetRarityPoint()
        {
            switch (Item.EquipmentSlot)
            {
                case EquipmentType.MainWeapon:
                case EquipmentType.SecondaryWeapon:
                    {
                        int point = CharacterHelper.RarityPoint(Rare, Item.IsHeroic ? (short)(95 + Item.LevelMinimum) : Item.LevelMinimum, false);
                        Concentrate = 0;
                        HitRate = 0;
                        DamageMinimum = 0;
                        DamageMaximum = 0;
                        if (Rare >= 0)
                        {
                            for (int i = 0; i < point; i++)
                            {
                                int rndn = ServerManager.RandomNumber(0, 3);
                                if (rndn == 0)
                                {
                                    Concentrate++;
                                    HitRate++;
                                }
                                else
                                {
                                    DamageMinimum++;
                                    DamageMaximum++;
                                }
                            }
                        }
                        else
                        {
                            for (int i = 0; i > Rare * 10; i--)
                            {
                                DamageMinimum--;
                                DamageMaximum--;
                            }
                        }
                    }
                    break;

                case EquipmentType.Armor:
                    {
                        int point = CharacterHelper.RarityPoint(Rare, Item.IsHeroic ? (short)(95 + Item.LevelMinimum) : Item.LevelMinimum, true);
                        DefenceDodge = 0;
                        DistanceDefenceDodge = 0;
                        DistanceDefence = 0;
                        MagicDefence = 0;
                        CloseDefence = 0;
                        double NewDistanceDefence = 0;
                        double NewMagicDefence = 0;
                        double NewCloseDefence = 0;
                        if (Rare >= 0)
                        {
                            for (int i = 0; i < point; i++)
                            {
                                int rndn = ServerManager.RandomNumber(0, 5);
                                if (rndn == 0)
                                {
                                    DefenceDodge++;
                                    DistanceDefenceDodge++;
                                }
                                else
                                {
                                    NewDistanceDefence = NewDistanceDefence + 0.9;
                                    NewMagicDefence = NewMagicDefence + 0.35;
                                    NewCloseDefence = NewCloseDefence + 0.95;
                                }
                            }
                            DistanceDefence = (short)NewDistanceDefence;
                            MagicDefence = (short)NewMagicDefence;
                            CloseDefence = (short)NewCloseDefence;
                        }
                        else
                        {
                            for (int i = 0; i > Rare * 10; i--)
                            {
                                DistanceDefence--;
                                MagicDefence--;
                                CloseDefence--;
                            }
                        }
                    }
                    break;
            }
        }
        
        public void Sum(ClientSession session, ItemInstance itemToSum)
        {
            if (!session.HasCurrentMapInstance)
            {
                return;
            }
            if (Upgrade < 6)
            {
                short[] upsuccess = { 100, 100, 85, 70, 50, 20 };
                int[] goldprice = { 1500, 3000, 6000, 12000, 24000, 48000 };
                short[] sand = { 5, 10, 15, 20, 25, 30 };
                const int sandVnum = 1027;
                if (Upgrade + itemToSum.Upgrade < 6 && ((itemToSum.Item.EquipmentSlot == EquipmentType.Gloves && Item.EquipmentSlot == EquipmentType.Gloves) || (Item.EquipmentSlot == EquipmentType.Boots && itemToSum.Item.EquipmentSlot == EquipmentType.Boots)))
                {
                    if (session.Character.Gold < goldprice[Upgrade])
                    {
                        return;
                    }
                    if (session.Character.Inventory.CountItem(sandVnum) < sand[Upgrade])
                    {
                        return;
                    }
                    session.Character.Inventory.RemoveItemAmount(sandVnum, (byte)sand[Upgrade]);
                    session.Character.Gold -= goldprice[Upgrade];

                    int rnd = ServerManager.RandomNumber();
                    if (rnd < upsuccess[Upgrade + itemToSum.Upgrade])
                    {
                        Logger.LogUserEvent("SUM_ITEM", session.GenerateIdentity(), $"[SumItem]ItemId {Id} ItemToSumId: {itemToSum.Id} Upgrade: {Upgrade} ItemToSumUpgrade: {itemToSum.Upgrade} Result: Success");

                        Upgrade += (byte)(itemToSum.Upgrade + 1);
                        DarkResistance += (short)(itemToSum.DarkResistance + itemToSum.Item.DarkResistance);
                        LightResistance += (short)(itemToSum.LightResistance + itemToSum.Item.LightResistance);
                        WaterResistance += (short)(itemToSum.WaterResistance + itemToSum.Item.WaterResistance);
                        FireResistance += (short)(itemToSum.FireResistance + itemToSum.Item.FireResistance);
                        session.Character.DeleteItemByItemInstanceId(itemToSum.Id);
                        session.SendPacket($"pdti 10 {ItemVNum} 1 27 {Upgrade} 0");
                        session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("SUM_SUCCESS"), 0));
                        session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("SUM_SUCCESS"), 12));
                        session.SendPacket(UserInterfaceHelper.GenerateGuri(19, 1, session.Character.CharacterId, 1324));
                        session.SendPacket(GenerateInventoryAdd());
                    }
                    else
                    {
                        Logger.LogUserEvent("SUM_ITEM", session.GenerateIdentity(), $"[SumItem]ItemId {Id} ItemToSumId: {itemToSum.Id} Upgrade: {Upgrade} ItemToSumUpgrade: {itemToSum.Upgrade} Result: Fail");

                        session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("SUM_FAILED"), 0));
                        session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("SUM_FAILED"), 11));
                        session.SendPacket(UserInterfaceHelper.GenerateGuri(19, 1, session.Character.CharacterId, 1332));
                        session.Character.DeleteItemByItemInstanceId(itemToSum.Id);
                        session.Character.DeleteItemByItemInstanceId(Id);
                    }
                    session.CurrentMapInstance?.Broadcast(UserInterfaceHelper.GenerateGuri(6, 1, session.Character.CharacterId), session.Character.PositionX, session.Character.PositionY);
                    session.SendPacket(session.Character.GenerateGold());
                    session.SendPacket("shop_end 1");
                }
            }
        }

        public void UpgradeItem(ClientSession session, UpgradeMode mode, UpgradeProtection protection, bool isCommand = false)
        {
            if (!session.HasCurrentMapInstance)
            {
                return;
            }
            if (Upgrade < 10)
            {
                byte[] upfail;
                byte[] upfix;
                int[] goldprice;
                short[] cella;
                byte[] gem;

                if (Rare >= 8)
                {
                    if (ServerManager.Instance.Configuration.DoubleEqUp == true)
                    {
                        upfix = ItemHelper.R8ItemUpgradeFixRate;
                        upfail = ItemHelper.R8ItemUpgradeFailRateEvent;
                    }
                    else
                    {
                        upfix = ItemHelper.R8ItemUpgradeFixRate;
                        upfail = ItemHelper.R8ItemUpgradeFailRateEvent;
                    }

                    goldprice = new[] { 5000, 15000, 30000, 100000, 300000, 800000, 1500000, 4000000, 7000000, 10000000 };
                    cella = new short[] { 40, 100, 160, 240, 320, 440, 560, 760, 960, 1200 };
                    gem = new byte[] { 2, 2, 4, 4, 6, 2, 2, 4, 4, 6 };
                }
                else
                {
                    if (ServerManager.Instance.Configuration.DoubleEqUp)
                    {
                        upfix = ItemHelper.ItemUpgradeFixRate;
                        upfail = ItemHelper.ItemUpgradeFailRateEvent;
                    }
                    else
                    {
                        upfix = ItemHelper.ItemUpgradeFixRate;
                        upfail = ItemHelper.ItemUpgradeFailRate;
                    }

                    goldprice = new[] { 500, 1500, 3000, 10000, 30000, 80000, 150000, 400000, 700000, 1000000 };
                    cella = new short[] { 20, 50, 80, 120, 160, 220, 280, 380, 480, 600 };
                    gem = new byte[] { 1, 1, 2, 2, 3, 1, 1, 2, 2, 3 };
                }

                const short cellaVnum = 1014;
                const short gemVnum = 1015;
                const short gemFullVnum = 1016;
                const double reducedpricefactor = 0.5;
                const short normalScrollVnum = 1218;
                const short goldScrollVnum = 5369;

                if (IsFixed)
                {
                    session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("ITEM_IS_FIXED"), 10));
                    session.SendPacket("shop_end 1");
                    return;
                }
                switch (mode)
                {
                    case UpgradeMode.Free:
                        break;

                    case UpgradeMode.Reduced:
                        if (session.Character.Gold < (long)(goldprice[Upgrade] * reducedpricefactor))
                        {
                            session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NOT_ENOUGH_MONEY"), 10));
                            return;
                        }
                        if (session.Character.Inventory.CountItem(cellaVnum) < cella[Upgrade] * reducedpricefactor)
                        {
                            session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("NOT_ENOUGH_ITEMS"), ServerManager.GetItem(cellaVnum).Name, cella[Upgrade] * reducedpricefactor), 10));
                            return;
                        }
                        if (protection == UpgradeProtection.Protected && !isCommand && session.Character.Inventory.CountItem(goldScrollVnum) < 1)
                        {
                            session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("NOT_ENOUGH_ITEMS"), ServerManager.GetItem(goldScrollVnum).Name, cella[Upgrade] * reducedpricefactor), 10));
                            return;
                        }
                        if (Upgrade < 5)
                        {
                            if (session.Character.Inventory.CountItem(gemVnum) < gem[Upgrade])
                            {
                                session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("NOT_ENOUGH_ITEMS"), ServerManager.GetItem(gemVnum).Name, gem[Upgrade]), 10));
                                return;
                            }
                            session.Character.Inventory.RemoveItemAmount(gemVnum, gem[Upgrade]);
                        }
                        else
                        {
                            if (session.Character.Inventory.CountItem(gemFullVnum) < gem[Upgrade])
                            {
                                session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("NOT_ENOUGH_ITEMS"), ServerManager.GetItem(gemFullVnum).Name, gem[Upgrade]), 10));
                                return;
                            }
                            session.Character.Inventory.RemoveItemAmount(gemFullVnum, gem[Upgrade]);
                        }
                        if (protection == UpgradeProtection.Protected && !isCommand)
                        {
                            session.Character.Inventory.RemoveItemAmount(goldScrollVnum);
                            session.SendPacket("shop_end 2");
                        }
                        session.Character.Gold -= (long)(goldprice[Upgrade] * reducedpricefactor);
                        session.Character.Inventory.RemoveItemAmount(cellaVnum, (int)(cella[Upgrade] * reducedpricefactor));
                        session.SendPacket(session.Character.GenerateGold());
                        break;

                    case UpgradeMode.Normal:
                        if (session.Character.Inventory.CountItem(cellaVnum) < cella[Upgrade])
                        {
                            return;
                        }
                        if (session.Character.Gold < goldprice[Upgrade])
                        {
                            session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NOT_ENOUGH_MONEY"), 10));
                            return;
                        }
                        if (protection == UpgradeProtection.Protected && !isCommand && session.Character.Inventory.CountItem(normalScrollVnum) < 1)
                        {
                            session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("NOT_ENOUGH_ITEMS"), ServerManager.GetItem(normalScrollVnum).Name, 1), 10));
                            return;
                        }
                        if (Upgrade < 5)
                        {
                            if (session.Character.Inventory.CountItem(gemVnum) < gem[Upgrade])
                            {
                                session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("NOT_ENOUGH_ITEMS"), ServerManager.GetItem(gemVnum).Name, gem[Upgrade]), 10));
                                return;
                            }
                            session.Character.Inventory.RemoveItemAmount(gemVnum, gem[Upgrade]);
                        }
                        else
                        {
                            if (session.Character.Inventory.CountItem(gemFullVnum) < gem[Upgrade])
                            {
                                session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("NOT_ENOUGH_ITEMS"), ServerManager.GetItem(gemFullVnum).Name, gem[Upgrade]), 10));
                                return;
                            }
                            session.Character.Inventory.RemoveItemAmount(gemFullVnum, gem[Upgrade]);
                        }
                        if (protection == UpgradeProtection.Protected && !isCommand)
                        {
                            session.Character.Inventory.RemoveItemAmount(normalScrollVnum);
                            session.SendPacket("shop_end 2");
                        }
                        session.Character.Inventory.RemoveItemAmount(cellaVnum, cella[Upgrade]);
                        session.Character.Gold -= goldprice[Upgrade];
                        session.SendPacket(session.Character.GenerateGold());
                        break;
                }
                ItemInstance wearable = session.Character.Inventory.GetItemInstanceById(Id);
                int rnd = ServerManager.RandomNumber();
                if (Rare == 8)
                {
                    if (rnd < upfail[Upgrade])
                    {
                        Logger.LogUserEvent("UPGRADE_ITEM", session.GenerateIdentity(), $"[UpgradeItem]ItemType: {wearable.Item.ItemType} Protection: {protection.ToString()} IIId: {Id} Upgrade: {wearable.Upgrade} Result: Fail");

                        if (protection == UpgradeProtection.None)
                        {
                            session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("UPGRADE_FAILED"), 11));
                            session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("UPGRADE_FAILED"), 0));
                            session.Character.DeleteItemByItemInstanceId(Id);
                        }
                        else
                        {
                            session.CurrentMapInstance.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, session.Character.CharacterId, 3004), session.Character.PositionX, session.Character.PositionY);
                            session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("SCROLL_PROTECT_USED"), 11));
                            session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("UPGRADE_FAILED_ITEM_SAVED"), 0));
                        }
                    }
                    else if (rnd < upfix[Upgrade])
                    {
                        Logger.LogUserEvent("UPGRADE_ITEM", session.GenerateIdentity(), $"[UpgradeItem]ItemType: {wearable.Item.ItemType} Protection: {protection.ToString()} IIId: {Id} Upgrade: {wearable.Upgrade} Result: Fixed");

                        session.CurrentMapInstance.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, session.Character.CharacterId, 3004), session.Character.PositionX, session.Character.PositionY);
                        wearable.IsFixed = true;
                        session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("UPGRADE_FIXED"), 11));
                        session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("UPGRADE_FIXED"), 0));
                    }
                    else
                    {
                        Logger.LogUserEvent("UPGRADE_ITEM", session.GenerateIdentity(), $"[UpgradeItem]ItemType: {wearable.Item.ItemType} Protection: {protection.ToString()} IIId: {Id} Upgrade: {wearable.Upgrade} Result: Success");

                        session.CurrentMapInstance.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, session.Character.CharacterId, 3005), session.Character.PositionX, session.Character.PositionY);
                        session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("UPGRADE_SUCCESS"), 12));
                        session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("UPGRADE_SUCCESS"), 0));
                        wearable.Upgrade++;
                        if (wearable.Upgrade > 4)
                        {
                            session.Character.Family?.InsertFamilyLog(FamilyLogType.ItemUpgraded, session.Character.Name, itemVNum: wearable.ItemVNum, upgrade: wearable.Upgrade);
                        }
                        session.SendPacket(wearable.GenerateInventoryAdd());
                    }
                }
                else
                {
                    if (rnd < upfix[Upgrade])
                    {
                        Logger.LogUserEvent("UPGRADE_ITEM", session.GenerateIdentity(), $"[UpgradeItem]ItemType: {wearable.Item.ItemType} Protection: {protection.ToString()} IIId: {Id} Upgrade: {wearable.Upgrade} Result: Fixed");

                        session.CurrentMapInstance.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, session.Character.CharacterId, 3004), session.Character.PositionX, session.Character.PositionY);
                        wearable.IsFixed = true;
                        session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("UPGRADE_FIXED"), 11));
                        session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("UPGRADE_FIXED"), 0));
                    }
                    else if (rnd < upfail[Upgrade] + upfix[Upgrade])
                    {
                        Logger.LogUserEvent("UPGRADE_ITEM", session.GenerateIdentity(), $"[UpgradeItem]ItemType: {wearable.Item.ItemType} Protection: {protection.ToString()} IIId: {Id} Upgrade: {wearable.Upgrade} Result: Fail");

                        if (protection == UpgradeProtection.None)
                        {
                            session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("UPGRADE_FAILED"), 11));
                            session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("UPGRADE_FAILED"), 0));
                            session.Character.DeleteItemByItemInstanceId(Id);
                        }
                        else
                        {
                            session.CurrentMapInstance.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, session.Character.CharacterId, 3004), session.Character.PositionX, session.Character.PositionY);
                            session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("SCROLL_PROTECT_USED"), 11));
                            session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("UPGRADE_FAILED_ITEM_SAVED"), 0));
                        }
                    }
                    else
                    {
                        Logger.LogUserEvent("UPGRADE_ITEM", session.GenerateIdentity(), $"[UpgradeItem]ItemType: {wearable.Item.ItemType} Protection: {protection.ToString()} IIId: {Id} Upgrade: {wearable.Upgrade} Result: Success");

                        session.CurrentMapInstance.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, session.Character.CharacterId, 3005), session.Character.PositionX, session.Character.PositionY);
                        session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("UPGRADE_SUCCESS"), 12));
                        session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("UPGRADE_SUCCESS"), 0));
                        wearable.Upgrade++;
                        if (wearable.Upgrade > 4)
                        {
                            session.Character.Family?.InsertFamilyLog(FamilyLogType.ItemUpgraded, session.Character.Name, itemVNum: wearable.ItemVNum, upgrade: wearable.Upgrade);
                        }
                        session.SendPacket(wearable.GenerateInventoryAdd());
                    }
                }
                session.SendPacket("shop_end 1");
            }
        }

        public void ConvertToPartnerEquipment(ClientSession session)
        {
            const int sandVnum = 1027;
            long goldprice = 2000 + Item.LevelMinimum * 300;
            
            if (session.Character.Gold >= goldprice && session.Character.Inventory.CountItem(sandVnum) >= Item.LevelMinimum)
            {
                session.Character.Inventory.RemoveItemAmount(sandVnum, Item.LevelMinimum);
                session.Character.Gold -= goldprice;

                IsPartnerEquipment = true;
                ShellEffects.Clear();
                ShellRarity = null;
                DAOFactory.ShellEffectDAO.DeleteByEquipmentSerialId(EquipmentSerialId);
                BoundCharacterId = null;
                HoldingVNum = ItemVNum;
                
                switch (Item.EquipmentSlot)
                {
                    case EquipmentType.MainWeapon:
                    case EquipmentType.SecondaryWeapon:
                        switch (Item.Class)
                        {
                            case 2:
                                ItemVNum = 990;
                                break;
                            case 4:
                                ItemVNum = 991;
                                break;
                            case 8:
                                ItemVNum = 992;
                                break;
                        }
                        break;
                    case EquipmentType.Armor:
                        switch (Item.Class)
                        {
                            case 2:
                                ItemVNum = 997;
                                break;
                            case 4:
                                ItemVNum = 996;
                                break;
                            case 8:
                                ItemVNum = 995;
                                break;
                        }
                        break;
                }
                session.SendPacket(GenerateInventoryAdd());
                session.SendPacket("shop_end 1");
            }
        }

        #endregion
    }
}