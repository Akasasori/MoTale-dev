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
using OpenNos.GameObject;
using OpenNos.GameObject.Helpers;
using System;
using System.Collections.Generic;
using System.Threading;
using OpenNos.GameObject.Networking;
using OpenNos.Master.Library.Client;
using OpenNos.Master.Library.Data;
using System.Linq;
using System.Reactive.Linq;

namespace OpenNos.Handler
{
    public class BazaarPacketHandler : IPacketHandler
    {
        #region Instantiation

        public BazaarPacketHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        private ClientSession Session { get; }

        #endregion

        #region Methods

        /// <summary>
        /// c_buy packet
        /// </summary>
        /// <param name="cBuyPacket"></param>
        public void BuyBazaar(CBuyPacket cBuyPacket)
        {
            if (!Session.Account.VerifiedLock)
            {
                Session.SendPacket(UserInterfaceHelper.GenerateMsg("You cant do this because your account is blocked. Use $Unlock", 0));
                return;
            }

            BazaarItemDTO bz = DAOFactory.BazaarItemDAO.LoadById(cBuyPacket.BazaarId);
            if (bz != null && cBuyPacket.Amount > 0)
            {
                long price = cBuyPacket.Amount * bz.Price;

                if (Session.Character.Gold >= price)
                {
                    BazaarItemLink bzcree = new BazaarItemLink {BazaarItem = bz};
                    if (DAOFactory.CharacterDAO.LoadById(bz.SellerId) != null)
                    {
                        bzcree.Owner = DAOFactory.CharacterDAO.LoadById(bz.SellerId)?.Name;
                        bzcree.Item = new ItemInstance(DAOFactory.ItemInstanceDAO.LoadById(bz.ItemInstanceId));
                    }
                    else
                    {
                        return;
                    }

                    if (cBuyPacket.Amount <= bzcree.Item.Amount)
                    {
                        if (!Session.Character.Inventory.CanAddItem(bzcree.Item.ItemVNum))
                        {
                            Session.SendPacket(
                                UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_PLACE"),
                                    0));
                            return;
                        }

                        if (bzcree.Item != null)
                        {
                            if (bz.IsPackage && cBuyPacket.Amount != bz.Amount)
                            {
                                return;
                            }

                            ItemInstanceDTO bzitemdto =
                                DAOFactory.ItemInstanceDAO.LoadById(bzcree.BazaarItem.ItemInstanceId);
                            if (bzitemdto.Amount < cBuyPacket.Amount)
                            {
                                return;
                            }

                            // Edit this soo we dont generate new guid every single time we take something out.
                            ItemInstance newBz = bzcree.Item.DeepCopy();
                            newBz.Id = Guid.NewGuid();
                            newBz.Amount = cBuyPacket.Amount;
                            newBz.Type = newBz.Item.Type;
                            List<ItemInstance> newInv = Session.Character.Inventory.AddToInventory(newBz);

                            if (newInv.Count > 0)
                            {
                                bzitemdto.Amount -= cBuyPacket.Amount;
                                Session.Character.Gold -= price;
                                Session.SendPacket(Session.Character.GenerateGold());
                                DAOFactory.ItemInstanceDAO.InsertOrUpdate(bzitemdto);
                                ServerManager.Instance.BazaarRefresh(bzcree.BazaarItem.BazaarItemId);
                                Session.SendPacket(
                                    $"rc_buy 1 {bzcree.Item.Item.VNum} {bzcree.Owner} {cBuyPacket.Amount} {cBuyPacket.Price} 0 0 0");

                                Session.SendPacket(Session.Character.GenerateSay(
                                    $"{Language.Instance.GetMessageFromKey("ITEM_ACQUIRED")}: {bzcree.Item.Item.Name} x {cBuyPacket.Amount}",
                                    10));

                                CommunicationServiceClient.Instance.SendMessageToCharacter(new SCSCharacterMessage
                                {
                                    DestinationCharacterId = bz.SellerId,
                                    SourceWorldId = ServerManager.Instance.WorldId,
                                    Message = StaticPacketHelper.Say(1, bz.SellerId, 12, string.Format(Language.Instance.GetMessageFromKey("BAZAAR_ITEM_SOLD"), cBuyPacket.Amount, bzcree.Item.Item.Name)),
                                    Type = MessageType.Other
                                });
                                
                                Logger.LogUserEvent("BAZAAR_BUY", Session.GenerateIdentity(),
                                    $"BazaarId: {cBuyPacket.BazaarId} VNum: {cBuyPacket.VNum} Amount: {cBuyPacket.Amount} Price: {cBuyPacket.Price}");
                            }
                        }
                    }
                    else
                    {
                        Session.SendPacket(
                            UserInterfaceHelper.GenerateModal(Language.Instance.GetMessageFromKey("STATE_CHANGED"), 1));
                    }
                }
                else
                {
                    Session.SendPacket(
                        Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NOT_ENOUGH_MONEY"), 10));
                    Session.SendPacket(
                        UserInterfaceHelper.GenerateModal(Language.Instance.GetMessageFromKey("NOT_ENOUGH_MONEY"), 1));
                }
            }
            else
            {
                Session.SendPacket(
                    UserInterfaceHelper.GenerateModal(Language.Instance.GetMessageFromKey("STATE_CHANGED"), 1));
            }
        }

