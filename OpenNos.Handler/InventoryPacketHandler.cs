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
using OpenNos.Core.Handling;
using OpenNos.Data;
using OpenNos.Domain;
using OpenNos.GameObject;
using OpenNos.GameObject.Helpers;
using OpenNos.GameObject.Packets.ClientPackets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using OpenNos.GameObject.Networking;
using static OpenNos.Domain.BCardType;

namespace OpenNos.Handler
{
    public class InventoryPacketHandler : IPacketHandler
    {
        #region Instantiation

        public InventoryPacketHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        private ClientSession Session { get; }

        #endregion

        #region Methods

        /// <summary>
        /// b_i packet
        /// </summary>
        /// <param name="bIPacket"></param>
        public void AskToDelete(BIPacket bIPacket)
        {
            if (bIPacket != null)
            {
                if (!Session.Account.VerifiedLock)
                {
                    Session.SendPacket(UserInterfaceHelper.GenerateMsg("You cant do this because your account is blocked. Use $Unlock", 0));
                    return;
                }
                switch (bIPacket.Option)
                {
                    case null:
                        Session.SendPacket(UserInterfaceHelper.GenerateDialog(
                            $"#b_i^{(byte)bIPacket.InventoryType}^{bIPacket.Slot}^1 #b_i^0^0^5 {Language.Instance.GetMessageFromKey("ASK_TO_DELETE")}"));
                        break;

                    case 1:
                        Session.SendPacket(UserInterfaceHelper.GenerateDialog(
                            $"#b_i^{(byte)bIPacket.InventoryType}^{bIPacket.Slot}^2 #b_i^{(byte)bIPacket.InventoryType}^{bIPacket.Slot}^5 {Language.Instance.GetMessageFromKey("SURE_TO_DELETE")}"));
                        break;

                    case 2:
                        if (Session.Character.InExchangeOrTrade || bIPacket.InventoryType == InventoryType.Bazaar)
                        {
                            return;
                        }

                        ItemInstance delInstance =
                            Session.Character.Inventory.LoadBySlotAndType(bIPacket.Slot, bIPacket.InventoryType);
                        Session.Character.DeleteItem(bIPacket.InventoryType, bIPacket.Slot);

                        if (delInstance != null)
                        {
                            Logger.LogUserEvent("ITEM_DELETE", Session.GenerateIdentity(),
                                $"[DeleteItem]IIId: {delInstance.Id} ItemVNum: {delInstance.ItemVNum} Amount: {delInstance.Amount} MapId: {Session.CurrentMapInstance?.Map.MapId} MapX: {Session.Character.PositionX} MapY: {Session.Character.PositionY}");
                        }

                        break;
                }
            }
        }

        /// <summary>
        /// deposit packet
        /// </summary>
        /// <param name="depositPacket"></param>
        public void Deposit(DepositPacket depositPacket)
        {
            if (depositPacket != null)
            {
                if (depositPacket.Inventory == InventoryType.Bazaar
                    || depositPacket.Inventory == InventoryType.FamilyWareHouse
                    || depositPacket.Inventory == InventoryType.Miniland)
                {
                    return;
                }
                if (Session.Character.Authority == AuthorityType.User)
                {
                    return;
                }
                ItemInstance item =
                    Session.Character.Inventory.LoadBySlotAndType(depositPacket.Slot, depositPacket.Inventory);
                ItemInstance itemdest = Session.Character.Inventory.LoadBySlotAndType(depositPacket.NewSlot,
                    depositPacket.PartnerBackpack ? InventoryType.PetWarehouse : InventoryType.Warehouse);

                // check if the destination slot is out of range
                if (depositPacket.NewSlot >= (depositPacket.PartnerBackpack
                        ? (Session.Character.StaticBonusList.Any(s => s.StaticBonusType == StaticBonusType.PetBackPack)
                            ? 50
                            : 0)
                        : Session.Character.WareHouseSize))
                {
                    return;
                }

                // check if the character is allowed to move the item
                if (Session.Character.InExchangeOrTrade)
                {
                    return;
                }
                
                // actually move the item from source to destination
                Session.Character.Inventory.DepositItem(depositPacket.Inventory, depositPacket.Slot,
                    depositPacket.Amount, depositPacket.NewSlot, ref item, ref itemdest, depositPacket.PartnerBackpack);
                Logger.LogUserEvent("STASH_DEPOSIT", Session.GenerateIdentity(),
                    $"[Deposit]OldIIId: {item?.Id} NewIIId: {itemdest?.Id} Amount: {depositPacket.Amount} PartnerBackpack: {depositPacket.PartnerBackpack}");
            }
        }

        /// <summary>
        /// eqinfo packet
        /// </summary>
        /// <param name="equipmentInfoPacket"></param>
        public void EquipmentInfo(EquipmentInfoPacket equipmentInfoPacket)
        {
            if (equipmentInfoPacket != null)
            {
                bool isNpcShopItem = false;
                ItemInstance inventory = null;
                switch (equipmentInfoPacket.Type)
                {
                    case 0:
                        inventory = Session.Character.Inventory.LoadBySlotAndType(equipmentInfoPacket.Slot,
                            InventoryType.Wear);
                        break;

                    case 1:
                        inventory = Session.Character.Inventory.LoadBySlotAndType(equipmentInfoPacket.Slot,
                            InventoryType.Equipment);
                        break;

                    case 2:
                        isNpcShopItem = true;
                        if (ServerManager.GetItem(equipmentInfoPacket.Slot) != null)
                        {
                            inventory = new ItemInstance(equipmentInfoPacket.Slot, 1);
                            break;
                        }

                        return;

                    case 5:
                        if (Session.Character.ExchangeInfo != null)
                        {
                            ClientSession sess =
                                ServerManager.Instance.GetSessionByCharacterId(Session.Character.ExchangeInfo
                                    .TargetCharacterId);
                            if (sess?.Character.ExchangeInfo?.ExchangeList?.ElementAtOrDefault(equipmentInfoPacket
                                    .Slot) != null)
                            {
                                Guid id = sess.Character.ExchangeInfo.ExchangeList[equipmentInfoPacket.Slot].Id;

                                inventory = sess.Character.Inventory.GetItemInstanceById(id);
                            }
                        }

                        break;

                    case 6:
                        if (equipmentInfoPacket.ShopOwnerId != null)
                        {
                            KeyValuePair<long, MapShop> shop =
                                Session.CurrentMapInstance.UserShops.FirstOrDefault(mapshop =>
                                    mapshop.Value.OwnerId.Equals(equipmentInfoPacket.ShopOwnerId));
                            PersonalShopItem item =
                                shop.Value?.Items.Find(i => i.ShopSlot.Equals(equipmentInfoPacket.Slot));
                            if (item != null)
                            {
                                inventory = item.ItemInstance;
                            }
                        }

                        break;

                    case 7:
                        inventory = Session.Character.Inventory.LoadBySlotAndType(equipmentInfoPacket.MateSlot,
                            (InventoryType)(12 + equipmentInfoPacket.Slot));
                        break;

                    case 10:
                        inventory = Session.Character.Inventory.LoadBySlotAndType(equipmentInfoPacket.Slot,
                            InventoryType.Specialist);
                        break;

                    case 11:
                        inventory = Session.Character.Inventory.LoadBySlotAndType(equipmentInfoPacket.Slot,
                            InventoryType.Costume);
                        break;
                }

                if (inventory?.Item != null)
                {
                    if (inventory.IsEmpty || isNpcShopItem)
                    {
                        Session.SendPacket(inventory.GenerateEInfo());
                        return;
                    }

                    Session.SendPacket(inventory.Item.EquipmentSlot != EquipmentType.Sp ? inventory.GenerateEInfo() :
                        inventory.Item.SpType == 0 && inventory.Item.ItemSubType == 4 ? inventory.GeneratePslInfo() :
                        inventory.GenerateSlInfo(Session));
                }
            }
        }

        // TODO: TRANSLATE IT TO PACKETDEFINITION!
        [Packet("exc_list")]
        public void ExchangeList(string packet)
        {
            string[] packetsplit = packet.Split(' ');

            if (!long.TryParse(packetsplit[2], out long gold))
            {
                return;
            }

            if (!long.TryParse(packetsplit[3], out long GoldBank))
            {
                return;
            }

            if (gold < 0 || gold > Session.Character.Gold || GoldBank < 0 || GoldBank > Session.Character.GoldBank || Session.Character.ExchangeInfo == null || Session.Character.ExchangeInfo.ExchangeList.Any())
            {
                return;
            }

            if (!Session.Account.VerifiedLock)
            {
                Session.SendPacket(UserInterfaceHelper.GenerateMsg("You cant do this because your account is blocked. Use $Unlock", 0));
                return;
            }

            Logger.LogUserEvent("EXC_LIST", Session.GenerateIdentity(),
                $"Packet string: {packet.ToString()}");

            if (Session.Character.ExchangeInfo == null)
            {
                return;
            }
            ClientSession targetSession =
                ServerManager.Instance.GetSessionByCharacterId(Session.Character.ExchangeInfo.TargetCharacterId);
            if (Session.Character.HasShopOpened || targetSession?.Character.HasShopOpened == true)
            {
                CloseExchange(Session, targetSession);
                return;
            }

            if (packetsplit.Length < 4)
            {
                Session.SendPacket("exc_close 0");
                Session.CurrentMapInstance?.Broadcast(Session, "exc_close 0", ReceiverType.OnlySomeone,
                    "", Session.Character.ExchangeInfo.TargetCharacterId);

                if (targetSession != null)
                {
                    targetSession.Character.ExchangeInfo = null;
                }
                Session.Character.ExchangeInfo = null;
                return;
            }

            byte[] type = new byte[10];
            short[] slot = new short[10], qty = new short[10];
            string packetList = "";

            if (gold < 0 || gold > Session.Character.Gold || Session.Character.ExchangeInfo == null
                || Session.Character.ExchangeInfo.ExchangeList.Count > 0)
            {
                return;
            }

            if (GoldBank < 0 || GoldBank > Session.Character.GoldBank || Session.Character.ExchangeInfo == null
                || Session.Character.ExchangeInfo.ExchangeList.Count > 0)
            {
                return;
            }

            for (int j = 7, i = 0; j <= packetsplit.Length && i < 10; j += 3, i++)
            {
                byte.TryParse(packetsplit[j - 3], out type[i]);
                short.TryParse(packetsplit[j - 2], out slot[i]);
                short.TryParse(packetsplit[j - 1], out qty[i]);
                if ((InventoryType)type[i] == InventoryType.Bazaar)
                {
                    CloseExchange(Session, targetSession);
                    return;
                }

                ItemInstance item = Session.Character.Inventory.LoadBySlotAndType(slot[i], (InventoryType)type[i]);
                if (item == null)
                {
                    return;
                }

                if (qty[i] <= 0 || item.Amount < qty[i])
                {
                    return;
                }

                ItemInstance it = item.DeepCopy();
                if (it.Item.IsTradable && !it.IsBound)
                {
                    it.Amount = qty[i];
                    Session.Character.ExchangeInfo.ExchangeList.Add(it);
                    if (type[i] != 0)
                    {
                        packetList += $"{i}.{type[i]}.{it.ItemVNum}.{qty[i]} ";
                    }
                    else
                    {
                        packetList += $"{i}.{type[i]}.{it.ItemVNum}.{it.Rare}.{it.Upgrade} ";
                    }
                }
                else if (it.IsBound)
                {
                    Session.SendPacket("exc_close 0");
                    Session.CurrentMapInstance?.Broadcast(Session, "exc_close 0", ReceiverType.OnlySomeone,
                        "", Session.Character.ExchangeInfo.TargetCharacterId);

                    if (targetSession != null)
                    {
                        targetSession.Character.ExchangeInfo = null;
                    }
                    Session.Character.ExchangeInfo = null;
                    return;
                }
            }

            Session.Character.ExchangeInfo.Gold = gold;
            Session.Character.ExchangeInfo.BankGold = GoldBank;
            Session.CurrentMapInstance?.Broadcast(Session, $"exc_list 1 {Session.Character.CharacterId} {gold} {GoldBank} {packetList}", ReceiverType.OnlySomeone, string.Empty, Session.Character.ExchangeInfo.TargetCharacterId);
            //Session.CurrentMapInstance?.Broadcast(Session, $"exc_list 1 {Session.Character.CharacterId} {gold} {GoldBank} {packetList}", ReceiverType.OnlySomeone, "", Session.Character.ExchangeInfo.TargetCharacterId);
            Session.Character.ExchangeInfo.Validated = true;
        }

