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
using System.Collections.Generic;
using System.Linq;
using OpenNos.GameObject.Networking;
using System;

namespace OpenNos.GameObject
{
    public class BoxItem : Item
    {
        #region Instantiation

        public BoxItem(ItemDTO item) : base(item)
        {
        }

        #endregion

        #region Methods

        public override void Use(ClientSession session, ref ItemInstance inv, byte Option = 0, string[] packetsplit = null)
        {
            if (session.Character.IsVehicled && Effect != 888)
            {
                session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("CANT_DO_VEHICLED"), 10));
                return;
            }

            if (inv.ItemVNum == 333 || inv.ItemVNum == 334) // Sealed Jajamaru Specialist Card & Sealed Princess Sakura Bead
            {
                return;
            }

            switch (Effect)
            {
                case 0:
                    if (Option == 0)
                    {
                        if (packetsplit?.Length == 9)
                        {
                            ItemInstance box = session.Character.Inventory.LoadBySlotAndType(inv.Slot, InventoryType.Equipment);
                            if (box != null)
                            {
                                if (box.Item.ItemSubType == 3)
                                {
                                    session.SendPacket($"qna #guri^300^8023^{inv.Slot} {Language.Instance.GetMessageFromKey("ASK_OPEN_BOX")}");
                                }
                                else if (box.HoldingVNum == 0)
                                {
                                    session.SendPacket($"qna #guri^300^8023^{inv.Slot}^{packetsplit[3]} {Language.Instance.GetMessageFromKey("ASK_STORE_PET")}");
                                }
                                else
                                {
                                    session.SendPacket($"qna #guri^300^8023^{inv.Slot} {Language.Instance.GetMessageFromKey("ASK_RELEASE_PET")}");
                                }
                            }
                        }
                    }
                    else
                    {
                        //u_i 2 2000000 0 21 0 0
                        ItemInstance box = session.Character.Inventory.LoadBySlotAndType(inv.Slot, InventoryType.Equipment);
                        if (box != null)
                        {
                            if (box.Item.ItemSubType == 3)
                            {
                                List<RollGeneratedItemDTO> roll = box.Item.RollGeneratedItems.Where(s => s.MinimumOriginalItemRare <= box.Rare
                                                   && s.MaximumOriginalItemRare >= box.Rare
                                                   && s.OriginalItemDesign == box.Design).ToList();
                                int probabilities = roll.Sum(s => s.Probability);
                                int rnd = ServerManager.RandomNumber(0, probabilities);
                                int currentrnd = 0;
                                foreach (RollGeneratedItemDTO rollitem in roll.OrderBy(s => ServerManager.RandomNumber()))
                                {
                                    currentrnd += rollitem.Probability;
                                    if (currentrnd >= rnd)
                                    {
                                        Item i = ServerManager.GetItem(rollitem.ItemGeneratedVNum);
                                        sbyte rare = 0;
                                        byte upgrade = 0;
                                        if (i.ItemType == ItemType.Armor || i.ItemType == ItemType.Weapon || i.ItemType == ItemType.Shell || i.ItemType == ItemType.Box)
                                        {
                                            rare = box.Rare;
                                        }
                                        if (i.ItemType == ItemType.Shell)
                                        {
                                            if (rare < 1)
                                            {
                                                rare = 1;
                                            }
                                            else if (rare > 7)
                                            {
                                                rare = 7;
                                            }
                                            upgrade = (byte)ServerManager.RandomNumber(50, 81);
                                        }
                                        if (rollitem.IsRareRandom)
                                        {
                                            rnd = ServerManager.RandomNumber(0, 100);

                                            for (int j = ItemHelper.RareRate.Length - 1; j >= 0; j--)
                                            {
                                                if (rnd < ItemHelper.RareRate[j])
                                                {
                                                    rare = (sbyte)j;
                                                    break;
                                                }
                                            }
                                            if (rare < 1)
                                            {
                                                rare = 1;
                                            }
                                        }
                                        session.Character.GiftAdd(rollitem.ItemGeneratedVNum, rollitem.ItemGeneratedAmount, (byte)rare, upgrade, rollitem.ItemGeneratedDesign);
                                        session.SendPacket($"rdi {rollitem.ItemGeneratedVNum} {rollitem.ItemGeneratedAmount}");
                                        session.Character.Inventory.RemoveItemFromInventory(box.Id);
                                        return;

                                        //newInv = session.Character.Inventory.AddNewToInventory(rollitem.ItemGeneratedVNum, amount: rollitem.ItemGeneratedAmount, Design: design, Rare: rare);
                                        //if (newInv.Count > 0)
                                        //{
                                        //    short Slot = inv.Slot;
                                        //    if (Slot != -1)
                                        //    {
                                        //        session.SendPacket(session.Character.GenerateSay(
                                        //            $"{Language.Instance.GetMessageFromKey("ITEM_ACQUIRED")}: {newInv.FirstOrDefault(s => s != null)?.Item?.Name} x {rollitem.ItemGeneratedAmount}",
                                        //            12));
                                        //        newInv.Where(s => s != null).ToList()
                                        //            .ForEach(s => session.SendPacket(s.GenerateInventoryAdd()));
                                        //        session.Character.Inventory
                                        //            .RemoveItemAmountFromInventory(box.Id);
                                        //    }
                                        //}
                                    }
                                }
                            }
                            else if (box.HoldingVNum == 0)
                            {
                                if (packetsplit.Length == 1 && int.TryParse(packetsplit[0], out int PetId) && session.Character.Mates.Find(s => s.MateTransportId == PetId) is Mate mate)
                                {
                                    if (ItemSubType == 0 && mate.MateType != MateType.Pet || ItemSubType == 1 && mate.MateType != MateType.Partner)
                                    {
                                        return;
                                    }
                                    if (mate.MateType == MateType.Partner && mate.GetInventory().Count > 0)
                                    {
                                        session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("EQ_NOT_EMPTY"), 0));
                                        return;
                                    }
                                    box.HoldingVNum = mate.NpcMonsterVNum;
                                    box.SpLevel = mate.Level;
                                    box.SpDamage = mate.Attack;
                                    box.SpDefence = mate.Defence;
                                    session.Character.Mates.Remove(mate);
                                    if (mate.MateType == MateType.Partner)
                                    {
                                        byte i = 0;
                                        session.Character.Mates.Where(s => s.MateType == MateType.Partner).ToList().ForEach(s =>
                                        {
                                            s.GetInventory().ForEach(item => item.Type = (InventoryType)(13 + i));
                                            s.PetId = i;
                                            i++;
                                        });
                                    }
                                    session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("PET_STORED")));
                                    session.SendPacket(UserInterfaceHelper.GeneratePClear());
                                    session.SendPackets(session.Character.GenerateScP());
                                    session.SendPackets(session.Character.GenerateScN());
                                    session.CurrentMapInstance?.Broadcast(mate.GenerateOut());
                                }
                            }
                            else
                            {
                                NpcMonster heldMonster = ServerManager.GetNpcMonster(box.HoldingVNum);
                                if (heldMonster != null)
                                {
                                    Mate mate = new Mate(session.Character, heldMonster, box.SpLevel, ItemSubType == 0 ? MateType.Pet : MateType.Partner)
                                    {
                                        Attack = box.SpDamage,
                                        Defence = box.SpDefence
                                    };
                                    if (session.Character.AddPet(mate))
                                    {
                                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                        session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("PET_LEAVE_BEAD")));
                                    }
                                }
                            }
                        }
                    }
                    break;

                case 1:
                    if (Option == 0)
                    {
                        session.SendPacket($"qna #guri^300^8023^{inv.Slot} {Language.Instance.GetMessageFromKey("ASK_RELEASE_PET")}");
                    }
                    else
                    {
                        NpcMonster heldMonster = ServerManager.GetNpcMonster((short)EffectValue);
                        if (session.CurrentMapInstance == session.Character.Miniland && heldMonster != null)
                        {
                            Mate mate = new Mate(session.Character, heldMonster, LevelMinimum, ItemSubType == 1 ? MateType.Partner : MateType.Pet);
                            if (session.Character.AddPet(mate))
                            {
                                session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("PET_LEAVE_BEAD")));
                            }
                        }
                        else
                        {
                            session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NOT_IN_MINILAND"), 12));
                        }
                    }

                    break;

                case 69:
                case 6969:
                    if (EffectValue == 1 || EffectValue == 2)
                    {
                        ItemInstance box = session.Character.Inventory.LoadBySlotAndType(inv.Slot, InventoryType.Equipment);
                        if (box != null)
                        {
                            if (box.HoldingVNum == 0)
                            {
                                session.SendPacket($"wopen 44 {inv.Slot}");
                            }
                            else
                            {
                                List<ItemInstance> newInv = session.Character.Inventory.AddNewToInventory(box.HoldingVNum);
                                if (newInv.Count > 0)
                                {
                                    ItemInstance itemInstance = newInv[0];
                                    ItemInstance specialist = session.Character.Inventory.LoadBySlotAndType(itemInstance.Slot, itemInstance.Type);
                                    if (specialist != null)
                                    {
                                        specialist.SlDamage = box.SlDamage;
                                        specialist.SlDefence = box.SlDefence;
                                        specialist.SlElement = box.SlElement;
                                        specialist.SlHP = box.SlHP;
                                        specialist.SpDamage = box.SpDamage;
                                        specialist.SpDark = box.SpDark;
                                        specialist.SpDefence = box.SpDefence;
                                        specialist.SpElement = box.SpElement;
                                        specialist.SpFire = box.SpFire;
                                        specialist.SpHP = box.SpHP;
                                        specialist.SpLevel = box.SpLevel;
                                        specialist.SpLight = box.SpLight;
                                        specialist.SpStoneUpgrade = box.SpStoneUpgrade;
                                        specialist.SpWater = box.SpWater;
                                        specialist.Upgrade = box.Upgrade;
                                        specialist.EquipmentSerialId = box.EquipmentSerialId;
                                        specialist.XP = box.XP;
                                    }
                                    short Slot = inv.Slot;
                                    if (Slot != -1)
                                    {
                                        if (specialist != null)
                                        {
                                            session.SendPacket(session.Character.GenerateSay($"{Language.Instance.GetMessageFromKey("ITEM_ACQUIRED")}: {specialist.Item.Name} + {specialist.Upgrade}", 12));
                                            newInv.ForEach(s => session.SendPacket(specialist.GenerateInventoryAdd()));
                                        }
                                        session.Character.Inventory.RemoveItemFromInventory(box.Id);
                                    }
                                }
                                else
                                {
                                    session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_PLACE"), 0));
                                }
                            }
                        }
                    }
                    if (EffectValue == 3)
                    {
                        ItemInstance box = session.Character.Inventory.LoadBySlotAndType(inv.Slot, InventoryType.Equipment);
                        if (box != null)
                        {
                            if (box.HoldingVNum == 0)
                            {
                                session.SendPacket($"guri 26 0 {inv.Slot}");
                            }
                            else
                            {
                                List<ItemInstance> newInv = session.Character.Inventory.AddNewToInventory(box.HoldingVNum);
                                if (newInv.Count > 0)
                                {
                                    ItemInstance itemInstance = newInv[0];
                                    ItemInstance fairy = session.Character.Inventory.LoadBySlotAndType(itemInstance.Slot, itemInstance.Type);
                                    if (fairy != null)
                                    {
                                        fairy.ElementRate = box.ElementRate;
                                    }
                                    short Slot = inv.Slot;
                                    if (Slot != -1)
                                    {
                                        if (fairy != null)
                                        {
                                            session.SendPacket(session.Character.GenerateSay($"{Language.Instance.GetMessageFromKey("ITEM_ACQUIRED")}: {fairy.Item.Name} ({fairy.ElementRate}%)", 12));
                                            newInv.ForEach(s => session.SendPacket(fairy.GenerateInventoryAdd()));
                                        }
                                        session.Character.Inventory.RemoveItemFromInventory(box.Id);
                                    }
                                }
                                else
                                {
                                    session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_PLACE"), 0));
                                }
                            }
                        }
                    }
                    if (EffectValue == 4)
                    {
                        ItemInstance box = session.Character.Inventory.LoadBySlotAndType(inv.Slot, InventoryType.Equipment);
                        if (box != null)
                        {
                            if (box.HoldingVNum == 0)
                            {
                                session.SendPacket($"guri 24 0 {inv.Slot}");
                            }
                            else
                            {
                                List<ItemInstance> newInv = session.Character.Inventory.AddNewToInventory(box.HoldingVNum);
                                if (newInv.Count > 0)
                                {
                                    short Slot = inv.Slot;
                                    if (Slot != -1)
                                    {
                                        session.SendPacket(session.Character.GenerateSay($"{Language.Instance.GetMessageFromKey("ITEM_ACQUIRED")}: {newInv[0].Item.Name} x 1)", 12));
                                        newInv.ForEach(s => session.SendPacket(s.GenerateInventoryAdd()));
                                        session.Character.Inventory.RemoveItemFromInventory(box.Id);
                                    }
                                }
                                else
                                {
                                    session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_PLACE"), 0));
                                }
                            }
                        }
                    }
                    break;
                case 888:
                    if (session.Character.IsVehicled)
                    {
                        if (!session.Character.Buff.Any(s => s.Card.CardId == 336))
                        {
                            if (inv.ItemDeleteTime == null)
                            {
                                inv.ItemDeleteTime = DateTime.Now.AddHours(LevelMinimum);
                            }
                            session.Character.VehicleItem.BCards.ForEach(s => s.ApplyBCards(session.Character.BattleEntity, session.Character.BattleEntity));
                            session.CurrentMapInstance.Broadcast($"eff 1 {session.Character.CharacterId} 885");
                        }
                    }
                    break;

                default:
                    Logger.Warn(string.Format(Language.Instance.GetMessageFromKey("NO_HANDLER_ITEM"), GetType(), VNum, Effect, EffectValue));
                    break;
            }
        }

        #endregion
    }
}