        /// <summary>
        /// c_scalc packet
        /// </summary>
        /// <param name="cScalcPacket"></param>
        public void GetBazaar(CScalcPacket cScalcPacket)
        {
            lock (Session.Character.Inventory)
            {
               
                SpinWait.SpinUntil(() => !ServerManager.Instance.InBazaarRefreshMode);

                BazaarItemDTO bazaarItemDTO = DAOFactory.BazaarItemDAO.LoadById(cScalcPacket.BazaarId);

                if (bazaarItemDTO != null)
                {
                    ItemInstanceDTO itemInstanceDTO = DAOFactory.ItemInstanceDAO.LoadById(bazaarItemDTO.ItemInstanceId);

                    if (itemInstanceDTO == null)
                    {
                        return;
                    }

                    ItemInstance itemInstance = new ItemInstance(itemInstanceDTO);

                    if (itemInstance == null)
                    {
                        return;
                    }

                    if (bazaarItemDTO.SellerId != Session.Character.CharacterId)
                    {
                        return;
                    }

                    if ((bazaarItemDTO.DateStart.AddHours(bazaarItemDTO.Duration).AddDays(bazaarItemDTO.MedalUsed ? 30 : 7) - DateTime.Now).TotalMinutes <= 0)
                    {
                        return;
                    }

                    int soldAmount = bazaarItemDTO.Amount - itemInstance.Amount;
                    long taxes = bazaarItemDTO.MedalUsed ? 0 : (long)(bazaarItemDTO.Price * 0.10 * soldAmount);
                    long price = (bazaarItemDTO.Price * soldAmount) - taxes;

                    int vnum = itemInstance.ItemVNum;
                    string name = itemInstance.Item?.Name ?? "None";
                    if (soldAmount < 0)
                    {
                        return;
                    }
                    if (price < 0)
                    {
                        return;
                    }
                    if (taxes < 0)
                    {
                        return;
                    }
                    if (itemInstance.Amount == 0 || Session.Character.Inventory.CanAddItem(itemInstance.ItemVNum))
                    {
                        if (Session.Character.Gold + price <= ServerManager.Instance.Configuration.MaxGold)
                        {
                            Session.Character.Gold += price;
                            Session.SendPacket(Session.Character.GenerateGold());
                            Session.SendPacket(Session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("REMOVE_FROM_BAZAAR"), price), 10));

                            // Edit this soo we dont generate new guid every single time we take something out.
                            if (itemInstance.Amount != 0)
                            {
                                ItemInstance newItemInstance = itemInstance.DeepCopy();
                                newItemInstance.Id = Guid.NewGuid();
                                newItemInstance.Type = newItemInstance.Item.Type;
                                Session.Character.Inventory.AddToInventory(newItemInstance);
                            }

                            Session.SendPacket(UserInterfaceHelper.GenerateBazarRecollect(bazaarItemDTO.Price, soldAmount, bazaarItemDTO.Amount, taxes, price, vnum));

                            Logger.LogUserEvent("BAZAAR_REMOVE", Session.GenerateIdentity(), $"BazaarId: {cScalcPacket.BazaarId}, IId: {itemInstance.Id} VNum: {itemInstance.ItemVNum} Amount: {bazaarItemDTO.Amount} RemainingAmount: {itemInstance.Amount} Price: {bazaarItemDTO.Price}");

                            if (DAOFactory.BazaarItemDAO.LoadById(bazaarItemDTO.BazaarItemId) != null)
                            {
                                DAOFactory.BazaarItemDAO.Delete(bazaarItemDTO.BazaarItemId);
                            }

                            DAOFactory.ItemInstanceDAO.Delete(itemInstance.Id);

                            Session.Character.Inventory.RemoveItemFromInventory(itemInstance.Id, itemInstance.Amount);

                            ServerManager.Instance.BazaarRefresh(bazaarItemDTO.BazaarItemId);

                            Observable.Timer(TimeSpan.FromMilliseconds(1000)).Subscribe(o => RefreshPersonalBazarList(new CSListPacket()));
                            Session.SendPacket($"c_slist 0 0");
                        }
                        else
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("MAX_GOLD"), 0));
                            Session.SendPacket(UserInterfaceHelper.GenerateBazarRecollect(bazaarItemDTO.Price, 0, bazaarItemDTO.Amount, 0, 0, vnum));
                        }
                    }
                    else
                    {
                        Session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("NOT_ENOUGH_PLACE")));
                        Session.SendPacket(UserInterfaceHelper.GenerateBazarRecollect(bazaarItemDTO.Price, 0, bazaarItemDTO.Amount, 0, 0, vnum));
                    }
                }
                else
                {
                    Session.SendPacket(UserInterfaceHelper.GenerateBazarRecollect(0, 0, 0, 0, 0, 0));
                }
            }
        }

        /// <summary>
        /// c_skill packet
        /// </summary>
        /// <param name="cSkillPacket"></param>
        public void OpenBazaar(CSkillPacket cSkillPacket)
        {
            SpinWait.SpinUntil(() => !ServerManager.Instance.InBazaarRefreshMode);

            StaticBonusDTO medal = Session.Character.StaticBonusList.Find(s => s.StaticBonusType == StaticBonusType.BazaarMedalGold || s.StaticBonusType == StaticBonusType.BazaarMedalSilver);

            if (medal != null)
            {
                MedalType medalType = medal.StaticBonusType == StaticBonusType.BazaarMedalGold ? MedalType.Gold : MedalType.Silver;

                int time = (int)(medal.DateEnd - DateTime.Now).TotalHours;

                Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOTICE_BAZAAR"), 0));
                Session.SendPacket($"wopen 32 {(byte)medalType} {time}");
            }
            else
            {
                Session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("INFO_BAZAAR")));
            }
        }

        /// <summary>
        /// c_blist packet
        /// </summary>
        /// <param name="cbListPacket"></param>
        public void RefreshBazarList(CBListPacket cbListPacket)
        {
          
            SpinWait.SpinUntil(() => !ServerManager.Instance.InBazaarRefreshMode);
            Session.SendPacket(UserInterfaceHelper.GenerateRCBList(cbListPacket));
        }

        /// <summary>
        /// c_slist packet
        /// </summary>
        /// <param name="csListPacket"></param>
        public void RefreshPersonalBazarList(CSListPacket csListPacket)
        {
           

            SpinWait.SpinUntil(() => !ServerManager.Instance.InBazaarRefreshMode);
            Session.SendPacket(Session.Character.GenerateRCSList(csListPacket));
        }

        /// <summary>
        /// c_reg packet
        /// </summary>
        /// <param name="cRegPacket"></param>
        public void SellBazaar(CRegPacket cRegPacket)
        {
            lock (Session.Character.Inventory)
            {
                if (!Session.Account.VerifiedLock)
                {
                    Session.SendPacket(UserInterfaceHelper.GenerateMsg("You cant do this because your account is blocked. Use $Unlock", 0));
                    return;
                }

                if (cRegPacket.Inventory != 0 && cRegPacket.Inventory != 1 &&
                   cRegPacket.Inventory != 2 && cRegPacket.Inventory != 4)
                {
                    return;
                }

                InventoryType currentInventoryType = cRegPacket.Inventory == 4
                    ? InventoryType.Equipment
                    : (InventoryType)cRegPacket.Inventory;

                InventoryType[] allowedInventoryTypes = { InventoryType.Equipment, InventoryType.Main, InventoryType.Etc };

                if (allowedInventoryTypes.All(s => s != currentInventoryType))
                {
                    return;
                }
                // prevents for dupe if a time is already deployed :)
                if (cRegPacket.Inventory == 9)
                {
                    Logger.Log.Info($"{Session.Character.Name} tried to dupe via bazar");
                    Session.SendPacket(UserInterfaceHelper.GenerateInfo($"#SERVER_LOG"));
                    Session.SendPacket(UserInterfaceHelper.GenerateMsg($"{Session.Character.Name} tried to dupe via bazar, but thank you for the try", 12));
                    return;
                }

                SpinWait.SpinUntil(() => !ServerManager.Instance.InBazaarRefreshMode);
                StaticBonusDTO medal = Session.Character.StaticBonusList.Find(s =>
                    s.StaticBonusType == StaticBonusType.BazaarMedalGold
                    || s.StaticBonusType == StaticBonusType.BazaarMedalSilver);

                long price = cRegPacket.Price * cRegPacket.Amount;
                long taxmax = price > 100000 ? price / 200 : 500;
                long taxmin = price >= 4000
                    ? (60 + ((price - 4000) / 2000 * 30) > 10000 ? 10000 : 60 + ((price - 4000) / 2000 * 30))
                    : 50;
                long tax = medal == null ? taxmax : taxmin;
                long maxGold = ServerManager.Instance.Configuration.MaxGold;
                if (Session.Character.Gold < tax || cRegPacket.Amount <= 0
                    || Session.Character.ExchangeInfo?.ExchangeList.Count > 0 || Session.Character.IsShopping)
                {
                    return;
                }

                ItemInstance it = Session.Character.Inventory.LoadBySlotAndType(cRegPacket.Slot,
                    cRegPacket.Inventory == 4 ? 0 : (InventoryType)cRegPacket.Inventory);

                if (it == null || !it.Item.IsSoldable || !it.Item.IsTradable || it.IsBound)
                {
                    return;
                }

                if (Session.Character.Inventory.CountBazaarItems()
                    >= 10 * (medal == null ? 2 : 10))
                {
                    Session.SendPacket(
                        UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("LIMIT_EXCEEDED"), 0));
                    return;
                }

                if (cRegPacket.Price >= (medal == null ? 1000000 : maxGold))
                {
                    Session.SendPacket(
                        UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("PRICE_EXCEEDED"), 0));
                    return;
                }

                if (cRegPacket.Price <= 0)
                {
                    return;
                }

                ItemInstance bazaar = Session.Character.Inventory.AddIntoBazaarInventory(
                    cRegPacket.Inventory == 4 ? 0 : (InventoryType)cRegPacket.Inventory, cRegPacket.Slot,
                    cRegPacket.Amount);
                if (bazaar == null)
                {
                    return;
                }

                short duration;
                switch (cRegPacket.Durability)
                {
                    case 1:
                        duration = 24;
                        break;

                    case 2:
                        duration = 168;
                        break;

                    case 3:
                        duration = 360;
                        break;

                    case 4:
                        duration = 720;
                        break;

                    default:
                        return;
                }

                DAOFactory.ItemInstanceDAO.InsertOrUpdate(bazaar);

                BazaarItemDTO bazaarItem = new BazaarItemDTO
                {
                    Amount = bazaar.Amount,
                    DateStart = DateTime.Now,
                    Duration = duration,
                    IsPackage = cRegPacket.IsPackage != 0,
                    MedalUsed = medal != null,
                    Price = cRegPacket.Price,
                    SellerId = Session.Character.CharacterId,
                    ItemInstanceId = bazaar.Id
                };

                DAOFactory.BazaarItemDAO.InsertOrUpdate(ref bazaarItem);
                ServerManager.Instance.BazaarRefresh(bazaarItem.BazaarItemId);

                Session.Character.Gold -= tax;
                Session.SendPacket(Session.Character.GenerateGold());

                Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("OBJECT_IN_BAZAAR"),
                    10));
                Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("OBJECT_IN_BAZAAR"),
                    0));

                Logger.LogUserEvent("BAZAAR_INSERT", Session.GenerateIdentity(),
                    $"BazaarId: {bazaarItem.BazaarItemId}, IIId: {bazaarItem.ItemInstanceId} VNum: {bazaar.ItemVNum} Amount: {cRegPacket.Amount} Price: {cRegPacket.Price} Time: {duration}");

                Session.SendPacket("rc_reg 1");
            }
        }
        /// <summary>
        /// c_mod packet
        /// </summary>
        /// <param name="cModPacket"></param>
        public void ModPriceBazaar(CModPacket cModPacket)
        {
            if (!Session.Account.VerifiedLock)
            {
                Session.SendPacket(UserInterfaceHelper.GenerateMsg("You cant do this because your account is blocked. Use $Unlock", 0));
                return;
            }

          
            BazaarItemDTO bz = DAOFactory.BazaarItemDAO.LoadById(cModPacket.BazaarId);
            if (bz != null)
            {
                if (bz.SellerId != Session.Character.CharacterId)
                {
                    return;
                }

                ItemInstance itemInstance = new ItemInstance(DAOFactory.ItemInstanceDAO.LoadById(bz.ItemInstanceId));
                if (itemInstance == null || bz.Amount != itemInstance.Amount)
                {
                    return;
                }

                if ((bz.DateStart.AddHours(bz.Duration).AddDays(bz.MedalUsed ? 30 : 7) - DateTime.Now).TotalMinutes <= 0)
                {
                    return;
                }

                if (cModPacket.Price <= 0)
                {
                    return;
                }

                StaticBonusDTO medal = Session.Character.StaticBonusList.Find(s =>
                    s.StaticBonusType == StaticBonusType.BazaarMedalGold
                    || s.StaticBonusType == StaticBonusType.BazaarMedalSilver);
                if (cModPacket.Price >= (medal == null ? 1000000 : ServerManager.Instance.Configuration.MaxGold))
                {
                    Session.SendPacket(
                        UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("PRICE_EXCEEDED"), 0));
                    return;
                }

                bz.Price = cModPacket.Price;

                DAOFactory.BazaarItemDAO.InsertOrUpdate(ref bz);
                ServerManager.Instance.BazaarRefresh(bz.BazaarItemId);
                
                Session.SendPacket(Session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("OBJECT_MOD_IN_BAZAAR"), bz.Price),
                    10));
                Session.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("OBJECT_MOD_IN_BAZAAR"), bz.Price),
                    0));

                Logger.LogUserEvent("BAZAAR_MOD", Session.GenerateIdentity(),
                    $"BazaarId: {bz.BazaarItemId}, IIId: {bz.ItemInstanceId} VNum: {itemInstance.ItemVNum} Amount: {bz.Amount} Price: {bz.Price} Time: {bz.Duration}");
                
                RefreshPersonalBazarList(new CSListPacket());
            }
        }

        #endregion
    }
}