        /// <summary>
        /// req_exc packet
        /// </summary>
        /// <param name="exchangeRequestPacket"></param>
        public void ExchangeRequest(ExchangeRequestPacket exchangeRequestPacket)
        {
            if (!Session.Account.VerifiedLock)
            {
                Session.SendPacket(UserInterfaceHelper.GenerateMsg("You cant do this because your account is blocked. Use $Unlock", 0));
                return;
            }

            if (exchangeRequestPacket != null)
            {
                ClientSession sess = ServerManager.Instance.GetSessionByCharacterId(exchangeRequestPacket.CharacterId);

                if (sess != null && Session.Character.MapInstanceId != sess.Character.MapInstanceId)
                {
                    sess.Character.ExchangeInfo = null;
                    Session.Character.ExchangeInfo = null;
                }
                else
                {
                    switch (exchangeRequestPacket.RequestType)
                    {
                        case RequestExchangeType.Requested:
                            if (!Session.HasCurrentMapInstance)
                            {
                                return;
                            }

                            ClientSession targetSession =
                                Session.CurrentMapInstance.GetSessionByCharacterId(exchangeRequestPacket.CharacterId);
                            if (targetSession?.Account == null)
                            {
                                return;
                            }

                            if (!targetSession.Account.VerifiedLock)
                            {
                                Session.SendPacket(UserInterfaceHelper.GenerateMsg("You cant do this because his account is blocked. Use $Unlock", 0));
                                return;
                            }


                            if (targetSession.CurrentMapInstance?.MapInstanceType == MapInstanceType.TalentArenaMapInstance)
                            {
                                return;
                            }

                            if (targetSession.Character.Group != null
                                && targetSession.Character.Group?.GroupType != GroupType.Group)
                            {
                                Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                    Language.Instance.GetMessageFromKey("EXCHANGE_NOT_ALLOWED_IN_RAID"), 0));
                                return;
                            }

                            if (Session.Character.Group != null
                                && Session.Character.Group?.GroupType != GroupType.Group)
                            {
                                Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                    Language.Instance.GetMessageFromKey("EXCHANGE_NOT_ALLOWED_WITH_RAID_MEMBER"), 0));
                                return;
                            }

                            if (Session.Character.IsBlockedByCharacter(exchangeRequestPacket.CharacterId))
                            {
                                Session.SendPacket(
                                    UserInterfaceHelper.GenerateInfo(
                                        Language.Instance.GetMessageFromKey("BLACKLIST_BLOCKED")));
                                return;
                            }

                            if (Session.Character.Speed == 0 || targetSession.Character.Speed == 0)
                            {
                                Session.Character.ExchangeBlocked = true;
                            }

                            if (targetSession.Character.LastSkillUse.AddSeconds(20) > DateTime.Now
                                || targetSession.Character.LastDefence.AddSeconds(20) > DateTime.Now)
                            {
                                Session.SendPacket(UserInterfaceHelper.GenerateInfo(
                                    string.Format(Language.Instance.GetMessageFromKey("PLAYER_IN_BATTLE"),
                                        targetSession.Character.Name)));
                                return;
                            }

                            if (Session.Character.LastSkillUse.AddSeconds(20) > DateTime.Now
                                || Session.Character.LastDefence.AddSeconds(20) > DateTime.Now)
                            {
                                Session.SendPacket(
                                    UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("IN_BATTLE")));
                                return;
                            }

                            if (Session.Character.HasShopOpened || targetSession.Character.HasShopOpened)
                            {
                                Session.SendPacket(
                                    UserInterfaceHelper.GenerateMsg(
                                        Language.Instance.GetMessageFromKey("HAS_SHOP_OPENED"), 10));
                                return;
                            }
                            
