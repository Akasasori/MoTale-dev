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
using OpenNos.GameObject.Networking;
using OpenNos.Master.Library.Client;
using OpenNos.Master.Library.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace OpenNos.GameObject
{
    public static class NRunHandler
    {
        #region Methods

        public static void NRun(ClientSession Session, NRunPacket packet)
        {
            if (!Session.HasCurrentMapInstance)
            {
                return;
            }

            MapNpc npc = Session.CurrentMapInstance.Npcs.Find(s => s.MapNpcId == packet.NpcId);

            TeleporterDTO tp;

            var rand = new Random();
            switch (packet.Runner)
            {
                case 308:
                    if(npc.NpcVNum == 1093)
                    {
                        if(npc.Name.Contains("Dalore"))
                        {
                            if (packet.Type >= 0 && Session.Character.Gold >= 500000 * (1 + packet.Type))
                            {
                                Session.Character.Gold -= 500000 * (1 + packet.Type);
                                Session.SendPacket(Session.Character.GenerateGold());
                                int Random = ServerManager.RandomNumber(0, 100);
                                int rnd = ServerManager.RandomNumber(0, 1000);
                                if (rnd < 970)
                                {                                   
                                    if (Random < 80)
                                    {
                                        short[] vnums =
{
                                            1246, 1247, 1248, 1249, 1244, 2332
                                        };
                                        byte[] counts =
                                        {
                                            5, 5, 5, 2, 10, 2
                                        };
                                        int item = ServerManager.RandomNumber(0, 5);
                                        Session.Character.GiftAdd(vnums[item], counts[item]);
                                    }
                                    else
                                    {
                                        short[] vnums =
{
                                            1366, 1904, 5061, 1244, 1218
                                        };
                                        byte[] counts =
                                        {
                                            1, 1, 1, 50, 3
                                        };
                                        int item = ServerManager.RandomNumber(0, 4);
                                        Session.Character.GiftAdd(vnums[item], counts[item]);
                                    }

                                }
                                else
                                {

                                    short[] vnums =
{
                                            4126, 5432, 5431
                                        };
                                    byte[] counts =
                                    {
                                            1, 1, 1
                                    };
                                    int item = ServerManager.RandomNumber(0, 2);
                                    Session.Character.GiftAdd(vnums[item], counts[item]);
                                    CommunicationServiceClient.Instance.SendMessageToCharacter(new SCSCharacterMessage
                                    {
                                        DestinationCharacterId = null,
                                        SourceCharacterId = 0,
                                        SourceWorldId = ServerManager.Instance.WorldId,
                                        Message = $"Player {Session.Character.Name} Obtain Big Reward!",
                                        Type = MessageType.Shout
                                    });
                                }
                            }
                            else
                            {
                                Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NOT_ENOUGH_MONEY"), 10));
                            }
                            
                        }
                    }
                    if (npc.NpcVNum == 648)
                    {
                        if (npc.Name.Contains("Zitrux"))
                        {

                            if (packet.Type == 0)
                            {
                                if (Session.Character.Level > 16)
                                {
                                    Session.SendPacket(UserInterfaceHelper.GenerateInfo("You've already choose your pet. Change map!"));
                                    return;

                                }
                                Session.Character.Level++;
                                Session.Character.AddZitrux(Session);
                            }
                        }
                    }

                    if (npc.NpcVNum == 645)
                    {
                        if (npc.Name.Contains("Darti"))
                        {

                            if (packet.Type == 0)
                            {
                                if (Session.Character.Level > 16)
                                {
                                    Session.SendPacket(UserInterfaceHelper.GenerateInfo("You've already choose your pet. Change map!"));
                                    return;

                                }
                                Session.Character.Level++;
                                Session.Character.AddDarti(Session);
                            }
                        }
                    }

                    if (npc.NpcVNum == 651)
                    {
                        if (npc.Name.Contains("Tisked"))
                        {

                            if (packet.Type == 0)
                            {
                                if (Session.Character.Level > 16)
                                {
                                    Session.SendPacket(UserInterfaceHelper.GenerateInfo("You've already choose your pet. Change map!"));
                                    return;

                                }
                                Session.Character.Level++;
                                Session.Character.AddTisked(Session);
                            }
                        }
                    }

                    if (npc.NpcVNum == 660)
                    {
                        if (npc.Name.Contains("Strain"))
                        {

                            if (packet.Type == 0)
                            {
                                if (Session.Character.Level > 16)
                                {
                                    Session.SendPacket(UserInterfaceHelper.GenerateInfo("You've already choose your pet. Change map!"));
                                    return;

                                }
                                Session.Character.Level++;
                                Session.Character.AddStrain(Session);
                            }
                        }
                    }

                    if (npc.NpcVNum == 657)
                    {
                        if (npc.Name.Contains("Soki"))
                        {

                            if (packet.Type == 0)
                            {
                                if (Session.Character.Level > 16)
                                {
                                    Session.SendPacket(UserInterfaceHelper.GenerateInfo("You've already choose your pet. Change map!"));
                                    return;

                                }
                                Session.Character.Level++;
                                Session.Character.AddSoki(Session);
                            }
                        }
                    }

                    if (npc.NpcVNum == 654)
                    {
                        if (npc.Name.Contains("Bors"))
                        {

                            if (packet.Type == 0)
                            {
                                if (Session.Character.Level > 16)
                                {
                                    Session.SendPacket(UserInterfaceHelper.GenerateInfo("You've already choose your pet. Change map!"));
                                    return;

                                }
                                Session.Character.Level++;
                                Session.Character.AddBors(Session);
                            }
                        }
                    }
                    break;

                case 3000:
                    switch (packet.Type)
                    {
                        case 0:
                            switch (packet.Value)
                            {
                                case 2:
                                    break;
                            }
                            break;
                    }
                    break;
                case 1:


                    if (Session.Character.Class != (byte)ClassType.Adventurer)
                    {
                        Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ADVENTURER"), 0));
                        return;
                    }
                    if (Session.Character.Level < 15 || Session.Character.JobLevel < 20)
                    {
                        Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("LOW_LVL"), 0));
                        return;
                    }
                    if (packet.Type > 3 || packet.Type < 1)
                    {
                        return;
                    }
                    if (packet.Type == (byte)Session.Character.Class)
                    {
                        return;
                    }
                    if (Session.Character.Inventory.All(i => i.Type != InventoryType.Wear))
                    {
                        Session.Character.Inventory.AddNewToInventory((short)(4 + (packet.Type * 14)), 1, InventoryType.Wear);
                        Session.Character.Inventory.AddNewToInventory((short)(81 + (packet.Type * 13)), 1, InventoryType.Wear);

                        switch (packet.Type)
                        {
                            case 1:
                                Session.Character.Inventory.AddNewToInventory(68, 1, InventoryType.Wear);

                                break;
                            case 2:

                                Session.Character.Inventory.AddNewToInventory(78, 1, InventoryType.Wear);
                                break;

                            case 3:

                                Session.Character.Inventory.AddNewToInventory(86, 1, InventoryType.Wear);
                                break;
                        }

                        Session.CurrentMapInstance?.Broadcast(Session.Character.GenerateEq());
                        Session.SendPacket(Session.Character.GenerateEquipment());
                        Session.Character.ChangeClass((ClassType)packet.Type, false);
                      //  string message = $"<Administrateur> {Session.Character.Name} is changed into {Session.Character.Class}";
                     //   Session.SendPacket(Session.Character.GenerateSay(message, 10));
                        Session.Character.JobLevel = 20;
                        switch (Session.Character.Class)
                        {
                            case ClassType.Swordsman:
                                //Session.Character.GiftAdd(901, 1, upgrade: 5);
                                //   ServerManager.Instance.ChangeMap(Session.Character.CharacterId, 10000, 2, 6);
                                //   Session.Character.GiftAdd(1440, 1);
                                // Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("SPAWN"), 0));
                                Session.Character.AddQuest(6510);
                                break;
                            case ClassType.Archer:
                                //    Session.Character.GiftAdd(903, 1, upgrade: 5);
                                //  ServerManager.Instance.ChangeMap(Session.Character.CharacterId, 10000, 2, 6);
                                //  Session.Character.GiftAdd(1440, 1);
                                //  Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("SPAWN"), 0));
                                Session.Character.AddQuest(6510);
                                break;
                            case ClassType.Magician:
                                //   Session.Character.GiftAdd(905, 1, upgrade: 5);
                                //   ServerManager.Instance.ChangeMap(Session.Character.CharacterId, 10000, 2, 6);
                                // Session.Character.GiftAdd(1440, 1);
                                //  Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("SPAWN"), 0));
                                Session.Character.AddQuest(6510);
                                break;
                        }
              
                        if (Session.Character.Class == ClassType.MartialArtist)
                        {
                            Session.Character.GiftAdd(1011, 99);
                            Session.Character.GiftAdd(5798, 1);
                            Session.Character.Level = 80;
                          //  ServerManager.Instance.ChangeMap(Session.Character.CharacterId, 10000, 2, 6);
                            Session.Character.GiftAdd(1440, 1);
                         //   Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("SPAWN"), 0));

                        }

                    }
                    else
                    {
                        Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("EQ_NOT_EMPTY"), 0));
                    }
                    break;

                case 2:
                    Session.SendPacket("wopen 1 0");
                    break;

                case 3:
                    NpcMonster heldMonster = ServerManager.GetNpcMonster((short)packet.Type);
                    if (heldMonster != null && !Session.Character.Mates.Any(m => m.NpcMonsterVNum == heldMonster.NpcMonsterVNum && !m.IsTemporalMate) && Session.Character.Mates.FirstOrDefault(s => s.NpcMonsterVNum == heldMonster.NpcMonsterVNum && s.IsTemporalMate && s.IsTsReward) is Mate partnerToReceive)
                    {
                        Session.Character.RemoveTemporalMates();
                        Mate partner = new Mate(Session.Character, heldMonster, heldMonster.Level, MateType.Partner);
                        partner.Experience = partnerToReceive.Experience;
                        if (!Session.Character.Mates.Any(s => s.MateType == MateType.Partner && s.IsTeamMember))
                        {
                            partner.IsTeamMember = true;
                        }
                        Session.Character.AddPet(partner);
                    }
                    break;

                case 4:
                    Mate mate = Session.Character.Mates.Find(s => s.MateTransportId == packet.NpcId);
                    switch (packet.Type)
                    {
                        case 2:
                            if (mate != null)
                            {
                                if (Session.Character.Miniland == Session.Character.MapInstance || Session.Character.MapId==54)
                                {
                                    if (Session.Character.Level >= mate.Level)
                                    {
                                        Mate teammate = Session.Character.Mates.Where(s => s.IsTeamMember).FirstOrDefault(s => s.MateType == mate.MateType);
                                        if (teammate != null)
                                        {
                                            teammate.RemoveTeamMember();
                                        }
                                        mate.AddTeamMember();
                                    }
                                    else
                                    {
                                        Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("PET_HIGHER_LEVEL"), 0));
                                    }
                                }
                            }
                            break;

                        case 3:
                            if (mate != null && Session.Character.Miniland == Session.Character.MapInstance)
                            {
                               
                                mate.RemoveTeamMember();
                            }
                            break;

                        case 4:
                            if (mate != null)
                            {
                                if (Session.Character.Miniland == Session.Character.MapInstance)
                                {
                                    mate.RemoveTeamMember(false);
                                    mate.MapX = mate.PositionX;
                                    mate.MapY = mate.PositionY;
                                }
                                else
                                {
                                    Session.SendPacket($"qna #n_run^4^5^3^{mate.MateTransportId} {Language.Instance.GetMessageFromKey("ASK_KICK_PET")}");
                                }
                                break;
                            }
                            break;

                        case 5:
                            if (mate != null)
                            {
                                Session.SendPacket(UserInterfaceHelper.GenerateDelay(3000, 10, $"#n_run^4^6^3^{mate.MateTransportId}"));
                            }
                            break;

                        case 6:
                            if (mate != null && Session.Character.Miniland != Session.Character.MapInstance)
                            {
                                mate.BackToMiniland();
                            }
                            break;

                        case 7:
                            if (mate != null)
                            {
                                if (Session.Character.Mates.Any(s => s.MateType == mate.MateType && s.IsTeamMember))
                                {
                                    Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("ALREADY_PET_IN_TEAM"), 11));
                                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("ALREADY_PET_IN_TEAM"), 0));
                                }
                                else
                                {
                                    mate.RemoveTeamMember();
                                    Session.SendPacket(UserInterfaceHelper.GenerateDelay(3000, 10, $"#n_run^4^9^3^{mate.MateTransportId}"));
                                }
                            }
                            break;

                        case 9:
                            if (mate != null && mate.IsSummonable && Session.Character.MapInstance.MapInstanceType != MapInstanceType.TalentArenaMapInstance)
                            {
                                if (Session.Character.Level >= mate.Level)
                                {
                                    mate.PositionX = (short)(Session.Character.PositionX + (mate.MateType == MateType.Partner ? -1 : 1));
                                    mate.PositionY = (short)(Session.Character.PositionY + 1);
                                    mate.AddTeamMember();
                                    Parallel.ForEach(Session.CurrentMapInstance.Sessions.Where(s => s.Character != null), s =>
                                    {
                                        if (ServerManager.Instance.ChannelId != 51 || Session.Character.Faction == s.Character.Faction)
                                        {
                                            s.SendPacket(mate.GenerateIn(false, ServerManager.Instance.ChannelId == 51));
                                        }
                                        else
                                        {
                                            s.SendPacket(mate.GenerateIn(true, ServerManager.Instance.ChannelId == 51, s.Account.Authority));
                                        }
                                    });
                                }
                                else
                                {
                                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("PET_HIGHER_LEVEL"), 0));
                                }
                            }
                            break;
                    }
                    Session.SendPacket(Session.Character.GeneratePinit());
                    Session.SendPackets(Session.Character.GeneratePst());
                    break;

                case 10:
                    Session.SendPacket("wopen 3 0");
                    break;

                case 12:
                    Session.SendPacket($"wopen {packet.Type} 0");
                    break;

                case 14:
                    Session.SendPacket("wopen 27 0");
                    string recipelist = "m_list 2";
                    if (npc != null)
                    {
                        List<Recipe> tps = npc.Recipes;
                        recipelist = tps.Where(s => s.Amount > 0).Aggregate(recipelist, (current, s) => current + $" {s.ItemVNum}");
                        recipelist += " -100";
                        Session.SendPacket(recipelist);
                    }
                    break;

                case 15:
                    if (npc != null)
                    {
                        if (packet.Value == 2)
                        {
                            Session.SendPacket($"qna #n_run^15^1^1^{npc.MapNpcId} {Language.Instance.GetMessageFromKey("ASK_CHANGE_SPAWNLOCATION")}");
                        }
                        else
                        {
                            switch (npc.MapId)
                            {
                                case 1:
                                    Session.Character.SetRespawnPoint(1, 79, 116);
                                    break;

                                case 20:
                                    Session.Character.SetRespawnPoint(20, 9, 92);
                                    break;

                                case 145:
                                    Session.Character.SetRespawnPoint(145, 13, 110);
                                    break;

                                case 170:
                                    Session.Character.SetRespawnPoint(170, 79, 47);
                                    break;

                                case 177:
                                    Session.Character.SetRespawnPoint(177, 149, 74);
                                    break;

                                case 189:
                                    Session.Character.SetRespawnPoint(189, 58, 166);
                                    break;
                            }
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("RESPAWNLOCATION_CHANGED"), 0));
                        }
                    }
                    break;

                case 16:
                    tp = npc?.Teleporters?.FirstOrDefault(s => s.Index == packet.Type);
                    if (tp != null)
                    {
                        if (packet.Type >= 0 && Session.Character.Gold >= 1000 * packet.Type)
                        {
                            Session.Character.Gold -= 1000 * packet.Type;
                            Session.SendPacket(Session.Character.GenerateGold());
                            ServerManager.Instance.ChangeMap(Session.Character.CharacterId, tp.MapId, tp.MapX, tp.MapY);
                        }
                        else
                        {
                            Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NOT_ENOUGH_MONEY"), 10));
                        }
                    }
                    break;

                case 17:
                    if (Session.Character.MapInstance.MapInstanceType != MapInstanceType.BaseMapInstance)
                    {
                        return;
                    }
                    if (packet.Value == 1)
                    {
                        Session.SendPacket($"qna #n_run^{packet.Runner}^{packet.Type}^2^{packet.NpcId} {string.Format(Language.Instance.GetMessageFromKey("ASK_ENETER_GOLD"), 500 * (1 + packet.Type))}");
                    }
                    else
                    {
                        double currentRunningSeconds = (DateTime.Now - Process.GetCurrentProcess().StartTime.AddSeconds(-50)).TotalSeconds;
                        double timeSpanSinceLastPortal = currentRunningSeconds - Session.Character.LastPortal;
                        if (!(timeSpanSinceLastPortal >= 4) || !Session.HasCurrentMapInstance || ServerManager.Instance.ChannelId == 51 || Session.CurrentMapInstance.MapInstanceId == ServerManager.Instance.ArenaInstance.MapInstanceId || Session.CurrentMapInstance.MapInstanceId == ServerManager.Instance.FamilyArenaInstance.MapInstanceId)
                        {
                            Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("CANT_MOVE"), 10));
                            return;
                        }
                        if (packet.Type >= 0 && Session.Character.Gold >= 500 * (1 + packet.Type))
                        {
                            Session.Character.LastPortal = currentRunningSeconds;
                            Session.Character.Gold -= 500 * (1 + packet.Type);
                            Session.SendPacket(Session.Character.GenerateGold());
                            MapCell pos = packet.Type == 0 ? ServerManager.Instance.ArenaInstance.Map.GetRandomPosition() : ServerManager.Instance.FamilyArenaInstance.Map.GetRandomPosition();
                            ServerManager.Instance.ChangeMapInstance(Session.Character.CharacterId, packet.Type == 0 ? ServerManager.Instance.ArenaInstance.MapInstanceId : ServerManager.Instance.FamilyArenaInstance.MapInstanceId, 17 , 32);
                        }
                        else
                        {
                            Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NOT_ENOUGH_MONEY"), 10));
                        }
                    }
                    break;

                case 18:
                    if (Session.Character.MapInstance.MapInstanceType != MapInstanceType.BaseMapInstance)
                    {
                        return;
                    }
                    Session.SendPacket(Session.Character.GenerateNpcDialog(17));
                    break;

                case 26:
                    tp = npc?.Teleporters?.FirstOrDefault(s => s.Index == packet.Type);
                    if (tp != null)
                    {
                        if (Session.Character.Gold >= 5000 * packet.Type)
                        {
                            Session.Character.Gold -= 5000 * packet.Type;
                            ServerManager.Instance.ChangeMap(Session.Character.CharacterId, tp.MapId, tp.MapX, tp.MapY);
                        }
                        else
                        {
                            Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NOT_ENOUGH_MONEY"), 10));
                        }
                    }
                    break;

                case 45:
                    tp = npc?.Teleporters?.FirstOrDefault(s => s.Index == packet.Type);
                    if (tp != null)
                    {
                        if (Session.Character.Gold >= 500)
                        {
                            Session.Character.Gold -= 500;
                            Session.SendPacket(Session.Character.GenerateGold());
                            ServerManager.Instance.ChangeMap(Session.Character.CharacterId, tp.MapId, tp.MapX, tp.MapY);
                        }
                        else
                        {
                            Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NOT_ENOUGH_MONEY"), 10));
                        }
                    }
                    break;

                case 61:
                    if (npc == null)
                    {
                        return;
                    }
                    if (packet.Type == 0)
                    {
                        Session.SendPacket($"qna #n_run^{packet.Runner}^56^{packet.Value}^{packet.NpcId} {Language.Instance.GetMessageFromKey("ASK_TRADE")}");
                    }
                    else
                    {
                        if (Session.Character.Inventory.CountItem(5917) < 1 || Session.Character.Inventory.CountItem(5918) < 1)
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_INGREDIENTS"), 0));
                            return;
                        }
                        Session.Character.GiftAdd(5922, 1);
                        Session.Character.Inventory.RemoveItemAmount(5917);
                        Session.Character.Inventory.RemoveItemAmount(5918);
                    }
                    break;

                case 62:
                    if (npc == null)
                    {
                        return;
                    }
                    if (packet.Type == 0)
                    {
                        Session.SendPacket($"qna #n_run^{packet.Runner}^56^{packet.Value}^{packet.NpcId} {Language.Instance.GetMessageFromKey("ASK_TRADE")}");
                    }
                    else
                    {
                        if (Session.Character.Inventory.CountItem(5919) < 1)
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_INGREDIENTS"), 0));
                            return;
                        }
                        ServerManager.Instance.ChangeMap(Session.Character.CharacterId, 2536, 26, 31);
                        Session.Character.Inventory.RemoveItemAmount(5919);
                    }
                    break;

                case 65:
                    {
                        if (npc != null)
                        {
                            Session.Character.AddQuest(5514);
                        }
                    }
                    break;

                case 66:
                    {
                        if (npc != null)
                        {
                            Session.Character.AddQuest(5914);
                        }
                    }
                    break;

                case 67:
                    {
                        if (npc != null)
                        {
                            Session.Character.AddQuest(5908);
                        }
                    }
                    break;

                case 68:
                    {
                        if (npc != null)
                        {
                            Session.Character.AddQuest(5919);
                        }
                    }
                    break;

                case 69:
                    if (npc == null)
                    {
                        return;
                    }
                    if (packet.Type == 0)
                    {
                        Session.SendPacket($"qna #n_run^{packet.Runner}^56^{packet.Value}^{packet.NpcId} {Language.Instance.GetMessageFromKey("ASK_TRADE")}");
                    }
                    else
                    {
                        if (Session.Character.Inventory.CountItem(5910) < 5)
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_INGREDIENTS"), 0));
                            return;
                        }
                        Session.Character.GiftAdd(5929, 10);
                        Session.Character.Inventory.RemoveItemAmount(5910, 5);
                    }
                    break;

                case 70:
                    if (npc == null)
                    {
                        return;
                    }
                    if (packet.Type == 0)
                    {
                        Session.SendPacket($"qna #n_run^{packet.Runner}^56^{packet.Value}^{packet.NpcId} {Language.Instance.GetMessageFromKey("ASK_TRADE")}");
                    }
                    else
                    {
                        if (Session.Character.Inventory.CountItem(5910) < 90)
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_INGREDIENTS"), 0));
                            return;
                        }
                        Session.Character.GiftAdd(5923, 1);
                        Session.Character.Inventory.RemoveItemAmount(5910, 90);
                    }
                    break;

                case 71:
                    if (npc == null)
                    {
                        return;
                    }
                    if (packet.Type == 0)
                    {
                        Session.SendPacket($"qna #n_run^{packet.Runner}^56^{packet.Value}^{packet.NpcId} {Language.Instance.GetMessageFromKey("ASK_TRADE")}");
                    }
                    else
                    {
                        if (Session.Character.Inventory.CountItem(5910) < 300)
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_INGREDIENTS"), 0));
                            return;
                        }
                        Session.Character.GiftAdd(5914, 1);
                        Session.Character.Inventory.RemoveItemAmount(5910, 300);
                    }
                    break;

                case 72: // Exchange 10 Yellow Pumpkin Sweets (2322) for a Halloween Costume Scroll (1915)
                    if (npc == null || !ServerManager.Instance.Configuration.HalloweenEvent)
                    {
                        return;
                    }
                    if (packet.Type == 0)
                    {
                        Session.SendPacket($"qna #n_run^{packet.Runner}^56^{packet.Value}^{packet.NpcId} {Language.Instance.GetMessageFromKey("ASK_TRADE")}");
                    }
                    else
                    {
                        if (Session.Character.Inventory.CountItem(2322) < 10)
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_INGREDIENTS"), 0));
                            return;
                        }
                        Session.Character.GiftAdd(1915, 1);
                        Session.Character.Inventory.RemoveItemAmount(2322, 10);
                    }
                    break;

                case 73: // Exchange 10 Black Pumpkin Sweets (2324) for a Halloween Costume Scroll (1915)
                    if (npc == null || !ServerManager.Instance.Configuration.HalloweenEvent)
                    {
                        return;
                    }
                    if (packet.Type == 0)
                    {
                        Session.SendPacket($"qna #n_run^{packet.Runner}^56^{packet.Value}^{packet.NpcId} {Language.Instance.GetMessageFromKey("ASK_TRADE")}");
                    }
                    else
                    {
                        if (Session.Character.Inventory.CountItem(2324) < 10)
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_INGREDIENTS"), 0));
                            return;
                        }
                        Session.Character.GiftAdd(1915, 1);
                        Session.Character.Inventory.RemoveItemAmount(2324, 10);
                    }
                    break;

                case 74: // Exchange 30 Yellow Pumpkin Sweets (2322) for Jack O'Lantern's Seal (1916)
                    if (npc == null || !ServerManager.Instance.Configuration.HalloweenEvent)
                    {
                        return;
                    }
                    if (packet.Type == 0)
                    {
                        Session.SendPacket($"qna #n_run^{packet.Runner}^56^{packet.Value}^{packet.NpcId} {Language.Instance.GetMessageFromKey("ASK_TRADE")}");
                    }
                    else
                    {
                        if (Session.Character.Inventory.CountItem(2322) < 30)
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_INGREDIENTS"), 0));
                            return;
                        }
                        Session.Character.GiftAdd(1916, 1);
                        Session.Character.Inventory.RemoveItemAmount(2322, 30);
                    }
                    break;

                case 75: // Exchange 30 Black Pumpkin Sweets (2324) for Jack O'Lantern's Seal (1916)
                    if (npc == null || !ServerManager.Instance.Configuration.HalloweenEvent)
                    {
                        return;
                    }
                    if (packet.Type == 0)
                    {
                        Session.SendPacket($"qna #n_run^{packet.Runner}^56^{packet.Value}^{packet.NpcId} {Language.Instance.GetMessageFromKey("ASK_TRADE")}");
                    }
                    else
                    {
                        if (Session.Character.Inventory.CountItem(2324) < 30 || npc == null || !ServerManager.Instance.Configuration.HalloweenEvent)
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_INGREDIENTS"), 0));
                            return;
                        }
                        Session.Character.GiftAdd(1916, 1);
                        Session.Character.Inventory.RemoveItemAmount(2324, 30);
                    }
                    break;

                case 76: // Exchange Bag of Sweets (1917) for Jack O'Lantern's Seal (1916)
                    if (npc == null || !ServerManager.Instance.Configuration.HalloweenEvent)
                    {
                        return;
                    }
                    if (packet.Type == 0)
                    {
                        Session.SendPacket($"qna #n_run^{packet.Runner}^56^{packet.Value}^{packet.NpcId} {Language.Instance.GetMessageFromKey("ASK_TRADE")}");
                    }
                    else
                    {
                        if (Session.Character.Inventory.CountItem(1917) < 1)
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_INGREDIENTS"), 0));
                            return;
                        }
                        Session.Character.GiftAdd(1916, 1);
                        Session.Character.Inventory.RemoveItemAmount(1917, 1);
                    }
                    break;

                case 77:
                    {
                        if (npc != null && ServerManager.Instance.Configuration.HalloweenEvent)
                        {
                            Session.Character.AddQuest(5924);
                        }
                    }
                    break;

                case 78:
                    {
                        if (npc != null && ServerManager.Instance.Configuration.HalloweenEvent)
                        {
                            Session.Character.AddQuest(5926);
                        }
                    }
                    break;

                case 79:
                    {
                        if (npc != null && ServerManager.Instance.Configuration.HalloweenEvent)
                        {
                            Session.Character.AddQuest(5928);
                        }
                    }
                    break;

                case 80:
                    {
                        if (npc != null && ServerManager.Instance.Configuration.HalloweenEvent)
                        {
                            Session.Character.AddQuest(5930);
                        }
                    }
                    break;

                case 81:
                    {
                        if (npc != null && ServerManager.Instance.Configuration.HalloweenEvent)
                        {
                            Session.Character.AddQuest(5922);
                        }
                    }
                    break;

                case 82:
                    {
                        if (npc != null && ServerManager.Instance.Configuration.ChristmasEvent)
                        {
                            Session.Character.AddQuest(5932);
                        }
                    }
                    break;

                case 84:
                    {
                        if (npc == null || !ServerManager.Instance.Configuration.ChristmasEvent)
                        {
                            return;
                        }

                        if (packet.Type == 0)
                        {
                            Session.SendPacket($"qna #n_run^{packet.Runner}^56^{packet.Value}^{packet.NpcId} {Language.Instance.GetMessageFromKey("ASK_TRADE")}");
                        }
                        else
                        {
                            if (Session.Character.Inventory.CountItem(2327) < 30)
                            {
                                Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_INGREDIENTS"), 0));
                                return;
                            }

                            Session.Character.GiftAdd(5064, 1);
                            Session.Character.Inventory.RemoveItemAmount(2327, 30);
                        }
                    }
                    break;

                case 85:
                    {
                        if (npc == null || !ServerManager.Instance.Configuration.ChristmasEvent)
                        {
                            return;
                        }

                        if (packet.Type == 0)
                        {
                            Session.SendPacket($"qna #n_run^{packet.Runner}^56^{packet.Value}^{packet.NpcId} {Language.Instance.GetMessageFromKey("ASK_TRADE")}");
                        }
                        else
                        {
                            if (Session.Character.Inventory.CountItem(2326) < 30)
                            {
                                Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_INGREDIENTS"), 0));
                                return;
                            }

                            Session.Character.GiftAdd(5064, 1);
                            Session.Character.Inventory.RemoveItemAmount(2326, 30);
                        }
                    }
                    break;

                case 86:
                    {
                        if (npc == null || !ServerManager.Instance.Configuration.ChristmasEvent)
                        {
                            return;
                        }

                        if (packet.Type == 0)
                        {
                            Session.SendPacket($"qna #n_run^{packet.Runner}^56^{packet.Value}^{packet.NpcId} {Language.Instance.GetMessageFromKey("ASK_TRADE")}");
                        }
                        else
                        {
                            if (Session.Character.Inventory.CountItem(2327) < 30)
                            {
                                Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_INGREDIENTS"), 0));
                                return;
                            }

                            Session.Character.GiftAdd(1371, 1);
                            Session.Character.Inventory.RemoveItemAmount(2327, 30);
                        }
                    }
                    break;

                case 87:
                    {
                        if (npc == null || !ServerManager.Instance.Configuration.ChristmasEvent)
                        {
                            return;
                        }

                        if (packet.Type == 0)
                        {
                            Session.SendPacket($"qna #n_run^{packet.Runner}^56^{packet.Value}^{packet.NpcId} {Language.Instance.GetMessageFromKey("ASK_TRADE")}");
                        }
                        else
                        {
                            if (Session.Character.Inventory.CountItem(2326) < 30)
                            {
                                Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_INGREDIENTS"), 0));
                                return;
                            }

                            Session.Character.GiftAdd(1371, 1);
                            Session.Character.Inventory.RemoveItemAmount(2326, 30);
                        }
                    }
                    break;

                case 88:
                    {
                        if (npc == null || !ServerManager.Instance.Configuration.ChristmasEvent)
                        {
                            return;
                        }

                        if (packet.Type == 0)
                        {
                            Session.SendPacket($"qna #n_run^{packet.Runner}^56^{packet.Value}^{packet.NpcId} {Language.Instance.GetMessageFromKey("ASK_TRADE")}");
                        }
                        else
                        {
                            if (Session.Character.Inventory.CountItem(1367) < 5)
                            {
                                Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_INGREDIENTS"), 0));
                                return;
                            }

                            Session.Character.GiftAdd(5206, 1);
                            Session.Character.Inventory.RemoveItemAmount(1367, 5);
                        }
                    }
                    break;

                case 325:
                    {
                        if (npc == null || !ServerManager.Instance.Configuration.ChristmasEvent)
                        {
                            return;
                        }

                        if (packet.Type == 0)
                        {
                            Session.SendPacket($"qna #n_run^{packet.Runner}^56^{packet.Value}^{packet.NpcId} {Language.Instance.GetMessageFromKey("ASK_TRADE")}");
                        }
                        else
                        {
                            if ((Session.Character.Inventory.CountItem(5712) < 1 && Session.Character.Inventory.CountItem(9138) < 1) || (Session.Character.Inventory.CountItem(4406) < 1 && Session.Character.Inventory.CountItem(8369) < 1))
                            {
                                Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_INGREDIENTS"), 0));
                                return;
                            }

                            if (Session.Character.Inventory.CountItem(9138) > 0)
                            {
                                Session.Character.Inventory.RemoveItemAmount(9138, 1);
                                Session.Character.GiftAdd(9140, 1);
                            }
                            else
                            {
                                Session.Character.Inventory.RemoveItemAmount(5712, 1);
                                Session.Character.GiftAdd(5714, 1);
                            }

                            if (Session.Character.Inventory.CountItem(8369) > 0)
                            {
                                Session.Character.Inventory.RemoveItemAmount(8369, 1);
                            }
                            else
                            {
                                Session.Character.Inventory.RemoveItemAmount(4406, 1);
                            }
                        }
                    }
                    break;

                case 326:
                    {
                        if (npc != null && ServerManager.Instance.Configuration.ChristmasEvent)
                        {
                            Session.Character.AddQuest(6325);
                        }
                    }
                    break;

                case 89:
                    {
                        if (npc != null && ServerManager.Instance.Configuration.ChristmasEvent)
                        {
                            Session.Character.AddQuest(5934);
                        }
                    }
                    break;

                case 90:
                    {
                        if (npc != null && ServerManager.Instance.Configuration.ChristmasEvent)
                        {
                            Session.Character.AddQuest(5936);
                        }
                    }
                    break;

                case 91:
                    {
                        if (npc != null && ServerManager.Instance.Configuration.ChristmasEvent)
                        {
                            Session.Character.AddQuest(5938);
                        }
                    }
                    break;

                case 92:
                    {
                        if (npc != null && ServerManager.Instance.Configuration.ChristmasEvent)
                        {
                            Session.Character.AddQuest(5940);
                        }
                    }
                    break;

                case 93:
                    {
                        if (npc != null && ServerManager.Instance.Configuration.ChristmasEvent)
                        {
                            Session.Character.AddQuest(5942);
                        }
                    }
                    break;

                case 6013:
                    if (!ServerManager.Instance.Configuration.ChristmasEvent)
                    {
                        return;
                    }
                    if (npc == null)
                    {
                        return;
                    }
                    if (packet.Type == 0)
                    {
                        Session.SendPacket($"qna #n_run^{packet.Runner}^56^{packet.Value}^{packet.NpcId} {Language.Instance.GetMessageFromKey("ASK_TRADE")}");
                    }
                    else
                    {
                        if (Session.Character.Inventory.CountItem(1611) < 1 || Session.Character.Inventory.CountItem(1612) < 1
                         || Session.Character.Inventory.CountItem(1613) < 2 || Session.Character.Inventory.CountItem(1614) < 1)
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_INGREDIENTS"), 0));
                            return;
                        }
                        Session.Character.GiftAdd(1621, 1);
                        Session.Character.Inventory.RemoveItemAmount(1611);
                        Session.Character.Inventory.RemoveItemAmount(1612);
                        Session.Character.Inventory.RemoveItemAmount(1613, 2);
                        Session.Character.Inventory.RemoveItemAmount(1614);
                    }
                    break;

                case 129:
                    if (!ServerManager.Instance.Configuration.ChristmasEvent)
                    {
                        return;
                    }
                    if (npc == null)
                    {
                        return;
                    }
                    if (packet.Type == 0)
                    {
                        Session.SendPacket($"qna #n_run^{packet.Runner}^56^{packet.Value}^{packet.NpcId} {Language.Instance.GetMessageFromKey("ASK_TRADE")}");
                    }
                    else
                    {
                        if (Session.Character.Inventory.CountItem(1611) < 10 || Session.Character.Inventory.CountItem(1612) < 10
                         || Session.Character.Inventory.CountItem(1613) < 20 || Session.Character.Inventory.CountItem(1614) < 10)
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_INGREDIENTS"), 0));
                            return;
                        }
                        Session.Character.GiftAdd(1621, 10);
                        Session.Character.Inventory.RemoveItemAmount(1611, 10);
                        Session.Character.Inventory.RemoveItemAmount(1612, 10);
                        Session.Character.Inventory.RemoveItemAmount(1613, 20);
                        Session.Character.Inventory.RemoveItemAmount(1614, 10);
                    }
                    break;

                case 6014:
                    if (!ServerManager.Instance.Configuration.ChristmasEvent)
                    {
                        return;
                    }
                    if (npc == null)
                    {
                        return;
                    }
                    if (packet.Type == 0)
                    {
                        Session.SendPacket($"qna #n_run^{packet.Runner}^56^{packet.Value}^{packet.NpcId} {Language.Instance.GetMessageFromKey("ASK_TRADE")}");
                    }
                    else
                    {
                        if (Session.Character.Inventory.CountItem(1615) < 1 || Session.Character.Inventory.CountItem(1616) < 2 || Session.Character.Inventory.CountItem(1617) < 1
                         || Session.Character.Inventory.CountItem(1618) < 1 || Session.Character.Inventory.CountItem(1619) < 1 || Session.Character.Inventory.CountItem(1620) < 1)
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_INGREDIENTS"), 0));
                            return;
                        }
                        Session.Character.GiftAdd(1622, 1);
                        Session.Character.Inventory.RemoveItemAmount(1615);
                        Session.Character.Inventory.RemoveItemAmount(1616, 2);
                        Session.Character.Inventory.RemoveItemAmount(1617);
                        Session.Character.Inventory.RemoveItemAmount(1618);
                        Session.Character.Inventory.RemoveItemAmount(1619);
                        Session.Character.Inventory.RemoveItemAmount(1620);
                    }
                    break;

                case 130:
                    if (!ServerManager.Instance.Configuration.ChristmasEvent)
                    {
                        return;
                    }
                    if (npc == null)
                    {
                        return;
                    }
                    if (packet.Type == 0)
                    {
                        Session.SendPacket($"qna #n_run^{packet.Runner}^56^{packet.Value}^{packet.NpcId} {Language.Instance.GetMessageFromKey("ASK_TRADE")}");
                    }
                    else
                    {
                        if (Session.Character.Inventory.CountItem(1615) < 10 || Session.Character.Inventory.CountItem(1616) < 20 || Session.Character.Inventory.CountItem(1617) < 10
                         || Session.Character.Inventory.CountItem(1618) < 10 || Session.Character.Inventory.CountItem(1619) < 10 || Session.Character.Inventory.CountItem(1620) < 10)
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_INGREDIENTS"), 0));
                            return;
                        }
                        Session.Character.GiftAdd(1622, 10);
                        Session.Character.Inventory.RemoveItemAmount(1615, 10);
                        Session.Character.Inventory.RemoveItemAmount(1616, 20);
                        Session.Character.Inventory.RemoveItemAmount(1617, 10);
                        Session.Character.Inventory.RemoveItemAmount(1618, 10);
                        Session.Character.Inventory.RemoveItemAmount(1619, 10);
                        Session.Character.Inventory.RemoveItemAmount(1620, 10);
                    }
                    break;

                case 1503:
                    if (!ServerManager.Instance.Configuration.HalloweenEvent)
                    {
                        return;
                    }

                    if (Session.Character.Level < 20)
                    {
                        Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("TOO_LOW_LVL"), 0));
                        return;
                    }

                    if (!Session.Character.GeneralLogs.Any(s => s.LogType == "DailyReward" && short.Parse(s.LogData) == 1917 && s.Timestamp.Date == DateTime.Today))
                    {
                        Session.Character.GeneralLogs.Add(new GeneralLogDTO
                        {
                            AccountId = Session.Account.AccountId,
                            CharacterId = Session.Character.CharacterId,
                            IpAddress = Session.IpAddress,
                            LogData = "1917",
                            LogType = "DailyReward",
                            Timestamp = DateTime.Now
                        });
                        short amount = 1;
                        if (Session.Character.IsMorphed)
                        {
                            amount *= 2;
                        }
                        Session.Character.GiftAdd(1917, amount);
                    }
                    else
                    {
                        Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("QUEST_ALREADY_DONE"), 0));
                    }
                    break;

                case 110:
                    {
                        if (npc != null)
                        {
                            Session.Character.AddQuest(5954);
                        }
                    }
                    break;

                case 111:
                    if (npc == null)
                    {
                        return;
                    }
                    if (packet.Type == 0)
                    {
                        Session.SendPacket($"qna #n_run^{packet.Runner}^56^{packet.Value}^{packet.NpcId} {Language.Instance.GetMessageFromKey("ASK_TRADE")}");
                    }
                    else
                    {
                        if (Session.Character.Inventory.CountItem(1012) < 20 || Session.Character.Inventory.CountItem(1013) < 20 || Session.Character.Inventory.CountItem(1027) < 20)
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_INGREDIENTS"), 0));
                            return;
                        }
                        Session.Character.GiftAdd(5500, 1);
                        Session.Character.Inventory.RemoveItemAmount(1012, 20);
                        Session.Character.Inventory.RemoveItemAmount(1013, 20);
                        Session.Character.Inventory.RemoveItemAmount(1027, 20);
                    }
                    break;

                case 131:
                    {
                        if (npc != null)
                        {
                            Session.Character.AddQuest(5982);
                        }
                    }
                    break;

                case 132:
                    tp = npc?.Teleporters?.FirstOrDefault(s => s.Index == packet.Type);
                    if (tp != null)
                    {
                        ServerManager.Instance.ChangeMap(Session.Character.CharacterId, tp.MapId, tp.MapX, tp.MapY);
                    }
                    break;

                case 133:
                    if (npc == null)
                    {
                        return;
                    }
                    if (packet.Type == 0)
                    {
                        Session.SendPacket($"qna #n_run^{packet.Runner}^56^{packet.Value}^{packet.NpcId} {Language.Instance.GetMessageFromKey("ASK_TRADE")}");
                    }
                    else
                    {
                        if (Session.Character.Inventory.CountItem(1012) < 20 || Session.Character.Inventory.CountItem(2307) < 20 || Session.Character.Inventory.CountItem(5911) < 20)
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_INGREDIENTS"), 0));
                            return;
                        }
                        Session.Character.GiftAdd(5512, 1);
                        Session.Character.Inventory.RemoveItemAmount(1012, 20);
                        Session.Character.Inventory.RemoveItemAmount(2307, 20);
                        Session.Character.Inventory.RemoveItemAmount(5911, 20);
                    }
                    break;

                case 134:
                    if (npc == null || !Session.Character.Quests.Any(s => s.Quest.QuestObjectives.Any(o => o.SpecialData == 5518)))
                    {
                        return;
                    }
                    short vNum = 0;
                    for (short i = 4494; i <= 4496; i++)
                    {
                        if (Session.Character.Inventory.CountItem(i) > 0)
                        {
                            vNum = i;
                            break;
                        }
                    }
                    if (vNum > 0)
                    {
                        Session.Character.GiftAdd(5518, 1);
                        Session.Character.GiftAdd(4504, 1);
                        Session.Character.Inventory.RemoveItemAmount(vNum, 1);
                    }
                    else
                    {
                        Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_INGREDIENTS"), 0));
                    }
                    break;

                case 137:
                    Session.SendPacket("taw_open");
                    break;

                case 138:
                    ConcurrentBag<ArenaTeamMember> at = ServerManager.Instance.ArenaTeams.ToList().Where(s => s.Any(c => c.Session?.CurrentMapInstance != null)).OrderBy(s => rand.Next()).FirstOrDefault();
                    if (at != null)
                    {
                        ServerManager.Instance.ChangeMapInstance(Session.Character.CharacterId, at.FirstOrDefault().Session.CurrentMapInstance.MapInstanceId, 69, 100);

                        var zenas = at.OrderBy(s => s.Order).FirstOrDefault(s => s.Session != null && !s.Dead && s.ArenaTeamType == ArenaTeamType.ZENAS);
                        var erenia = at.OrderBy(s => s.Order).FirstOrDefault(s => s.Session != null && !s.Dead && s.ArenaTeamType == ArenaTeamType.ERENIA);
                        Session.SendPacket(erenia?.Session?.Character?.GenerateTaM(0));
                        Session.SendPacket(erenia?.Session?.Character?.GenerateTaM(3));
                        Session.SendPacket("taw_sv 0");
                        Session.SendPacket(zenas?.Session?.Character?.GenerateTaP(0, true));
                        Session.SendPacket(erenia?.Session?.Character?.GenerateTaP(2, true));
                        Session.SendPacket(zenas?.Session?.Character?.GenerateTaFc(0));
                        Session.SendPacket(erenia?.Session?.Character?.GenerateTaFc(1));
                    }
                    else
                    {
                        Session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("NO_TEAM_ARENA")));
                    }

                    break;

                case 135:
                    if (!ServerManager.Instance.StartedEvents.Contains(EventType.TALENTARENA))
                    {
                        TimeSpan time = ServerManager.Instance.Schedules.ToList().FirstOrDefault(s => s.Event == EventType.TALENTARENA)?.Time ?? TimeSpan.FromSeconds(0);
                        Session.SendPacket(npc?.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("ARENA_NOT_OPEN"), string.Format("{0:D2}:{1:D2} - {2:D2}:{3:D2}", time.Hours, time.Minutes, (time.Hours + 4) % 24, time.Minutes)), 10));
                    }
                    else
                    {
                        if (Session.Character.Level < 30)
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("LOW_LVL_30")));
                            return;
                        }

                        var tickets = 10 - Session.Character.GeneralLogs.CountLinq(s => s.LogType == "TalentArena" && s.Timestamp.Date == DateTime.Today);

                        if (tickets > 0)
                        {
                            if (ServerManager.Instance.ArenaMembers.ToList().All(s => s.Session != Session))
                            {
                                if (ServerManager.Instance.IsCharacterMemberOfGroup(Session.Character.CharacterId))
                                {
                                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("TALENT_ARENA_GROUP"), 0));
                                    Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("TALENT_ARENA_GROUP"), 10));
                                }
                                else
                                {
                                    Session.SendPacket(Session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("ARENA_TICKET_LEFT"), tickets), 10));

                                    ServerManager.Instance.ArenaMembers.Add(new ArenaMember
                                    {
                                        ArenaType = EventType.TALENTARENA,
                                        Session = Session,
                                        GroupId = null,
                                        Time = 0
                                    });
                                }
                            }
                        }
                        else
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("TALENT_ARENA_NO_MORE_TICKET"), 0));
                            Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("TALENT_ARENA_NO_MORE_TICKET"), 10));
                        }
                    }
                    break;

                case 19:
                case 144:
                    if (Session.Character.Timespace != null)
                    {
                        if (Session.Character.MapInstance.InstanceBag.EndState == 10)
                        {
                            EventHelper.Instance.RunEvent(new EventContainer(Session.Character.MapInstance, EventActionType.SCRIPTEND, (byte)5));
                        }
                    }
                    break;

                case 145:
                    if (npc == null)
                    {
                        return;
                    }
                    if (packet.Type == 0)
                    {
                        Session.SendPacket($"qna #n_run^{packet.Runner}^56^{packet.Value}^{packet.NpcId} {Language.Instance.GetMessageFromKey("ASK_TRADE")}");
                    }
                    else
                    {
                        if (Session.Character.Inventory.CountItem(2522) < 50)
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_INGREDIENTS"), 0));
                            return;
                        }
                        switch (Session.Character.Class)
                        {
                            case ClassType.Swordsman:
                                Session.Character.GiftAdd(4500, 1);
                                break;

                            case ClassType.Archer:
                                Session.Character.GiftAdd(4501, 1);
                                break;

                            case ClassType.Magician:
                                Session.Character.GiftAdd(4502, 1);
                                break;
                        }
                        Session.Character.Inventory.RemoveItemAmount(2522, 50);
                    }
                    break;

                case 146:
                    if (npc == null)
                    {
                        return;
                    }
                    if (packet.Type == 0)
                    {
                        Session.SendPacket($"qna #n_run^{packet.Runner}^56^{packet.Value}^{packet.NpcId} {Language.Instance.GetMessageFromKey("ASK_TRADE")}");
                    }
                    else
                    {
                        if (Session.Character.Inventory.CountItem(2522) < 50)
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_INGREDIENTS"), 0));
                            return;
                        }
                        Session.Character.GiftAdd(2518, 1);
                        Session.Character.Inventory.RemoveItemAmount(2522, 50);
                    }
                    break;

                case 147:
                    if (npc == null)
                    {
                        return;
                    }
                    if (packet.Type == 0)
                    {
                        Session.SendPacket($"qna #n_run^{packet.Runner}^56^{packet.Value}^{packet.NpcId} {Language.Instance.GetMessageFromKey("ASK_TRADE")}");
                    }
                    else
                    {
                        if (Session.Character.Inventory.CountItem(2523) < 50)
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_INGREDIENTS"), 0));
                            return;
                        }
                        switch (Session.Character.Class)
                        {
                            case ClassType.Swordsman:
                                Session.Character.GiftAdd(4497, 1);
                                break;

                            case ClassType.Archer:
                                Session.Character.GiftAdd(4498, 1);
                                break;

                            case ClassType.Magician:
                                Session.Character.GiftAdd(4499, 1);
                                break;
                        }
                        Session.Character.Inventory.RemoveItemAmount(2523, 50);
                    }
                    break;

                case 148:
                    if (npc == null)
                    {
                        return;
                    }
                    if (packet.Type == 0)
                    {
                        Session.SendPacket($"qna #n_run^{packet.Runner}^56^{packet.Value}^{packet.NpcId} {Language.Instance.GetMessageFromKey("ASK_TRADE")}");
                    }
                    else
                    {
                        if (Session.Character.Inventory.CountItem(2523) < 50)
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_INGREDIENTS"), 0));
                            return;
                        }
                        Session.Character.GiftAdd(2519, 1);
                        Session.Character.Inventory.RemoveItemAmount(2523, 50);
                    }
                    break;

                case 150:
                    if (npc != null)
                    {
                        if (Session.Character.Level >= 55)
                        {
                            ServerManager.Instance.ChangeMap(Session.Character.CharacterId, 150, 153, 145);
                        }
                        else
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("LOD_REQUIERE_LVL"), 0));
                        }                        

                    }
                    break;

                case 305:
                    if (npc != null)
                    {
                        ServerManager.Instance.ChangeMap(Session.Character.CharacterId, 243, 71, 245);
                    }
                    break;

                case 193:
                    {
                        if (npc != null)
                        {
                            Session.Character.AddQuest(6021);
                        }
                    }
                    break;

                case 194:
                    if (npc == null)
                    {
                        return;
                    }
                    if (packet.Type == 0)
                    {
                        Session.SendPacket($"qna #n_run^{packet.Runner}^56^{packet.Value}^{packet.NpcId} {Language.Instance.GetMessageFromKey("ASK_TRADE")}");
                    }
                    else
                    {
                        if (Session.Character.Inventory.CountItem(5986) < 3)
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_INGREDIENTS"), 0));
                            return;
                        }
                        Session.Character.GiftAdd(5984, 3);
                        Session.Character.Inventory.RemoveItemAmount(5986, 3);
                    }
                    break;

                case 195:
                    if (npc == null)
                    {
                        return;
                    }
                    if (packet.Type == 0)
                    {
                        Session.SendPacket($"qna #n_run^{packet.Runner}^56^{packet.Value}^{packet.NpcId} {Language.Instance.GetMessageFromKey("ASK_TRADE")}");
                    }
                    else
                    {
                        if (Session.Character.Inventory.CountItem(5987) < 5)
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_INGREDIENTS"), 0));
                            return;
                        }
                        Session.Character.GiftAdd(5977, 2);
                        Session.Character.Inventory.RemoveItemAmount(5987, 5);
                    }
                    break;

                case 300:
                    {
                        if (npc != null)
                        {
                            Session.Character.AddQuest(6040);
                        }
                    }
                    break;

                case 301:
                    tp = npc?.Teleporters?.FirstOrDefault(s => s.Index == packet.Type);
                    if (tp != null)
                    {
                        if (tp.MapId == 228 && Session.Character.Level <= 87)
                        {
                            Session.SendPacket(Session.Character.GenerateSay("You need Level 88", 12));
                            return;
                        }
                        if (tp.MapId == 228 && Session.Character.Level >= 88 && Session.Character.HeroLevel == 0)
                        {
                            Session.Character.HeroLevel = 1;
                        }
                        ServerManager.Instance.ChangeMap(Session.Character.CharacterId, tp.MapId, tp.MapX, tp.MapY);
                    }
                    break;

                case 334:
                    tp = npc?.Teleporters?.FirstOrDefault(s => s.Index == packet.Type);
                    if (tp != null)
                    {
                        if (Session.Character.HeroLevel < 30)
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_HERO_LV_30"), 0));
                            return;
                        }
                        ServerManager.Instance.ChangeMap(Session.Character.CharacterId, tp.MapId, tp.MapX, tp.MapY);
                        Session.Character.Gold -= 25000;
                        Session.SendPacket(Session.Character.GenerateGold());
                    }
                    break;

                case 335:
                    tp = npc?.Teleporters?.FirstOrDefault(s => s.Index == packet.Type);
                    if (tp != null)
                    {
                        if (Session.Character.HeroLevel < 30)
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_HERO_LV_30"), 0));
                            return;
                        }
                        ServerManager.Instance.ChangeMap(Session.Character.CharacterId, tp.MapId, tp.MapX, tp.MapY);
                        Session.Character.Gold -= 25000;
                        Session.SendPacket(Session.Character.GenerateGold());
                    }
                    break;

                case 336:
                    tp = npc?.Teleporters?.FirstOrDefault(s => s.Index == packet.Type);
                    if (tp != null)
                    {
                        if (Session.Character.HeroLevel < 30)
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_HERO_LV_30"), 0));
                            return;
                        }
                        switch (tp.Index)
                        {
                            case 170:
                                ServerManager.Instance.ChangeMap(Session.Character.CharacterId, tp.MapId, tp.MapX, tp.MapY);
                                Session.Character.Gold -= 25000;
                                Session.SendPacket(Session.Character.GenerateGold());
                                break;

                            case 145:
                                ServerManager.Instance.ChangeMap(Session.Character.CharacterId, tp.MapId, tp.MapX, tp.MapY);
                                Session.Character.Gold -= 30000;
                                Session.SendPacket(Session.Character.GenerateGold());
                                break;
                        }
                        if (tp.TeleporterId == 248)
                        {
                            ServerManager.Instance.ChangeMap(Session.Character.CharacterId, tp.MapId, tp.MapX, tp.MapY);
                            Session.Character.Gold -= 30000;
                            Session.SendPacket(Session.Character.GenerateGold());
                        }

                    }
                    break;

                case 321:
                    {
                        Session.SendPacket(Session.Character.GenerateGB(3));
                        Session.SendPacket(Session.Character.GenerateSMemo(6, "A warm welcome to the Cuarry Bank. You can deposit or withdraw from 1,000 to 100 billion units of gold."));
                    }
                    break;

                case 322://Dialog = 356
                    {
                        if (packet.Type == 0 && packet.Value == 2)
                        {
                            var Item = Session.Character.Inventory.CountItem(5836);
                            if (Item == 0)
                            {
                                var iteminfo = ServerManager.GetItem(5836);
                                var inv = Session.Character.Inventory.AddNewToInventory(5836).FirstOrDefault();
                                if (inv != null)
                                {

                                    Session.SendPacket("info Item Cuarry Bank Savings Book received");
                                }
                                else
                                {
                                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_PLACE"), 0));
                                }
                            }
                            else
                            {
                                Session.SendPacket($"say 1 {Session.Character.CharacterId} 10 It's already been received by you.");
                            }
                        }
                    }
                    break;

                case 1000:
                    if (npc == null)
                    {
                        return;
                    }

                    if ( Session.Character.Quests.Any(s => s.Quest.DialogNpcVNum == npc.NpcVNum && s.Quest.QuestObjectives.Any(o => o.SpecialData == packet.Type)))
                    {
                        if (ServerManager.Instance.TimeSpaces.FirstOrDefault(s => s.QuestTimeSpaceId == packet.Type) is ScriptedInstance timeSpace)
                        {
                            Session.Character.EnterInstance(timeSpace);
                        }
                    }

                    break;

                case 94:
                    // Quest Easter Mimi 
                    if (npc == null)
                    {
                        return;
                    }
                    if (npc != null && ServerManager.Instance.Configuration.EasterEvent)
                    {
                        Session.Character.AddQuest(5946);
                    }               
                    break;

                case 95:
                    if (npc == null)
                    {
                        return;
                    }
                    // 5 GoldenEggs vs 1 Box
                    const short GoldenEggs = 5258;
                    const short BoxPascal = 5261;
                    switch (packet.Type)
                    {
                        case 0:
                            Session.SendPacket($"qna #n_run^{packet.Runner}^61^{packet.Value}^{packet.NpcId} {Language.Instance.GetMessageFromKey("EXCHANGE_MATERIAL")}");
                            break;
                        case 61:
                            if (Session.Character.Inventory.CountItem(GoldenEggs) <= 5)
                            {
                                // No GoldenEggs                   
                                Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NOT_ENOUGH_INGREDIENT"), 11));
                                return;
                            }
                            Session.Character.GiftAdd(BoxPascal, 1);
                            Session.Character.Inventory.RemoveItemAmount(GoldenEggs, 5);
                            break;
                    }
                    break;

                case 96:
                    if (npc == null)
                    {
                        return;
                    }
                    // 30 Rabbits vs 1 Seal Chicken king
                    const short ChocolateRabbits = 2405;
                    const short SealChik = 5109;
                    switch (packet.Type)
                    {
                        case 0:
                            Session.SendPacket($"qna #n_run^{packet.Runner}^61^{packet.Value}^{packet.NpcId} {Language.Instance.GetMessageFromKey("EXCHANGE_MATERIAL")}");
                            break;
                        case 61:
                            if (Session.Character.Inventory.CountItem(ChocolateRabbits) <= 30)
                            {
                                // No Lapin                  
                                Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NOT_ENOUGH_INGREDIENT"), 11));
                                return;
                            }
                            Session.Character.GiftAdd(SealChik, 1);
                            Session.Character.Inventory.RemoveItemAmount(ChocolateRabbits, 30);
                            break;
                    }
                    break;

                case 97:
                    // Quest Easter Slugg
                    if (npc == null)
                    {
                        return;
                    }
                    if (npc != null && ServerManager.Instance.Configuration.EasterEvent)
                    {
                        Session.Character.AddQuest(5948);
                    }                 
                    break;

                case 98:
                    // Quest Easter Calvin
                    if (npc == null)
                    {
                        return;
                    }

                    if (npc != null && ServerManager.Instance.Configuration.EasterEvent)
                    {
                        Session.Character.AddQuest(5950);
                    }                
                    break;

                case 99:
                    // Quest Eva Easter
                    if (npc == null)
                    {
                        return;
                    }
                    if (npc != null && ServerManager.Instance.Configuration.EasterEvent)
                    {
                        Session.Character.AddQuest(5953);
                    }                 
                    break;

                case 100:
                    // Quest Easter Malcolm
                    if (npc == null)
                    {
                        return;
                    }
                    if (npc != null && ServerManager.Instance.Configuration.EasterEvent)
                    {
                        Session.Character.AddQuest(5945);
                    }                 
                    break;

                case 1500:
                    {
                        if (npc != null)
                        {
                            Session.Character.AddQuest(2255);
                        }
                    }
                    break;

                case 1600:
                    {
                        if (!Session.Account.VerifiedLock)
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg("You cant do this because your account is blocked. Use $Unlock", 0));
                            return;
                        }


                        Session.SendPacket(Session.Character.OpenFamilyWarehouse());
                    }
                    break;

                case 1601:
                    Session.SendPackets(Session.Character.OpenFamilyWarehouseHist());
                    break;

                case 1602:
                    if (Session.Character.Family?.FamilyLevel >= 2 && Session.Character.Family.WarehouseSize < 21)
                    {
                        if (Session.Character.FamilyCharacter.Authority == FamilyAuthority.Head)
                        {
                            if (500000 >= Session.Character.Gold)
                            {
                                Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NOT_ENOUGH_MONEY"), 10));
                                return;
                            }
                            Session.Character.Family.WarehouseSize = 21;
                            Session.Character.Gold -= 500000;
                            Session.SendPacket(Session.Character.GenerateGold());
                            FamilyDTO fam = Session.Character.Family;
                            DAOFactory.FamilyDAO.InsertOrUpdate(ref fam);
                            ServerManager.Instance.FamilyRefresh(Session.Character.Family.FamilyId);
                        }
                        else
                        {
                            Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NO_FAMILY_HEAD"), 10));
                            Session.SendPacket(UserInterfaceHelper.GenerateModal(Language.Instance.GetMessageFromKey("NO_FAMILY_HEAD"), 1));
                        }
                    }
                    break;

                case 1603:
                    if (Session.Character.Family?.FamilyLevel >= 7 && Session.Character.Family.WarehouseSize < 49)
                    {
                        if (Session.Character.FamilyCharacter.Authority == FamilyAuthority.Head)
                        {
                            if (2000000 >= Session.Character.Gold)
                            {
                                Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NOT_ENOUGH_MONEY"), 10));
                                return;
                            }
                            Session.Character.Family.WarehouseSize = 49;
                            Session.Character.Gold -= 2000000;
                            Session.SendPacket(Session.Character.GenerateGold());
                            FamilyDTO fam = Session.Character.Family;
                            DAOFactory.FamilyDAO.InsertOrUpdate(ref fam);
                            ServerManager.Instance.FamilyRefresh(Session.Character.Family.FamilyId);
                        }
                        else
                        {
                            Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NO_FAMILY_HEAD"), 10));
                            Session.SendPacket(UserInterfaceHelper.GenerateModal(Language.Instance.GetMessageFromKey("NO_FAMILY_HEAD"), 1));
                        }
                    }
                    break;

                case 1604:
                    if (Session.Character.Family?.FamilyLevel >= 5 && Session.Character.Family.MaxSize < 70)
                    {
                        if (Session.Character.FamilyCharacter.Authority == FamilyAuthority.Head)
                        {
                            if (5000000 >= Session.Character.Gold)
                            {
                                Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NOT_ENOUGH_MONEY"), 10));
                                return;
                            }
                            Session.Character.Family.MaxSize = 70;
                            Session.Character.Gold -= 5000000;
                            Session.SendPacket(Session.Character.GenerateGold());
                            FamilyDTO fam = Session.Character.Family;
                            DAOFactory.FamilyDAO.InsertOrUpdate(ref fam);
                            ServerManager.Instance.FamilyRefresh(Session.Character.Family.FamilyId);
                        }
                        else
                        {
                            Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NO_FAMILY_HEAD"), 10));
                            Session.SendPacket(UserInterfaceHelper.GenerateModal(Language.Instance.GetMessageFromKey("NO_FAMILY_HEAD"), 1));
                        }
                    }
                    break;

                case 1605:
                    if (Session.Character.Family?.FamilyLevel >= 9 && Session.Character.Family.MaxSize < 100)
                    {
                        if (Session.Character.FamilyCharacter.Authority == FamilyAuthority.Head)
                        {
                            if (10000000 >= Session.Character.Gold)
                            {
                                Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NOT_ENOUGH_MONEY"), 10));
                                return;
                            }
                            Session.Character.Family.MaxSize = 100;
                            Session.Character.Gold -= 10000000;
                            Session.SendPacket(Session.Character.GenerateGold());
                            FamilyDTO fam = Session.Character.Family;
                            DAOFactory.FamilyDAO.InsertOrUpdate(ref fam);
                            ServerManager.Instance.FamilyRefresh(Session.Character.Family.FamilyId);
                        }
                        else
                        {
                            Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NO_FAMILY_HEAD"), 10));
                            Session.SendPacket(UserInterfaceHelper.GenerateModal(Language.Instance.GetMessageFromKey("NO_FAMILY_HEAD"), 1));
                        }
                    }
                    break;

                case 23:
                    if (packet.Type == 0)
                    {
                        if (Session.Character.Group?.SessionCount == 3)
                        {
                            foreach (ClientSession s in Session.Character.Group.Sessions.GetAllItems())
                            {
                                if (s.Character.Family != null)
                                {
                                    Session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("GROUP_MEMBER_ALREADY_IN_FAMILY")));
                                    return;
                                }
                            }
                        }
                        if (Session.Character.Group == null || Session.Character.Group.SessionCount != 3)
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("FAMILY_GROUP_NOT_FULL")));
                            return;
                        }
                        Session.SendPacket(UserInterfaceHelper.GenerateInbox($"#glmk^ {14} 1 {Language.Instance.GetMessageFromKey("CREATE_FAMILY").Replace(' ', '^')}"));
                    }
                    else
                    {
                        if (Session.Character.Family == null)
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("NOT_IN_FAMILY")));
                            return;
                        }
                        if (Session.Character.Family != null && Session.Character.FamilyCharacter != null && Session.Character.FamilyCharacter.Authority != FamilyAuthority.Head)
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("NOT_FAMILY_HEAD")));
                            return;
                        }
                        Session.SendPacket($"qna #glrm^1 {Language.Instance.GetMessageFromKey("DISSOLVE_FAMILY")}");
                    }

                    break;

                case 60:
                    {
                       

                        MedalType medalType = 0;
                        int time = 0;

                        StaticBonusDTO medal = Session.Character.StaticBonusList.Find(s => s.StaticBonusType == StaticBonusType.BazaarMedalGold || s.StaticBonusType == StaticBonusType.BazaarMedalSilver);

                        if (medal != null)
                        {
                            time = (int)(medal.DateEnd - DateTime.Now).TotalHours;

                            switch (medal.StaticBonusType)
                            {
                                case StaticBonusType.BazaarMedalGold:
                                    medalType = MedalType.Gold;
                                    break;
                                case StaticBonusType.BazaarMedalSilver:
                                    medalType = MedalType.Silver;
                                    break;
                            }
                        }

                        Session.SendPacket($"wopen 32 {(byte)medalType} {time}");
                    }
                    break;

                // case 3000:
                //    {
                //        if (npc != null)
                //        {
                //            Session.Character.AddQuest(5478, true);
                //        }
                //    }
                //    break;

                case 3006:
                    {
                        if (npc != null)
                        {
                            Session.Character.AddQuest(packet.Type);
                        }
                    }
                    break;

                case 200:
                    {
                        if (npc != null)
                        {
                            if (Session.Character.Quests.Any(s => s.Quest.QuestType == (int)QuestType.Dialog2 && s.Quest.QuestObjectives.Any(b => b.Data == npc.NpcVNum)))
                            {
                                Session.Character.AddQuest(packet.Type);
                                Session.Character.IncrementQuests(QuestType.Dialog2, npc.NpcVNum);
                            }
                        }
                    }
                    break;



                case 5001:
                    if (npc != null)
                    {
                        MapInstance map = null;
                        switch (Session.Character.Faction)
                        {
                            case FactionType.None:
                                Session.SendPacket(UserInterfaceHelper.GenerateInfo("You need to be part of a faction to join Act 4!"));
                                return;

                            case FactionType.Angel:
                                map = ServerManager.GetAllMapInstances().Find(s => s.MapInstanceType.Equals(MapInstanceType.Act4ShipAngel));

                                break;

                            case FactionType.Demon:
                                map = ServerManager.GetAllMapInstances().Find(s => s.MapInstanceType.Equals(MapInstanceType.Act4ShipDemon));

                                break;
                        }
                        if (map == null || npc.EffectActivated)
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("SHIP_NOTARRIVED"), 0));
                            return;
                        }
                        if (3000 > Session.Character.Gold)
                        {
                            Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NOT_ENOUGH_MONEY"), 10));
                            return;
                        }
                        Session.Character.Gold -= 3000;
                        Session.SendPacket(Session.Character.GenerateGold());
                        MapCell pos = map.Map.GetRandomPosition();
                        ServerManager.Instance.ChangeMapInstance(Session.Character.CharacterId, map.MapInstanceId, pos.X, pos.Y);
                    }
                    break;

                case 5002:
                    tp = npc?.Teleporters?.FirstOrDefault(s => s.Index == packet.Type);
                    if (tp != null)
                    {
                        Session.SendPacket("it 3");
                        if (ServerManager.Instance.ChannelId == 51)
                        {
                            string connection = CommunicationServiceClient.Instance.RetrieveOriginWorld(Session.Account.AccountId);
                            if (string.IsNullOrWhiteSpace(connection))
                            {
                                return;
                            }
                            Session.Character.MapId = tp.MapId;
                            Session.Character.MapX = tp.MapX;
                            Session.Character.MapY = tp.MapY;
                            int port = Convert.ToInt32(connection.Split(':')[1]);
                            Session.Character.ChangeChannel(connection.Split(':')[0], port, 3);
                        }
                        else
                        {
                            ServerManager.Instance.ChangeMap(Session.Character.CharacterId, tp.MapId, tp.MapX, tp.MapY);
                        }
                    }
                    break;

                case 5004:
                    if (npc != null)
                    {
                        ServerManager.Instance.ChangeMap(Session.Character.CharacterId, 2628, 22, 60);
                    }
                    break;

                case 5011:
                    if (npc != null)
                    {
                        if (30000 > Session.Character.Gold)
                        {
                            Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NOT_ENOUGH_MONEY"), 10));
                            return;
                        }
                        Session.Character.Gold -= 30000;
                        Session.SendPacket(Session.Character.GenerateGold());
                        ServerManager.Instance.ChangeMap(Session.Character.CharacterId, 170, 127, 46);
                    }
                    break;

                case 5012:
                    tp = npc?.Teleporters?.FirstOrDefault(s => s.Index == packet.Type);
                    if (tp != null)
                    {
                        ServerManager.Instance.ChangeMap(Session.Character.CharacterId, tp.MapId, tp.MapX, tp.MapY);
                    }
                    break;

                case 2000:
                    {
                        if (npc != null)
                        {
                            if (packet.Type == 2000 && npc.NpcVNum == 932 && !Session.Character.Quests.Any(s => s.QuestId >= 2000 && s.QuestId <= 2007) // Pajama
                                || packet.Type == 2008 && npc.NpcVNum == 933 && !Session.Character.Quests.Any(s => s.QuestId >= 2008 && s.QuestId <= 2013) // SP 1
                                || packet.Type == 2014 && npc.NpcVNum == 934 && !Session.Character.Quests.Any(s => s.QuestId >= 2014 && s.QuestId <= 2020) // SP 2
                                || packet.Type == 2060 && npc.NpcVNum == 948 && !Session.Character.Quests.Any(s => s.QuestId >= 2060 && s.QuestId <= 2095) // SP 3
                                || packet.Type == 2100 && npc.NpcVNum == 954 && !Session.Character.Quests.Any(s => s.QuestId >= 2100 && s.QuestId <= 2134) // SP 4
                                || packet.Type == 2030 && npc.NpcVNum == 422 && !Session.Character.Quests.Any(s => s.QuestId >= 2030 && s.QuestId <= 2046)
                                || packet.Type == 2048 && npc.NpcVNum == 303 && !Session.Character.Quests.Any(s => s.QuestId >= 2048 && s.QuestId <= 2050))
                            {
                                Session.Character.AddQuest(packet.Type);
                            }
                        }
                    }
                    break;

                case 2001:
                    {
                        switch (packet.Type)
                        {
                            case 1: // Pajama
                                {
                                    if (Session.Character.MapInstance.Npcs.Any(s => s.NpcVNum == 932))
                                    {
                                        Session.Character.Reputation += 350;
                                    }
                                }
                                break;
                            case 2: // SP 1
                                {
                                    if (Session.Character.MapInstance.Npcs.Any(s => s.NpcVNum == 933))
                                    {
                                        Session.Character.Reputation += 750;
                                    }                                     
                                }
                                break;
                            case 3: // SP 2
                                {
                                    if (Session.Character.MapInstance.Npcs.Any(s => s.NpcVNum == 934))
                                    {
                                        Session.Character.Reputation += 1500;
                                    }
                                }
                                break;
                        }
                    }
                    break;

                case 5:
                    if (packet.Type == 0 && packet.Value == 1)
                    {
                        if (Session.Character.MapInstance.Npcs.Any(s => s.NpcVNum == 948 /* SP 3 */ || s.NpcVNum == 954 /* SP 4 */))
                        {
                            switch (Session.Character.Class)
                            {
                                case ClassType.Swordsman:
                                    Session.Character.Reputation += 8500;
                                    break;
                                case ClassType.Archer:
                                    Session.Character.Reputation += 8500;
                                    break;
                                case ClassType.Magician:
                                    Session.Character.Reputation += 8500;
                                    break;
                            }

                            switch (Session.Character.Class)
                            {
                                case ClassType.Swordsman:
                                    Session.Character.Reputation += 15000;
                                    break;
                                case ClassType.Archer:
                                    Session.Character.Reputation += 15000;
                                    break;
                                case ClassType.Magician:
                                    Session.Character.Reputation += 15000;
                                    break;
                            }
                        }
                    }
                    break;

                case 2002:
                    if (npc != null)
                    {
                        int gemNpcVnum = 0;

                        switch (npc.NpcVNum)
                        {
                            //Credits to CryForMe
                            //pijama
                            case 935:
                                MapInstance mapSPPijama = ServerManager.GenerateMapInstance(2107, MapInstanceType.NormalInstance, new InstanceBag());
                                MapNpc mapNpcPijama = new MapNpc
                                {
                                    NpcVNum = 932,
                                    MapX = 5,
                                    MapY = 5,
                                    MapId = 2107,
                                    Dialog = 6314,
                                    MapNpcId = mapSPPijama.GetNextNpcId(),
                                    IsMoving = false,
                                    Position = 1,
                                    IsSitting = false

                                };
                                Portal portalPijama = new Portal
                                {
                                    SourceMapId = 2107,
                                    SourceX = 5,
                                    SourceY = 1,
                                    DestinationMapId = 1,
                                    DestinationX = 0,
                                    DestinationY = 0,
                                    Type = -1
                                };
                                mapSPPijama.CreatePortal(portalPijama);
                                mapNpcPijama.Initialize(mapSPPijama);
                                mapSPPijama.AddNPC(mapNpcPijama);
                                Session.CurrentMapInstance.Broadcast(mapNpcPijama.GenerateIn());
                                MapCell pos = mapSPPijama.Map.GetRandomPosition();
                                ServerManager.Instance.ChangeMapInstance(Session.Character.CharacterId, mapSPPijama.MapInstanceId, pos.X, pos.Y);
                                break;
                                //SP 1
                            case 936:
                                MapInstance mapSP1 = ServerManager.GenerateMapInstance(2107, MapInstanceType.NormalInstance, new InstanceBag());
                                MapNpc mapNpcSP1 = new MapNpc
                                {
                                    NpcVNum = 933,
                                    MapX = 5,
                                    MapY = 5,
                                    MapId = 2107,
                                    Dialog = 6325,
                                    MapNpcId = mapSP1.GetNextNpcId(),
                                    IsMoving = false,
                                    Position = 1,
                                    IsSitting = false

                                };
                                Portal portalSP1 = new Portal
                                {
                                    SourceMapId = 2107,
                                    SourceX = 5,
                                    SourceY = 1,
                                    DestinationMapId = 1,
                                    DestinationX = 0,
                                    DestinationY = 0,
                                    Type = -1
                                };
                                mapSP1.CreatePortal(portalSP1);
                                mapNpcSP1.Initialize(mapSP1);
                                mapSP1.AddNPC(mapNpcSP1);
                                Session.CurrentMapInstance.Broadcast(mapNpcSP1.GenerateIn());
                                MapCell pos1 = mapSP1.Map.GetRandomPosition();
                                ServerManager.Instance.ChangeMapInstance(Session.Character.CharacterId, mapSP1.MapInstanceId, pos1.X, pos1.Y);
                                break;
                                //SP 2
                            case 937:
                                MapInstance mapSP2 = ServerManager.GenerateMapInstance(2107, MapInstanceType.NormalInstance, new InstanceBag());
                                MapNpc mapNpcSP2 = new MapNpc
                                {
                                    NpcVNum = 934,
                                    MapX = 5,
                                    MapY = 5,
                                    MapId = 2107,
                                    Dialog = 6333,
                                    MapNpcId = mapSP2.GetNextNpcId(),
                                    IsMoving = false,
                                    Position = 1,
                                    IsSitting = false

                                };
                                Portal portalSP2 = new Portal
                                {
                                    SourceMapId = 2107,
                                    SourceX = 5,
                                    SourceY = 1,
                                    DestinationMapId = 1,
                                    DestinationX = 0,
                                    DestinationY = 0,
                                    Type = -1
                                };
                                mapSP2.CreatePortal(portalSP2);
                                mapNpcSP2.Initialize(mapSP2);
                                mapSP2.AddNPC(mapNpcSP2);
                                Session.CurrentMapInstance.Broadcast(mapNpcSP2.GenerateIn());
                                MapCell pos2 = mapSP2.Map.GetRandomPosition();
                                ServerManager.Instance.ChangeMapInstance(Session.Character.CharacterId, mapSP2.MapInstanceId, pos2.X, pos2.Y);
                                break;
                                //SP 3
                            case 952:
                                MapInstance mapSP3 = ServerManager.GenerateMapInstance(2107, MapInstanceType.NormalInstance, new InstanceBag());
                                MapNpc mapNpcSP3 = new MapNpc
                                {
                                    NpcVNum = 948,
                                    MapX = 5,
                                    MapY = 5,
                                    MapId = 2107,
                                    Dialog = 7201,
                                    MapNpcId = mapSP3.GetNextNpcId(),
                                    IsMoving = false,
                                    Position = 1,
                                    IsSitting = false

                                };
                                Portal portalSP3 = new Portal
                                {
                                    SourceMapId = 2107,
                                    SourceX = 5,
                                    SourceY = 1,
                                    DestinationMapId = 1,
                                    DestinationX = 0,
                                    DestinationY = 0,
                                    Type = -1
                                };
                                mapSP3.CreatePortal(portalSP3);
                                mapNpcSP3.Initialize(mapSP3);
                                mapSP3.AddNPC(mapNpcSP3);
                                Session.CurrentMapInstance.Broadcast(mapNpcSP3.GenerateIn());
                                MapCell pos3 = mapSP3.Map.GetRandomPosition();
                                ServerManager.Instance.ChangeMapInstance(Session.Character.CharacterId, mapSP3.MapInstanceId, pos3.X, pos3.Y);
                                break;
                                //SP 4
                            case 953:
                                MapInstance mapSP4 = ServerManager.GenerateMapInstance(2107, MapInstanceType.NormalInstance, new InstanceBag());
                                MapNpc mapNpcSP4 = new MapNpc
                                {
                                    NpcVNum = 954,
                                    MapX = 5,
                                    MapY = 5,
                                    MapId = 2107,
                                    Dialog = 7300,
                                    MapNpcId = mapSP4.GetNextNpcId(),
                                    IsMoving = false,
                                    Position = 1,
                                    IsSitting = false

                                };
                                Portal portalSP4 = new Portal
                                {
                                    SourceMapId = 2107,
                                    SourceX = 5,
                                    SourceY = 1,
                                    DestinationMapId = 1,
                                    DestinationX = 0,
                                    DestinationY = 0,
                                    Type = -1
                                };
                                mapSP4.CreatePortal(portalSP4);
                                mapNpcSP4.Initialize(mapSP4);
                                mapSP4.AddNPC(mapNpcSP4);
                                Session.CurrentMapInstance.Broadcast(mapNpcSP4.GenerateIn());
                                MapCell pos4 = mapSP4.Map.GetRandomPosition();
                                ServerManager.Instance.ChangeMapInstance(Session.Character.CharacterId, mapSP4.MapInstanceId, pos4.X, pos4.Y);
                                break;
                        }
                    }
                        break;

                case 666: // Hero Equipment Downgrade
                    {
                        // 4949 ~ 4966 = c25/c28
                        // 4978 ~ 4986 = c45/c48

                        const long price = 10000000;

                        ItemInstance itemInstance = Session?.Character?.Inventory?.LoadBySlotAndType(0, InventoryType.Equipment);

                        if (itemInstance?.Item != null && ((itemInstance.ItemVNum >= 4949 && itemInstance.ItemVNum <= 4966) || (itemInstance.ItemVNum >= 4978 && itemInstance.ItemVNum <= 4986)) && itemInstance.Rare == 8)
                        {
                            if (Session.Character.Gold < price)
                            {
                                Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NOT_ENOUGH_MONEY"), 10));
                                return;
                            }

                            Session.Character.Gold -= price;
                            Session.SendPacket(Session.Character.GenerateGold());

                            itemInstance.RarifyItem(Session, RarifyMode.HeroEquipmentDowngrade, RarifyProtection.None);

                            Session.SendPacket(itemInstance.GenerateInventoryAdd());
                        }
                    }
                    break;

               
                case 324: // MA Quest SP2
                    {
                        if (npc != null)
                        {
                            Session.Character.AddQuest(6307);
                        }
                    }
                    break;

                case 340: // MA Quest SP3
                    {
                        if (npc != null)
                        {
                            Session.Character.AddQuest(6332);
                        }
                    }
                    break;

                default:
                    {
                        Logger.Warn(string.Format(Language.Instance.GetMessageFromKey("NO_NRUN_HANDLER"), packet.Runner));
                    }
                    break;

             

                case 332:
                    if (npc == null)
                    {
                        return;
                    }

                    Session.Character.AddQuest(6500);
                    break;

               
            }
        }

        #endregion
    }
}