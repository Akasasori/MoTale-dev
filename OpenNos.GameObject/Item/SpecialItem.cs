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
using OpenNos.Core.Extensions;
using OpenNos.DAL;
using OpenNos.Data;
using OpenNos.Domain;
using OpenNos.GameObject.Helpers;
using System;
using System.Linq;
using OpenNos.GameObject.Networking;
using System.Collections.Generic;
using OpenNos.Master.Library.Client;
using OpenNos.Master.Library.Data;
using System.Threading.Tasks;

namespace OpenNos.GameObject
{
    public class SpecialItem : Item
    {
        #region Instantiation

        public SpecialItem(ItemDTO item) : base(item)
        {
        }

        #endregion

        #region Methods
        public bool IsNotAllowedInArena(int ItemVnum)
        {
            switch (ItemVnum)
            {
                case 1246:
                case 1247:
                case 1248:
                case 1296:
                case 9020:
                case 9021:
                case 9022:
                case 9074:
                case 1500:
                case 1002:
                case 1003:
                case 1004:
                case 1005:
                case 1006:
                case 1007:
                case 1008:
                case 1009:
                case 1010:
                case 1011:
                case 1087:
                case 4713:
                case 4714:
                case 4715:
                case 4716:


                    return true;
                default:
                    return false;
            }
        }

        public override void Use(ClientSession session, ref ItemInstance inv, byte Option = 0, string[] packetsplit = null)
        {
            if (session.Character.MapInstance.IsPVP || ServerManager.Instance.ChannelId == 51 && IsNotAllowedInArena(inv.ItemVNum))
                return;
            short itemDesign = inv.Design;

            #region BoxItem

            List<BoxItemDTO> boxItemDTOs = ServerManager.Instance.BoxItems.Where(boxItem => boxItem.OriginalItemVNum == VNum && boxItem.OriginalItemDesign == itemDesign).ToList();

            if (boxItemDTOs.Any())
            {
                session.Character.Inventory.RemoveItemFromInventory(inv.Id);

                foreach (BoxItemDTO boxItemDTO in boxItemDTOs)
                {
                    if (ServerManager.RandomNumber() < boxItemDTO.Probability)
                    {
                        session.Character.GiftAdd(boxItemDTO.ItemGeneratedVNum, boxItemDTO.ItemGeneratedAmount, boxItemDTO.ItemGeneratedRare, boxItemDTO.ItemGeneratedUpgrade, boxItemDTO.ItemGeneratedDesign);
                    }
                }

                return;
            }

            #endregion

            if (inv.ItemVNum == 1949) //Sealed Boxes by Zro
            {
                int pc = ServerManager.RandomNumber<int>(1, 3);

                short[] vnums = { 1428, 1218, 2173, 1122, 2282, 1030, 1286, 2158, 2187, 2160, 2159};
                byte[] counts = { 10, 1, 20, 10, 5, 5, 1, 2, 1, 5, 1};

                if (pc == 1)
                {
                    int pc2 = ServerManager.RandomNumber<int>(1, 2);
                    session.Character.GiftAdd(vnums[pc2], counts[pc2]);
                }
                else
                {
                    int pc3 = ServerManager.RandomNumber<int>(3, 9);
                    session.Character.GiftAdd(vnums[pc3], counts[pc3]);
                }

                session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                return;
            }

            if (inv.ItemVNum == 5099) //Fullmoon box small
            {
                int rnd = ServerManager.RandomNumber(0, 100);

                if (rnd < 5)
                {
                    short[] vnums =
                                                       {
                                5087
                            };
                    byte[] counts =
                    {
                              1
                            };
                    int item = ServerManager.RandomNumber(0, 1);
                    session.Character.GiftAdd(vnums[item], counts[item]);
                    session.SendPacket($"rdi {vnums[item]} {counts[item]}");
                }
                else
                {
                    short[] vnums =
                                                        {
                                1030
                            };
                    byte[] counts =
                    {
                              20
                            };
                    int item = ServerManager.RandomNumber(0, 1);
                    session.Character.GiftAdd(vnums[item], counts[item]);
                    session.SendPacket($"rdi {vnums[item]} {counts[item]}");
                }
                session.Character.Inventory.RemoveItemFromInventory(inv.Id);
            }

            if (inv.ItemVNum == 5100) //Fullmoon box large
            {
                int rnd = ServerManager.RandomNumber(0, 100);

                if (rnd < 5)
                {
                    short[] vnums =
                                                       {
                                5087
                            };
                    byte[] counts =
                    {
                              1
                            };
                    int item = ServerManager.RandomNumber(0, 1);
                    session.Character.GiftAdd(vnums[item], counts[item]);
                    session.SendPacket($"rdi {vnums[item]} {counts[item]}");
                }
                else
                {
                    short[] vnums =
                                                        {
                                1030
                            };
                    byte[] counts =
                    {
                              50
                            };
                    int item = ServerManager.RandomNumber(0, 1);
                    session.Character.GiftAdd(vnums[item], counts[item]);
                    session.SendPacket($"rdi {vnums[item]} {counts[item]}");
                }
                session.Character.Inventory.RemoveItemFromInventory(inv.Id);
            }

            if (inv.ItemVNum == 5749) //feather box large
            {
               // int rnd = ServerManager.RandomNumber(0, 100);
                    short[] vnums =
                                                       {
                                2282, 2282, 2282, 2282, 2282
                            };
                    byte[] counts =
                    {
                              200, 100, 50, 20, 10
                            };
                    int item = ServerManager.RandomNumber(0, 5);
                    session.Character.GiftAdd(vnums[item], counts[item]);
                    session.SendPacket($"rdi {vnums[item]} {counts[item]}");                             
                    session.Character.Inventory.RemoveItemFromInventory(inv.Id);
            }

            if (inv.ItemVNum == 5462) //Wondrous Wings & More Box
            {
                int pc = ServerManager.RandomNumber<int>(1, 3);

                short[] vnums = { 5498, 5499, 5432, 5431, 5372, 5087, 5203, 2282, 1030};
                byte[] counts = { 1, 1, 1, 1, 1, 1, 1, 25, 25};

                if (pc == 1)
                {
                    int pc2 = ServerManager.RandomNumber<int>(1, 2);
                    session.Character.GiftAdd(vnums[pc2], counts[pc2]);
                }
                else
                {
                    int pc3 = ServerManager.RandomNumber<int>(3, 9);
                    session.Character.GiftAdd(vnums[pc3], counts[pc3]);
                }

                session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                return;
            }
            if (inv.ItemVNum == 5746) //Rainbow Random Box
            {
                int pc = ServerManager.RandomNumber<int>(1, 3);

                short[] vnums = { 2282, 2282, 2282, 1030, 1030, 1030, 1078, 1011, 1011, 1246, 1247, 1248, 2321, 2321 };
                byte[] counts = { 10, 20, 30, 5, 10, 25, 2, 20, 50, 5, 5, 5, 2, 5 };

                if (pc == 1)
                {
                    int pc2 = ServerManager.RandomNumber<int>(1, 2);
                    session.Character.GiftAdd(vnums[pc2], counts[pc2]);
                }
                else
                {
                    int pc3 = ServerManager.RandomNumber<int>(3, 9);
                    session.Character.GiftAdd(vnums[pc3], counts[pc3]);
                }

                session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                return;
            }

            if (session.Character.IsVehicled && Effect != 1000)
            {
                if (VNum == 5119 || VNum == 9071) // Speed Booster
                {
                    if (!session.Character.Buff.Any(s => s.Card.CardId == 336))
                    {
                        session.Character.VehicleItem.BCards.ForEach(s => s.ApplyBCards(session.Character.BattleEntity, session.Character.BattleEntity));
                        session.CurrentMapInstance.Broadcast($"eff 1 {session.Character.CharacterId} 885");
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                    }
                }
                else
                {
                    session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("CANT_DO_VEHICLED"), 10));
                }
                return;
            }