                            if (targetSession.Character.ExchangeBlocked)
                            {
                                Session.SendPacket(
                                    Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("TRADE_BLOCKED"),
                                        11));
                            }
                            else
                            {
                                if (Session.Character.InExchangeOrTrade || targetSession.Character.InExchangeOrTrade)
                                {
                                    Session.SendPacket(
                                        UserInterfaceHelper.GenerateModal(
                                            Language.Instance.GetMessageFromKey("ALREADY_EXCHANGE"), 0));
                                }
                                else
                                {
                                    Session.SendPacket(UserInterfaceHelper.GenerateModal(
                                        string.Format(Language.Instance.GetMessageFromKey("YOU_ASK_FOR_EXCHANGE"),
                                            targetSession.Character.Name), 0));

                                    Logger.LogUserEvent("TRADE_REQUEST", Session.GenerateIdentity(),
                                        $"[ExchangeRequest][{targetSession.GenerateIdentity()}]");

                                    Session.Character.TradeRequests.Add(targetSession.Character.CharacterId);
                                    targetSession.SendPacket(UserInterfaceHelper.GenerateDialog(
                                        $"#req_exc^2^{Session.Character.CharacterId} #req_exc^5^{Session.Character.CharacterId} {string.Format(Language.Instance.GetMessageFromKey("INCOMING_EXCHANGE"), Session.Character.Name)}"));
                                }
                            }

                            break;

                        case RequestExchangeType.Confirmed: // click Trade button in exchange window
                            if (Session.HasCurrentMapInstance && Session.HasSelectedCharacter
                                                              && Session.Character.ExchangeInfo != null
                                                              && Session.Character.ExchangeInfo.TargetCharacterId
                                                              != Session.Character.CharacterId)
                            {
                                if (!Session.HasCurrentMapInstance)
                                {
                                    return;
                                }

                                targetSession =
                                    Session.CurrentMapInstance.GetSessionByCharacterId(Session.Character.ExchangeInfo
                                        .TargetCharacterId);

                                if (targetSession == null)
                                {
                                    return;
                                }

                                if (Session.Character.Group != null
                                    && Session.Character.Group?.GroupType != GroupType.Group)
                                {
                                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                        Language.Instance.GetMessageFromKey("EXCHANGE_NOT_ALLOWED_IN_RAID"), 0));
                                    return;
                                }

                                if (targetSession.Character.Group != null
                                    && targetSession.Character.Group?.GroupType != GroupType.Group)
                                {
                                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                        Language.Instance.GetMessageFromKey("EXCHANGE_NOT_ALLOWED_WITH_RAID_MEMBER"),
                                        0));
                                    return;
                                }

                                if (Session.IsDisposing || targetSession.IsDisposing)
                                {
                                    CloseExchange(Session, targetSession);
                                    return;
                                }

                                lock (targetSession.Character.Inventory)
                                {
                                    lock (Session.Character.Inventory)
                                    {
                                        ExchangeInfo targetExchange = targetSession.Character.ExchangeInfo;
                                        Inventory inventory = targetSession.Character.Inventory;

                                        long gold = targetSession.Character.Gold;
                                        var backpack = targetSession.Character.HaveBackpack() ? 1 : 0;
                                        long goldBank = targetSession.Character.GoldBank;
                                        long maxGold = ServerManager.Instance.Configuration.MaxGold;
                                        var maxBankGold = ServerManager.Instance.MaxBankGold;

                                        if (targetExchange == null || Session.Character.ExchangeInfo == null)
                                        {
                                            Logger.LogUserEvent("TRADE_ACCEPT", Session.GenerateIdentity(),
                                            $"[ExchangeAccept][{targetSession.GenerateIdentity()}]");
                                            return;

                                        }

                                        if (Session.Character.ExchangeInfo.Validated && targetExchange.Validated)
                                        {
                                            Session.Character.ExchangeInfo.Confirmed = true;
                                            if (targetExchange.Confirmed && Session.Character.ExchangeInfo.Confirmed)
                                            {
                                                Session.SendPacket("exc_close 1");
                                                targetSession.SendPacket("exc_close 1");

                                                var @continue = true;
                                                var goldmax = false;
                                                if (!Session.Character.Inventory.EnoughPlaceV2(targetExchange.ExchangeList, Session.Character.HaveBackpack() ? 1 : 0))
                                                {
                                                    @continue = false;
                                                }

                                                if (!inventory.EnoughPlaceV2(Session.Character.ExchangeInfo.ExchangeList, backpack))
                                                {
                                                    @continue = false;
                                                }

                                                if (Session.Character.ExchangeInfo.Gold + gold > maxGold)
                                                {
                                                    goldmax = true;
                                                }
                                                if (Session.Character.ExchangeInfo.BankGold + goldBank > maxBankGold)
                                                    goldmax = true;
                                                if (Session.Character.ExchangeInfo.BankGold > Session.Character.GoldBank)
                                                {
                                                    return;
                                                }

                                                if (Session.Character.ExchangeInfo.Gold > Session.Character.Gold)
                                                {
                                                    return;
                                                }



                                                if (targetExchange.BankGold + Session.Character.ExchangeInfo.BankGold > maxBankGold)
                                                    goldmax = true;
                                                if (targetExchange.Gold + Session.Character.Gold > maxGold)
                                                {
                                                    goldmax = true;
                                                }

                                                if (!@continue || goldmax)
                                                {
                                                    var message = !@continue ? UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_PLACE"), 0)
                                                        : UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("MAX_GOLD"), 0);
                                                    Session.SendPacket(message);
                                                    targetSession.SendPacket(message);
                                                    CloseExchange(Session, targetSession);
                                                }
                                                else
                                                {
                                                    if (Session.Character.ExchangeInfo.ExchangeList.Any(ei => !(ei.Item.IsTradable || ei.IsBound)))
                                                    {
                                                        Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("ITEM_NOT_TRADABLE"), 0));
                                                        CloseExchange(Session, targetSession);
                                                    }
                                                    else // all items can be traded
                                                    {
                                                        Session.Character.IsExchanging = targetSession.Character.IsExchanging = true;

                                                        // exchange all items from target to source
                                                        Exchange(targetSession, Session);

                                                        // exchange all items from source to target
                                                        Exchange(Session, targetSession);

                                                        Session.Character.IsExchanging = targetSession.Character.IsExchanging = false;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                Session.SendPacket(UserInterfaceHelper.GenerateInfo(string.Format(Language.Instance.GetMessageFromKey("IN_WAITING_FOR"), targetSession.Character.Name)));
                                            }
                                        }
                                        {
                                            
                                            try
                                            {
                                                Session.Character.ExchangeInfo.Confirmed = true;
                                                if (targetExchange.Confirmed
                                                    && Session.Character.ExchangeInfo.Confirmed)
                                                {
                                                    Session.SendPacket("exc_close 1");
                                                    targetSession.SendPacket("exc_close 1");

                                                    bool continues = true;
                                                    bool goldmax = false;
                                                    if (!Session.Character.Inventory.EnoughPlace(targetExchange
                                                        .ExchangeList))
                                                    {
                                                        continues = false;
                                                    }

                                                    continues &=
                                                        inventory.EnoughPlace(Session.Character.ExchangeInfo
                                                            .ExchangeList);
                                                    goldmax |= Session.Character.ExchangeInfo.Gold + gold > maxGold;
                                                    goldmax |= Session.Character.ExchangeInfo.BankGold + goldBank > maxGold;
                                                    if (Session.Character.ExchangeInfo.Gold > Session.Character.Gold
                                                        || Session.Character.ExchangeInfo.BankGold > Session.Character.GoldBank)
                                                    {
                                                        return;
                                                    }

                                                    goldmax |= targetExchange.Gold + Session.Character.Gold > maxGold;
                                                    goldmax |= targetExchange.BankGold + Session.Character.GoldBank > maxGold;
                                                    if (!continues || goldmax)
                                                    {
                                                        string message = !continues
                                                            ? UserInterfaceHelper.GenerateMsg(
                                                                Language.Instance.GetMessageFromKey("NOT_ENOUGH_PLACE"),
                                                                0)
                                                            : UserInterfaceHelper.GenerateMsg(
                                                                Language.Instance.GetMessageFromKey("MAX_GOLD"), 0);
                                                        Session.SendPacket(message);
                                                        targetSession.SendPacket(message);
                                                        CloseExchange(Session, targetSession);
                                                    }
                                                    else if (Session.Character.Gold < Session.Character.ExchangeInfo.Gold || targetSession.Character.Gold < targetExchange.Gold
                                                        || Session.Character.GoldBank < Session.Character.ExchangeInfo.BankGold || targetSession.Character.GoldBank < targetExchange.BankGold)
                                                    {
                                                        string message = UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("ERROR_ON_EXANGE"), 0);
                                                        Session.SendPacket(message);
                                                        targetSession.SendPacket(message);
                                                        CloseExchange(Session, targetSession);
                                                    }
                                                    else
                                                    {
                                                        if (Session.Character.ExchangeInfo.ExchangeList.Any(ei =>
                                                            !(ei.Item.IsTradable || ei.IsBound)))
                                                        {
                                                            Session.SendPacket(
                                                                UserInterfaceHelper.GenerateMsg(
                                                                    Language.Instance.GetMessageFromKey(
                                                                        "ITEM_NOT_TRADABLE"), 0));
                                                            CloseExchange(Session, targetSession);
                                                        }
                                                        if (targetSession.Character.ExchangeInfo.ExchangeList.Any(ei =>
                                                            !(ei.Item.IsTradable || ei.IsBound)))
                                                        {
                                                            targetSession.SendPacket(
                                                                UserInterfaceHelper.GenerateMsg(
                                                                    Language.Instance.GetMessageFromKey(
                                                                        "ITEM_NOT_TRADABLE"), 0));
                                                            CloseExchange(targetSession, Session);
                                                        }
                                                        else // all items can be traded
                                                        {
                                                            Session.Character.IsExchanging =
                                                                targetSession.Character.IsExchanging = true;

                                                            // exchange all items from target to source
                                                            Exchange(targetSession, Session);

                                                            // exchange all items from source to target
                                                            Exchange(Session, targetSession);

                                                            Session.Character.IsExchanging =
                                                                targetSession.Character.IsExchanging = false;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    Session.SendPacket(UserInterfaceHelper.GenerateInfo(
                                                        string.Format(
                                                            Language.Instance.GetMessageFromKey("IN_WAITING_FOR"),
                                                            targetSession.Character.Name)));
                                                }
                                            }
                                            catch (NullReferenceException nre)
                                            {
                                                Logger.Error(nre);
                                            }
                                        }
                                    }
                                }
                            }

                            break;

                        case RequestExchangeType.Cancelled: // cancel trade thru exchange window
                            if (Session.HasCurrentMapInstance && Session.Character.ExchangeInfo != null)
                            {
                                targetSession =
                                    Session.CurrentMapInstance.GetSessionByCharacterId(Session.Character.ExchangeInfo
                                        .TargetCharacterId);
                                CloseExchange(Session, targetSession);
                            }

                            break;

                        case RequestExchangeType.List:
                            if (sess != null && (!Session.Character.InExchangeOrTrade || !sess.Character.InExchangeOrTrade))
                            {
                                ClientSession otherSession =
                                    ServerManager.Instance.GetSessionByCharacterId(exchangeRequestPacket.CharacterId);
                                if (exchangeRequestPacket.CharacterId == Session.Character.CharacterId
                                    || Session.Character.Speed == 0 || otherSession == null
                                    || otherSession.Character.TradeRequests.All(s => s != Session.Character.CharacterId))
                                {
                                    return;
                                }

                                if (Session.Character.Group != null
                                    && Session.Character.Group?.GroupType != GroupType.Group)
                                {
                                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                        Language.Instance.GetMessageFromKey("EXCHANGE_NOT_ALLOWED_IN_RAID"), 0));
                                    return;
                                }

                                if (otherSession.Character.Group != null
                                    && otherSession.Character.Group?.GroupType != GroupType.Group)
                                {
                                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                        Language.Instance.GetMessageFromKey("EXCHANGE_NOT_ALLOWED_WITH_RAID_MEMBER"),
                                        0));
                                    return;
                                }

                                Session.SendPacket($"exc_list 1 {exchangeRequestPacket.CharacterId} -1");
                                Session.SendPacket($"gbex {Session.Character.GoldBank / 1000} {Session.Character.Gold} 0 0");
                                Session.Character.ExchangeInfo = new ExchangeInfo
                                {
                                    TargetCharacterId = exchangeRequestPacket.CharacterId,
                                    Confirmed = false
                                };
                                sess.Character.ExchangeInfo = new ExchangeInfo
                                {
                                    TargetCharacterId = Session.Character.CharacterId,
                                    Confirmed = false
                                };
                                Session.CurrentMapInstance?.Broadcast(Session,
                                    $"exc_list 1 {Session.Character.CharacterId} -1", ReceiverType.OnlySomeone,
                                    "", exchangeRequestPacket.CharacterId);
                                ClientSession test = ServerManager.Instance.GetSessionByCharacterId(exchangeRequestPacket.CharacterId);
                                test.SendPacket($"gbex {test.Character.GoldBank / 1000} {test.Character.Gold} 0 0");
                            }
                            else
                            {
                                Session.CurrentMapInstance?.Broadcast(Session,
                                    UserInterfaceHelper.GenerateModal(
                                        Language.Instance.GetMessageFromKey("ALREADY_EXCHANGE"), 0),
                                    ReceiverType.OnlySomeone, "", exchangeRequestPacket.CharacterId);
                            }

                            break;

                        case RequestExchangeType.Declined:
                            if (sess != null)
                            {
                                sess.Character.ExchangeInfo = null;
                            }
                            Session.Character.ExchangeInfo = null;
                            Session.SendPacket(
                                Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("YOU_REFUSED"), 10));
                            if (sess != null)
                            {
                                sess.SendPacket(
                                    Session.Character.GenerateSay(
                                        string.Format(Language.Instance.GetMessageFromKey("EXCHANGE_REFUSED"),
                                            Session.Character.Name), 10));

                            }

                            break;

                        default:
                            Logger.Warn(
                                $"Exchange-Request-Type not implemented. RequestType: {exchangeRequestPacket.RequestType})");
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// get packet
        /// </summary>
        /// <param name="getPacket"></param>
        public void GetItem(GetPacket getPacket)
        {
            if (!Session.Account.VerifiedLock)
            {
                Session.SendPacket(UserInterfaceHelper.GenerateMsg("You cant do this because your account is blocked. Use $Unlock", 0));
                return;
            }

            if (getPacket == null || Session.Character.LastSkillUse.AddSeconds(1) > DateTime.Now
                || (Session.Character.IsVehicled
                 && Session.CurrentMapInstance?.MapInstanceType != MapInstanceType.EventGameInstance)
                || !Session.HasCurrentMapInstance
                || Session.Character.IsSeal
                || (Session.CurrentMapInstance.MapInstanceType == MapInstanceType.TimeSpaceInstance && Session.CurrentMapInstance.InstanceBag.EndState != 0))
            {
                return;
            }


            if (getPacket.TransportId < 100000)
            {
                MapButton button = Session.CurrentMapInstance.Buttons.Find(s => s.MapButtonId == getPacket.TransportId);
                if (button != null)
                {
                    Session.SendPacket(UserInterfaceHelper.GenerateDelay(2000, 1, $"#git^{button.MapButtonId}"));
                }
            }
            else
            {
                lock (Session.CurrentMapInstance.DroppedList)
                {
                    if (!Session.CurrentMapInstance.DroppedList.ContainsKey(getPacket.TransportId))
                    {
                        return;
                    }

                    MapItem mapItem = Session.CurrentMapInstance.DroppedList[getPacket.TransportId];

                    if (mapItem != null)
                    {
                        bool canpick = false;
                        switch (getPacket.PickerType)
                        {
                            case 1:
                                canpick = Session.Character.IsInRange(mapItem.PositionX, mapItem.PositionY, 8);
                                break;

                            case 2:
                                Mate mate = Session.Character.Mates.Find(s =>
                                    s.MateTransportId == getPacket.PickerId && s.CanPickUp);
                                if (mate != null)
                                {
                                    canpick = mate.IsInRange(mapItem.PositionX, mapItem.PositionY, 8);
                                }

                                break;
                        }

                        

                        if (canpick && Session.HasCurrentMapInstance)
                        {
                            if (mapItem is MonsterMapItem item)
                            {
                                MonsterMapItem monsterMapItem = item;
                                if (Session.CurrentMapInstance.MapInstanceType != MapInstanceType.LodInstance
                                    && monsterMapItem.OwnerId.HasValue && monsterMapItem.OwnerId.Value != -1)
                                {
                                    Group group = ServerManager.Instance.Groups.Find(g =>
                                        g.IsMemberOfGroup(monsterMapItem.OwnerId.Value)
                                        && g.IsMemberOfGroup(Session.Character.CharacterId));
                                    if (item.CreatedDate.AddSeconds(30) > DateTime.Now
                                        && !(monsterMapItem.OwnerId == Session.Character.CharacterId
                                          || (group?.SharingMode == (byte)GroupSharingType.Everyone)))
                                    {
                                        Session.SendPacket(
                                            Session.Character.GenerateSay(
                                                Language.Instance.GetMessageFromKey("NOT_YOUR_ITEM"), 10));
                                        return;
                                    }
                                }

                                // initialize and rarify
                                item.Rarify(null);
                            }

                            if (mapItem.ItemVNum != 1046)
                            {
                                ItemInstance mapItemInstance = mapItem.GetItemInstance();

                                if (mapItemInstance?.Item == null)
                                {
                                    return;
                                }

                                if (mapItemInstance.Item.ItemType == ItemType.Map || mapItem.IsQuest)
                                {
                                    if (mapItem is MonsterMapItem)
                                    {
                                        Session.Character.IncrementQuests(QuestType.Collect1, mapItem.ItemVNum);
                                        if (mapItem.IsQuest)
                                        {
                                            Session.Character.IncrementQuests(QuestType.Collect2, mapItem.ItemVNum);
                                            Session.Character.IncrementQuests(QuestType.Collect4, mapItem.ItemVNum);
                                        }
                                    }
                                    if (mapItemInstance.Item.Effect == 71)
                                    {
                                        Session.Character.SpPoint += mapItem.GetItemInstance().Item.EffectValue;
                                        if (Session.Character.SpPoint > 10000)
                                        {
                                            Session.Character.SpPoint = 10000;
                                        }

                                        Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                            string.Format(Language.Instance.GetMessageFromKey("SP_POINTSADDED"),
                                                mapItem.GetItemInstance().Item.EffectValue), 0));
                                        Session.SendPacket(Session.Character.GenerateSpPoint());
                                    }

                                    #region Flower Quest

                                    if (mapItem.ItemVNum == 1086 && ServerManager.Instance.FlowerQuestId != null)
                                    {
                                        Session.Character.AddQuest((long)ServerManager.Instance.FlowerQuestId, false);
                                    }

                                    #endregion

                                    Session.CurrentMapInstance.DroppedList.Remove(getPacket.TransportId);

                                    Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateGet(getPacket.PickerType, getPacket.PickerId, getPacket.TransportId));

                                    if (getPacket.PickerType == 2)
                                    {
                                        Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Npc, getPacket.PickerId, 5004));
                                    }
                                }
                                else
                                {
                                    lock (Session.Character.Inventory)
                                    {
                                        long characterDropperId = 0;
                                        if (mapItemInstance.CharacterId > 0)
                                        {
                                            characterDropperId = mapItemInstance.CharacterId;
                                        }
                                        short amount = mapItem.Amount;
                                        ItemInstance inv = Session.Character.Inventory.AddToInventory(mapItemInstance)
                                            .FirstOrDefault();
                                        if (inv != null)
                                        {
                                            Session.CurrentMapInstance.DroppedList.Remove(getPacket.TransportId);

                                            Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateGet(getPacket.PickerType, getPacket.PickerId, getPacket.TransportId));

                                            if (getPacket.PickerType == 2)
                                            {
                                                Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Npc, getPacket.PickerId, 5004));
                                                Session.SendPacket(Session.Character.GenerateIcon(1, 1, inv.ItemVNum));
                                            }

                                            Session.SendPacket(Session.Character.GenerateSay(
                                                $"{Language.Instance.GetMessageFromKey("ITEM_ACQUIRED")}: {inv.Item.Name} x {amount}",
                                                12));
                                            if (Session.CurrentMapInstance.MapInstanceType == MapInstanceType.LodInstance)
                                            {
                                                Session.CurrentMapInstance?.Broadcast(
                                                    Session.Character.GenerateSay(
                                                        $"{string.Format(Language.Instance.GetMessageFromKey("ITEM_ACQUIRED_LOD"), Session.Character.Name)}: {inv.Item.Name} x {mapItem.Amount}",
                                                        10));
                                            }

                                            Logger.LogUserEvent("CHARACTER_ITEM_GET", Session.GenerateIdentity(),
                                                $"[GetItem]IIId: {inv.Id} ItemVNum: {inv.ItemVNum} Amount: {amount}");
                                        }
                                        else
                                        {
                                            //Test also then change to msgi shit
                                            Session.SendPacket($"msgi 0 414 1 0");
                                         //   Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                           //     Language.Instance.GetMessageFromKey("NOT_ENOUGH_PLACE"), 0));
                                        }
                                    }
                                }
                            }
                            else
                            {
                                // handle gold drop
                                long maxGold = ServerManager.Instance.Configuration.MaxGold;

                                double multiplier = 1 + (Session.Character.GetBuff(CardType.Item, (byte)AdditionalTypes.Item.IncreaseEarnedGold)[0] / 100D);
                                multiplier += (Session.Character.ShellEffectMain.FirstOrDefault(s => s.Effect == (byte)ShellWeaponEffectType.GainMoreGold)?.Value ?? 0) / 100D;
                                
                                if (mapItem is MonsterMapItem droppedGold
                                    && Session.Character.Gold + (droppedGold.GoldAmount * multiplier) <= maxGold)
                                {
                                    if (getPacket.PickerType == 2)
                                    {
                                        Session.SendPacket(Session.Character.GenerateIcon(1, 1, 1046));
                                    }

                                    if (ServerManager.Instance.Configuration.DoubleGold == true)
                                    {
                                        Session.Character.Gold += (int)(droppedGold.GoldAmount * multiplier) * 2;

                                        Logger.LogUserEvent("CHARACTER_ITEM_GET", Session.GenerateIdentity(), $"[GetItem]Gold: {(int)(droppedGold.GoldAmount * multiplier) * 2})");

                                        Session.SendPacket(Session.Character.GenerateSay(
                                            $"{Language.Instance.GetMessageFromKey("ITEM_ACQUIRED")}: {mapItem.GetItemInstance().Item.Name} x {droppedGold.GoldAmount * 2}{(multiplier > 1 ? $" + {(int)(droppedGold.GoldAmount * multiplier) * 2 - droppedGold.GoldAmount * 2}" : "")}",
                                            12));
                                    }
                                    else
                                    {
                                        Session.Character.Gold += (int)(droppedGold.GoldAmount * multiplier);

                                        Logger.LogUserEvent("CHARACTER_ITEM_GET", Session.GenerateIdentity(), $"[GetItem]Gold: {(int)(droppedGold.GoldAmount * multiplier)})");

                                        Session.SendPacket(Session.Character.GenerateSay(
                                            $"{Language.Instance.GetMessageFromKey("ITEM_ACQUIRED")}: {mapItem.GetItemInstance().Item.Name} x {droppedGold.GoldAmount}{(multiplier > 1 ? $" + {(int)(droppedGold.GoldAmount * multiplier) - droppedGold.GoldAmount}" : "")}",
                                            12));
                                    }
                                }
                                else
                                {
                                    Session.Character.Gold = maxGold;
                                    Logger.LogUserEvent("CHARACTER_ITEM_GET", Session.GenerateIdentity(), "[MaxGold]");
                                    Session.SendPacket(
                                        UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("MAX_GOLD"),
                                            0));
                                }

                                Session.SendPacket(Session.Character.GenerateGold());
                                Session.CurrentMapInstance.DroppedList.Remove(getPacket.TransportId);

                                Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateGet(getPacket.PickerType, getPacket.PickerId, getPacket.TransportId));

                                if (getPacket.PickerType == 2)
                                {
                                    Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Npc, getPacket.PickerId, 5004));
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// mve packet
        /// </summary>
        /// <param name="mvePacket"></param>
        public void MoveEquipment(MvePacket mvePacket)
        {
            if (mvePacket != null && mvePacket.InventoryType != InventoryType.Bazaar)
            {
                lock (Session.Character.Inventory)
                {
                    if (mvePacket.Slot.Equals(mvePacket.DestinationSlot)
                        && mvePacket.InventoryType.Equals(mvePacket.DestinationInventoryType))
                    {
                        return;
                    }

                    if (mvePacket.DestinationSlot > 48 + ((Session.Character.HaveBackpack() ? 1 : 0) * 72))
                    {
                        return;
                    }
                    InventoryType[] allowedInventoryTypes = { InventoryType.Equipment, InventoryType.Specialist, InventoryType.Costume };

                    if (allowedInventoryTypes.All(s => s != mvePacket.InventoryType) || allowedInventoryTypes.All(s => s != mvePacket.DestinationInventoryType))
                    {
                        return;
                    }
                    if (Session.Character.InExchangeOrTrade)
                    {
                        return;
                    }
                    if (mvePacket.DestinationInventoryType == InventoryType.Miniland)
                    {

                        //later you can do this clear
                        // Session.SendPacket("msg Stop trying to dupe.");
                        return;
                    }
                    ItemInstance sourceItem =
                        Session.Character.Inventory.LoadBySlotAndType(mvePacket.Slot, mvePacket.InventoryType);
                    if (sourceItem?.Item.ItemType == ItemType.Specialist
                        || sourceItem?.Item.ItemType == ItemType.Fashion)
                    {
                        if (mvePacket.DestinationInventoryType==InventoryType.Miniland)
                        {

                            //later you can do this clear
                           // Session.SendPacket("msg Stop trying to dupe.");
                            return;
                        }
                        ItemInstance inv = Session.Character.Inventory.MoveInInventory(mvePacket.Slot,
                            mvePacket.InventoryType, mvePacket.DestinationInventoryType, mvePacket.DestinationSlot,
                            false);
                        if (inv != null)
                        {
                            Session.SendPacket(inv.GenerateInventoryAdd());
                            Session.SendPacket(
                                UserInterfaceHelper.Instance.GenerateInventoryRemove(mvePacket.InventoryType,
                                    mvePacket.Slot));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// mvi packet
        /// </summary>
        /// <param name="mviPacket"></param>
        public void MoveItem(MviPacket mviPacket)
        {
            if (!Session.Account.VerifiedLock)
            {
                Session.SendPacket(UserInterfaceHelper.GenerateMsg("You cant do this because your account is blocked. Use $Unlock", 0));
                return;
            }

            if (mviPacket != null)
            {
                if (mviPacket.InventoryType != InventoryType.Equipment
                    && mviPacket.InventoryType != InventoryType.Main
                    && mviPacket.InventoryType != InventoryType.Etc
                    && mviPacket.InventoryType != InventoryType.Miniland)
                {
                    return;
                }

                if (mviPacket.Amount < 1)
                {
                    return;
                }

                if (mviPacket.Slot == mviPacket.DestinationSlot)
                {
                    return;
                }

                if (mviPacket.InventoryType == InventoryType.Wear)
                {
                    return;
                }


                lock (Session.Character.Inventory)
                {
                    // check if the destination slot is out of range
                    if (mviPacket.DestinationSlot > 48 + ((Session.Character.HaveBackpack() ? 1 : 0) * 72))
                    {
                        return;
                    }

                    // check if the character is allowed to move the item
                    if (Session.Character.InExchangeOrTrade)
                    {
                        return;
                    }

                    // actually move the item from source to destination
                    Session.Character.Inventory.MoveItem(mviPacket.InventoryType, mviPacket.InventoryType,
                        mviPacket.Slot, mviPacket.Amount, mviPacket.DestinationSlot, out ItemInstance previousInventory,
                        out ItemInstance newInventory);
                    if (newInventory == null)
                    {
                        return;
                    }

                    Session.SendPacket(newInventory.GenerateInventoryAdd());

                    Session.SendPacket(previousInventory != null
                        ? previousInventory.GenerateInventoryAdd()
                        : UserInterfaceHelper.Instance.GenerateInventoryRemove(mviPacket.InventoryType,
                            mviPacket.Slot));
                }
            }
        }

        /// <summary>
        /// put packet
        /// </summary>
        /// <param name="putPacket"></param>
        public void PutItem(PutPacket putPacket)
        {


            if (putPacket == null || Session.Character.HasShopOpened)
            {
                return;
            }
            if (!Session.Account.VerifiedLock)
            {
                Session.SendPacket(UserInterfaceHelper.GenerateMsg("You cant do this because your account is blocked. Use $Unlock", 0));
                return;
            }

            lock (Session.Character.Inventory)
            {
                ItemInstance invitem =
                    Session.Character.Inventory.LoadBySlotAndType(putPacket.Slot, putPacket.InventoryType);
                if (invitem?.Item.IsDroppable == true && invitem.Item.IsTradable
                    && !Session.Character.InExchangeOrTrade && putPacket.InventoryType != InventoryType.Bazaar)
                {
                    if (putPacket.Amount > 0 && putPacket.Amount < 1000)
                    {
                        if (Session.Character.MapInstance.DroppedList.Count < 200 && Session.HasCurrentMapInstance)
                        {
                            MapItem droppedItem = Session.CurrentMapInstance.PutItem(putPacket.InventoryType,
                                putPacket.Slot, putPacket.Amount, ref invitem, Session);
                            if (droppedItem == null)
                            {
                                Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                    Language.Instance.GetMessageFromKey("ITEM_NOT_DROPPABLE_HERE"), 0));
                                return;
                            }

                            Session.SendPacket(invitem.GenerateInventoryAdd());

                            if (invitem.Amount == 0)
                            {
                                Session.Character.DeleteItem(invitem.Type, invitem.Slot);
                            }

                            Logger.LogUserEvent("CHARACTER_ITEM_DROP", Session.GenerateIdentity(),
                                $"[PutItem]IIId: {invitem.Id} ItemVNum: {droppedItem.ItemVNum} Amount: {droppedItem.Amount} MapId: {Session.CurrentMapInstance.Map.MapId} MapX: {droppedItem.PositionX} MapY: {droppedItem.PositionY}");
                            Session.CurrentMapInstance?.Broadcast(
                                $"drop {droppedItem.ItemVNum} {droppedItem.TransportId} {droppedItem.PositionX} {droppedItem.PositionY} {droppedItem.Amount} 0 -1");
                        }
                        else
                        {
                            Session.SendPacket(
                                UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("DROP_MAP_FULL"),
                                    0));
                        }
                    }
                    else
                    {
                        Session.SendPacket(
                            UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("BAD_DROP_AMOUNT"), 0));
                    }
                }
                else
                {
                    Session.SendPacket(
                        UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("ITEM_NOT_DROPPABLE"), 0));
                }
            }
        }

        /// <summary>
        /// remove packet
        /// </summary>
        /// <param name="removePacket"></param>
        public void Remove(RemovePacket removePacket)
        {
            if (!Session.Account.VerifiedLock)
            {
                Session.SendPacket(UserInterfaceHelper.GenerateMsg("You cant do this because your account is blocked. Use $Unlock", 0));
                return;
            }

            if (removePacket != null)
            {
                InventoryType equipment;
                Mate mate = null;
                if (removePacket.Type > 0)
                {
                    equipment = (InventoryType)(12 + removePacket.Type);
                    mate = Session.Character.Mates.Find(s => s.MateType == MateType.Partner && s.PetId == removePacket.Type - 1);
                    if (mate.IsTemporalMate)
                    {
                        return;
                    }
                }
                else
                {
                    equipment = InventoryType.Wear;
                }

                if (Session.HasCurrentMapInstance
                    && Session.CurrentMapInstance.UserShops.FirstOrDefault(mapshop =>
                        mapshop.Value.OwnerId.Equals(Session.Character.CharacterId)).Value == null
                    && (Session.Character.ExchangeInfo == null
                     || (Session.Character.ExchangeInfo?.ExchangeList).Count == 0))
                {
                    ItemInstance inventory =
                        Session.Character.Inventory.LoadBySlotAndType(removePacket.InventorySlot, equipment);
                    if (inventory != null)
                    {
                        double currentRunningSeconds =
                            (DateTime.Now - Process.GetCurrentProcess().StartTime.AddSeconds(-50)).TotalSeconds;
                        double timeSpanSinceLastSpUsage = currentRunningSeconds - Session.Character.LastSp;
                        if (removePacket.Type == 0)
                        {
                            if (removePacket.InventorySlot == (byte)EquipmentType.Sp && Session.Character.UseSp && !Session.Character.IsSeal)
                            {
                                if (Session.Character.IsVehicled)
                                {
                                    Session.SendPacket(
                                        UserInterfaceHelper.GenerateMsg(
                                            Language.Instance.GetMessageFromKey("REMOVE_VEHICLE"), 0));
                                    return;
                                }

                                if (Session.Character.LastSkillUse.AddSeconds(2) > DateTime.Now)
                                {
                                    return;
                                }

                                if (Session.Character.Timespace != null && Session.Character.Timespace.SpNeeded?[(byte)Session.Character.Class] != 0 && Session.Character.Timespace.InstanceBag.Lock)
                                {
                                    return;
                                }

                                if (!Session.Character.RemoveSp(inventory.ItemVNum, false))
                                {
                                    return;
                                }

                                Session.Character.LastSp =
                                    (DateTime.Now - Process.GetCurrentProcess().StartTime.AddSeconds(-50)).TotalSeconds;
                            }
                            else if (removePacket.InventorySlot == (byte)EquipmentType.Sp
                                     && !Session.Character.UseSp
                                     && timeSpanSinceLastSpUsage <= Session.Character.SpCooldown)
                            {
                                Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                    string.Format(Language.Instance.GetMessageFromKey("SP_INLOADING"),
                                        Session.Character.SpCooldown - (int)Math.Round(timeSpanSinceLastSpUsage, 0)),
                                    0));
                                return;
                            }
                            else if (removePacket.InventorySlot == (byte)EquipmentType.Fairy
                                     && Session.Character.IsUsingFairyBooster)
                            {
                                Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                    Language.Instance.GetMessageFromKey("REMOVE_FAIRY_WHILE_USING_BOOSTER"), 0));
                                return;
                            }
                            
                            if ((inventory.ItemDeleteTime >= DateTime.Now || inventory.DurabilityPoint > 0) && Session.Character.Buff.ContainsKey(62))
                            {
                                Session.Character.RemoveBuff(62);
                            }

                            Session.Character.EquipmentBCards.RemoveAll(o => o.ItemVNum == inventory.ItemVNum);
                        }

                        ItemInstance inv = Session.Character.Inventory.MoveInInventory(removePacket.InventorySlot,
                            equipment, InventoryType.Equipment);

                        if (inv == null)
                        {
                            Session.SendPacket(
                                UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_PLACE"),
                                    0));
                            return;
                        }

                        if (inv.Slot != -1)
                        {
                            Session.SendPacket(inventory.GenerateInventoryAdd());
                        }

                        if (removePacket.Type == 0)
                        {
                            Session.SendPackets(Session.Character.GenerateStatChar());
                            Session.CurrentMapInstance?.Broadcast(Session.Character.GenerateEq());
                            Session.SendPacket(Session.Character.GenerateEquipment());
                            Session.CurrentMapInstance?.Broadcast(Session.Character.GeneratePairy());
                        }
                        else if (mate != null)
                        {
                            switch (inv.Item.EquipmentSlot)
                            {
                                case EquipmentType.Armor:
                                    mate.ArmorInstance = null;
                                    break;

                                case EquipmentType.MainWeapon:
                                    mate.WeaponInstance = null;
                                    break;

                                case EquipmentType.Gloves:
                                    mate.GlovesInstance = null;
                                    break;

                                case EquipmentType.Boots:
                                    mate.BootsInstance = null;
                                    break;

                                case EquipmentType.Sp:
                                    {
                                        if (mate.IsUsingSp)
                                        {
                                            mate.RemoveSp();
                                            mate.StartSpCooldown();
                                        }

                                        mate.Sp = null;
                                    }
                                    break;
                            }
                            mate.BattleEntity.BCards.RemoveAll(o => o.ItemVNum == inventory.HoldingVNum);
                            Session.SendPacket(mate.GenerateScPacket());
                        }
                        ItemInstance ring = Session.Character.Inventory.LoadBySlotAndType((byte)EquipmentType.Ring, InventoryType.Wear);
                        ItemInstance bracelet = Session.Character.Inventory.LoadBySlotAndType((byte)EquipmentType.Bracelet, InventoryType.Wear);
                        ItemInstance necklace = Session.Character.Inventory.LoadBySlotAndType((byte)EquipmentType.Necklace, InventoryType.Wear);
                        Session.Character.CellonOptions.Clear();
                        if (ring != null)
                        {
                            Session.Character.CellonOptions.AddRange(ring.CellonOptions);
                        }
                        if (bracelet != null)
                        {
                            Session.Character.CellonOptions.AddRange(bracelet.CellonOptions);
                        }
                        if (necklace != null)
                        {
                            Session.Character.CellonOptions.AddRange(necklace.CellonOptions);
                        }
                        Session.SendPacket(Session.Character.GenerateStat());
                    }
                }
            }
        }

        /// <summary>
        /// repos packet
        /// </summary>
        /// <param name="reposPacket"></param>
        public void Repos(ReposPacket reposPacket)
        {
            if (reposPacket != null)
            {
                Logger.LogUserEvent("STASH_REPOS", Session.GenerateIdentity(),
                    $"[ItemReposition]OldSlot: {reposPacket.OldSlot} NewSlot: {reposPacket.NewSlot} Amount: {reposPacket.Amount} PartnerBackpack: {reposPacket.PartnerBackpack}");

                if (reposPacket.Amount < 1)
                {
                    return;
                }

                if (reposPacket.OldSlot == reposPacket.NewSlot)
                {
                    return;
                }

                // check if the destination slot is out of range
                if (reposPacket.NewSlot >= (reposPacket.PartnerBackpack
                        ? (Session.Character.StaticBonusList.Any(s => s.StaticBonusType == StaticBonusType.PetBackPack)
                            ? 50
                            : 0)
                        : Session.Character.WareHouseSize))
                {
                    return;
                }

                // check if the character is allowed to move the item
                if (Session.Character.InExchangeOrTrade)
                {
                    return;
                }

                // actually move the item from source to destination
                Session.Character.Inventory.MoveItem(
                    reposPacket.PartnerBackpack ? InventoryType.PetWarehouse : InventoryType.Warehouse,
                    reposPacket.PartnerBackpack ? InventoryType.PetWarehouse : InventoryType.Warehouse,
                    reposPacket.OldSlot, reposPacket.Amount, reposPacket.NewSlot, out ItemInstance previousInventory,
                    out ItemInstance newInventory);

                if (newInventory == null)
                {
                    return;
                }

                Session.SendPacket(reposPacket.PartnerBackpack
                    ? newInventory.GeneratePStash()
                    : newInventory.GenerateStash());
                Session.SendPacket(previousInventory != null
                    ? (reposPacket.PartnerBackpack
                        ? previousInventory.GeneratePStash()
                        : previousInventory.GenerateStash())
                    : (reposPacket.PartnerBackpack
                        ? UserInterfaceHelper.Instance.GeneratePStashRemove(reposPacket.OldSlot)
                        : UserInterfaceHelper.Instance.GenerateStashRemove(reposPacket.OldSlot)));
            }
        }

        /// <summary>
        /// sortopen packet
        /// </summary>
        /// <param name="sortOpenPacket"></param>
        public void SortOpen(SortOpenPacket sortOpenPacket)
        {
            if (sortOpenPacket != null)
            {
                bool gravity = true;
                while (gravity)
                {
                    gravity = false;
                    for (short i = 0; i < 2; i++)
                    {
                        for (short x = 0; x < 44; x++)
                        {
                            InventoryType type = i == 0 ? InventoryType.Specialist : InventoryType.Costume;
                            if (Session.Character.Inventory.LoadBySlotAndType(x, type) == null
                                && Session.Character.Inventory.LoadBySlotAndType((short)(x + 1), type)
                                != null)
                            {
                                Session.Character.Inventory.MoveItem(type, type, (short)(x + 1), 1, x,
                                    out ItemInstance _, out ItemInstance invdest);
                                Session.SendPacket(invdest.GenerateInventoryAdd());
                                Session.Character.DeleteItem(type, (short)(x + 1));
                                gravity = true;
                            }
                        }

                        Session.Character.Inventory.Reorder(Session,
                            i == 0 ? InventoryType.Specialist : InventoryType.Costume);
                    }
                }
            }
        }

        /// <summary>
        /// s_carrier packet
        /// </summary>
        /// <param name="specialistHolderPacket"></param>
        public void SpecialistHolder(SpecialistHolderPacket specialistHolderPacket)
        {
            if (specialistHolderPacket != null)
            {
               ItemInstance specialist =
                    Session.Character.Inventory.LoadBySlotAndType(specialistHolderPacket.Slot, InventoryType.Equipment);

                ItemInstance holder = Session.Character.Inventory.LoadBySlotAndType(specialistHolderPacket.HolderSlot,
                    InventoryType.Equipment);

                if (specialist?.Item == null || holder?.Item == null)
                {
                    return;
                }

                if (!holder.Item.IsHolder)
                {
                    return;
                }

                if (holder.HoldingVNum > 0)
                {
                    return;
                }

                if (holder.Item.ItemType == ItemType.Box && holder.Item.ItemSubType == 2)
                {
                    if (specialist.Item.ItemType != ItemType.Specialist || !specialist.Item.IsSoldable || specialist.Item.Class == 0)
                    {
                        return;
                    }

                    if (specialist.ItemVNum >= 4494 && specialist.ItemVNum <= 4496)
                    {
                        Session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("CANT_HOLD_SP")));
                        Session.SendPacket("shop_end 2");
                        return;
                    }

                    Session.Character.Inventory.RemoveItemFromInventory(specialist.Id);

                    holder.HoldingVNum = specialist.ItemVNum;
                    holder.SlDamage = specialist.SlDamage;
                    holder.SlDefence = specialist.SlDefence;
                    holder.SlElement = specialist.SlElement;
                    holder.SlHP = specialist.SlHP;
                    holder.SpDamage = specialist.SpDamage;
                    holder.SpDark = specialist.SpDark;
                    holder.SpDefence = specialist.SpDefence;
                    holder.SpElement = specialist.SpElement;
                    holder.SpFire = specialist.SpFire;
                    holder.SpHP = specialist.SpHP;
                    holder.SpLevel = specialist.SpLevel;
                    holder.SpLight = specialist.SpLight;
                    holder.SpStoneUpgrade = specialist.SpStoneUpgrade;
                    holder.SpWater = specialist.SpWater;
                    holder.Upgrade = specialist.Upgrade;
                    holder.XP = specialist.XP;
                    holder.EquipmentSerialId = specialist.EquipmentSerialId;

                    Session.SendPacket("shop_end 2");
                }
            }
        }

        /// <summary>
        /// sl packet
        /// </summary>
        /// <param name="spTransformPacket"></param>
        public void SpTransform(SpTransformPacket spTransformPacket)
        {
            if (spTransformPacket != null && !Session.Character.IsSeal && !Session.Character.IsMorphed)
            {
                ItemInstance specialistInstance =
                    Session.Character.Inventory.LoadBySlotAndType((byte)EquipmentType.Sp, InventoryType.Wear);

                if (spTransformPacket.Type == 10)
                {
                    short specialistDamage = spTransformPacket.SpecialistDamage,
                        specialistDefense = spTransformPacket.SpecialistDefense,
                        specialistElement = spTransformPacket.SpecialistElement,
                        specialistHealpoints = spTransformPacket.SpecialistHP;
                    int transportId = spTransformPacket.TransportId;
                    if (!Session.Character.UseSp || specialistInstance == null
                        || transportId != specialistInstance.TransportId)
                    {
                        Session.SendPacket(
                            UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("SPUSE_NEEDED"), 0));
                        return;
                    }

                    if (CharacterHelper.SPPoint(specialistInstance.SpLevel, specialistInstance.Upgrade)
                        - specialistInstance.SlDamage - specialistInstance.SlHP - specialistInstance.SlElement
                        - specialistInstance.SlDefence - specialistDamage - specialistDefense - specialistElement
                        - specialistHealpoints < 0)
                    {
                        return;
                    }

                    if (specialistDamage < 0 || specialistDefense < 0 || specialistElement < 0
                        || specialistHealpoints < 0)
                    {
                        return;
                    }

                    specialistInstance.SlDamage += specialistDamage;
                    specialistInstance.SlDefence += specialistDefense;
                    specialistInstance.SlElement += specialistElement;
                    specialistInstance.SlHP += specialistHealpoints;

                    ItemInstance mainWeapon =
                        Session.Character.Inventory.LoadBySlotAndType((byte)EquipmentType.MainWeapon,
                            InventoryType.Wear);
                    ItemInstance secondaryWeapon =
                        Session.Character.Inventory.LoadBySlotAndType((byte)EquipmentType.MainWeapon,
                            InventoryType.Wear);
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
                        return effects.Where(s => s.Effect == (byte)effectType).OrderByDescending(s => s.Value)
                                   .FirstOrDefault()?.Value ?? 0;
                    }

                    int slElement = CharacterHelper.SlPoint(specialistInstance.SlElement, 2)
                                    + GetShellWeaponEffectValue(ShellWeaponEffectType.SLElement)
                                    + GetShellWeaponEffectValue(ShellWeaponEffectType.SLGlobal);
                    int slHp = CharacterHelper.SlPoint(specialistInstance.SlHP, 3)
                               + GetShellWeaponEffectValue(ShellWeaponEffectType.SLHP)
                               + GetShellWeaponEffectValue(ShellWeaponEffectType.SLGlobal);
                    int slDefence = CharacterHelper.SlPoint(specialistInstance.SlDefence, 1)
                                    + GetShellWeaponEffectValue(ShellWeaponEffectType.SLDefence)
                                    + GetShellWeaponEffectValue(ShellWeaponEffectType.SLGlobal);
                    int slHit = CharacterHelper.SlPoint(specialistInstance.SlDamage, 0)
                                + GetShellWeaponEffectValue(ShellWeaponEffectType.SLDamage)
                                + GetShellWeaponEffectValue(ShellWeaponEffectType.SLGlobal);

                    #region slHit

                    specialistInstance.DamageMinimum = 0;
                    specialistInstance.DamageMaximum = 0;
                    specialistInstance.HitRate = 0;
                    specialistInstance.CriticalLuckRate = 0;
                    specialistInstance.CriticalRate = 0;
                    specialistInstance.DefenceDodge = 0;
                    specialistInstance.DistanceDefenceDodge = 0;
                    specialistInstance.ElementRate = 0;
                    specialistInstance.DarkResistance = 0;
                    specialistInstance.LightResistance = 0;
                    specialistInstance.FireResistance = 0;
                    specialistInstance.WaterResistance = 0;
                    specialistInstance.CriticalDodge = 0;
                    specialistInstance.CloseDefence = 0;
                    specialistInstance.DistanceDefence = 0;
                    specialistInstance.MagicDefence = 0;
                    specialistInstance.HP = 0;
                    specialistInstance.MP = 0;

                    if (slHit >= 1)
                    {
                        specialistInstance.DamageMinimum += 5;
                        specialistInstance.DamageMaximum += 5;
                    }

                    if (slHit >= 10)
                    {
                        specialistInstance.HitRate += 10;
                    }

                    if (slHit >= 20)
                    {
                        specialistInstance.CriticalLuckRate += 2;
                    }

                    if (slHit >= 30)
                    {
                        specialistInstance.DamageMinimum += 5;
                        specialistInstance.DamageMaximum += 5;
                        specialistInstance.HitRate += 10;
                    }

                    if (slHit >= 40)
                    {
                        specialistInstance.CriticalRate += 10;
                    }

                    if (slHit >= 50)
                    {
                        specialistInstance.HP += 200;
                        specialistInstance.MP += 200;
                    }

                    if (slHit >= 60)
                    {
                        specialistInstance.HitRate += 15;
                    }

                    if (slHit >= 70)
                    {
                        specialistInstance.HitRate += 15;
                        specialistInstance.DamageMinimum += 5;
                        specialistInstance.DamageMaximum += 5;
                    }

                    if (slHit >= 80)
                    {
                        specialistInstance.CriticalLuckRate += 3;
                    }

                    if (slHit >= 90)
                    {
                        specialistInstance.CriticalRate += 20;
                    }

                    if (slHit >= 100)
                    {
                        specialistInstance.CriticalLuckRate += 3;
                        specialistInstance.CriticalRate += 20;
                        specialistInstance.HP += 200;
                        specialistInstance.MP += 200;
                        specialistInstance.DamageMinimum += 5;
                        specialistInstance.DamageMaximum += 5;
                        specialistInstance.HitRate += 20;
                    }

                    #endregion

                    #region slDefence

                    if (slDefence >= 10)
                    {
                        specialistInstance.DefenceDodge += 5;
                        specialistInstance.DistanceDefenceDodge += 5;
                    }

                    if (slDefence >= 20)
                    {
                        specialistInstance.CriticalDodge += 2;
                    }

                    if (slDefence >= 30)
                    {
                        specialistInstance.HP += 100;
                    }

                    if (slDefence >= 40)
                    {
                        specialistInstance.CriticalDodge += 2;
                    }

                    if (slDefence >= 50)
                    {
                        specialistInstance.DefenceDodge += 5;
                        specialistInstance.DistanceDefenceDodge += 5;
                    }

                    if (slDefence >= 60)
                    {
                        specialistInstance.HP += 200;
                    }

                    if (slDefence >= 70)
                    {
                        specialistInstance.CriticalDodge += 3;
                    }

                    if (slDefence >= 75)
                    {
                        specialistInstance.FireResistance += 2;
                        specialistInstance.WaterResistance += 2;
                        specialistInstance.LightResistance += 2;
                        specialistInstance.DarkResistance += 2;
                    }

                    if (slDefence >= 80)
                    {
                        specialistInstance.DefenceDodge += 10;
                        specialistInstance.DistanceDefenceDodge += 10;
                        specialistInstance.CriticalDodge += 3;
                    }

                    if (slDefence >= 90)
                    {
                        specialistInstance.FireResistance += 3;
                        specialistInstance.WaterResistance += 3;
                        specialistInstance.LightResistance += 3;
                        specialistInstance.DarkResistance += 3;
                    }

                    if (slDefence >= 95)
                    {
                        specialistInstance.HP += 300;
                    }

                    if (slDefence >= 100)
                    {
                        specialistInstance.DefenceDodge += 20;
                        specialistInstance.DistanceDefenceDodge += 20;
                        specialistInstance.FireResistance += 5;
                        specialistInstance.WaterResistance += 5;
                        specialistInstance.LightResistance += 5;
                        specialistInstance.DarkResistance += 5;
                    }

                    #endregion

                    #region slHp

                    if (slHp >= 5)
                    {
                        specialistInstance.DamageMinimum += 5;
                        specialistInstance.DamageMaximum += 5;
                    }

                    if (slHp >= 10)
                    {
                        specialistInstance.DamageMinimum += 5;
                        specialistInstance.DamageMaximum += 5;
                    }

                    if (slHp >= 15)
                    {
                        specialistInstance.DamageMinimum += 5;
                        specialistInstance.DamageMaximum += 5;
                    }

                    if (slHp >= 20)
                    {
                        specialistInstance.DamageMinimum += 5;
                        specialistInstance.DamageMaximum += 5;
                        specialistInstance.CloseDefence += 10;
                        specialistInstance.DistanceDefence += 10;
                        specialistInstance.MagicDefence += 10;
                    }

                    if (slHp >= 25)
                    {
                        specialistInstance.DamageMinimum += 5;
                        specialistInstance.DamageMaximum += 5;
                    }

                    if (slHp >= 30)
                    {
                        specialistInstance.DamageMinimum += 5;
                        specialistInstance.DamageMaximum += 5;
                    }

                    if (slHp >= 35)
                    {
                        specialistInstance.DamageMinimum += 5;
                        specialistInstance.DamageMaximum += 5;
                    }

                    if (slHp >= 40)
                    {
                        specialistInstance.DamageMinimum += 5;
                        specialistInstance.DamageMaximum += 5;
                        specialistInstance.CloseDefence += 15;
                        specialistInstance.DistanceDefence += 15;
                        specialistInstance.MagicDefence += 15;
                    }

                    if (slHp >= 45)
                    {
                        specialistInstance.DamageMinimum += 10;
                        specialistInstance.DamageMaximum += 10;
                    }

                    if (slHp >= 50)
                    {
                        specialistInstance.DamageMinimum += 10;
                        specialistInstance.DamageMaximum += 10;
                        specialistInstance.FireResistance += 2;
                        specialistInstance.WaterResistance += 2;
                        specialistInstance.LightResistance += 2;
                        specialistInstance.DarkResistance += 2;
                    }

                    if (slHp >= 55)
                    {
                        specialistInstance.DamageMinimum += 10;
                        specialistInstance.DamageMaximum += 10;
                    }

                    if (slHp >= 60)
                    {
                        specialistInstance.DamageMinimum += 10;
                        specialistInstance.DamageMaximum += 10;
                    }

                    if (slHp >= 65)
                    {
                        specialistInstance.DamageMinimum += 10;
                        specialistInstance.DamageMaximum += 10;
                    }

                    if (slHp >= 70)
                    {
                        specialistInstance.DamageMinimum += 10;
                        specialistInstance.DamageMaximum += 10;
                        specialistInstance.CloseDefence += 20;
                        specialistInstance.DistanceDefence += 20;
                        specialistInstance.MagicDefence += 20;
                    }

                    if (slHp >= 75)
                    {
                        specialistInstance.DamageMinimum += 15;
                        specialistInstance.DamageMaximum += 15;
                    }

                    if (slHp >= 80)
                    {
                        specialistInstance.DamageMinimum += 15;
                        specialistInstance.DamageMaximum += 15;
                    }

                    if (slHp >= 85)
                    {
                        specialistInstance.DamageMinimum += 15;
                        specialistInstance.DamageMaximum += 15;
                        specialistInstance.CriticalDodge++;
                    }

                    if (slHp >= 86)
                    {
                        specialistInstance.CriticalDodge++;
                    }

                    if (slHp >= 87)
                    {
                        specialistInstance.CriticalDodge++;
                    }

                    if (slHp >= 88)
                    {
                        specialistInstance.CriticalDodge++;
                    }

                    if (slHp >= 90)
                    {
                        specialistInstance.DamageMinimum += 15;
                        specialistInstance.DamageMaximum += 15;
                        specialistInstance.CloseDefence += 25;
                        specialistInstance.DistanceDefence += 25;
                        specialistInstance.MagicDefence += 25;
                    }

                    if (slHp >= 91)
                    {
                        specialistInstance.DefenceDodge += 2;
                        specialistInstance.DistanceDefenceDodge += 2;
                    }

                    if (slHp >= 92)
                    {
                        specialistInstance.DefenceDodge += 2;
                        specialistInstance.DistanceDefenceDodge += 2;
                    }

                    if (slHp >= 93)
                    {
                        specialistInstance.DefenceDodge += 2;
                        specialistInstance.DistanceDefenceDodge += 2;
                    }

                    if (slHp >= 94)
                    {
                        specialistInstance.DefenceDodge += 2;
                        specialistInstance.DistanceDefenceDodge += 2;
                    }

                    if (slHp >= 95)
                    {
                        specialistInstance.DamageMinimum += 20;
                        specialistInstance.DamageMaximum += 20;
                        specialistInstance.DefenceDodge += 2;
                        specialistInstance.DistanceDefenceDodge += 2;
                    }

                    if (slHp >= 96)
                    {
                        specialistInstance.DefenceDodge += 2;
                        specialistInstance.DistanceDefenceDodge += 2;
                    }

                    if (slHp >= 97)
                    {
                        specialistInstance.DefenceDodge += 2;
                        specialistInstance.DistanceDefenceDodge += 2;
                    }

                    if (slHp >= 98)
                    {
                        specialistInstance.DefenceDodge += 2;
                        specialistInstance.DistanceDefenceDodge += 2;
                    }

                    if (slHp >= 99)
                    {
                        specialistInstance.DefenceDodge += 2;
                        specialistInstance.DistanceDefenceDodge += 2;
                    }

                    if (slHp >= 100)
                    {
                        specialistInstance.FireResistance += 3;
                        specialistInstance.WaterResistance += 3;
                        specialistInstance.LightResistance += 3;
                        specialistInstance.DarkResistance += 3;
                        specialistInstance.CloseDefence += 30;
                        specialistInstance.DistanceDefence += 30;
                        specialistInstance.MagicDefence += 30;
                        specialistInstance.DamageMinimum += 20;
                        specialistInstance.DamageMaximum += 20;
                        specialistInstance.DefenceDodge += 2;
                        specialistInstance.DistanceDefenceDodge += 2;
                        specialistInstance.CriticalDodge++;
                    }

                    #endregion

                    #region slElement

                    if (slElement >= 1)
                    {
                        specialistInstance.ElementRate += 2;
                    }

                    if (slElement >= 10)
                    {
                        specialistInstance.MP += 100;
                    }

                    if (slElement >= 20)
                    {
                        specialistInstance.MagicDefence += 5;
                    }

                    if (slElement >= 30)
                    {
                        specialistInstance.FireResistance += 2;
                        specialistInstance.WaterResistance += 2;
                        specialistInstance.LightResistance += 2;
                        specialistInstance.DarkResistance += 2;
                        specialistInstance.ElementRate += 2;
                    }

                    if (slElement >= 40)
                    {
                        specialistInstance.MP += 100;
                    }

                    if (slElement >= 50)
                    {
                        specialistInstance.MagicDefence += 5;
                    }

                    if (slElement >= 60)
                    {
                        specialistInstance.FireResistance += 3;
                        specialistInstance.WaterResistance += 3;
                        specialistInstance.LightResistance += 3;
                        specialistInstance.DarkResistance += 3;
                        specialistInstance.ElementRate += 2;
                    }

                    if (slElement >= 70)
                    {
                        specialistInstance.MP += 100;
                    }

                    if (slElement >= 80)
                    {
                        specialistInstance.MagicDefence += 5;
                    }

                    if (slElement >= 90)
                    {
                        specialistInstance.FireResistance += 4;
                        specialistInstance.WaterResistance += 4;
                        specialistInstance.LightResistance += 4;
                        specialistInstance.DarkResistance += 4;
                        specialistInstance.ElementRate += 2;
                    }

                    if (slElement >= 100)
                    {
                        specialistInstance.FireResistance += 6;
                        specialistInstance.WaterResistance += 6;
                        specialistInstance.LightResistance += 6;
                        specialistInstance.DarkResistance += 6;
                        specialistInstance.MagicDefence += 5;
                        specialistInstance.MP += 200;
                        specialistInstance.ElementRate += 2;
                    }

                    #endregion

                    Session.SendPackets(Session.Character.GenerateStatChar());
                    Session.SendPacket(Session.Character.GenerateStat());
                    Session.SendPacket(specialistInstance.GenerateSlInfo(Session));
                    Session.SendPacket(
                        UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("POINTS_SET"), 0));
                }
                else if (!Session.Character.IsSitting)
                {
                    if (Session.Character.Buff.Any(s => s.Card.BuffType == BuffType.Bad))
                    {
                        Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("CANT_TRASFORM_WITH_DEBUFFS"),
                            0));
                        return;
                    }

                    if (Session.Character.Skills.Any(s => !s.CanBeUsed(true)))
                    {
                        Session.SendPacket(
                            UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("SKILLS_IN_LOADING"),
                                0));
                        return;
                    }

                    if (specialistInstance == null)
                    {
                        Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NO_SP"),
                            0));
                        return;
                    }

                    if (Session.Character.IsVehicled)
                    {
                        Session.SendPacket(
                            UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("REMOVE_VEHICLE"), 0));
                        return;
                    }

                    double currentRunningSeconds =
                        (DateTime.Now - Process.GetCurrentProcess().StartTime.AddSeconds(-50)).TotalSeconds;

                    if (Session.Character.UseSp)
                    {
                        if (Session.Character.Timespace != null && Session.Character.Timespace.SpNeeded?[(byte)Session.Character.Class] != 0 && Session.Character.Timespace.InstanceBag.Lock)
                        {
                            return;
                        }
                        Session.Character.LastSp = currentRunningSeconds;
                        Session.Character.RemoveSp(specialistInstance.ItemVNum, false);
                    }
                    else
                    {
                        if (Session.Character.LastMove.AddSeconds(1) >= DateTime.Now
                            || Session.Character.LastSkillUse.AddSeconds(2) >= DateTime.Now)
                        {
                            return;
                        }

                        if (Session.Character.SpPoint == 0 && Session.Character.SpAdditionPoint == 0)
                        {
                            Session.SendPacket(
                                UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("SP_NOPOINTS"), 0));
                        }

                        double timeSpanSinceLastSpUsage = currentRunningSeconds - Session.Character.LastSp;
                        if (timeSpanSinceLastSpUsage >= Session.Character.SpCooldown)
                        {
                            if (spTransformPacket.Type == 1)
                            {
                             //   DateTime delay = DateTime.Now.AddSeconds(-6);
                               // if (Session.Character.LastDelay > delay
                             //      && Session.Character.LastDelay < delay.AddSeconds(2))
                              //  {
                                    ChangeSp();
                            //    }
                            }
                            else
                            {
                               // Session.Character.LastDelay = DateTime.Now;
                                Session.SendPacket(UserInterfaceHelper.GenerateDelay(1000, 3, "#sl^1"));
                                Session.CurrentMapInstance?.Broadcast(
                                    UserInterfaceHelper.GenerateGuri(2, 1, Session.Character.CharacterId),
                                    Session.Character.PositionX, Session.Character.PositionY);
                            }
                        }
                        else
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                string.Format(Language.Instance.GetMessageFromKey("SP_INLOADING"),
                                    Session.Character.SpCooldown - (int)Math.Round(timeSpanSinceLastSpUsage, 0)), 0));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// up_gr packet
        /// </summary>
        /// <param name="upgradePacket"></param>
        public void Upgrade(UpgradePacket upgradePacket)
        {
            if (upgradePacket == null || Session.Character.ExchangeInfo?.ExchangeList.Count > 0
                || Session.Character.Speed == 0 || Session.Character.LastDelay.AddSeconds(5) > DateTime.Now)
            {
                return;
            }

            InventoryType inventoryType = upgradePacket.InventoryType;
            byte uptype = upgradePacket.UpgradeType, slot = upgradePacket.Slot;
            Session.Character.LastDelay = DateTime.Now;
            ItemInstance inventory;
            switch (uptype)
            {
                case 0:
                    inventory = Session.Character.Inventory.LoadBySlotAndType(slot, inventoryType);
                    if (inventory != null)
                    {
                        if ((inventory.Item.EquipmentSlot == EquipmentType.Armor
                             || inventory.Item.EquipmentSlot == EquipmentType.MainWeapon
                             || inventory.Item.EquipmentSlot == EquipmentType.SecondaryWeapon)
                            && inventory.Item.ItemType != ItemType.Shell && inventory.Item.Type == InventoryType.Equipment)
                        {
                            inventory.ConvertToPartnerEquipment(Session);
                        }
                    }
                    break;

                case 1:
                    inventory = Session.Character.Inventory.LoadBySlotAndType(slot, inventoryType);
                    if (inventory != null)
                    {
                        if ((inventory.Item.EquipmentSlot == EquipmentType.Armor
                             || inventory.Item.EquipmentSlot == EquipmentType.MainWeapon
                             || inventory.Item.EquipmentSlot == EquipmentType.SecondaryWeapon)
                            && inventory.Item.ItemType != ItemType.Shell && inventory.Item.Type == InventoryType.Equipment)
                        {
                            inventory.UpgradeItem(Session, UpgradeMode.Normal, UpgradeProtection.None);
                        }
                    }
                    break;

                case 3:

                    //up_gr 3 0 0 7 1 1 20 99
                    string[] originalSplit = upgradePacket.OriginalContent.Split(' ');
                    if (originalSplit.Length == 10
                        && byte.TryParse(originalSplit[5], out byte firstSlot)
                        && byte.TryParse(originalSplit[8], out byte secondSlot))
                    {
                        inventory = Session.Character.Inventory.LoadBySlotAndType(firstSlot, InventoryType.Equipment);
                        if (inventory != null
                            && (inventory.Item.EquipmentSlot == EquipmentType.Necklace
                             || inventory.Item.EquipmentSlot == EquipmentType.Bracelet
                             || inventory.Item.EquipmentSlot == EquipmentType.Ring)
                            && inventory.Item.ItemType != ItemType.Shell && inventory.Item.Type == InventoryType.Equipment)
                        {
                            ItemInstance cellon =
                                Session.Character.Inventory.LoadBySlotAndType(secondSlot,
                                    InventoryType.Main);
                            if (cellon?.ItemVNum > 1016 && cellon.ItemVNum < 1027)
                            {
                                inventory.OptionItem(Session, cellon.ItemVNum);
                            }
                        }
                    }
                    break;

                case 7:
                    inventory = Session.Character.Inventory.LoadBySlotAndType(slot, inventoryType);
                    if (inventory != null)
                    {
                        if (inventory.Item.EquipmentSlot == EquipmentType.Armor || inventory.Item.EquipmentSlot == EquipmentType.MainWeapon || inventory.Item.EquipmentSlot == EquipmentType.SecondaryWeapon)
                        {
                            RarifyMode mode = RarifyMode.Normal;
                            RarifyProtection protection = RarifyProtection.None;
                            ItemInstance amulet = Session.Character.Inventory.LoadBySlotAndType((short)EquipmentType.Amulet, InventoryType.Wear);
                            if (amulet != null)
                            {
                                switch (amulet.Item.Effect)
                                {
                                    case 791:
                                        protection = RarifyProtection.RedAmulet;
                                        break;
                                    case 792:
                                        protection = RarifyProtection.BlueAmulet;
                                        break;
                                    case 794:
                                        protection = RarifyProtection.HeroicAmulet;
                                        break;
                                    case 795:
                                        protection = RarifyProtection.RandomHeroicAmulet;
                                        break;
                                    case 796:
                                        if (inventory.Item.IsHeroic)
                                        {
                                            mode = RarifyMode.Success;
                                        }
                                        break;
                                }
                            }
                            inventory.RarifyItem(Session, mode, protection);
                        }

                        Session.SendPacket("shop_end 1");
                    }
                    break;

                case 8:
                    inventory = Session.Character.Inventory.LoadBySlotAndType(slot, inventoryType);
                    if (upgradePacket.InventoryType2 != null && upgradePacket.Slot2 != null)
                    {
                        ItemInstance inventory2 =
                            Session.Character.Inventory.LoadBySlotAndType((byte)upgradePacket.Slot2,
                                (InventoryType)upgradePacket.InventoryType2);

                        if (inventory != null && inventory2 != null && !Equals(inventory, inventory2))
                        {
                            inventory.Sum(Session, inventory2);
                        }
                    }
                    break;

                case 9:
                    ItemInstance specialist = Session.Character.Inventory.LoadBySlotAndType(slot, inventoryType);
                    if (specialist != null)
                    {
                        if (specialist.Rare != -2)
                        {
                            if (specialist.Item.EquipmentSlot == EquipmentType.Sp)
                            {
                                specialist.UpgradeSp(Session, UpgradeProtection.None);
                            }
                        }
                        else
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                Language.Instance.GetMessageFromKey("CANT_UPGRADE_DESTROYED_SP"), 0));
                        }
                    }
                    break;

                case 20:
                    inventory = Session.Character.Inventory.LoadBySlotAndType(slot, inventoryType);
                    if (inventory != null)
                    {
                        if ((inventory.Item.EquipmentSlot == EquipmentType.Armor
                             || inventory.Item.EquipmentSlot == EquipmentType.MainWeapon
                             || inventory.Item.EquipmentSlot == EquipmentType.SecondaryWeapon)
                            && inventory.Item.ItemType != ItemType.Shell && inventory.Item.Type == InventoryType.Equipment)
                        {
                            inventory.UpgradeItem(Session, UpgradeMode.Normal, UpgradeProtection.Protected);
                        }
                    }
                    break;

                case 21:
                    inventory = Session.Character.Inventory.LoadBySlotAndType(slot, inventoryType);
                    if (inventory != null)
                    {
                        if ((inventory.Item.EquipmentSlot == EquipmentType.Armor
                             || inventory.Item.EquipmentSlot == EquipmentType.MainWeapon
                             || inventory.Item.EquipmentSlot == EquipmentType.SecondaryWeapon)
                            && inventory.Item.ItemType != ItemType.Shell && inventory.Item.Type == InventoryType.Equipment)
                        {
                            inventory.RarifyItem(Session, RarifyMode.Normal, RarifyProtection.Scroll);
                        }
                    }
                    break;

                case 25:
                    specialist = Session.Character.Inventory.LoadBySlotAndType(slot, inventoryType);
                    if (specialist != null)
                    {
                        if (specialist.Rare != -2)
                        {
                            if (specialist.Upgrade > 9)
                            {
                                Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                    string.Format(Language.Instance.GetMessageFromKey("MUST_USE_ITEM"), ServerManager.GetItem(1364).Name), 0));
                                return;
                            }
                            if (specialist.Item.EquipmentSlot == EquipmentType.Sp)
                            {                           
                                    specialist.UpgradeSp(Session, UpgradeProtection.Protected);
                            }
                        }
                        else
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                Language.Instance.GetMessageFromKey("CANT_UPGRADE_DESTROYED_SP"), 0));
                        }
                    }
                    break;

                case 26:
                    specialist = Session.Character.Inventory.LoadBySlotAndType(slot, inventoryType);
                    if (specialist != null)
                    {
                        if (specialist.Rare != -2)
                        {
                            if (specialist.Upgrade <= 9)
                            {
                                Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                    string.Format(Language.Instance.GetMessageFromKey("MUST_USE_ITEM"), ServerManager.GetItem(1363).Name), 0));
                                return;
                            }
                            if (specialist.Item.EquipmentSlot == EquipmentType.Sp)
                            {                           
                                    specialist.UpgradeSp(Session, UpgradeProtection.Protected);
                            }
                        }
                        else
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                Language.Instance.GetMessageFromKey("CANT_UPGRADE_DESTROYED_SP"), 0));
                        }
                    }
                    break;

                case 38:
                    specialist = Session.Character.Inventory.LoadBySlotAndType(slot, inventoryType);
                    if (specialist != null)
                    {
                        if (specialist.Rare != -2)
                        {
                            if (specialist.Item.EquipmentSlot == EquipmentType.Sp)
                            {
                              //  var start = DateTime.Now;
                              //  while (DateTime.Now - start < TimeSpan.FromSeconds(2))
                               // {
                                    specialist.UpgradeSp(Session, UpgradeProtection.Event);
                               // }
                                
                            }
                        }
                        else
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                Language.Instance.GetMessageFromKey("CANT_UPGRADE_DESTROYED_SP"), 0));
                        }
                    }
                    break;

                case 41:
                    specialist = Session.Character.Inventory.LoadBySlotAndType(slot, inventoryType);
                    if (specialist != null)
                    {
                        if (specialist.Rare != -2)
                        {
                            if (specialist.Item.EquipmentSlot == EquipmentType.Sp)
                            {
                                var start = DateTime.Now;
                           //     while (DateTime.Now - start < TimeSpan.FromSeconds(2))
                             //   {
                                    specialist.PerfectSP(Session);
                               // }
                            }
                        }
                        else
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                Language.Instance.GetMessageFromKey("CANT_UPGRADE_DESTROYED_SP"), 0));
                        }
                    }
                    break;

                case 43:
                    inventory = Session.Character.Inventory.LoadBySlotAndType(slot, inventoryType);
                    if (inventory != null)
                    {
                        if ((inventory.Item.EquipmentSlot == EquipmentType.Armor
                             || inventory.Item.EquipmentSlot == EquipmentType.MainWeapon
                             || inventory.Item.EquipmentSlot == EquipmentType.SecondaryWeapon)
                            && inventory.Item.ItemType != ItemType.Shell && inventory.Item.Type == InventoryType.Equipment)
                        {
                            inventory.UpgradeItem(Session, UpgradeMode.Reduced, UpgradeProtection.Protected);
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// u_i packet
        /// </summary>
        /// <param name="useItemPacket"></param>
        public void UseItem(UseItemPacket useItemPacket)
        {
            if (!Session.Account.VerifiedLock)
            {
                Session.SendPacket(UserInterfaceHelper.GenerateMsg("You cant do this because your account is blocked. Use $Unlock", 0));
                return;
            }

            if (useItemPacket == null || (byte)useItemPacket.Type >= 9)
            {
                return;
            }

            ItemInstance itemInstance = Session.Character.Inventory.LoadBySlotAndType(useItemPacket.Slot, useItemPacket.Type);

            string[] packet = useItemPacket.OriginalContent.Split(' ', '^');

            if (packet.Length >= 2 && packet[1].Length > 0)
            {
                itemInstance?.Item.Use(Session, ref itemInstance, packet[1][0] == '#' ? (byte)255 : (byte)0, packet);
            }            
        }

        /// <summary>
        /// wear packet
        /// </summary>
        /// <param name="wearPacket"></param>
        public void Wear(WearPacket wearPacket)
        {
            if (wearPacket == null || Session.Character.ExchangeInfo?.ExchangeList.Count > 0
                || Session.Character.Speed == 0)
            {
                return;
            }

            if (Session.HasCurrentMapInstance && Session.CurrentMapInstance.UserShops
                    .FirstOrDefault(mapshop => mapshop.Value.OwnerId.Equals(Session.Character.CharacterId)).Value
                == null)
            {
                ItemInstance inv =
                    Session.Character.Inventory.LoadBySlotAndType(wearPacket.InventorySlot, InventoryType.Equipment);
                if (inv?.Item != null)
                {
                    //proper dupe stats fix
                    Mate mate = Session.Character.Mates.Find(s => s.MateType == MateType.Partner && s.PetId == wearPacket.Type - 1);
                    if (mate != null)
                    {
                        ItemInstance mateinventory = null;
                        switch (inv.Item.EquipmentSlot)
                        {
                            case EquipmentType.Armor:
                                mateinventory = mate.ArmorInstance;
                                break;

                            case EquipmentType.MainWeapon:
                                mateinventory = mate.WeaponInstance;
                                break;

                            case EquipmentType.Gloves:
                                mateinventory = mate.GlovesInstance;
                                break;

                            case EquipmentType.Boots:
                                mateinventory = mate.BootsInstance;
                                break;
                            default:
                                mate.BattleEntity.BCards.RemoveAll(o => o.ItemVNum == (inv.HoldingVNum == 0 ? inv.ItemVNum : inv.HoldingVNum));
                                break;

                        }
                        if (mateinventory != null)
                        {
                            mate.BattleEntity.BCards.RemoveAll(o => o.ItemVNum == (mateinventory.HoldingVNum == 0 ? mateinventory.ItemVNum : mateinventory.HoldingVNum));
                        }
                    }
                    inv.Item.Use(Session, ref inv, wearPacket.Type);
                    Session.Character.LoadSpeed();
                    Session.SendPacket(StaticPacketHelper.GenerateEff(UserType.Player, Session.Character.CharacterId,
                        123));

                    ItemInstance ring = Session.Character.Inventory.LoadBySlotAndType((byte)EquipmentType.Ring, InventoryType.Wear);
                    ItemInstance bracelet = Session.Character.Inventory.LoadBySlotAndType((byte)EquipmentType.Bracelet, InventoryType.Wear);
                    ItemInstance necklace = Session.Character.Inventory.LoadBySlotAndType((byte)EquipmentType.Necklace, InventoryType.Wear);
                    Session.Character.CellonOptions.Clear();
                    if (ring != null)
                    {
                        Session.Character.CellonOptions.AddRange(ring.CellonOptions);
                    }
                    if (bracelet != null)
                    {
                        Session.Character.CellonOptions.AddRange(bracelet.CellonOptions);
                    }
                    if (necklace != null)
                    {
                        Session.Character.CellonOptions.AddRange(necklace.CellonOptions);
                    }
                    Session.SendPacket(Session.Character.GenerateStat());
                }
            }
        }

        /// <summary>
        /// withdraw packet
        /// </summary>
        /// <param name="withdrawPacket"></param>
        public void Withdraw(WithdrawPacket withdrawPacket)
        {
            if (Session.Character.Authority == AuthorityType.User)
            {
                return;
            }
            if (withdrawPacket != null)
            {
                ItemInstance previousInventory = Session.Character.Inventory.LoadBySlotAndType(withdrawPacket.Slot,
                    withdrawPacket.PetBackpack ? InventoryType.PetWarehouse : InventoryType.Warehouse);
                if (withdrawPacket.Amount <= 0 || previousInventory == null
                    || withdrawPacket.Amount > previousInventory.Amount
                    || !Session.Character.Inventory.CanAddItem(previousInventory.ItemVNum))
                {
                    return;
                }

                ItemInstance item2 = previousInventory.DeepCopy();
                item2.Id = Guid.NewGuid();
                item2.Amount = withdrawPacket.Amount;
                Logger.LogUserEvent("STASH_WITHDRAW", Session.GenerateIdentity(),
                    $"[Withdraw]OldIIId: {previousInventory.Id} NewIIId: {item2.Id} Amount: {withdrawPacket.Amount} PartnerBackpack: {withdrawPacket.PetBackpack}");
                Session.Character.Inventory.RemoveItemFromInventory(previousInventory.Id, withdrawPacket.Amount);
                Session.Character.Inventory.AddToInventory(item2, item2.Item.Type);
                Session.Character.Inventory.LoadBySlotAndType(withdrawPacket.Slot,
                    withdrawPacket.PetBackpack ? InventoryType.PetWarehouse : InventoryType.Warehouse);
                if (previousInventory.Amount > 0)
                {
                    Session.SendPacket(withdrawPacket.PetBackpack ? previousInventory.GeneratePStash() : previousInventory.GenerateStash());
                }
                else
                {
                    Session.SendPacket(withdrawPacket.PetBackpack
                        ? UserInterfaceHelper.Instance.GeneratePStashRemove(withdrawPacket.Slot)
                        : UserInterfaceHelper.Instance.GenerateStashRemove(withdrawPacket.Slot));
                }
            }
        }

        /// <summary>
        /// changesp private method
        /// </summary>
        private void ChangeSp()
        {
            ItemInstance sp =
                Session.Character.Inventory.LoadBySlotAndType((byte)EquipmentType.Sp, InventoryType.Wear);
            ItemInstance fairy =
                Session.Character.Inventory.LoadBySlotAndType((byte)EquipmentType.Fairy, InventoryType.Wear);
            if (sp != null)
            {
                if (Session.Character.GetReputationIco() < sp.Item.ReputationMinimum)
                {
                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("LOW_REP"),
                        0));
                    return;
                }

                if (fairy != null && sp.Item.Element != 0 && fairy.Item.Element != sp.Item.Element
                    && fairy.Item.Element != sp.Item.SecondaryElement)
                {
                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("BAD_FAIRY"),
                        0));
                    return;
                }

                if (new int[] { 4494, 4495, 4496 }.Contains(sp.ItemVNum))
                {
                    if (Session.Character.Timespace == null)
                    {
                        return;
                    }
                    else if (ServerManager.Instance.TimeSpaces.Any(s => s.SpNeeded?[(byte)Session.Character.Class] == sp.ItemVNum))
                    {
                        if (Session.Character.Timespace.SpNeeded?[(byte)Session.Character.Class] != sp.ItemVNum)
                        {
                            return;
                        }
                    }
                    else
                    {
                        return;
                    }
                }

                Session.Character.DisableBuffs(BuffType.All);
                Session.Character.EquipmentBCards.AddRange(sp.Item.BCards);
                Session.Character.LastTransform = DateTime.Now;
                Session.Character.UseSp = true;
                Session.Character.Morph = sp.Item.Morph;
                Session.Character.MorphUpgrade = sp.Upgrade;
                Session.Character.MorphUpgrade2 = sp.Design;
                Session.CurrentMapInstance?.Broadcast(Session.Character.GenerateCMode());
                Session.SendPacket(Session.Character.GenerateLev());
                Session.CurrentMapInstance?.Broadcast(
                    StaticPacketHelper.GenerateEff(UserType.Player, Session.Character.CharacterId, 196),
                    Session.Character.PositionX, Session.Character.PositionY);
                Session.CurrentMapInstance?.Broadcast(
                    UserInterfaceHelper.GenerateGuri(6, 1, Session.Character.CharacterId), Session.Character.PositionX,
                    Session.Character.PositionY);
                Session.SendPacket(Session.Character.GenerateSpPoint());
                Session.Character.LoadSpeed();
                Session.SendPacket(Session.Character.GenerateCond());
                Session.SendPacket(Session.Character.GenerateStat());
                Session.SendPackets(Session.Character.GenerateStatChar());
                Session.Character.SkillsSp = new ThreadSafeSortedList<int, CharacterSkill>();
                Parallel.ForEach(ServerManager.GetAllSkill(), skill =>
                {
                    var morphUpdate = 31;

                    if (sp.ItemVNum == 4485 || sp.ItemVNum == 4437 || sp.ItemVNum == 4416)
                    {
                        morphUpdate = 30;
                    }

                    if (skill.Class == Session.Character.Morph + morphUpdate && sp.SpLevel >= skill.LevelMinimum)
                    {
                        Session.Character.SkillsSp[skill.SkillVNum] = new CharacterSkill
                        {
                            SkillVNum = skill.SkillVNum,
                            CharacterId = Session.Character.CharacterId
                        };
                    }
                });
                Session.SendPacket(Session.Character.GenerateSki());
                Session.SendPackets(Session.Character.GenerateQuicklist());
                Logger.LogUserEvent("CHARACTER_SPECIALIST_CHANGE", Session.GenerateIdentity(),
                    $"Specialist: {sp.Item.Morph}");
            }
        }

        /// <summary>
        /// exchange closure method
        /// </summary>
        /// <param name="session"></param>
        /// <param name="targetSession"></param>
        private static void CloseExchange(ClientSession session, ClientSession targetSession)
        {
            if (targetSession?.Character.ExchangeInfo != null)
            {
                targetSession.SendPacket("exc_close 0");
                targetSession.Character.ExchangeInfo = null;
            }

            if (session?.Character.ExchangeInfo != null)
            {
                session.SendPacket("exc_close 0");
                session.Character.ExchangeInfo = null;
            }
        }

        /// <summary>
        /// exchange initialization method
        /// </summary>
        /// <param name="sourceSession"></param>
        /// <param name="targetSession"></param>
        private static void Exchange(ClientSession sourceSession, ClientSession targetSession)
        {
            if (sourceSession?.Character.ExchangeInfo == null)
            {
                return;
            }

            string data = "";

            // remove all items from source session
            foreach (ItemInstance item in sourceSession.Character.ExchangeInfo.ExchangeList)
            {
                ItemInstance invtemp = sourceSession.Character.Inventory.GetItemInstanceById(item.Id);
                if (invtemp?.Amount >= item.Amount)
                {
                    sourceSession.Character.Inventory.RemoveItemFromInventory(invtemp.Id, item.Amount);
                }
                else
                {
                    return;
                }
            }

            // add all items to target session
            foreach (ItemInstance item in sourceSession.Character.ExchangeInfo.ExchangeList)
            {
                ItemInstance item2 = item.DeepCopy();
                item2.Id = Guid.NewGuid();
                data += $"[OldIIId: {item.Id} NewIIId: {item2.Id} ItemVNum: {item.ItemVNum} Amount: {item.Amount} Rare: {item.Rare} Upgrade: {item.Upgrade}]";
                List<ItemInstance> inv = targetSession.Character.Inventory.AddToInventory(item2);
                if (inv.Count == 0)
                {
                    // do what?
                }
            }

            data += $"[Gold: {sourceSession.Character.ExchangeInfo.Gold}]";
            data += $"[BankGold: {sourceSession.Character.ExchangeInfo.BankGold}]";

            // handle gold
            sourceSession.Character.Gold -= sourceSession.Character.ExchangeInfo.Gold;
            sourceSession.Character.GoldBank -= sourceSession.Character.ExchangeInfo.BankGold;
            sourceSession.SendPacket(sourceSession.Character.GenerateGold());
            sourceSession.Character.GoldBank -= (sourceSession.Character.ExchangeInfo.BankGold * 1000);
            targetSession.Character.Gold += sourceSession.Character.ExchangeInfo.Gold;
            targetSession.Character.GoldBank += sourceSession.Character.ExchangeInfo.BankGold;
            targetSession.SendPacket(targetSession.Character.GenerateGold());
            targetSession.Character.GoldBank += (sourceSession.Character.ExchangeInfo.BankGold * 1000);


            // all items and gold from sourceSession have been transferred, clean exchange info

            Logger.LogUserEvent("TRADE_COMPLETE", sourceSession.GenerateIdentity(),
                $"[{targetSession.GenerateIdentity()}]Data: {data}");

            sourceSession.Character.ExchangeInfo = null;
        }
        

        #endregion
    }
}
 