            if (inv.ItemVNum >= 9300 && inv.ItemVNum <= 9400)
            {
                try
                {
                    if (DAOFactory.CharacterTitlesDAO.LoadByCharacterId(session.Character.CharacterId).Any(s => s.TitleId == VNum))
                    {
                        session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("TITLE_OWN")));
                        return;

                    }
                    CharacterTitlesDTO characterTitlesDTO = new CharacterTitlesDTO()
                    {
                        CharacterId = session.Character.CharacterId,
                        TitleId = inv.ItemVNum,

                    };
                    DAOFactory.CharacterTitlesDAO.InsertOrUpdate(ref characterTitlesDTO);
                    session.Character.EffTit = inv.ItemVNum;
                    session.Character.VisTit = inv.ItemVNum;
                    session.Character.GenerateTitle();
                    session.Character.ViewTittle();
                    session.SendPacket(session.Character.GenerateTit());
                    session.SendPacket(session.Character.GenerateTitInfo());
                    session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                    session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("TITLE_ADDED")));
                }
                catch (Exception ex)
                {

                }
            }
            if (VNum == 5511)
            {
                session.Character.GeneralLogs.Where(s => s.LogType == "InstanceEntry" && (short.Parse(s.LogData) == 16 || short.Parse(s.LogData) == 17) && s.Timestamp.Date == DateTime.Today).ToList().ForEach(s =>
                {
                    s.LogType = "NulledInstanceEntry";
                    DAOFactory.GeneralLogDAO.InsertOrUpdate(ref s);
                });
                session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                return;
            }

            if (session.CurrentMapInstance?.MapInstanceType != MapInstanceType.TalentArenaMapInstance
            && (VNum == 5936 || VNum == 5937 || VNum == 5938 || VNum == 5939 || VNum == 5940 || VNum == 5942 || VNum == 5943 || VNum == 5944 || VNum == 5945 || VNum == 5946))
            {
                return;
            }
            if (session.CurrentMapInstance?.MapInstanceType == MapInstanceType.TalentArenaMapInstance
            && VNum != 5936 && VNum != 5937 && VNum != 5938 && VNum != 5939 && VNum != 5940 && VNum != 5942 && VNum != 5943 && VNum != 5944 && VNum != 5945 && VNum != 5946)
            {
                return;
            }

            if (BCards.Count > 0 && Effect != 1000)
            {
                if (BCards.Any(s => s.Type == (byte)BCardType.CardType.Buff && s.SubType == 1 && new Buff((short)s.SecondData, session.Character.Level).Card.BCards.Any(newbuff => session.Character.Buff.GetAllItems().Any(b => b.Card.BCards.Any(buff =>
                    buff.CardId != newbuff.CardId
                 && ((buff.Type == 33 && buff.SubType == 5 && (newbuff.Type == 33 || newbuff.Type == 58)) || (newbuff.Type == 33 && newbuff.SubType == 5 && (buff.Type == 33 || buff.Type == 58))
                 || (buff.Type == 33 && (buff.SubType == 1 || buff.SubType == 3) && (newbuff.Type == 58 && (newbuff.SubType == 1))) || (buff.Type == 33 && (buff.SubType == 2 || buff.SubType == 4) && (newbuff.Type == 58 && (newbuff.SubType == 3)))
                 || (newbuff.Type == 33 && (newbuff.SubType == 1 || newbuff.SubType == 3) && (buff.Type == 58 && (buff.SubType == 1))) || (newbuff.Type == 33 && (newbuff.SubType == 2 || newbuff.SubType == 4) && (buff.Type == 58 && (buff.SubType == 3)))
                 || (buff.Type == 33 && newbuff.Type == 33 && buff.SubType == newbuff.SubType) || (buff.Type == 58 && newbuff.Type == 58 && buff.SubType == newbuff.SubType)))))))
                {
                    return;
                }
                BCards.ForEach(c => c.ApplyBCards(session.Character.BattleEntity, session.Character.BattleEntity));
                session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                return;
            }

            switch (Effect)
            {
                

                // starter pack
                case 1440:
                    session.Character.GiftAdd(4301, 1);
                    session.Character.GiftAdd(4303, 1);
                    session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                    break;

                case 1441: // reward item
                    short[] vnums14 =
                    {
                                 5010, 5422, 1012, 1244, 1242, 1243, 1452, 1011, 2511, 2512, 2513, 2283, 2284, 2285, 1363, 1364, 2333, 1249, 1246, 1247, 1248
                            };
                    byte[] counts14 = { 5, 5, 20, 20, 20, 20, 1, 20, 5, 5, 5, 5, 5, 5, 1, 1, 2, 1, 3, 3, 3 };
                    int item14 = ServerManager.RandomNumber(0, 21);
                    session.Character.GiftAdd(vnums14[item14], counts14[item14]);
                    session.Character.AddBuff(new Buff(166, session.Character.Level), session.Character.BattleEntity);
                    session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                    break;

                case 5998:
                    {
                        session.Character.GiftAdd(5997, 1);
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        break;
                    }


                // Seal Mini-Game
                case 1717:
                    switch (EffectValue)
                    {
                        case 1:// King Ratufu Mini Game
                               // Not Created for moment .
                            break;
                        case 2: // Sheep Mini Game
                            session.SendPacket($"say 1 {session.Character.CharacterId} 10 L'inscription commence dans 5 secondes.");
                            //EventHelper.Instance.GenerateEvent(EventType.SHEEPGAME);
                            break;
                        case 3: // Meteor Mini Game
                            session.SendPacket($"say 1 {session.Character.CharacterId} 10 L'inscription commence dans 5 secondes.");
                            //EventHelper.Instance.GenerateEvent(EventType.METEORITEGAME);
                            break;
                    }
                    break;
                case 930:
                    switch (EffectValue)
                    {
                        case 505:
                            if(session.Character.Group != null)
                            {
                                session.SendPacket(UserInterfaceHelper.GenerateInfo("Please disband your group."));
                                return;
                            }
                            if (session.Character.MapId != 1)
                            {
                                session.SendPacket(UserInterfaceHelper.GenerateInfo("Return to to the PTS area."));
                                return;
                            }
                            if (session.Character.MapId == 1)
                            {
                                int dist = Map.GetDistance(
                                    new MapCell { X = session.Character.PositionX, Y = session.Character.PositionY },
                                    new MapCell { X = 120, Y = 56 });
                                if (dist < 5)
                                {
                                    GameObject.Event.PTS.GeneratePTS(1805, session);
                                  
                                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                    
                                }
                                else
                                {
                                    session.SendPacket(UserInterfaceHelper.GenerateInfo("Return to to the PTS area."));
                                }
                            }
                      


                            break;
                    }
                    break;
                ////btk register
                case 1227:
                    {
                        if (ServerManager.Instance.CanRegisterRainbowBattle == true)
                        {
                            if (session.Character.Family != null)
                            {
                                if (session.Character.Family.FamilyCharacters.Where(s => s.CharacterId == session.Character.CharacterId).First().Authority == FamilyAuthority.Head || session.Character.Family.FamilyCharacters.Where(s => s.CharacterId == session.Character.CharacterId).First().Authority == FamilyAuthority.Familykeeper)
                                {
                                    if (ServerManager.Instance.IsCharacterMemberOfGroup(session.Character.CharacterId))
                                    {
                                        session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("RAINBOWBATTLE_OPEN_GROUP"), 12));
                                        return;
                                    }
                                    Group group = new Group
                                    {
                                        GroupType = GroupType.BigTeam
                                    };
                                    group.JoinGroup(session.Character.CharacterId);
                                    ServerManager.Instance.AddGroup(group);
                                    session.SendPacket(session.Character.GenerateFbt(2));
                                    session.SendPacket(session.Character.GenerateFbt(0));
                                    session.SendPacket(session.Character.GenerateFbt(1));
                                    session.SendPacket(group.GenerateFblst());
                                    session.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("RAINBOWBATTLE_LEADER"), session.Character.Name), 0));
                                    session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("RAINBOWBATTLE_LEADER"), session.Character.Name), 10));
                                    ItemInstance RainbowBattleSeal = session.Character.Inventory.LoadBySlotAndType(inv.Slot, InventoryType.Main);
                                    session.Character.Inventory.RemoveItemFromInventory(RainbowBattleSeal.Id);
                                }
                            }
                        }
                    }
                    break;
                case 9300:
                    
                    break;
                case 882: // morcos
                    int rnd7 = ServerManager.RandomNumber(0, 1000);
                    int random = ServerManager.RandomNumber(5, 8);
                    short[] vnums7 = null;
                    if (rnd7 < 900)
                    {
                        vnums7 = new short[] { 567, 570, 573, 576, 579, 582, 585, 588 };
                    }
                    else
                    {
                        vnums7 = new short[] { 567, 570, 573, 576, 579, 582, 585, 588 };
                    }
                    session.Character.GiftAdd(vnums7[ServerManager.RandomNumber(0, 7)], 1, (byte)random);
                    session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                    break;

                case 5836://Carry-Item
                    session.SendPacket($"gb 0 {session.Character.GoldBank / 1000} {session.Character.Gold} 0 0");
                    session.SendPacket($"s_memo 6 [Account balance]: {session.Character.GoldBank} gold; [Owned]: {session.Character.Gold} gold\nWe will do our best. Thank you for using the services of Cuarry Bank.");
                    break;

                case 1400://No se que bob
                    Mate equipedMate = session.Character.Mates?.SingleOrDefault(s => s.IsTeamMember && s.MateType == MateType.Partner);

                    if (equipedMate != null)
                    {
                        equipedMate.RemoveTeamMember();
                        session.Character.MapInstance.Broadcast(equipedMate.GenerateOut());
                    }

                    Mate mate = new Mate(session.Character, ServerManager.GetNpcMonster(317), 24, MateType.Partner);
                    session.Character.Mates?.Add(mate);
                    mate.RefreshStats();
                    session.SendPacket($"ctl 2 {mate.PetId} 3");
                    session.Character.MapInstance.Broadcast(mate.GenerateIn());
                    session.SendPacket(UserInterfaceHelper.GeneratePClear());
                    session.SendPackets(session.Character.GenerateScP());
                    session.SendPackets(session.Character.GenerateScN());
                    session.SendPacket(session.Character.GeneratePinit());
                    session.SendPackets(session.Character.Mates.Where(s => s.IsTeamMember)
                        .OrderBy(s => s.MateType)
                        .Select(s => s.GeneratePst()));
                    session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                    break;
                case 1419:
                    Mate equipedMates = session.Character.Mates?.SingleOrDefault(s => s.IsTeamMember && s.MateType == MateType.Partner);

                    if (equipedMates != null)
                    {
                        equipedMates.RemoveTeamMember();
                        session.Character.MapInstance.Broadcast(equipedMates.GenerateOut());
                    }

                    Mate mates = new Mate(session.Character, ServerManager.GetNpcMonster(318), 31, MateType.Partner);
                    session.Character.Mates?.Add(mates);
                    mates.RefreshStats();
                    session.SendPacket($"ctl 2 {mates.PetId} 3");
                    session.Character.MapInstance.Broadcast(mates.GenerateIn());
                    session.SendPacket(UserInterfaceHelper.GeneratePClear());
                    session.SendPackets(session.Character.GenerateScP());
                    session.SendPackets(session.Character.GenerateScN());
                    session.SendPacket(session.Character.GeneratePinit());
                    session.SendPackets(session.Character.Mates.Where(s => s.IsTeamMember)
                        .OrderBy(s => s.MateType)
                        .Select(s => s.GeneratePst()));
                    session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                    break;
                case 1431:
                    Mate equipedMat = session.Character.Mates?.SingleOrDefault(s => s.IsTeamMember && s.MateType == MateType.Partner);

                    if (equipedMat != null)
                    {
                        equipedMat.RemoveTeamMember();
                        session.Character.MapInstance.Broadcast(equipedMat.GenerateOut());
                    }

                    Mate mat = new Mate(session.Character, ServerManager.GetNpcMonster(319), 48, MateType.Partner);
                    session.Character.Mates?.Add(mat);
                    mat.RefreshStats();
                    session.SendPacket($"ctl 2 {mat.PetId} 3");
                    session.Character.MapInstance.Broadcast(mat.GenerateIn());
                    session.SendPacket(UserInterfaceHelper.GeneratePClear());
                    session.SendPackets(session.Character.GenerateScP());
                    session.SendPackets(session.Character.GenerateScN());
                    session.SendPacket(session.Character.GeneratePinit());
                    session.SendPackets(session.Character.Mates.Where(s => s.IsTeamMember)
                        .OrderBy(s => s.MateType)
                        .Select(s => s.GeneratePst()));
                    session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                    break;
                case 5511:
                    session.Character.GeneralLogs.Where(s => s.LogType == "InstanceEntry" && (short.Parse(s.LogData) == 16 || short.Parse(s.LogData) == 17) && s.Timestamp.Date == DateTime.Today).ToList().ForEach(s =>
                    {
                        s.LogType = "NulledInstanceEntry";
                        DAOFactory.GeneralLogDAO.InsertOrUpdate(ref s);
                    });
                    session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                    break;
                case 5936://Si estas en arena no puedes usar este item
                case 5937://Si estas en arena no puedes usar este item
                case 5938://Si estas en arena no puedes usar este item
                case 5939://Si estas en arena no puedes usar este item
                case 5940://Si estas en arena no puedes usar este item
                case 5942://Si estas en arena no puedes usar este item
                case 5943://Si estas en arena no puedes usar este item
                case 5944://Si estas en arena no puedes usar este item
                case 5945://Si estas en arena no puedes usar este item
                case 5946://Si estas en arena no puedes usar este item
                    if (session.CurrentMapInstance?.MapInstanceType != MapInstanceType.TalentArenaMapInstance)
                    {
                        session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("CANT_DO_NOT_IN_ARENA"), 10));
                        return;
                    }
                    break;
                default:
                    if (session.CurrentMapInstance?.MapInstanceType == MapInstanceType.TalentArenaMapInstance
                    && VNum != 5936 && VNum != 5937 && VNum != 5938 && VNum != 5939 && VNum != 5940 && VNum != 5942 && VNum != 5943 && VNum != 5944 && VNum != 5945 && VNum != 5946)
                    {
                        session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("CANT_DO_NOT_OUT_ARENA"), 10));
                        return;
                    }
                    break;
            }

            switch (Effect)
            {
                case 604:
                    if (session.Character.StaticBonusList.All(s => s.StaticBonusType != StaticBonusType.BackPack))
                    {
                        session.Character.StaticBonusList.Add(new StaticBonusDTO
                        {
                            CharacterId = session.Character.CharacterId,
                            DateEnd = DateTime.Now.AddYears(15),
                            StaticBonusType = StaticBonusType.BackPack
                        });
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        session.SendPacket(session.Character.GenerateExts());
                        session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("EFFECT_ACTIVATED"), Name), 12));
                    }
                    break;

                // Honour Medals
                case 69:
                    session.Character.Reputation += ReputPrice;
                    session.SendPacket(session.Character.GenerateFd());
                    session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("REPUT_INCREASE"), ReputPrice), 11));
                    session.CurrentMapInstance?.Broadcast(session, session.Character.GenerateIn(InEffect: 1), ReceiverType.AllExceptMe);
                    session.CurrentMapInstance?.Broadcast(session, session.Character.GenerateGidx(), ReceiverType.AllExceptMe);
                    session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                    break;

                // TimeSpace Stones
                case 140:
                    switch (EffectValue)
                    {
                       case 401:
                            {
                                if (VNum == 1329)
                                {
                                        
                                            Guid MapInstanceId = ServerManager.GetBaseMapInstanceIdByMapId(session.Character.MapId);
                                            MapInstance map = ServerManager.GetMapInstance(MapInstanceId);
                                            ScriptedInstance timespace = map.ScriptedInstances.Find(s => s.ScriptedInstanceId == 1);
                                            ScriptedInstance instance = timespace.Copy();
                                            instance.LoadScript(MapInstanceType.TimeSpaceInstance,creator:session.Character);
                                                if (instance.FirstMap == null)
                                                {
                                                    return;
                                                }
                                                session.Character.MapX = instance.PositionX;
                                                session.Character.MapY = instance.PositionY;
                                                ServerManager.Instance.TeleportOnRandomPlaceInMap(session, instance.FirstMap.MapInstanceId);
                                                instance.InstanceBag.CreatorId = session.Character.CharacterId;
                                                session.SendPackets(instance.GenerateMinimap());
                                                session.SendPacket(instance.GenerateMainInfo());
                                                session.SendPacket(instance.FirstMap.InstanceBag.GenerateScore());
                                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                        session.Character.Timespace = instance;
                                   
                                    }
                                    
                                
                            }
                            break;
                    }
                    break;

                // SP Potions
                case 150:
                case 151:
                    {
                        if (session.Character.SpAdditionPoint >= 1000000)
                        {
                            session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("SP_POINTS_FULL"), 0));
                            break;
                        }

                        session.Character.SpAdditionPoint += EffectValue;

                        if (session.Character.SpAdditionPoint > 1000000)
                        {
                            session.Character.SpAdditionPoint = 1000000;
                        }

                        session.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("SP_POINTSADDED"), EffectValue), 0));
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        session.SendPacket(session.Character.GenerateSpPoint());
                    }
                    break;

                // ArenaWinner Key   
                case 1400:
                    {
                        int arena = 0;
                        if (session.Character.ArenaWinner == 0)
                            arena = 1;
                        session.Character.ArenaWinner = arena;
                        session.CurrentMapInstance?.Broadcast(session.Character.GenerateCMode());
                    }
                    break;

                // Specialist Medal
                case 204:
                    {
                        if (session.Character.SpPoint >= 10000
                            && session.Character.SpAdditionPoint >= 1000000)
                        {
                            session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("SP_POINTS_FULL"), 0));
                            break;
                        }

                        session.Character.SpPoint += EffectValue;

                        if (session.Character.SpPoint > 10000)
                        {
                            session.Character.SpPoint = 10000;
                        }

                        session.Character.SpAdditionPoint += EffectValue * 3;

                        if (session.Character.SpAdditionPoint > 1000000)
                        {
                            session.Character.SpAdditionPoint = 1000000;
                        }

                        session.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("SP_POINTSADDEDBOTH"), EffectValue, EffectValue * 3), 0));
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        session.SendPacket(session.Character.GenerateSpPoint());
                    }
                    break;

                // Raid Seals
                case 301:
                    ItemInstance raidSeal = session.Character.Inventory.LoadBySlotAndType(inv.Slot, InventoryType.Main);

                    if (raidSeal != null)
                    {
                        ScriptedInstance raid = ServerManager.Instance.Raids.FirstOrDefault(s => s.Id == raidSeal.Item.EffectValue)?.Copy();

                        if (raid != null)
                        {
                            if (ServerManager.Instance.ChannelId == 51 || session.CurrentMapInstance.MapInstanceType != MapInstanceType.BaseMapInstance)
                            {
                                return;
                            }

                            if (ServerManager.Instance.IsCharacterMemberOfGroup(session.Character.CharacterId))
                            {
                                session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("RAID_OPEN_GROUP"), 12));
                                return;
                            }

                            var entries = raid.DailyEntries - session.Character.GeneralLogs.CountLinq(s => s.LogType == "InstanceEntry" && short.Parse(s.LogData) == raid.Id && s.Timestamp.Date == DateTime.Today);

                            if (raid.DailyEntries > 0 && entries <= 0)
                            {
                                session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("INSTANCE_NO_MORE_ENTRIES"), 0));
                                session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("INSTANCE_NO_MORE_ENTRIES"), 10));
                                return;
                            }
                            if (raidSeal.ItemVNum == 5500 && session.Character.Inventory
                                      .LoadBySlotAndType((short)EquipmentType.Amulet, InventoryType.Wear)?.ItemVNum != 4503 || raidSeal.ItemVNum == 5512 && session.Character.Inventory
                                      .LoadBySlotAndType((short)EquipmentType.Amulet, InventoryType.Wear)?.ItemVNum != 4504)
                            {
                                session.SendPacket(session.Character.GenerateSay(
                                                   Language.Instance.GetMessageFromKey("RAID_MISSING_ITEM"), 10));
                                return;
                            }

                            if (session.Character.Level > raid.LevelMaximum || session.Character.Level < raid.LevelMinimum)
                            {
                                session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("RAID_LEVEL_INCORRECT_HIGH"), 10));                               
                            }
                          if(session.Character.Level < raid.LevelMinimum)
                            {
                                session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("RAID_LEVEL_INCORRECT_LOW"), 10));
                                return;
                            }

                            Group group = new Group
                            {
                                GroupType = raid.IsGiantTeam ? GroupType.GiantTeam : GroupType.BigTeam,
                                Raid = raid
                            };

                            if (group.JoinGroup(session))
                            {
                                ServerManager.Instance.AddGroup(group);
                                session.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("RAID_LEADER"), session.Character.Name), 0));
                                session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("RAID_LEADER"), session.Character.Name), 10));
                                session.SendPacket(session.Character.GenerateRaid(2));
                                session.SendPacket(session.Character.GenerateRaid(0));
                                session.SendPacket(session.Character.GenerateRaid(1));
                                session.SendPacket(group.GenerateRdlst());
                                session.Character.Inventory.RemoveItemFromInventory(raidSeal.Id);
                            }
                        }
                    }
                    break;

                // Partner Suits/Skins
                case 305:
                    Mate mate = session.Character.Mates.Find(s => s.MateTransportId == int.Parse(packetsplit[3]));
                    if (mate != null && EffectValue == mate.NpcMonsterVNum && mate.Skin == 0)
                    {
                        mate.Skin = Morph;
                        session.SendPacket(mate.GenerateCMode(mate.Skin));
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                    }
                    break;

                //suction Funnel (Quest Item / QuestId = 1724)
                case 400:
                    if (session.Character == null || session.Character.Quests.All(q => q.QuestId != 1724))
                    {
                        break;
                    }
                    if (session.Character.Quests.FirstOrDefault(q => q.QuestId == 1724) is CharacterQuest kenkoQuest)
                    {
                        MapMonster kenko = session.CurrentMapInstance?.Monsters.FirstOrDefault(m => m.MapMonsterId == session.Character.LastNpcMonsterId && m.MonsterVNum > 144 && m.MonsterVNum < 154);
                        if (kenko == null || session.Character.Inventory.CountItem(1174) > 0)
                        {
                            break;
                        }
                        if (session.Character.LastFunnelUse.AddSeconds(30) <= DateTime.Now)
                        {
                            if (kenko.CurrentHp / kenko.MaxHp * 100 < 30)
                            {
                                if (ServerManager.RandomNumber() < 30)
                                {
                                    kenko.SetDeathStatement();
                                    session.Character.MapInstance.Broadcast(StaticPacketHelper.Out(UserType.Monster, kenko.MapMonsterId));
                                    session.Character.Inventory.AddNewToInventory(1174); // Kenko Bead
                                    session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                    session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("KENKO_CATCHED"), 0));
                                }
                                else { session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("QUEST_CATCH_FAIL"), 0)); }
                                session.Character.LastFunnelUse = DateTime.Now;
                            }
                            else { session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("HP_TOO_HIGH"), 0)); }
                        }
                    }
                    break;

                // Fairy Booster
                case 250:
                    if (!session.Character.Buff.ContainsKey(131))
                    {
                        session.Character.AddStaticBuff(new StaticBuffDTO { CardId = 131 });
                        session.CurrentMapInstance?.Broadcast(session.Character.GeneratePairy());
                        session.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("EFFECT_ACTIVATED"), inv.Item.Name), 0));
                        session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, session.Character.CharacterId, 3014), session.Character.PositionX, session.Character.PositionY);
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                    }
                    else
                    {
                        session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("ITEM_IN_USE"), 0));
                    }
                    break;

                // Rainbow Pearl/Magic Eraser
                case 666:
                    if (EffectValue == 1 && byte.TryParse(packetsplit[9], out byte islot))
                    {
                        ItemInstance wearInstance = session.Character.Inventory.LoadBySlotAndType(islot, InventoryType.Equipment);

                        if (wearInstance != null && (wearInstance.Item.ItemType == ItemType.Weapon || wearInstance.Item.ItemType == ItemType.Armor) && wearInstance.ShellEffects.Count != 0 && !wearInstance.Item.IsHeroic)
                        {
                            wearInstance.ShellEffects.Clear();
                            wearInstance.ShellRarity = null;
                            DAOFactory.ShellEffectDAO.DeleteByEquipmentSerialId(wearInstance.EquipmentSerialId);
                            if (wearInstance.EquipmentSerialId == Guid.Empty)
                            {
                                wearInstance.EquipmentSerialId = Guid.NewGuid();
                            }
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                            session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("OPTION_DELETE"), 0));
                        }
                    }
                    else
                    {
                        session.SendPacket("guri 18 0");
                    }
                    break;

                // Atk/Def/HP/Exp potions
                case 6600:
                    session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                    break;

                // Ancelloan's Blessing
                case 208:
                    if (!session.Character.Buff.ContainsKey(121))
                    {
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        session.Character.AddStaticBuff(new StaticBuffDTO { CardId = 121 });
                    }
                    else
                    {
                        session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("ITEM_IN_USE"), 0));
                    }
                    break;

                    //Autoloot 7 Days
                case 9888:
                    if (session.Character.StaticBonusList.All(s => s.StaticBonusType != StaticBonusType.AutoLoot))
                    {
                        session.Character.StaticBonusList.Add(new StaticBonusDTO
                        {
                            CharacterId = session.Character.CharacterId,
                            DateEnd = DateTime.Now.AddYears(7),
                            StaticBonusType = StaticBonusType.AutoLoot
                        });
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                       // session.SendPacket(session.Character.GenerateExts());
                        session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("EFFECT_ACTIVATED"), Name), 12));
                    }
                    else
                    {
                        session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("ITEM_IN_USE"), 0));
                    }
                    break;

                // Guardian Angel's Blessing
                case 210:
                    if (!session.Character.Buff.ContainsKey(122))
                    {
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        session.Character.AddStaticBuff(new StaticBuffDTO { CardId = 122 });
                    }
                    else
                    {
                        session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("ITEM_IN_USE"), 0));
                    }
                    break;

                case 2081:
                    if (!session.Character.Buff.ContainsKey(146))
                    {
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        session.Character.AddStaticBuff(new StaticBuffDTO { CardId = 146 });
                    }
                    else
                    {
                        session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("ITEM_IN_USE"), 0));
                    }
                    break;

                // Divorce letter
                case 6969:
                    if (session.Character.Group != null)
                    {
                        session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ALLOWED_IN_GROUP"), 0));
                        return;
                    }
                    CharacterRelationDTO rel = session.Character.CharacterRelations.FirstOrDefault(s => s.RelationType == CharacterRelationType.Spouse);
                    if (rel != null)
                    {
                        session.Character.DeleteRelation(rel.CharacterId == session.Character.CharacterId ? rel.RelatedCharacterId : rel.CharacterId, CharacterRelationType.Spouse);
                        session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("DIVORCED")));
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                    }
                    break;

                // Cupid's arrow
                case 34:
                    if (packetsplit != null && packetsplit.Length > 3)
                    {
                        if (long.TryParse(packetsplit[3], out long characterId))
                        {
                            if (session.Character.CharacterId == characterId)
                            {
                                return;
                            }
                            if (session.Character.CharacterRelations.Any(s => s.RelationType == CharacterRelationType.Spouse))
                            {
                                session.SendPacket($"info {Language.Instance.GetMessageFromKey("ALREADY_MARRIED")}");
                                return;
                            }
                            if (session.Character.Group != null)
                            {
                                session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ALLOWED_IN_GROUP"), 0));
                                return;
                            }
                            if (!session.Character.IsFriendOfCharacter(characterId))
                            {
                                session.SendPacket($"info {Language.Instance.GetMessageFromKey("MUST_BE_FRIENDS")}");
                                return;
                            }
                            ClientSession otherSession = ServerManager.Instance.GetSessionByCharacterId(characterId);
                            if (otherSession != null)
                            {
                                if (otherSession.Character.Group != null)
                                {
                                    session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("OTHER_PLAYER_IN_GROUP"), 0));
                                    return;
                                }
                                otherSession.SendPacket(UserInterfaceHelper.GenerateDialog(
                                    $"#fins^34^{session.Character.CharacterId} #fins^69^{session.Character.CharacterId} {string.Format(Language.Instance.GetMessageFromKey("MARRY_REQUEST"), session.Character.Name)}"));
                                session.Character.MarryRequestCharacters.Add(characterId);
                                session.Character.Inventory.RemoveItemFromInventory(inv.Id);

                            }
                        }
                    }
                    break;

                case 100: // Miniland Signpost
                    {
                        if (session.Character.BattleEntity.GetOwnedNpcs().Any(s => session.Character.BattleEntity.IsSignpost(s.NpcVNum)))
                        {
                            return;
                        }
                        if (session.CurrentMapInstance.MapInstanceType == MapInstanceType.BaseMapInstance && new short[] { 2628 }.Contains(session.CurrentMapInstance.Map.MapId))
                        {
                            MapNpc signPost = new MapNpc
                            {
                                NpcVNum = (short)EffectValue,
                                MapX = session.Character.PositionX,
                                MapY = session.Character.PositionY,
                                MapId = session.CurrentMapInstance.Map.MapId,
                                ShouldRespawn = false,
                                IsMoving = false,
                                MapNpcId = session.CurrentMapInstance.GetNextNpcId(),
                                Owner = session.Character.BattleEntity,
                                Dialog = 10000,
                                Position = 2,
                                Name = $"{session.Character.Name}'s^[Miniland]"
                            };
                            switch (EffectValue)
                            {
                                case 920:
                                case 1385:
                                case 1428:
                                case 1499:
                                case 1519:
                                    signPost.AliveTime = 3600;
                                    break;
                                default:
                                    signPost.AliveTime = 1800;
                                    break;
                            }
                            signPost.Initialize(session.CurrentMapInstance);
                            session.CurrentMapInstance.AddNPC(signPost);
                            session.CurrentMapInstance.Broadcast(signPost.GenerateIn());
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        }
                    }
                    break;

                case 550: // Campfire and other craft npcs
                    {
                        if (session.CurrentMapInstance.MapInstanceType == MapInstanceType.BaseMapInstance)
                        {
                            short dialog = 10023;
                            switch (EffectValue)
                            {
                                case 956:
                                    dialog = 10023;
                                    break;
                                case 957:
                                    dialog = 10024;
                                    break;
                                case 959:
                                    dialog = 10026;
                                    break;
                            }
                            MapNpc campfire = new MapNpc
                            {
                                NpcVNum = (short)EffectValue,
                                MapX = session.Character.PositionX,
                                MapY = session.Character.PositionY,
                                MapId = session.CurrentMapInstance.Map.MapId,
                                ShouldRespawn = false,
                                IsMoving = false,
                                MapNpcId = session.CurrentMapInstance.GetNextNpcId(),
                                Owner = session.Character.BattleEntity,
                                Dialog = dialog,
                                Position = 2,
                            };
                            campfire.AliveTime = 180;
                            campfire.Initialize(session.CurrentMapInstance);
                            session.CurrentMapInstance.AddNPC(campfire);
                            session.CurrentMapInstance.Broadcast(campfire.GenerateIn());
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        }
                    }
                    break;

                // Faction Egg
                case 570:
                    if (session.Character.Faction == (FactionType)EffectValue)
                    {
                        return;
                    }
                    if (EffectValue < 3)
                    {
                        session.SendPacket(session.Character.Family == null
                            ? $"qna #guri^750^{EffectValue} {Language.Instance.GetMessageFromKey($"ASK_CHANGE_FACTION{EffectValue}")}"
                            : UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("IN_FAMILY"),
                            0));
                    }
                    else
                    {
                        session.SendPacket(session.Character.Family != null
                            ? $"qna #guri^750^{EffectValue} {Language.Instance.GetMessageFromKey($"ASK_CHANGE_FACTION{EffectValue}")}"
                            : UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NO_FAMILY"),
                            0));
                    }

                    break;

                // SP Wings
                case 650:
                    ItemInstance specialistInstance = session.Character.Inventory.LoadBySlotAndType((byte)EquipmentType.Sp, InventoryType.Wear);
                    if (session.Character.UseSp && specialistInstance != null && !session.Character.IsSeal)
                    {
                        if (Option == 0)
                        {
                            session.SendPacket($"qna #u_i^1^{session.Character.CharacterId}^{(byte)inv.Type}^{inv.Slot}^3 {Language.Instance.GetMessageFromKey("ASK_WINGS_CHANGE")}");
                        }
                        else
                        {
                            void disposeBuff(short vNum)
                            {
                                if (session.Character.BuffObservables.ContainsKey(vNum))
                                {
                                    session.Character.BuffObservables[vNum].Dispose();
                                    session.Character.BuffObservables.Remove(vNum);
                                }
                                session.Character.RemoveBuff(vNum);
                            }

                            disposeBuff(387);
                            disposeBuff(395);
                            disposeBuff(396);
                            disposeBuff(397);
                            disposeBuff(398);
                            disposeBuff(410);
                            disposeBuff(411);
                            disposeBuff(444);
                            disposeBuff(663);
                            disposeBuff(686);

                            specialistInstance.Design = (byte)EffectValue;

                            session.Character.MorphUpgrade2 = EffectValue;
                            session.CurrentMapInstance?.Broadcast(session.Character.GenerateCMode());
                            session.SendPacket(session.Character.GenerateStat());
                            session.SendPackets(session.Character.GenerateStatChar());
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        }
                    }
                    else
                    {
                        session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NO_SP"), 0));
                    }
                    break;

                // Self-Introduction
                case 203:
                    if (!session.Character.IsVehicled && Option == 0)
                    {
                        session.SendPacket(UserInterfaceHelper.GenerateGuri(10, 2, session.Character.CharacterId, 1));
                    }
                    break;

                // Magic Lamp
                case 651:
                    if (session.Character.Inventory.All(i => i.Type != InventoryType.Wear))
                    {
                        if (Option == 0)
                        {
                            session.SendPacket($"qna #u_i^1^{session.Character.CharacterId}^{(byte)inv.Type}^{inv.Slot}^3 {Language.Instance.GetMessageFromKey("ASK_USE")}");
                        }
                        else
                        {
                            session.Character.ChangeSex();
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        }
                    }
                    else
                    {
                        session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("EQ_NOT_EMPTY"), 0));
                    }
                    break;

                // Vehicles
                case 1000:
                    if (EffectValue != 0
                     || session.CurrentMapInstance?.MapInstanceType == MapInstanceType.EventGameInstance
                     || session.CurrentMapInstance?.MapInstanceType == (MapInstanceType.TalentArenaMapInstance)
                     || session.CurrentMapInstance?.MapInstanceType == (MapInstanceType.IceBreakerInstance)
                     || session.Character.IsSeal || session.Character.IsMorphed)
                    {
                        return;
                    }
                    short morph = Morph;
                    byte speed = Speed;
                    if (Morph < 0)
                    {
                        switch (VNum)
                        {
                            case 5923:
                                morph = 2513;
                                speed = 14;
                                break;
                        }
                    }
                    if (morph > 0)
                    {
                        if (Option == 0 && !session.Character.IsVehicled)
                        {
                            if (session.Character.Buff.Any(s => s.Card.BuffType == BuffType.Bad))
                            {
                                session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("CANT_TRASFORM_WITH_DEBUFFS"),
                                    0));
                                return;
                            }
                            if (session.Character.IsSitting)
                            {
                                session.Character.IsSitting = false;
                                session.CurrentMapInstance?.Broadcast(session.Character.GenerateRest());
                            }
                            session.Character.LastDelay = DateTime.Now;
                            session.SendPacket(UserInterfaceHelper.GenerateDelay(3000, 3, $"#u_i^1^{session.Character.CharacterId}^{(byte)inv.Type}^{inv.Slot}^2"));
                        }
                        else
                        {
                            if (!session.Character.IsVehicled && Option != 0)
                            {
                                DateTime delay = DateTime.Now.AddSeconds(-4);
                                if (session.Character.LastDelay > delay && session.Character.LastDelay < delay.AddSeconds(2))
                                {
                                    session.Character.IsVehicled = true;
                                    session.Character.VehicleSpeed = speed;
                                    session.Character.VehicleItem = this;
                                    session.Character.LoadSpeed();
                                    session.Character.MorphUpgrade = 0;
                                    session.Character.MorphUpgrade2 = 0;
                                    session.Character.Morph = morph + (byte)session.Character.Gender;
                                    session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, session.Character.CharacterId, 196), session.Character.PositionX, session.Character.PositionY);
                                    session.CurrentMapInstance?.Broadcast(session.Character.GenerateCMode());
                                    session.SendPacket(session.Character.GenerateCond());
                                    session.Character.LastSpeedChange = DateTime.Now;
                                    session.Character.Mates.Where(s => s.IsTeamMember).ToList()
                                        .ForEach(s => session.CurrentMapInstance?.Broadcast(s.GenerateOut()));
                                    if (Morph < 0)
                                    {
                                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                    }
                                }
                            }
                            else if (session.Character.IsVehicled)
                            {
                                session.Character.RemoveVehicle();
                                foreach (Mate teamMate in session.Character.Mates.Where(m => m.IsTeamMember))
                                {
                                    teamMate.PositionX =
                                        (short)(session.Character.PositionX + (teamMate.MateType == MateType.Partner ? -1 : 1));
                                    teamMate.PositionY = (short)(session.Character.PositionY + 1);
                                    if (session.Character.MapInstance.Map.IsBlockedZone(teamMate.PositionX, teamMate.PositionY))
                                    {
                                        teamMate.PositionX = session.Character.PositionX;
                                        teamMate.PositionY = session.Character.PositionY;
                                    }
                                    teamMate.UpdateBushFire();
                                    Parallel.ForEach(session.CurrentMapInstance.Sessions.Where(s => s.Character != null), s =>
                                    {
                                        if (ServerManager.Instance.ChannelId != 51 || session.Character.Faction == s.Character.Faction)
                                        {
                                            s.SendPacket(teamMate.GenerateIn(false, ServerManager.Instance.ChannelId == 51));
                                        }
                                        else
                                        {
                                            s.SendPacket(teamMate.GenerateIn(true, ServerManager.Instance.ChannelId == 51, s.Account.Authority));
                                        }
                                    });
                                }
                                session.SendPacket(session.Character.GeneratePinit());
                                session.Character.Mates.ForEach(s => session.SendPacket(s.GenerateScPacket()));
                                session.SendPackets(session.Character.GeneratePst());
                            }
                        }
                    }
                    break;

                // Sealed Vessel
                case 1002:
                    int type, secondaryType, inventoryType, slot;
                    if (packetsplit != null && int.TryParse(packetsplit[2], out type) && int.TryParse(packetsplit[3], out secondaryType) && int.TryParse(packetsplit[4], out inventoryType) && int.TryParse(packetsplit[5], out slot))
                    {
                        int packetType;
                        switch (EffectValue)
                        {
                            case 69:
                                if (int.TryParse(packetsplit[6], out packetType))
                                {
                                    switch (packetType)
                                    {
                                        case 0:
                                            session.SendPacket(UserInterfaceHelper.GenerateDelay(5000, 7, $"#u_i^{type}^{secondaryType}^{inventoryType}^{slot}^1"));
                                            break;
                                        case 1:
                                            int rnd = ServerManager.RandomNumber(0, 1000);
                                            if (rnd < 5)
                                            {
                                                short[] vnums =
                                                {
                                                        5560, 5591, 4099, 907, 1160, 4705, 4706, 4707, 4708, 4709, 4710, 4711, 4712, 4713, 4714,
                                                        4715, 4716
                                                    };
                                                byte[] counts = { 1, 1, 1, 1, 10, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
                                                int item = ServerManager.RandomNumber(0, 17);
                                                session.Character.GiftAdd(vnums[item], counts[item]);
                                            }
                                            else if (rnd < 30)
                                            {
                                                short[] vnums = { 361, 362, 363, 366, 367, 368, 371, 372, 373 };
                                                session.Character.GiftAdd(vnums[ServerManager.RandomNumber(0, 9)], 1);
                                            }
                                            else
                                            {
                                                short[] vnums =
                                                {
                                                        1161, 2282, 1030, 1244, 1218, 5369, 1012, 1363, 1364, 2160, 2173, 5959, 5983, 2514,
                                                        2515, 2516, 2517, 2518, 2519, 2520, 2521, 1685, 1686, 5087, 5203, 2418, 2310, 2303,
                                                        2169, 2280, 5892, 5893, 5894, 5895, 5896, 5897, 5898, 5899, 5332, 5105, 2161, 2162
                                                    };
                                                byte[] counts =
                                                {
                                                        10, 10, 20, 5, 1, 1, 99, 1, 1, 5, 5, 1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 1, 1, 1, 1, 5, 20,
                                                        20, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1
                                                    };
                                                int item = ServerManager.RandomNumber(0, 42);
                                                session.Character.GiftAdd(vnums[item], counts[item]);
                                            }
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                            break;
                                    }
                                }
                                break;
                            default:
                                if (int.TryParse(packetsplit[6], out packetType))
                                {
                                    if (session.Character.MapInstance.Map.MapTypes.Any(s => s.MapTypeId == (short)MapTypeEnum.Act4))
                                    {
                                        return;
                                    }

                                    if (!session.Account.VerifiedLock)
                                    {
                                        session.SendPacket(UserInterfaceHelper.GenerateMsg("You cant do this because your account is blocked. Use $Unlock", 0));
                                        return;
                                    }


                                    switch (packetType)
                                    {
                                        case 0:
                                            session.SendPacket(UserInterfaceHelper.GenerateDelay(5000, 7, $"#u_i^{type}^{secondaryType}^{inventoryType}^{slot}^1"));
                                            break;

                                        case 1:
                                            if (session.HasCurrentMapInstance && (session.Character.MapInstance == session.Character.Miniland  || session.CurrentMapInstance.MapInstanceType == MapInstanceType.BaseMapInstance) && (session.Character.LastVessel.AddSeconds(1) <= DateTime.Now || session.Character.StaticBonusList.Any(s => s.StaticBonusType == StaticBonusType.FastVessels)))
                                            {
                                                short[] vnums = { 1386, 1387, 1388, 1389, 1390, 1391, 1392, 1393, 1394, 1395, 1396, 1397, 1398, 1399, 1400, 1401, 1402, 1403, 1404, 1405 };
                                                short vnum = vnums[ServerManager.RandomNumber(0, 20)];

                                                NpcMonster npcmonster = ServerManager.GetNpcMonster(vnum);
                                                if (npcmonster == null)
                                                {
                                                    return;
                                                }
                                                MapMonster monster = new MapMonster
                                                {
                                                    MonsterVNum = vnum,
                                                    MapX = session.Character.PositionX,
                                                    MapY = session.Character.PositionY,
                                                    MapId = session.Character.MapInstance.Map.MapId,
                                                    Position = session.Character.Direction,
                                                    IsMoving = true,
                                                    MapMonsterId = session.CurrentMapInstance.GetNextMonsterId(),
                                                    ShouldRespawn = false
                                                };
                                                monster.Initialize(session.CurrentMapInstance);
                                                session.CurrentMapInstance.AddMonster(monster);
                                                session.CurrentMapInstance.Broadcast(monster.GenerateIn());
                                                session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                                session.Character.LastVessel = DateTime.Now;
                                            }
                                            break;
                                    }
                                }
                                break;
                        }
                    }
                    break;

                // Golden Bazaar Medal
                case 1003:
                    if (!session.Character.StaticBonusList.Any(s => s.StaticBonusType == StaticBonusType.BazaarMedalGold || s.StaticBonusType == StaticBonusType.BazaarMedalSilver))
                    {
                        session.Character.StaticBonusList.Add(new StaticBonusDTO
                        {
                            CharacterId = session.Character.CharacterId,
                            DateEnd = DateTime.Now.AddDays(EffectValue),
                            StaticBonusType = StaticBonusType.BazaarMedalGold
                        });
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("EFFECT_ACTIVATED"), Name), 12));
                    }
                    break;

                // Silver Bazaar Medal
                case 1004:
                    if (!session.Character.StaticBonusList.Any(s => s.StaticBonusType == StaticBonusType.BazaarMedalGold || s.StaticBonusType == StaticBonusType.BazaarMedalGold))
                    {
                        session.Character.StaticBonusList.Add(new StaticBonusDTO
                        {
                            CharacterId = session.Character.CharacterId,
                            DateEnd = DateTime.Now.AddDays(EffectValue),
                            StaticBonusType = StaticBonusType.BazaarMedalSilver
                        });
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("EFFECT_ACTIVATED"), Name), 12));
                    }
                    break;

                // Pet Slot Expansion
                case 1006:
                    if (Option == 0)
                    {
                        session.SendPacket($"qna #u_i^1^{session.Character.CharacterId}^{(byte)inv.Type}^{inv.Slot}^2 {Language.Instance.GetMessageFromKey("ASK_PET_MAX")}");
                    }
                    else if ((inv.Item?.IsSoldable == true && session.Character.MaxMateCount < 90) || session.Character.MaxMateCount < 30)
                    {
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        session.Character.MaxMateCount += 10;
                        session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("GET_PET_PLACES"), 10));
                        session.SendPacket(session.Character.GenerateScpStc());
                    }
                    break;

                // Permanent Backpack Expansion
                case 601:
                    if (session.Character.StaticBonusList.All(s => s.StaticBonusType != StaticBonusType.BackPack))
                    {
                        session.Character.StaticBonusList.Add(new StaticBonusDTO
                        {
                            CharacterId = session.Character.CharacterId,
                            DateEnd = DateTime.Now.AddYears(15),
                            StaticBonusType = StaticBonusType.BackPack
                        });
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        session.SendPacket(session.Character.GenerateExts());
                        session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("EFFECT_ACTIVATED"), Name), 12));
                    }
                    break;

                // Permanent Partner's Backpack
                case 602:
                    if (session.Character.StaticBonusList.All(s => s.StaticBonusType != StaticBonusType.PetBackPack))
                    {
                        session.Character.StaticBonusList.Add(new StaticBonusDTO
                        {
                            CharacterId = session.Character.CharacterId,
                            DateEnd = DateTime.Now.AddYears(15),
                            StaticBonusType = StaticBonusType.PetBackPack
                        });
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        session.SendPacket(session.Character.GenerateExts());
                        session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("EFFECT_ACTIVATED"), Name), 12));
                    }
                    break;

                // Permanent Pet Basket
                case 603:
                    if (session.Character.StaticBonusList.All(s => s.StaticBonusType != StaticBonusType.PetBasket))
                    {
                        session.Character.StaticBonusList.Add(new StaticBonusDTO
                        {
                            CharacterId = session.Character.CharacterId,
                            DateEnd = DateTime.Now.AddYears(15),
                            StaticBonusType = StaticBonusType.PetBasket
                        });
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        session.SendPacket(session.Character.GenerateExts());
                        session.SendPacket("ib 1278 1");
                        session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("EFFECT_ACTIVATED"), Name), 12));
                    }
                    break;

                // Pet Basket
                case 1007:
                    if (session.Character.StaticBonusList.All(s => s.StaticBonusType != StaticBonusType.PetBasket))
                    {
                        session.Character.StaticBonusList.Add(new StaticBonusDTO
                        {
                            CharacterId = session.Character.CharacterId,
                            DateEnd = DateTime.Now.AddDays(EffectValue),
                            StaticBonusType = StaticBonusType.PetBasket
                        });
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        session.SendPacket(session.Character.GenerateExts());
                        session.SendPacket("ib 1278 1");
                        session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("EFFECT_ACTIVATED"), Name), 12));
                    }
                    break;

                // Partner's Backpack
                case 1008:
                    if (session.Character.StaticBonusList.All(s => s.StaticBonusType != StaticBonusType.PetBackPack))
                    {
                        session.Character.StaticBonusList.Add(new StaticBonusDTO
                        {
                            CharacterId = session.Character.CharacterId,
                            DateEnd = DateTime.Now.AddDays(EffectValue),
                            StaticBonusType = StaticBonusType.PetBackPack
                        });
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        session.SendPacket(session.Character.GenerateExts());
                        session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("EFFECT_ACTIVATED"), Name), 12));
                    }
                    break;

                // Backpack Expansion
                case 1009:
                    if (session.Character.StaticBonusList.All(s => s.StaticBonusType != StaticBonusType.BackPack))
                    {
                        session.Character.StaticBonusList.Add(new StaticBonusDTO
                        {
                            CharacterId = session.Character.CharacterId,
                            DateEnd = DateTime.Now.AddDays(EffectValue),
                            StaticBonusType = StaticBonusType.BackPack
                        });
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        session.SendPacket(session.Character.GenerateExts());
                        session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("EFFECT_ACTIVATED"), Name), 12));
                    }
                    break;

                // Sealed Tarot Card
                case 1005:
                    session.Character.GiftAdd((short)(VNum - Effect), 1);
                    session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                    break;

                // Tarot Card Game
                case 1894:
                    if (EffectValue == 0)
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            session.Character.GiftAdd((short)(Effect + ServerManager.RandomNumber(0, 10)), 1);
                        }
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                    }
                    break;

                // Sealed Tarot Card
                case 2152:
                    session.Character.GiftAdd((short)(VNum + Effect), 1);
                    session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                    break;

                // Transformation scrolls
                case 1001:
                    if (session.Character.IsMorphed)
                    {
                        session.Character.IsMorphed = false;
                        session.Character.Morph = 0;
                        session.CurrentMapInstance?.Broadcast(session.Character.GenerateCMode());
                    }
                    else if (!session.Character.UseSp && !session.Character.IsVehicled)
                    {
                        if (Option == 0)
                        {
                            session.Character.LastDelay = DateTime.Now;
                            session.SendPacket(UserInterfaceHelper.GenerateDelay(3000, 3, $"#u_i^1^{session.Character.CharacterId}^{(byte)inv.Type}^{inv.Slot}^1"));
                        }
                        else
                        {
                            int[] possibleTransforms = null;

                            switch (EffectValue)
                            {
                                case 1: // Halloween
                                    possibleTransforms = new int[]
                                    {
                                    404, //Torturador pellizcador
                                    405, //Torturador enrollador
                                    406, //Torturador de acero
                                    446, //Guerrero yak
                                    447, //Mago yak
                                    441, //Guerrero de la muerte
                                    276, //Rey polvareda
                                    324, //Princesa Catrisha
                                    248, //Bruja oscura
                                    249, //Bruja de sangre
                                    438, //Bruja blanca fuerte
                                    236, //Guerrero esqueleto
                                    245, //Sombra nocturna
                                    439, //Guerrero esqueleto resucitado
                                    272, //Arquero calavera
                                    274, //Guerrero calavera
                                    2691, //Frankenstein
                                    };
                                    break;

                                case 2: // Ice Costume
                                    break;

                                case 3: // Bushtail Costume
                                    break;
                            }

                            if (possibleTransforms != null)
                            {
                                session.Character.IsMorphed = true;
                                session.Character.Morph = 1000 + possibleTransforms[ServerManager.RandomNumber(0, possibleTransforms.Length)];
                                session.CurrentMapInstance?.Broadcast(session.Character.GenerateCMode());
                                if (VNum != 1914)
                                {
                                    session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                }
                            }
                        }
                    }
                    break;

                default:
                    switch (EffectValue)
                    {
                        // Angel Base Flag
                        case 965:
                        // Demon Base Flag
                        case 966:
                            if (ServerManager.Instance.ChannelId == 51 && session.CurrentMapInstance?.Map.MapId != 130 && session.CurrentMapInstance?.Map.MapId != 131 && EffectValue - 964 == (short)session.Character.Faction)
                            {
                                session.CurrentMapInstance?.SummonMonster(new MonsterToSummon((short)EffectValue, new MapCell { X = session.Character.PositionX, Y = session.Character.PositionY }, null, false, isHostile: false, aliveTime: 1800));
                                session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                            }
                            break;

                        default:
                            switch (VNum)
                            {
                                //FairyExpe Potion
                                case 5370:
                                case 9116:
                                    if (!session.Character.Buff.ContainsKey(393))
                                    {
                                        session.Character.AddStaticBuff(new StaticBuffDTO { CardId = 393 });
                                        session.CurrentMapInstance?.Broadcast(session.Character.GeneratePairy());
                                        session.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("EFFECT_ACTIVATED"), inv.Item.Name), 0));
                                        session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, session.Character.CharacterId, 3014), session.Character.PositionX, session.Character.PositionY);
                                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                    }
                                    else
                                    {
                                        session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("ITEM_IN_USE"), 0));
                                    }
                                    break;

                                // -Mid Pack: 10$ = 800 coins
                                case 5638:
                                    {
                                        session.Character.GiftAdd(4200, 1);
                                        session.Character.GiftAdd(5323, 1);
                                        session.Character.GiftAdd(4181, 1);
                                        session.Character.GiftAdd(4185, 1);
                                        session.Character.GiftAdd(4808, 1);
                                        session.Character.GiftAdd(1286, 10);
                                        session.Character.GiftAdd(1296, 10);
                                        session.Character.GiftAdd(1249, 20);
                                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                        break;
                                    }

                                case 5722:
                                    {
                                        if(session.Character.Level < 40)
                                        {
                                            return;
                                        }
                                        switch(session.Character.Class)
                                        {
                                            case ClassType.Swordsman:
                                                {
                                                    session.Character.GiftAdd(262, 1, 6, 6);
                                                    session.Character.GiftAdd(291, 1, 6, 6);
                                                    session.Character.GiftAdd(297, 1, 6, 6);
                                                    session.Character.GiftAdd(141, 1, 6, 6);
                                                    session.Character.GiftAdd(292, 1, 6, 6);
                                                    session.Character.GiftAdd(298, 1, 6, 6);
                                                    session.Character.GiftAdd(1011, 99);
                                                    session.Character.GiftAdd(1904, 1);
                                                    session.Character.GiftAdd(1945, 2);
                                                    session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                                }
                                                break;

                                            case ClassType.Archer:
                                                {
                                                    session.Character.GiftAdd(265, 1, 6, 6);
                                                    session.Character.GiftAdd(289, 1, 6, 6);
                                                    session.Character.GiftAdd(295, 1, 6, 6);
                                                    session.Character.GiftAdd(148, 1, 6, 6);
                                                    session.Character.GiftAdd(290, 1, 6, 6);
                                                    session.Character.GiftAdd(296, 1, 6, 6);
                                                    session.Character.GiftAdd(1011, 99);
                                                    session.Character.GiftAdd(1904, 1);
                                                    session.Character.GiftAdd(1945, 2);
                                                    session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                                }
                                                break;

                                            case ClassType.Magician:
                                                {
                                                    session.Character.GiftAdd(268, 1, 6, 6);
                                                    session.Character.GiftAdd(293, 1, 6, 6);
                                                    session.Character.GiftAdd(271, 1, 6, 6);
                                                    session.Character.GiftAdd(155, 1, 6, 6);
                                                    session.Character.GiftAdd(294, 1, 6, 6);
                                                    session.Character.GiftAdd(272, 1, 6, 6);
                                                    session.Character.GiftAdd(1011, 99);
                                                    session.Character.GiftAdd(1904, 1);
                                                    session.Character.GiftAdd(1945, 2);
                                                    session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                                }
                                                break;

                                        }
                                    }
                                    break;

                                case 5723:
                                    {
                                        if (session.Character.Level < 70)
                                        {
                                            return;
                                        }
                                        switch (session.Character.Class)
                                        {
                                            case ClassType.Swordsman:
                                                {
                                                    session.Character.GiftAdd(400, 1, 6, 6);
                                                    session.Character.GiftAdd(401, 1, 6, 6);
                                                    session.Character.GiftAdd(402, 1, 6, 6);
                                                    session.Character.GiftAdd(409, 1, 6, 6);
                                                    session.Character.GiftAdd(9129, 20);
                                                    session.Character.GiftAdd(1904, 1);
                                                    session.Character.GiftAdd(1945, 2);
                                                    session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                                }
                                                break;

                                            case ClassType.Archer:
                                                {
                                                    session.Character.GiftAdd(403, 1, 6, 6);
                                                    session.Character.GiftAdd(404, 1, 6, 6);
                                                    session.Character.GiftAdd(405, 1, 6, 6);
                                                    session.Character.GiftAdd(4008, 1, 6, 6);
                                                    session.Character.GiftAdd(410, 1, 6, 6);
                                                    session.Character.GiftAdd(9129, 20);
                                                    session.Character.GiftAdd(1904, 1);
                                                    session.Character.GiftAdd(1945, 2);
                                                    session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                                }
                                                break;

                                            case ClassType.Magician:
                                                {
                                                    session.Character.GiftAdd(406, 1, 6, 6);
                                                    session.Character.GiftAdd(407, 1, 6, 6);
                                                    session.Character.GiftAdd(411, 1, 6, 6);
                                                    session.Character.GiftAdd(4010, 1, 6, 6);
                                                    session.Character.GiftAdd(9129, 20);
                                                    session.Character.GiftAdd(1904, 1);
                                                    session.Character.GiftAdd(1945, 2);
                                                    session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                                }
                                                break;

                                        }
                                    }
                                    break;

                                case 5724:
                                    {
                                        if (session.Character.Level < 80)
                                        {
                                            return;
                                        }
                                        switch (session.Character.Class)
                                        {
                                            case ClassType.Swordsman:
                                                {
                                                    session.Character.GiftAdd(349, 1, 6, 6);
                                                    session.Character.GiftAdd(352, 1, 6, 6);
                                                    session.Character.GiftAdd(9129, 30);
                                                    session.Character.GiftAdd(1904, 1);
                                                    session.Character.GiftAdd(1945, 2);
                                                    session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                                }
                                                break;

                                            case ClassType.Archer:
                                                {
                                                    session.Character.GiftAdd(4002, 1, 6, 6);
                                                    session.Character.GiftAdd(351, 1, 6, 6);
                                                    session.Character.GiftAdd(9129, 30);
                                                    session.Character.GiftAdd(1904, 1);
                                                    session.Character.GiftAdd(1945, 2);
                                                    session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                                }
                                                break;

                                            case ClassType.Magician:
                                                {
                                                    session.Character.GiftAdd(356, 1, 6, 6);
                                                    session.Character.GiftAdd(355, 1, 6, 6);
                                                    session.Character.GiftAdd(9129, 30);
                                                    session.Character.GiftAdd(1904, 1);
                                                    session.Character.GiftAdd(1945, 2);
                                                    session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                                }
                                                break;

                                        }
                                    }
                                    break;

                                // Big Pack: 15$= 1860 coins
                                case 5412:
                                    {
                                        session.Character.GiftAdd(5319, 1);
                                        session.Character.GiftAdd(4168, 1);
                                        session.Character.GiftAdd(8360, 1);
                                        session.Character.GiftAdd(8364, 1);
                                        session.Character.GiftAdd(4808, 1);
                                        session.Character.GiftAdd(1286, 20);
                                        session.Character.GiftAdd(1296, 20);
                                        session.Character.GiftAdd(1249, 30);
                                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                        break;
                                    }

                                case 5860:
                                    {
                                        session.Character.GiftAdd(1030, 2);
                                        session.Character.GiftAdd(2022, 2);
                                        session.Character.GiftAdd(2332, 2);
                                        session.Character.GiftAdd(1246, 2);
                                        session.Character.GiftAdd(1247, 2);
                                        session.Character.GiftAdd(1248, 2);
                                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                    }
                                    break;
                                case 5307:
                                    {
                                        session.Character.GiftAdd(9041, 1);
                                        session.Character.GiftAdd(9045, 1);
                                        session.Character.GiftAdd(9046, 1);
                                        session.Character.GiftAdd(9074, 2);
                                        session.Character.GiftAdd(9042, 99);
                                        session.Character.GiftAdd(9020, 10);
                                        session.Character.GiftAdd(9021, 10);
                                        session.Character.GiftAdd(9022, 10);
                                        session.Character.GiftAdd(9032, 1);
                                        session.Character.GiftAdd(9116, 2);
                                        session.Character.GiftAdd(1012, 99);
                                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                    }
                                    break;

                                case 5213:
                                    {
                                        session.Character.GiftAdd(9058, 1);
                                        session.Character.GiftAdd(4301, 1);
                                        session.Character.GiftAdd(4303, 1);
                                        session.Character.GiftAdd(8005, 1);
                                        session.Character.GiftAdd(8006, 1);
                                        session.Character.GiftAdd(8007, 1);
                                        session.Character.GiftAdd(8008, 1);
                                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                    }
                                    break;
                                case 1926: // Magic Scooter Box
                                    {
                                        session.Character.GiftAdd(1906, 1);
                                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                    }
                                    break;
                                case 1927: // Magic Carpet Box
                                    {
                                        session.Character.GiftAdd(1907, 1);
                                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                    }
                                    break;
                                case 5237: // Doni Darkslide Box
                                    {
                                        session.Character.GiftAdd(5236, 1);
                                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                    }
                                    break;
                                case 5572: // Illusionist Costume Set 
                                    {
                                        session.Character.GiftAdd(4258, 1);
                                        session.Character.GiftAdd(4260, 1);
                                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                    }
                                    break;
                                case 5118: // Flufly Mcfly Box
                                    {
                                        session.Character.GiftAdd(5117, 1);
                                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                    }
                                    break;
                                case 5153: // Horned Sweeper Box
                                    {
                                        session.Character.GiftAdd(5152, 1);
                                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                    }
                                    break;
                                case 5197: // Nossi Dragon Box
                                    {
                                        session.Character.GiftAdd(5196, 1);
                                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                    }
                                    break;
                                case 5320: // White Unicorn Box
                                    {
                                        session.Character.GiftAdd(5319, 1);
                                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                    }
                                    break;
                                case 5322: // Pink Unicorn Box
                                    {
                                        session.Character.GiftAdd(5321, 1);
                                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                    }
                                    break;
                                case 5324: // Black Unicorn Box
                                    {
                                        session.Character.GiftAdd(5323, 1);
                                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                    }
                                    break;
                                case 5018: // Wedding Box
                                    {
                                        session.Character.GiftAdd(1981, 1);
                                        session.Character.GiftAdd(982, 2);
                                        session.Character.GiftAdd(986, 2);
                                        session.Character.GiftAdd(1984, 10);
                                        session.Character.GiftAdd(1986, 10);
                                        session.Character.GiftAdd(1988, 10);
                                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                    }
                                    break;
                                case 1966: //Magic Tiger White Box
                                    {
                                        session.Character.GiftAdd(1965, 1);
                                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                    }
                                    break;
                                case 5856: // Partner Slot Expansion
                                case 9113: // Partner Slot Expansion (Limited)
                                    {
                                        if (Option == 0)
                                        {
                                            session.SendPacket($"qna #u_i^1^{session.Character.CharacterId}^{(byte)inv.Type}^{inv.Slot}^2 {Language.Instance.GetMessageFromKey("ASK_PARTNER_MAX")}");
                                        }
                                        else if (session.Character.MaxPartnerCount < 12)
                                        {
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                            session.Character.MaxPartnerCount++;
                                            session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("GET_PARTNER_PLACES"), 10));
                                            session.SendPacket(session.Character.GenerateScpStc());
                                        }
                                    }
                                    break;

                                case 5931: // Tique de habilidad de compañero (una)
                                    {
                                        if (session?.Character?.Mates == null)
                                        {
                                            return;
                                        }

                                        if (packetsplit.Length != 10 || !byte.TryParse(packetsplit[8], out byte petId) || !byte.TryParse(packetsplit[9], out byte castId))
                                        {
                                            return;
                                        }

                                        if (castId < 0 || castId > 2)
                                        {
                                            return;
                                        }

                                        Mate partner = session.Character.Mates.ToList().FirstOrDefault(s => s.IsTeamMember && s.MateType == MateType.Partner && s.PetId == petId);

                                        if (partner?.Sp == null || partner.IsUsingSp)
                                        {
                                            return;
                                        }

                                        PartnerSkill skill = partner.Sp.GetSkill(castId);

                                        if (skill?.Skill == null)
                                        {
                                            return;
                                        }

                                        if (skill.Level == (byte)PartnerSkillLevelType.S)
                                        {
                                            return;
                                        }

                                        if (partner.Sp.RemoveSkill(castId))
                                        {
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);

                                            partner.Sp.ReloadSkills();
                                            partner.Sp.FullXp();

                                            session.SendPacket(UserInterfaceHelper.GenerateModal(Language.Instance.GetMessageFromKey("PSP_SKILL_RESETTED"), 1));
                                        }

                                        session.SendPacket(partner.GenerateScPacket());
                                    }
                                    break;
                                case 5932: // Tique de habilidad de compañero (todas)
                                    {
                                        if (packetsplit.Length != 10
                                            || session?.Character?.Mates == null)
                                        {
                                            return;
                                        }

                                        if (!byte.TryParse(packetsplit[8], out byte petId) || !byte.TryParse(packetsplit[9], out byte castId))
                                        {
                                            return;
                                        }

                                        if (castId < 0 || castId > 2)
                                        {
                                            return;
                                        }

                                        Mate partner = session.Character.Mates.ToList().FirstOrDefault(s => s.IsTeamMember && s.MateType == MateType.Partner && s.PetId == petId);

                                        if (partner?.Sp == null || partner.IsUsingSp)
                                        {
                                            return;
                                        }

                                        if (partner.Sp.GetSkillsCount() < 1)
                                        {
                                            return;
                                        }

                                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);

                                        partner.Sp.ClearSkills();
                                        partner.Sp.FullXp();

                                        session.SendPacket(UserInterfaceHelper.GenerateModal(Language.Instance.GetMessageFromKey("PSP_ALL_SKILLS_RESETTED"), 1));

                                        session.SendPacket(partner.GenerateScPacket());
                                    }
                                    break;

                                // Event Upgrade Scrolls
                                case 5107:
                                case 5207:
                                case 5519:
                                    if (EffectValue != 0)
                                    {
                                        if (session.Character.IsSitting)
                                        {
                                            session.Character.IsSitting = false;
                                            session.SendPacket(session.Character.GenerateRest());
                                        }
                                        session.SendPacket(UserInterfaceHelper.GenerateGuri(12, 1, session.Character.CharacterId, EffectValue));
                                    }
                                    break;

                                // Martial Artist Starter Pack
                                case 5832:
                                    {
                                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);

                                        // Steel Fist
                                        session.Character.GiftAdd(4756, 1, 5);

                                        // Trainee Martial Artist's Uniform
                                        session.Character.GiftAdd(4757, 1, 5);

                                        // Mystical Glacier Stone
                                        session.Character.GiftAdd(4504, 1);

                                        // Hero's Amulet of Fire
                                        session.Character.GiftAdd(4503, 1);

                                        // Fairy Fire/Water/Light/Dark (30%)
                                        for (short itemVNum = 884; itemVNum <= 887; itemVNum++)
                                        {
                                            session.Character.GiftAdd(itemVNum, 1);
                                        }
                                    }
                                    break;

                                // Soulstone Blessing
                                case 1362:
                                case 5195:
                                case 5211:
                                case 9075:
                                    if (!session.Character.Buff.ContainsKey(146))
                                    {
                                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                        session.Character.AddStaticBuff(new StaticBuffDTO { CardId = 146 });
                                    }
                                    else
                                    {
                                        session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("ITEM_IN_USE"), 0));
                                    }
                                    break;
                                case 1428:
                                    session.SendPacket("guri 18 1");
                                    break;
                                case 1429:
                                    session.SendPacket("guri 18 0");
                                    break;
                                case 1904:
                                    short[] items = { 1894, 1895, 1896, 1897, 1898, 1899, 1900, 1901, 1902, 1903 };
                                    for (int i = 0; i < 5; i++)
                                    {
                                        session.Character.GiftAdd(items[ServerManager.RandomNumber(0, items.Length)], 1);
                                    }
                                    session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                    break;
                                case 5841:
                                    int rnd = ServerManager.RandomNumber(0, 1000);
                                    short[] vnums = null;
                                    if (rnd < 900)
                                    {
                                        vnums = new short[] { 4356, 4357, 4358, 4359 };
                                    }
                                    else
                                    {
                                        vnums = new short[] { 4360, 4361, 4362, 4363 };
                                    }
                                    session.Character.GiftAdd(vnums[ServerManager.RandomNumber(0, 4)], 1);
                                    session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                    break;
                                case 5916:
                                case 5927:
                                    session.Character.AddStaticBuff(new StaticBuffDTO
                                    {
                                        CardId = 340,
                                        CharacterId = session.Character.CharacterId,
                                        RemainingTime = 7200
                                    });
                                    session.Character.RemoveBuff(339);
                                    session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                    break;
                                case 5929:
                                case 5930:
                                    session.Character.AddStaticBuff(new StaticBuffDTO
                                    {
                                        CardId = 340,
                                        CharacterId = session.Character.CharacterId,
                                        RemainingTime = 600
                                    });
                                    session.Character.RemoveBuff(339);
                                    session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                    break;

                                    // Mother Nature's Rune Pack (limited)
                                case 9117:
                                    rnd = ServerManager.RandomNumber(0, 1000);
                                    vnums = null;
                                    if (rnd < 900)
                                    {
                                        vnums = new short[] { 8312, 8313, 8314, 8315 };
                                    }
                                    else
                                    {
                                        vnums = new short[] { 8316, 8317, 8318, 8319 };
                                    }
                                    session.Character.GiftAdd(vnums[ServerManager.RandomNumber(0, 4)], 1);
                                    session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                    break;

                                default:
                                    IEnumerable<RollGeneratedItemDTO> roll = DAOFactory.RollGeneratedItemDAO.LoadByItemVNum(VNum);
                                    IEnumerable<RollGeneratedItemDTO> rollGeneratedItemDtos = roll as IList<RollGeneratedItemDTO> ?? roll.ToList();
                                    if (!rollGeneratedItemDtos.Any())
                                    {
                                        Logger.Warn(string.Format(Language.Instance.GetMessageFromKey("NO_HANDLER_ITEM"), GetType(), VNum, Effect, EffectValue));
                                        return;
                                    }
                                    int probabilities = rollGeneratedItemDtos.Where(s => s.Probability != 10000).Sum(s => s.Probability);
                                    int rnd2 = ServerManager.RandomNumber(0, probabilities);
                                    int currentrnd = 0;
                                    foreach (RollGeneratedItemDTO rollitem in rollGeneratedItemDtos.Where(s => s.Probability == 10000))
                                    {
                                        sbyte rare = 0;
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
                                        session.Character.GiftAdd(rollitem.ItemGeneratedVNum, rollitem.ItemGeneratedAmount, (byte)rare, design: rollitem.ItemGeneratedDesign);
                                    }
                                    foreach (RollGeneratedItemDTO rollitem in rollGeneratedItemDtos.Where(s => s.Probability != 10000).OrderBy(s => ServerManager.RandomNumber()))
                                    {
                                        sbyte rare = 0;
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

                                        currentrnd += rollitem.Probability;
                                        if (currentrnd < rnd2)
                                        {
                                            continue;
                                        }
                                        /*if (rollitem.IsSuperReward)
                                        {
                                            CommunicationServiceClient.Instance.SendMessageToCharacter(new SCSCharacterMessage
                                            {
                                                DestinationCharacterId = null,
                                                SourceCharacterId = session.Character.CharacterId,
                                                SourceWorldId = ServerManager.Instance.WorldId,
                                                Message = Language.Instance.GetMessageFromKey("SUPER_REWARD"),
                                                Type = MessageType.Shout
                                            });
                                        }*/
                                        session.Character.GiftAdd(rollitem.ItemGeneratedVNum, rollitem.ItemGeneratedAmount, (byte)rare, design: rollitem.ItemGeneratedDesign);//, rollitem.ItemGeneratedUpgrade);
                                        break;
                                    }
                                    session.Character.Inventory.RemoveItemAmount(VNum);
                                    break;
									
								
                            }
                            break;
                    }
                    break;
            }
            session.Character.IncrementQuests(QuestType.Use, inv.ItemVNum);
        }

        #endregion
    }
}