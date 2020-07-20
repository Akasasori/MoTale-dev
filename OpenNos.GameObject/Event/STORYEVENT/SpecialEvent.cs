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
using OpenNos.Domain;
using OpenNos.GameObject.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using OpenNos.GameObject.Networking;

namespace OpenNos.GameObject.Event
{
    public static class StoryEvent
    {

        #region Methods

        public static void GenerateStoryEvent()
        {
            //tell PEOPLE that the IC will start in X

            
            ServerManager.Instance.Broadcast(UserInterfaceHelper.GenerateMsg(string.Format("The story event will start in {0} seconds", 10), 1));
            Thread.Sleep(10 * 1000);
            ServerManager.Instance.Broadcast(UserInterfaceHelper.GenerateMsg("THE STORY EVENT HAS STARTED", 1));
            //send to everyone the request ic packet: IC icon appears, you click on it and see the string (battlequestion)
            ServerManager.Instance.Sessions.Where(s => s.Character?.MapInstance.MapInstanceType == MapInstanceType.BaseMapInstance).ToList().ForEach(s => s.SendPacket($"qnaml 3 #guri^506 JOIN THE STORY EVENT"));
            ServerManager.Instance.EventInWaiting = true;
            //wait before teleporting everyone  
            Thread.Sleep(30 * 1000);
            //Thread.Sleep(10 * 1000);
            ServerManager.Instance.Broadcast(UserInterfaceHelper.GenerateMsg("THE STORY EVENT HAS STARTED", 1));
            ServerManager.Instance.Sessions.Where(s => s.Character?.IsWaitingForEvent == false).ToList().ForEach(s => s.SendPacket("esf"));
            ServerManager.Instance.EventInWaiting = false;
            IEnumerable<ClientSession> sessions = ServerManager.Instance.Sessions.Where(s => s.Character?.IsWaitingForEvent == true && s.Character.MapInstance.MapInstanceType == MapInstanceType.BaseMapInstance);
            List<Tuple<MapInstance, byte>> maps = new List<Tuple<MapInstance, byte>>();
            MapInstance map = null;
            int i = -1;
            int level = 0;
            // ! FIRST MAP !
            short[] bufferMaps = { 2502 };//, 2542,  2004}; //maps 

            Random rnd = new Random();
            int casualC = 0;//rnd.Next(0, 3); // Next(min, max - 1) # Next(0,3) -> from 0 to 2
            //int[] portalCoordinatesX = {69, 42, 35}; //x  [NOT USED YET]
            //int[] portalCoordinatesY = {39, 48, 32}; //y
            byte instancelevel = 80;
            //put every character (person) into the ic at their level (it is a list)
            foreach (ClientSession s in sessions.OrderBy(s => s.Character?.Level))
            {
                i++;

                //if the players reach 50, create a new map and insert the next ones [SIANA LIKED THIS METHOD]
                if (i % 50 == 0)
                {
                    map = ServerManager.GenerateMapInstance(bufferMaps[casualC], MapInstanceType.NormalInstance, new InstanceBag());
                    maps.Add(new Tuple<MapInstance, byte>(map, instancelevel));
                }
                if (map != null)
                {
                    ServerManager.Instance.TeleportOnRandomPlaceInMap(s, map.MapInstanceId);
                }

                level = s.Character.Level;
            }
            //insert only the people that are registered
            ServerManager.Instance.Sessions.Where(s => s.Character != null).ToList().ForEach(s => s.Character.IsWaitingForEvent = false);
            ServerManager.Instance.StartedEvents.Remove(EventType.STORYEVENT);
            foreach (Tuple<MapInstance, byte> mapinstance in maps)
            {
                StoryEventTask task = new StoryEventTask();
                Observable.Timer(TimeSpan.FromMinutes(0)).Subscribe(X => StoryEventTask.Run(mapinstance, 0));
            }
        }

        #endregion

        #region Classes

        public class StoryEventTask
        {
            //10 rooms (9 without the first) # change this if you want to add/remove a room
            private static int maxRound = 11 - 1;
            #region Methods

            public static void Run(Tuple<MapInstance, byte> mapinstance, int Nround)
            {
                short[] battleMaps = { 2100, 4680, 4683, 4683, 2103, 2531, 2542, 4500, 4503, 2555 }; //maps BUFFER 4717 9305
                //create the var maxGold so when giving the rewards, there won't be any problems in overflowing the server datas
                long maxGold = ServerManager.Instance.Configuration.MaxGold;
                //10 seconds before checking/starting
                Thread.Sleep(10 * 1000);
                //check if on the map there are 3 people at least (if the objects are not empty)

                if (!mapinstance.Item1.Sessions.Skip(1 - 1).Any()) //because it does +1, that's why we subtract 1
                {
                    //do for all registered characters
                    mapinstance.Item1.Sessions.Where(s => s.Character != null).ToList().ForEach(s =>
                    {
                        //if the character have this buffs (hiding buffs), then remove them

                        s.Character.RemoveBuffByBCardTypeSubType(new List<KeyValuePair<byte, byte>>()
                        {
                            new KeyValuePair<byte, byte>((byte)BCardType.CardType.SpecialActions, (byte)AdditionalTypes.SpecialActions.Hide),
                            new KeyValuePair<byte, byte>((byte)BCardType.CardType.FalconSkill, (byte)AdditionalTypes.FalconSkill.Hide),
                            new KeyValuePair<byte, byte>((byte)BCardType.CardType.FalconSkill, (byte)AdditionalTypes.FalconSkill.Ambush)
                        });
                        //teleport the character to the map
                        ServerManager.Instance.ChangeMap(s.Character.CharacterId, s.Character.MapId, s.Character.MapX, s.Character.MapY);
                    });
                }
                //mapinstance.Item1.Broadcast(UserInterfaceHelper.GenerateMsg(string.Format("GET READT TO HAVE FUN ! ", Nround + 1), 0));
                //after 30 min, if players havent cleared the room, end event
                EventHelper.Instance.ScheduleEvent(TimeSpan.FromMinutes(30), new EventContainer(mapinstance.Item1, EventActionType.DISPOSEMAP, null));
                EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(1), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg(string.Format("The chapter will finish in {0} minutes", 30), 10)));
                EventHelper.Instance.ScheduleEvent(TimeSpan.FromMinutes(10), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg(string.Format("The chapter will finish in {0} minutes", 20), 10)));
                EventHelper.Instance.ScheduleEvent(TimeSpan.FromMinutes(20), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg(string.Format("The chapter will finish in {0} minutes", 10), 10)));
                EventHelper.Instance.ScheduleEvent(TimeSpan.FromMinutes(26), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg(string.Format("The chapter will finish in {0} minutes", 4), 10)));
                //EventHelper.Instance.ScheduleEvent(TimeSpan.FromMinutes(27), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg(string.Format("The story event will finish in {0} minutes", 3), 10)));
                //EventHelper.Instance.ScheduleEvent(TimeSpan.FromMinutes(28), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg(string.Format("The story event will finish in {0} minutes", 2), 10)));
                EventHelper.Instance.ScheduleEvent(TimeSpan.FromMinutes(29), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg(string.Format("The chapter will finish in {0} minutes", 1), 10)));
                //EventHelper.Instance.ScheduleEvent(TimeSpan.FromMinutes(29.5), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg(string.Format("The story event will finish in {0} seconds", 30), 10)));
                EventHelper.Instance.ScheduleEvent(TimeSpan.FromMinutes(29.5), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg(string.Format("The chapter will finish in {0} seconds", 30), 10)));
                //EventHelper.Instance.ScheduleEvent(TimeSpan.FromMinutes(0), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("THE BOSS IS COMING !!! GET READY !", 0)));


                getHistoryMessage(mapinstance.Item1.Map, mapinstance.Item2, Nround, mapinstance);

                int TimerCD = GetTime(Nround);
                int tpTimer = GetTimeLoot(Nround);

                Observable.Timer(TimeSpan.FromSeconds(TimerCD)).Subscribe(X =>
                {//we've put 180 seconds (delay 1 sec each cycle) because the ic is 15 min and we start this at min 12 (180 sec = 3 min)
                    for (int d = 0; d < 1200; d++)
                    {
                        //if every monster is dead in ic
                        if (!mapinstance.Item1.Monsters.Any(s => s.CurrentHp > 0))
                        {
                            getHistoryMessage(mapinstance.Item1.Map, mapinstance.Item2, (Nround + 100), mapinstance);
                            //drop the items
                            int Nrund = Nround;
                            EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(3), new EventContainer(mapinstance.Item1, EventActionType.DROPITEMS, getStoryBattleDrop(mapinstance.Item1.Map, mapinstance.Item2, Nround)));
                            if (Nround != maxRound)
                            {
                                mapinstance.Item1.Broadcast(UserInterfaceHelper.GenerateMsg(string.Format("YOU'VE CLEARED CHAPTER {0}", Nround + 1), 0));
                                Thread.Sleep(2000);
                            }
                            else
                            {
                                Nrund--;
                            }
                            //create a portal into the event map
                            //EventHelper.Instance.ScheduleEvent(TimeSpan.FromMinutes(0), new EventContainer(mapinstance.Item1, EventActionType.SPAWNPORTAL, new Portal { SourceX = 20, SourceY = 45, DestinationMapId = 2502 }));
                            MapInstance map = null;

                            map = ServerManager.GenerateMapInstance(battleMaps[Nrund], MapInstanceType.NormalInstance, new InstanceBag());
                            List<Tuple<MapInstance, byte>> maps = new List<Tuple<MapInstance, byte>>();
                            maps.Add(new Tuple<MapInstance, byte>(map, 2));


                            //for everyone in the ic, give the following rewards
                            foreach (ClientSession cli in mapinstance.Item1.Sessions.Where(s => s.Character != null).ToList())
                            {
                                cli.Character.GenerateFamilyXp(cli.Character.Level * 4);
                                cli.Character.GetReputation(cli.Character.Level * 50);
                                cli.Character.Gold += cli.Character.Level * 1000;
                                cli.Character.Gold = cli.Character.Gold > maxGold ? maxGold : cli.Character.Gold;
                                cli.Character.SpAdditionPoint += cli.Character.Level * 100;

                                //##################################            LOOK AT THIS SHIT AND CRY CRY CRY BECAUSE I AM LAZY TO DO A FUNCTION AND MAKE IT PROPERLY FOR THE PROGRAMMER OR PORNGAMER ALT MMMMMMMMM
                                if (Nround == 0)
                                {
                                    if (cli.Character.IsVehicled)
                                    {
                                        cli.Character.RemoveVehicle();
                                    }
                                    cli.Character.Morph = 28;
                                    cli.CurrentMapInstance?.Broadcast(cli.Character.GenerateCMode());
                                }
                                else if (Nround == 1)
                                {
                                    if (cli.Character.IsVehicled)
                                    {
                                        cli.Character.RemoveVehicle();
                                    }
                                    cli.Character.Morph = 3;
                                    cli.CurrentMapInstance?.Broadcast(cli.Character.GenerateCMode());
                                }
                                else if (Nround == 2)
                                {
                                    if (cli.Character.IsVehicled)
                                    {
                                        cli.Character.RemoveVehicle();
                                    }
                                    cli.Character.Morph = 20;
                                    cli.CurrentMapInstance?.Broadcast(cli.Character.GenerateCMode());
                                }
                                else if (Nround == 3)
                                {
                                    if (cli.Character.IsVehicled)
                                    {
                                        cli.Character.RemoveVehicle();
                                    }
                                    cli.Character.Morph = 20;
                                    cli.CurrentMapInstance?.Broadcast(cli.Character.GenerateCMode());
                                }
                                else if (Nround == 4)
                                {
                                    if (cli.Character.IsVehicled)
                                    {
                                        cli.Character.RemoveVehicle();
                                    }
                                    cli.Character.Morph = 11;
                                    cli.CurrentMapInstance?.Broadcast(cli.Character.GenerateCMode());
                                }
                                else if (Nround == 5)
                                {
                                    if (cli.Character.IsVehicled)
                                    {
                                        cli.Character.RemoveVehicle();
                                    }
                                    cli.Character.Morph = 25;
                                    cli.CurrentMapInstance?.Broadcast(cli.Character.GenerateCMode());
                                }
                                else if (Nround == 6)
                                {
                                    if (cli.Character.IsVehicled)
                                    {
                                        cli.Character.RemoveVehicle();
                                    }
                                    cli.Character.Morph = 5;
                                    cli.CurrentMapInstance?.Broadcast(cli.Character.GenerateCMode());
                                }
                                else if (Nround == 7)
                                {
                                    if (cli.Character.IsVehicled)
                                    {
                                        cli.Character.RemoveVehicle();
                                    }
                                    cli.Character.Morph = 8;
                                    cli.CurrentMapInstance?.Broadcast(cli.Character.GenerateCMode());
                                }
                                else if (Nround == 8)
                                {
                                    if (cli.Character.IsVehicled)
                                    {
                                        cli.Character.RemoveVehicle();
                                    }
                                    cli.Character.Morph = 19;
                                    cli.CurrentMapInstance?.Broadcast(cli.Character.GenerateCMode());
                                }
                                else if (Nround == 9)
                                {
                                    if (cli.Character.IsVehicled)
                                    {
                                        cli.Character.RemoveVehicle();
                                    }
                                    cli.Character.Morph = 27;
                                    cli.CurrentMapInstance?.Broadcast(cli.Character.GenerateCMode());
                                }
                                //#################################

                                /*
                                if (cli.Character.SpAdditionPoint > 1000000)
                                {
                                    cli.Character.SpAdditionPoint = 1000000;
                                }
                                
                                cli.SendPacket(cli.Character.GenerateSpPoint());
                                cli.SendPacket(cli.Character.GenerateGold());
                                cli.SendPacket(cli.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("WIN_MONEY"), cli.Character.Level * 1000), 10));
                                cli.SendPacket(cli.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("WIN_REPUT"), cli.Character.Level * 50), 10));
                                if (cli.Character.Family != null)
                                {
                                    cli.SendPacket(cli.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("WIN_FXP"), cli.Character.Level * 4), 10));
                                }
                                cli.SendPacket(cli.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("WIN_SP_POINT"), cli.Character.Level * 100), 10));

                                if(Nround == maxRound)
                                {
                                    cli.Character.SendGift(cli.Character.CharacterId, 945, 1, 0, 0, 0, false);
                                }
                                */

                                //2100
                                if (Nround != maxRound)
                                {
                                    Observable.Timer(TimeSpan.FromSeconds(tpTimer)).Subscribe(Y => ServerManager.Instance.TeleportOnRandomPlaceInMap(cli, map.MapInstanceId));
                                }
                                Thread.Sleep(100);
                            }
                            foreach (Tuple<MapInstance, byte> mapinstance2 in maps)
                            {
                                if (Nround != maxRound)
                                {
                                    Observable.Timer(TimeSpan.FromSeconds(tpTimer)).Subscribe(Y => StoryEventTask.Run(mapinstance2, Nround + 1));
                                }
                                else  // BATTLE WONz
                                {
                                    mapinstance.Item1.Broadcast(UserInterfaceHelper.GenerateMsg("CONGRATULATIONS, YOU'VE CLEARED THE BOSS BATTLE !!", 0));
                                    EventHelper.Instance.ScheduleEvent(TimeSpan.FromMinutes(0), new EventContainer(mapinstance.Item1, EventActionType.SPAWNPORTAL, new Portal { SourceX = 21, SourceY = 37, DestinationMapId = 2502 }));

                                }
                            }

                            break;
                        }
                        Thread.Sleep(1000);
                    }
                });

                //getHistoryMessage(mapinstance.Item1.Map, mapinstance.Item2, Nround, mapinstance);


                //EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(10), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("THE BOSS IS HERE !", 0)));

                //EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(10), new EventContainer(mapinstance.Item1, EventActionType.SPAWNMONSTERS, getStoryBattleMonster(mapinstance.Item1.Map, mapinstance.Item2, Nround)));
                //spawn the boss monster
                //EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(10 + (0 * 160)), new EventContainer(mapinstance.Item1, EventActionType.SPAWNMONSTERS, getStoryBattleMonster(mapinstance.Item1.Map, mapinstance.Item2, 4)));
            }

            //seconds for each round (chapter) to say when the story dialogues are finished and when can check if there are no mobs on the map
            private static int GetTime(int wave)
            {

                switch (wave)
                {
                    case 0:
                        return 80;
                    case 1:
                        return 60;
                    case 2:
                        return 55;
                    case 3:
                        return 70;
                    case 4:
                        return 60;
                    case 5:
                        return 100;
                    case 6:
                        return 3;
                    case 7:
                        return 3;
                    case 8:
                        return 27;
                    case 9:
                        return 52;
                    case 10:
                        return 90;
                    default:
                        return 30;
                }
            }

            private static int GetTimeLoot(int wave)
            {
                //TODO: create a variable for the end of the event (for looting)
                switch (wave)
                {
                    case 4:
                        return 48;

                    case 10:
                        return 24;
                    default:
                        return 40;
                }
            }

            //the dropping (ez read)
            private static IEnumerable<Tuple<short, int, short, short>> generateDrop(Map map, short vnum, int amountofdrop, int amount)
            {
                List<Tuple<short, int, short, short>> dropParameters = new List<Tuple<short, int, short, short>>();
                for (int i = 0; i < amountofdrop; i++)
                {
                    MapCell cell = map.GetRandomPosition();
                    dropParameters.Add(new Tuple<short, int, short, short>(vnum, amount, cell.X, cell.Y));
                }
                return dropParameters;
            }

            //drop loot by waves
            private static List<Tuple<short, int, short, short>> getStoryBattleDrop(Map map, short instantbattletype, int wave)
            {
                List<Tuple<short, int, short, short>> dropParameters = new List<Tuple<short, int, short, short>>();
                //for loots
                switch (wave)
                {
                    case 0: //for map 0 (the castra map)
                        //map, id of the item to drop, how many items to drop, number of the bunch of the item
                        dropParameters.AddRange(generateDrop(map, 1011, 150, 5));
                        dropParameters.AddRange(generateDrop(map, 2282, 150, 5));
                        dropParameters.AddRange(generateDrop(map, 1252, 50, 1));
                        break;

                    case 1: //for the magic field map
                        dropParameters.AddRange(generateDrop(map, 1046, 50, 200000));
                        dropParameters.AddRange(generateDrop(map, 2089, 150, 5));
                        dropParameters.AddRange(generateDrop(map, 2329, 150, 5));
                        dropParameters.AddRange(generateDrop(map, 1252, 100, 1));
                        break;

                    case 2: //for the ice map
                        dropParameters.AddRange(generateDrop(map, 1046, 10, 150000));
                        dropParameters.AddRange(generateDrop(map, 2515, 150, 1));
                        dropParameters.AddRange(generateDrop(map, 2282, 200, 1));
                        dropParameters.AddRange(generateDrop(map, 1030, 80, 1));
                        dropParameters.AddRange(generateDrop(map, 1363, 15, 1));
                        dropParameters.AddRange(generateDrop(map, 1252, 85, 1));
                        break;

                    case 3: //for the glacerus boss map (ice)
                        dropParameters.AddRange(generateDrop(map, 1046, 40, 100000));
                        dropParameters.AddRange(generateDrop(map, 5929, 25, 1));
                        dropParameters.AddRange(generateDrop(map, 2282, 60, 1));
                        dropParameters.AddRange(generateDrop(map, 1122, 75, 2));
                        dropParameters.AddRange(generateDrop(map, 1429, 10, 10));
                        dropParameters.AddRange(generateDrop(map, 1252, 88, 1));
                        break;
                    case 4:
                        dropParameters.AddRange(generateDrop(map, 1046, 40, 150000));
                        dropParameters.AddRange(generateDrop(map, 2519, 150, 1));
                        dropParameters.AddRange(generateDrop(map, 1244, 75, 2));
                        dropParameters.AddRange(generateDrop(map, 1218, 23, 1));
                        dropParameters.AddRange(generateDrop(map, 2282, 12, 3));
                        dropParameters.AddRange(generateDrop(map, 4052, 3, 1));
                        dropParameters.AddRange(generateDrop(map, 4053, 3, 1));
                        dropParameters.AddRange(generateDrop(map, 4051, 2, 1));
                        dropParameters.AddRange(generateDrop(map, 4050, 2, 1));
                        dropParameters.AddRange(generateDrop(map, 1252, 100, 3));
                        break;
                    case 5:
                        dropParameters.AddRange(generateDrop(map, 1046, 36, 666666));
                        dropParameters.AddRange(generateDrop(map, 2443, 1, 3));
                        dropParameters.AddRange(generateDrop(map, 2517, 100, 1));
                        dropParameters.AddRange(generateDrop(map, 1252, 66, 3));
                        dropParameters.AddRange(generateDrop(map, 11024, 1, 1));
                        dropParameters.AddRange(generateDrop(map, 5881, 15, 2));
                        break;
                    case 6:
                        dropParameters.AddRange(generateDrop(map, 1046, 80, 2000000));
                        dropParameters.AddRange(generateDrop(map, 5279, 1, 1));
                        dropParameters.AddRange(generateDrop(map, 2282, 3, 999));
                        dropParameters.AddRange(generateDrop(map, 11024, 1, 2));
                        dropParameters.AddRange(generateDrop(map, 1252, 1, 500));
                        dropParameters.AddRange(generateDrop(map, 1244, 5, 350));
                        dropParameters.AddRange(generateDrop(map, 1024, 13, 5));
                        dropParameters.AddRange(generateDrop(map, 1904, 6, 1));
                        dropParameters.AddRange(generateDrop(map, 5432, 1, 1));
                        break;
                    case 7:
                        dropParameters.AddRange(generateDrop(map, 1046, 30, 1500000));
                        dropParameters.AddRange(generateDrop(map, 2520, 150, 1));
                        dropParameters.AddRange(generateDrop(map, 1252, 100, 1));
                        dropParameters.AddRange(generateDrop(map, 4051, 3, 1));
                        dropParameters.AddRange(generateDrop(map, 5931, 3, 1));
                        dropParameters.AddRange(generateDrop(map, 1244, 100, 10));
                        dropParameters.AddRange(generateDrop(map, 1904, 5, 1));
                        dropParameters.AddRange(generateDrop(map, 1252, 133, 3));
                        break;
                    case 8:
                        dropParameters.AddRange(generateDrop(map, 2282, 150, 10));
                        break;
                    case 9:
                        dropParameters.AddRange(generateDrop(map, 1046, 6, 4000000));
                        dropParameters.AddRange(generateDrop(map, 5372, 1, 1));
                        dropParameters.AddRange(generateDrop(map, 5498, 1, 1));
                        dropParameters.AddRange(generateDrop(map, 2440, 2, 1));
                        dropParameters.AddRange(generateDrop(map, 2518, 200, 1));
                        dropParameters.AddRange(generateDrop(map, 2511, 10, 5));
                        dropParameters.AddRange(generateDrop(map, 10123, 10, 5));
                        dropParameters.AddRange(generateDrop(map, 10113, 1, 1));
                        dropParameters.AddRange(generateDrop(map, 1364, 30, 1));
                        dropParameters.AddRange(generateDrop(map, 11020, 2, 1));
                        dropParameters.AddRange(generateDrop(map, 11021, 2, 1));
                        dropParameters.AddRange(generateDrop(map, 2512, 10, 5));
                        dropParameters.AddRange(generateDrop(map, 2513, 10, 5));
                        dropParameters.AddRange(generateDrop(map, 1252, 150, 5));
                        break;
                    case 10:
                        dropParameters.AddRange(generateDrop(map, 1046, 6, 30000000));
                        dropParameters.AddRange(generateDrop(map, 1011, 30, 5));
                        dropParameters.AddRange(generateDrop(map, 1030, 30, 1));
                        dropParameters.AddRange(generateDrop(map, 2282, 150, 3));
                        dropParameters.AddRange(generateDrop(map, 1244, 150, 10));
                        dropParameters.AddRange(generateDrop(map, 2514, 150, 1));
                        dropParameters.AddRange(generateDrop(map, 2518, 150, 1));
                        dropParameters.AddRange(generateDrop(map, 2517, 150, 1));
                        dropParameters.AddRange(generateDrop(map, 2521, 150, 1));
                        dropParameters.AddRange(generateDrop(map, 2516, 150, 1));
                        dropParameters.AddRange(generateDrop(map, 2520, 150, 1));
                        dropParameters.AddRange(generateDrop(map, 2515, 150, 1));
                        dropParameters.AddRange(generateDrop(map, 2519, 150, 1));
                        dropParameters.AddRange(generateDrop(map, 1252, 300, 5));
                        break;
                }
                return dropParameters;
            }


            //summon monsters by waves --> this is the 
            private static List<MonsterToSummon> getStoryBattleMonster(Map map, short instantbattletype, int wave, Tuple<MapInstance, byte> mapinstance)
            {   //mob ideas: 2300   
                List<MonsterToSummon> SummonParameters = new List<MonsterToSummon>();
                switch (wave)
                {
                    //monster id, how many of them, is it moving, put it in list -> ([id], [number], [moving], new List)
                    case 0: // castra (285) hellknight (724)
                        SummonParameters.AddRange(map.GenerateMonsters(285, 1, true, new List<EventContainer>()));
                        break;

                    case 1: //whiteYerti (2651) vampireMob x2 (464) swordspell x1 (2594) swordspell2 x1 (2595) meteorSmall x5(2591) largeMeteor x3 (2592)               
                        SummonParameters.AddRange(map.GenerateMonsters(11303, 1, true, new List<EventContainer>()));
                        SummonParameters.AddRange(map.GenerateMonsters(11304, 1, true, new List<EventContainer>()));

                        //SummonParameters.AddRange(map.GenerateMonsters(2595, 1, true, new List<EventContainer>()));
                        //SummonParameters.AddRange(map.GenerateMonsters(2591, 5, true, new List<EventContainer>()));
                        //SummonParameters.AddRange(map.GenerateMonsters(2592, 3, true, new List<EventContainer>()));
                        break;

                    case 2: //amora x10 (2641) peng x8 (2205) monkey x6 (2207)
                        SummonParameters.AddRange(map.GenerateMonsters(11305, 10, true, new List<EventContainer>()));
                        SummonParameters.AddRange(map.GenerateMonsters(11306, 8, true, new List<EventContainer>()));
                        SummonParameters.AddRange(map.GenerateMonsters(11307, 6, true, new List<EventContainer>()));
                        break;

                    case 3: // glacerus (2049) 370(cavallette) 1446 (small ice whitch)
                        SummonParameters.AddRange(map.GenerateMonsters(2049, 1, true, new List<EventContainer>())); //i am saying, summon glacerus (id 2049) x 1 times and put it as moving
                        SummonParameters.AddRange(map.GenerateMonsters(11309, 18, true, new List<EventContainer>()));
                        break;

                    case 4: //mukraju(556)
                        SummonParameters.AddRange(map.GenerateMonsters(11311, 1, true, new List<EventContainer>()));
                        break;

                    case 5: //194(bull) 482(demon dog) 436(devil lady)
                        SummonParameters.AddRange(map.GenerateMonsters(11312, 1, true, new List<EventContainer>()));
                        SummonParameters.AddRange(map.GenerateMonsters(11313, 20, true, new List<EventContainer>()));
                        SummonParameters.AddRange(map.GenerateMonsters(11314, 25, true, new List<EventContainer>()));
                        //SummonParameters.AddRange(map.GenerateMonsters(11015, 1, true, new List<EventContainer>()));
                        break;
                    case 6: //only those who find the 10 sacred souls and free them, shall proceed through the secret path

                        SummonParameters.AddRange(map.GenerateMonsters(2575, 10, false, new List<EventContainer>()));
                        SummonParameters.AddRange(map.GenerateMonsters(2591, 30, false, new List<EventContainer>()));
                        SummonParameters.AddRange(map.GenerateMonsters(2327, 5, false, new List<EventContainer>())); //laurena's beast
                        SummonParameters.AddRange(map.GenerateMonsters(2687, 1, false, new List<EventContainer>())); //laurena 
                        SummonParameters.AddRange(map.GenerateMonsters(10069, 2, false, new List<EventContainer>())); //lootbox
                        Thread.Sleep(1000);
                        //meteors are taken like monsters, but they cant be killed, so we take off the mobs and if the mobs on map is 30 (meteors number) then remove all mobs and say that the room is cleared
                        /*
                        for (int c = 0; c < 5000; c++)
                        {
                            //if every monster is dead in ic
                            if (mapinstance.Item1.Monsters.Count() <= 30)
                            {
                                mapinstance.Item1.MapClear();
                                //mapinstance.Item1.Monsters.RemoveRange(0, 30);
                                c = 5000;
                            }
                            Thread.Sleep(1000);
                        }
                        */
                        break;
                    case 7: //2591 meteors 11315 reve 11228 bride 2639 yerti
                        SummonParameters.AddRange(map.GenerateMonsters(11315, 1, true, new List<EventContainer>()));
                        SummonParameters.AddRange(map.GenerateMonsters(11228, 1, true, new List<EventContainer>()));
                        SummonParameters.AddRange(map.GenerateMonsters(2591, 15, false, new List<EventContainer>()));
                        SummonParameters.AddRange(map.GenerateMonsters(2639, 1, true, new List<EventContainer>()));

                        Thread.Sleep(1000);
                        break;
                    case 8:
                        //SummonParameters.AddRange(map.GenerateMonsters(2530, 1, true, new List<EventContainer>()));
                        break;
                    case 9:
                        SummonParameters.AddRange(map.GenerateMonsters(11300, 1, true, new List<EventContainer>()));//ugly useless draco
                        SummonParameters.AddRange(map.GenerateMonsters(11302, 25, true, new List<EventContainer>()));//mini bastard
                        SummonParameters.AddRange(map.GenerateMonsters(2574, 2, true, new List<EventContainer>()));//fernon
                        SummonParameters.AddRange(map.GenerateMonsters(11316, 1, true, new List<EventContainer>()));//fernon
                        break;
                    case 10:
                        SummonParameters.AddRange(map.GenerateMonsters(11301, 1, true, new List<EventContainer>()));
                        SummonParameters.AddRange(map.GenerateMonsters(11302, 10, true, new List<EventContainer>()));
                        SummonParameters.AddRange(map.GenerateMonsters(11021, 1, true, new List<EventContainer>())); // kertos
                        SummonParameters.AddRange(map.GenerateMonsters(11022, 1, true, new List<EventContainer>())); //greni
                        SummonParameters.AddRange(map.GenerateMonsters(11018, 3, true, new List<EventContainer>())); //greni
                        break;

                    case 100:
                        SummonParameters.AddRange(map.GenerateMonsters(724, 1, true, new List<EventContainer>()));
                        break;
                    case 101:
                        SummonParameters.AddRange(map.GenerateMonsters(11308, 5, true, new List<EventContainer>()));
                        break;
                    case 104:
                        SummonParameters.AddRange(map.GenerateMonsters(11310, 18, true, new List<EventContainer>()));
                        break;
                    case 110:
                        SummonParameters.AddRange(map.GenerateMonsters(11302, 25, true, new List<EventContainer>()));
                        break;
                    case 1001:
                        SummonParameters.AddRange(map.GenerateMonsters(2594, 30, true, new List<EventContainer>()));
                        break;


                    default:
                        break;
                }
                return SummonParameters;
            }

            //show history messages ---> wave is the round (room)
            //1439 -> sp6 skill
            private static List<MonsterToSummon> getHistoryMessage(Map map, short instantbattletype, int wave, Tuple<MapInstance, byte> mapinstance)
            {
                List<MonsterToSummon> SummonParameters = new List<MonsterToSummon>();
                ClientSession[] session_dialogue = new ClientSession[150];
                Random rnd = new Random();
                switch (wave)                                   // SHUT THE FUCK UP IF YOU ARE SAYING THECODE IS UGLY !!!!! I FUCKING DIDN'T HAVE ANY MORE FUCKING TIME TO IMPROVE IT WITH FUNCTIONS ETC. !!!!!!!!
                {
                    case 0: // castra (285) hellknight (724)
                        //mapinstance.Item1.Broadcast(UserInterfaceHelper.GenerateMsg("THE STORY OF OUR HEROES BEGINS", 0));6

                        // THE FUCKING SHOUTS
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(10), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("Our heroes' story begins...", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(15), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("Our heroes are searching for the seed that is creating chaos into their homeland", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(20), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("They've found from where the dark magic comes from", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(25), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("While they are trying to seal it...", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(30), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("They realise it's too late ! The dark mage is awaking !", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(35), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("Staying there and watching the mage appearing infront of their eyes..", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(40), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("is something no one would expect...", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(45), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("They have to stop it before it brings the chaos to the whole world !", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(47), new EventContainer(mapinstance.Item1, EventActionType.SPAWNMONSTERS, getStoryBattleMonster(mapinstance.Item1.Map, mapinstance.Item2, wave, mapinstance)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(50), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("They are fighting it braverly but seems like the mage", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(55), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("hasn't gotten his full power yet, that's our heroes..'", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(60), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("one and only chance to stop him ! Before it's too late...", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(65), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("Oh no ! The mage summons supporters: \"Come to me hell knight !\"", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(67), new EventContainer(mapinstance.Item1, EventActionType.SPAWNMONSTERS, getStoryBattleMonster(mapinstance.Item1.Map, mapinstance.Item2, (wave + 100), mapinstance)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(70), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("...Please heroes...SAVE US !", 0)));
                        //this  is for the people in the event, just do this:
                        //shit variable
                        // THE FUKING YELLOW TEXT AND YES I KNOW IT CAN BE IMPROVED BUT SHUT THE FUCK UP OR YOU IMPROVE IT BECAUS I HAVE NO FUCKING TIME TO WASTE
                        int i = 0;
                        foreach (ClientSession cli in mapinstance.Item1.Sessions.Where(s => s.Character != null).ToList())
                        {
                            session_dialogue[i] = cli;
                            i++;
                        }
                        int y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(10), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Our heroes' story begins...", 10)));

                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(15), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Our heroes are searching for the seed that is creating chaos into their homeland", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(15), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("I've found the problem here ! Look at this gem", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(20), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("They've found from where the dark magic comes from", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(20), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Quick Lucìa and Lina, use your sealing magic to remove the devil from this world !", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(25), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("While they are trying to seal it...", 10)));

                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(30), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("They realise it's too late ! The dark mage is awaking !", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(35), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Staying there and watching the mage appearing infront of their eyes..", 10)));

                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(40), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("is something no one would expect...", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(45), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("They have to stop it before it brings the chaos to the whole world !", 10)));

                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(50), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("They are fighting it braverly but seems like the mage", 10)));

                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(55), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("hasn't gotten his full power yet, that's our heroes..'", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(60), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("one and only chance to stop him ! Before it's too late...", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(65), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Oh no ! The mage summons supporters: \"Come to me hell knight !\"", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(70), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("...Please heroes...SAVE US !", 10)));

                        //THE FOKIN' DIALOGUES BETWEEN PLAYERS
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(10), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Wooah we made it to the room", 0)));
                        y = rnd.Next(0, i); //to change the person who is saying this, use a random choosing session
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(25), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("I...i can't seal it?! Wait...something is coming out !", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(35), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("We can't just watch ! We have to do something !", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(45), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Quick everyone ! Let's stop this evil while we are on time !", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(50), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("GO GO GO, LET'S DO IT C'MON PEOPLE !!!", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(65), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("What a loser, he really thinks he can stop us with this poor Knight ahaha", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(65), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("HAH EASY PEASY LEMON SQUEEZY", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(65), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("HOT HOT HOT HOT", 0)));
                        //bonus chats while time
                        y = rnd.Next(0, i);
                        int TalkerA = y;
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(90), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Wiggle for me Dark Mage", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(94), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("I don't think it is a good idea to say that to him mr....what was your name?", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(99), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[TalkerA].Character.GenerateSay("..Mr. Papi, call me Mr. Papi", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(102), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Can you stop acting like idiots and fight ?!", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(102), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Shut up you 2 ALREADY !!!", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(106), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[TalkerA].Character.GenerateSay("Bla Bla Bla BLAAAAAAA", 0)));


                        break;

                    case 1: //whiteYerti (2651) vampireMob x2 (464) swordspell x1 (2594) swordspell2 x1 (2595) meteorSmall x5(2591) largeMeteor x3 (2592)
                        //SummonParameters.AddRange(map.GenerateMonsters(464, 2, true, new List<EventContainer>()));
                        //SummonParameters.AddRange(map.GenerateMonsters(2651, 1, true, new List<EventContainer>()));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(10), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("The heroes got teleported into a magic field", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(15), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("They have no idea where they are.", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(20), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("Suddenly a bright light appears, from it something comes out...", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(25), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("It...seems like...like...a person ?", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(30), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("Suddenly they realised...it is the white mage !", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(32), new EventContainer(mapinstance.Item1, EventActionType.SPAWNMONSTERS, getStoryBattleMonster(mapinstance.Item1.Map, mapinstance.Item2, wave, mapinstance)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(35), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("He opens portals from hell where 5 demon imps appear", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(37), new EventContainer(mapinstance.Item1, EventActionType.SPAWNMONSTERS, getStoryBattleMonster(mapinstance.Item1.Map, mapinstance.Item2, (wave + 100), mapinstance)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(40), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("They suddenly start attacking our heroes", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(45), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("They are strong...and that magic field fills the white mage with high mana", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(50), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("Our heroes are in trouble ! Go heroes, you can do it !", 0)));

                        i = 0;
                        foreach (ClientSession cli in mapinstance.Item1.Sessions.Where(s => s.Character != null).ToList())
                        {
                            session_dialogue[i] = cli;
                            i++;
                        }
                        y = rnd.Next(0, i);

                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(10), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("The heroes got teleported into a magic field", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(15), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("They have no idea where they are.", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(20), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Suddenly a bright light appears, from it something comes out...", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(25), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("It...seems like...like...a person ?", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(30), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Suddenly they realised...it is the white mage !", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(35), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("He opens portals from hell where demon imps appear", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(40), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("They suddenly start attacking our heroes", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(45), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("They are strong...and that magic field fills the white mage with high mana", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(50), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Our heroes are in trouble ! Go heroes, you can do it !", 10)));

                        //talking
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(11), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Ouch Ouch Ouch i am DIZZYYYY", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(11), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("My head hurts >.<", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(11), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("This wiggle made me waggle and boom boom my head", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(15), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Also...where are we?", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(20), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Huh, what's that light?", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(25), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Is that...a person coming out from it?", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(30), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Nop...it's worse...i've heard the legend of this character...boiz, watch out", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(30), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Be careful !", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(36), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Y'all know, I WISH THIS WERE GIRLS ! This devils....FUTAAAAA", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(45), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("ARA ARA DEVIL-SAN IS ALONE IN THE CORNER", 0)));
                        int DialogA = y;
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(50), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("YOOO WTF YOU DOIN' YOU SICK F*CK", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(54), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[DialogA].Character.GenerateSay("IT'S MY LIFE, DON'T LOOK AT ME LIKE THAT, I JUST WANT SOME \"LOOOOVE\"", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(59), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("I really want to suicide right now...my brain will never forget this scene....", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(71), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("VROOOOOM VROOOOOM kick that mage", 0)));


                        break;

                    case 2: //amora x10 (2641) peng x8 (2205) monkey x6 (2207)
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(10), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("Suddenly our heroes wake up", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(15), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("they look around and realise they are in the middle of an Icy Desert", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(20), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("Their power is lowered because of the lightnings and the teleportation...", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(25), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("Soon as they recover, they have to get out of there, it is freezing !", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(30), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("While they walk, they realise something is not right with the snow..", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(35), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("They hear something but they can't see anything because of the tempest", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(40), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("Suddenly the tempest stops...but what to see...", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(45), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("a horder of monsters are coming toward our heroes !", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(50), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("They have to react fast and reply to the attack before it's too late !", 0)));
                        //summon the monster
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(52), new EventContainer(mapinstance.Item1, EventActionType.SPAWNMONSTERS, getStoryBattleMonster(mapinstance.Item1.Map, mapinstance.Item2, wave, mapinstance)));


                        i = 0;
                        foreach (ClientSession cli in mapinstance.Item1.Sessions.Where(s => s.Character != null).ToList())
                        {
                            session_dialogue[i] = cli;
                            i++;
                        }
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(10), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Suddenly our heroes wake up", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(15), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("they look around and realise they are in the middle of an Icy Desert", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(20), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Their power is lowered because of the lightnings and the teleportation...", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(25), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Soon as they recover, they have to get out of there, it is freezing !", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(30), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("While they walk, they realise something is not right with the snow..", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(35), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("They hear something but they can't see anything because of the tempest", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(40), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Suddenly the tempest stops...but what to see...", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(45), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("a horder of monsters are coming toward our heroes !", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(50), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("They have to react fast and reply to the attack before it's too late !", 10)));


                        //FromSecond -> from which second to start executing the dialogue | GenerateSay -> Dialogue || this are the only ones that you should modify
                        //To generate a new person for the dialog, just copy and past the y=rnd.Next(0,i); -> it takes another person that says the dialog from the event
                        //THE FOKIN' DIALOGUES BETWEEN PLAYERS
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(10), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Uff that was a niceeeee dreaaamm", 0)));
                        y = rnd.Next(0, i); //to change the person who is saying this, use a random choosing session //random person
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(11), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Pleaseee 5 minutes more...", 0))); //random person dialog
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(11), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Wake up!!!", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(14), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("I had a dream, we where... wait, we are on Icy Desert!!", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(18), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("I feel weak", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(27), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Wohoo, someone have wooly wool to get warm? It's so cold there", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(28), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Can u shut up idiot? Move ur ass", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(37), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Someone heard it?", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(40), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Finally tempest stop! Now we can... No way", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(45), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Why us? It's fine, get ready!", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(49), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("GO! We must fight!", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(70), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Uhm... Wait, why no one is moving?", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(80), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Well, seems that i must do it by my own..", 0)));

                        break;

                    case 3: // glacerus (2049) mobs and shouts are here for each room, { this is room 3 }
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(10), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("After killing every enemy, they continued forward", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(15), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("They see some kind of ancient stone with hieroglyphics on it", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(20), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("While they are reading it, they feel a strong cold breeze from behind", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(25), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("When they turned around...a big icy rock is coming towards them slowly", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(30), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("They look each other confused because they couldn't see it properly", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(35), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("after a few seconds, the wind became silent..and they saw it was not a rock...", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(40), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("it was the Icy Cold Guardian ! ..but..not in his full form ", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(45), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("It is told that the Byakko is just a mere form of the real guardian.", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(50), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("Our mighty heroes are getting ready to counter the \"fire\" if they get attacked", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(55), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("The Byakko is starting to attack, that's dangerous for our heroes !", 0)));
                        //summon the monster
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(57), new EventContainer(mapinstance.Item1, EventActionType.SPAWNMONSTERS, getStoryBattleMonster(mapinstance.Item1.Map, mapinstance.Item2, wave, mapinstance)));
                        //continue with shouts
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(60), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("It is told that if you survive long enough, the Byakko might lose his", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(65), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("power for a period of time, that's when our heroes can KILL IT !", 0)));

                        i = 0;
                        foreach (ClientSession cli in mapinstance.Item1.Sessions.Where(s => s.Character != null).ToList())
                        {
                            session_dialogue[i] = cli;
                            i++;
                        }
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(10), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("After killing every enemy, they continued forward", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(15), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("They see some kind of ancient stone with hieroglyphics on it", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(20), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("While they are reading it, they feel a strong cold breeze from behind", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(25), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("When they turned around...a big icy rock is coming towards them slowly", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(30), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("They look each other confused because they couldn't see it properly", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(35), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("after a few seconds, the wind became silent..and they saw it was not a rock...", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(40), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("it was the Icy Cold Guardian ! ..but..not in his full form ", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(45), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("It is told that the Byakko is just a mere form of the real guardian.", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(50), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Our mighty heroes are getting ready to counter the \"fire\" if they get attacked", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(55), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("The Byakko is starting to attack, that's dangerous for our heroes !", 10)));
                        //continue with shouts
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(60), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("It is told that if you survive long enough, the Byakko might lose his", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(65), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("power for a period of time, that's when our heroes can KILL IT !", 10)));

                        Thread.Sleep(1000);

                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(11), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("We did it!", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(14), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Wao, what's that?", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(18), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("It's an ancient stone, let me take a look", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(27), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Wait, i used maria? I see a rock moving, lol", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(28), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("U'r not on drugs, i see it too", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(37), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("But, what's that?!", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(40), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Seems we awakened the Guardian", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(45), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("It's Byakko!!", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(49), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Get redy guys, it will be hard", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(60), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Please, don't do anything stupid, we must survive", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(63), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Wait, these comment is for me?", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(66), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Uhm, nono. But don't do it", 0)));


                        break;

                    case 4:
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(10), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("Suddenly a blue light made our heroes blind for 2 seconds", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(15), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("When the effect was gone...they realised it is the true Guardian", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(20), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("They've never seen much strong and cold power", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(25), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("The guardian with his blue icy body starts doing something", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(30), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("It seems like he is summoning an unknown spirit", 0)));
                        //map,map, wave to summon, where to summon -> usually you change just the wave and the timer
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(32), new EventContainer(mapinstance.Item1, EventActionType.SPAWNMONSTERS, getStoryBattleMonster(mapinstance.Item1.Map, mapinstance.Item2, wave, mapinstance))); //we need wave 4
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(35), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("What..is..this? Blue bugs? Oh..no..that's worse !", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(40), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("This are the defenders of the Icy Cold Guardian !", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(42), new EventContainer(mapinstance.Item1, EventActionType.SPAWNMONSTERS, getStoryBattleMonster(mapinstance.Item1.Map, mapinstance.Item2, wave + 100, mapinstance)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(45), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("They look so strange and at the same time..soo..familiar", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(50), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("But don't be fooled by their look, they are strong !", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(55), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("I hope our heroes will be alright ! Go heroes, i am cheering for you !", 0)));
                        //SummonParameters.AddRange(map.GenerateMonsters(11015, 1, true, new List<EventContainer>()));

                        i = 0;
                        foreach (ClientSession cli in mapinstance.Item1.Sessions.Where(s => s.Character != null).ToList())
                        {
                            session_dialogue[i] = cli;
                            i++;
                        }
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(10), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Suddenly a blue light made our heroes blind for 2 seconds", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(15), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("When the effect was gone...they realised it is the true Guardian", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(20), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("They've never seen such strong and cold power", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(25), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("The guardian with his blue icy body starts doing something", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(30), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("It seems like he is summoning an unknown spirit", 10)));
                        //map,map, wave to summon, where to summon -> usually you change just the wave and the timer
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(35), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("What..is..this? Blue bugs? Oh..no..that's worse !", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(40), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("This are the defenders of the Icy Cold Guardian !", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(45), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("They look so strange and at the same time..soo..familiar", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(50), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("But don't be fooled by their look, they are strong !", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(55), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("I hope our heroes will be alright ! Go heroes, i am cheering for you !", 10)));

                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(10), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Ugh... Can someone see something?", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(15), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Wait, i see now", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(20), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("He seems so powerful", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(25), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("He is doing something, he's dancing?", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(30), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("No, you jerk. He's using his power!", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(35), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("He summoned something but... what's that?!", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(35), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("He summoned bugs! Iugh!", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(50), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Pay attention!!! We must fight!", 0)));


                        break;
                    case 5:
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(10), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("Finally they've found some cover into the cave", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(15), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("The continued to go deeper and found out a mine", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(20), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("As they go on, they notice that the mine seems very old", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(25), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("They hear a sound similar to footsteps and get into a cover immediately", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(30), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("While they are behind some rocks, they hear someone saying", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(35), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("\"Is the ritual ready ? \"", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(40), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("\"We have to get it done before our boss comes !\"", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(45), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("Our heroes realised immediately that something is going on..", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(50), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("Something evil !", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(55), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("When the footsteps are gone our heroes continued exploring", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(60), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("and searching for information about the ritual that they've heard", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(65), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("As they go on, they get into a small room and suddenly they hear", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(70), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("dog...barks ?... and they are becoming louder and louder !!", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(75), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("a voice can be heard: \"go my creatures, show no mercy for those who dare come here !\"", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(80), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("Wow, a powerful magician appears and with her, her powerful creatures !", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(82), new EventContainer(mapinstance.Item1, EventActionType.SPAWNMONSTERS, getStoryBattleMonster(mapinstance.Item1.Map, mapinstance.Item2, wave, mapinstance)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(85), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("As she attacks our heroes, she speaks: \"you are done now !\"", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(90), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("It is not easy my heroes, but if you can stun them, you can do it !", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(95), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("I believe in you !!!", 0)));

                        i = 0;
                        foreach (ClientSession cli in mapinstance.Item1.Sessions.Where(s => s.Character != null).ToList())
                        {
                            session_dialogue[i] = cli;
                            i++;
                        }
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(10), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Finally they've found some cover into the cave", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(15), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("The continued to go deeper and found out a mine", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(20), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("As they go on, they notice that the mine seems very old", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(25), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("They hear a sound similar to footsteps and get into a cover immediately", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(30), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("While they are behind some rocks, they hear someone saying", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(35), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("\"Is the ritual ready ? \"", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(40), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("\"We have to get it done before our boss comes !\"", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(45), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Our heroes realised immediately that something is going on..", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(50), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Something evil !", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(55), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("When the footsteps are gone our heroes continued exploring", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(60), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("and searching for information about the ritual that they've heard", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(65), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("As they go on, they get into a small room and suddenly they hear", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(70), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("dog...barks ?... and they are becoming louder and louder !!", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(75), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("a voice can be heard: \"go my creatures, show no mercy for those who dare come here !\"", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(80), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Wow, a powerful magician appears and with her, her powerful creatures !", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(85), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("As she attacks our heroes, she speaks: \"you are done now !\"", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(90), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("It is not easy my heroes, but if you can stun them, you can do it !", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(95), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("I believe in you !!!", 10)));

                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(10), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Finally, we can rest a bit", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(15), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("No, we must continue", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(20), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("These place seems too old, it's full of cobwebs", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(25), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("I hear something, hide, fast!", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(50), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("I have a bad feeling", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(55), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("They are gone? Cmon, we must continue", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(60), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Someone find something about the ritual?", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(62), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("I guess, nvm.. forget it", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(70), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Dog barks?! Are u kidding me?", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(90), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("We can deal with it guys, don't surrender", 0)));

                        break;
                    case 6:
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(10), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("Our Brave team wakes up from the fall and finds out it is not a team anymore", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(15), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("Everyone has been scattered in that labirinth", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(20), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("Who knows what traps are waiting in it...everyone should be careful", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(25), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("One of them sees a door on which is written the following:", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(30), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("\"Only those who manage to kill the 10 elite whitches can proceede\"", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(35), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("The hero luckily knew 'loud magic' so he used a spell to make his voice", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(40), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("Really loud and warns others so they can comunicate ", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(45), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("He was really smart so he knew how to manage a whole team just with a voice ", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(50), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("without waiting for a reply", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(55), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("I am sure our heroes will get out from there but seems like the whitches", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(60), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("are summoning meteors which can hurt our heroes !", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(62), new EventContainer(mapinstance.Item1, EventActionType.SPAWNMONSTERS, getStoryBattleMonster(mapinstance.Item1.Map, mapinstance.Item2, wave, mapinstance)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(65), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("I am sure that if our heroes manage to kill all the 10 mages", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(70), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("If they are lucky, someone of them might get a really good treasure reward !", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(75), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("This should be not so hard but keep in mind that you can live this adventure", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(80), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("just for a period of time so SPEED UP haha !", 0)));

                        i = 0;
                        foreach (ClientSession cli in mapinstance.Item1.Sessions.Where(s => s.Character != null).ToList())
                        {
                            session_dialogue[i] = cli;
                            i++;
                        }
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(10), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Our Brave team wakes up from the fall and finds out it is not a team anymore", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(15), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Everyone has been scattered in that labirinth", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(20), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Who knows what traps are waiting in it...everyone should be careful", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(25), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("One of them sees a door on which is written the following:", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(30), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("\"Only those who manage to kill the 10 elite whitches can proceede\"", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(35), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("The hero luckily knew 'loud magic' so he used a spell to make his voice", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(40), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Really loud and warns others so they can comunicate ", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(45), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("He was really smart so he knew how to manage a whole team just with a voice ", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(50), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("without waiting for a reply", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(55), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("I am sure our heroes will get out from there but seems like the whitches", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(60), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("are summoning meteors which can hurt our heroes !", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(65), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("I am sure that if our heroes manage to kill all the 10 mages", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(70), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("If they are lucky, someone of them might get a really good treasure reward !", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(75), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("This should be not so hard but keep in mind that you can live this adventure", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(80), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("just for a period of time so SPEED UP haha !", 10)));

                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(11), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Ouch, where is everyone?", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(16), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Hello ! Can anybody hear me?", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(16), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("This place is scary", 0)));
                        y = rnd.Next(0, i);
                        DialogA = y;
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(28), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Huh? A door?.. \"Only those who manage to kill the 10 elite whitches can proceede\"", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(33), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Maybe i can *huh* use this magic...Srastnit Lenoas Miriganis ZvurjiNi Ledan", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(38), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("~now i can hear everyone and lead them as one~", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(44), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("~YOOOO ! Shut up and GET OUT OF MY MIND !!", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(48), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Wait, GOD IS THAT YOU ?!", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(52), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("HURRAYYYYY !!! I FINALLY DID IT, I BECAME CRAZY HURRAYYYY~~~", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(56), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Angela...MY LOVE...I KNEW YOU WERE ALIVE", 0)));
                        y = DialogA;
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(60), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("What ?? I am not Angela wtf.......", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(60), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Listen everyone, there are falling meteors coming and i've read a sign that says", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(65), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("that we have to kill 10 whitches to continue...but i am sure we will have to kill everything we see", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(70), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("otherwise we won't be able to get out ! So watch your back..please people", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(75), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("You 3...go left...you 2...go right...you 5...do what you want", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(79), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Please, we have to be fast, we don't have much time ! I am counting on everyone! AS A GROUP !", 0)));

                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(80), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Great...not only that i am stuck in a labirynth but i am also a slave of my mind...just great", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(85), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Hehehehehehehe I can hide here and just waaait everything to be done", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(90), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Good that i've brought my manga with me...wait...this is hentai !! EVEN BETTER! *unzips pants*", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(100), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Uffff fine, i will share some of my treasures with you...", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(100), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("OOOH C'MON BRO..i just wanted to see some loli hentai....pfff FINE .", 0)));
                        y = DialogA;
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(95), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("WHAT THE ACTUAL FK ARE YOU DOING?!?! I AM IN YOUR MIND I CAN SEEE EVERYTHING WTFFFFF WHAT'S WRONG WITH YOU !!!!!!!", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(95), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("._. get back to work before i call the police...", 0)));

                        Thread.Sleep(82000);
                        //IMPROVE THIS UGLY METHOD OR I'LL CUT YOUR TITS
                        for (int c = 0; c < 5000; c++)
                        {
                            //if every monster is dead in ic
                            if (mapinstance.Item1.Monsters.Count() <= 31)
                            {
                                mapinstance.Item1.DespawnMonster(2591);
                                mapinstance.Item1.DespawnMonster(10069);
                                Thread.Sleep(1000);
                                c = 5000;
                            }
                            Thread.Sleep(1000);
                        }
                        break;
                    case 7:
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(10), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("As they get out from the cave, they notice they are in a bigger cave", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(15), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("supposingly in the core of a mountain", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(20), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("They see craters on the ground..that is never a good sign...", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(25), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("they hear something like murmuring behind a rock", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(30), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("They slowly aproach it and see a giant eating the corpses", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(35), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("of the fallen soldiers...it is scary and disgusting ", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(40), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("Unluckily the giant saw them, they knew who was the giant because", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(45), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("of the legends told in their homeland...", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(50), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("They stare there afraid of the Giant Reve", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(55), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("He was a creature so UGLY and strong that our heroes can't compete with it", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(60), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("The giant UGLY Reve starts screaming really loudly summoning meteors", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(65), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("The field is full of falling meteors, our heroes have to be careful", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(70), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("While the giant is rushing towards them in trying to eat them", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(75), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("A lady zombie appears, looks like she is his bribe !", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(80), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("They are a cute ugly couple together...but..they have to dissapear !", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(82), new EventContainer(mapinstance.Item1, EventActionType.SPAWNMONSTERS, getStoryBattleMonster(mapinstance.Item1.Map, mapinstance.Item2, wave, mapinstance)));

                        i = 0;
                        foreach (ClientSession cli in mapinstance.Item1.Sessions.Where(s => s.Character != null).ToList())
                        {
                            session_dialogue[i] = cli;
                            i++;
                        }
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(10), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("As they get out from the cave, they notice they are in a bigger cave", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(15), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("supposingly in the core of a mountain", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(20), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("They see craters on the ground..that is never a good sign...", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(25), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("they hear something like murmuring behind a rock", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(30), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("They slowly aproach it and see a giant eating the corpses", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(35), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("of the fallen soldiers...it is scary and disgusting ", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(40), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Unluckily the giant saw them, they knew who was the giant because", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(45), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("of the legends told in their homeland...", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(50), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("They stare there afraid of the Giant Reve", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(55), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("He was a creature so UGLY and strong that our heroes can't compete with it", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(60), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("The giant UGLY Reve starts screaming really loudly summoning meteors", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(65), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("The field is full of falling meteors, our heroes have to be careful", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(70), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("While the giant is rushing towards them in trying to eat them", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(75), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("A lady zombie appears, looks like she is his bribe !", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(80), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("They are a cute ugly couple together...but..they have to dissapear !", 10)));

                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(11), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("hoho interesting, a new cave hoh", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(12), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("ECHO - echo - ech...", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(13), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("I HAVE IT BIG", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(15), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("So many craters, so big...and so empty...like my heart", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(17), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Wait guys...silence for a second....", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(21), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Do you hear this?", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(26), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("It comes from behind this rock", 0)));
                        y = rnd.Next(0, i);
                        DialogA = y;
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(27), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("EWWWWWW WHAT IS THISSSSSS EWWWWWW", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(30), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("It is a body without head you retarded cunt !", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(34), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[DialogA].Character.GenerateSay("Not the body, mrs. Stupid brain, look AT THAT THING", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(39), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Not the body, mrs. Stupid brain, look AT THAT THING", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(44), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("WHAT THE EWWWWWWWW IS THAT THE FACEEE EWWWW I CAN'T LOOK AT IT, IS IT THE UGLY REVE FROM THE LEGENDS?", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(45), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("~ewwwwwwwww", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(46), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("*VOMITS*", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(47), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("*INTENSE POKING IN THE EYES*", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(48), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("SOMEBODY PLEASE PLEASE END MY SUFFERY", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(49), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("WHAT'S THAT UGLY THING *VOMITS*", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(50), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("BYE PEOPLE, I'VE SEEN EVERYTHING ON THIS UGLY PLANET", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(55), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Perfect, he can be my star for my new show 'THE UGLY MONSTER FROM THE UGLY TOWN'", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(58), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Hmmm.. *typing in browser* How to seduce the uglies thing on Earth", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(62), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Watch out people ! This ugly mutafakar is summoning meteors from his ugly dimention !", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(67), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("He is rushing towards us, quick use the 'Matador' move !!!", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(72), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Hey everyone! Look at this sexy ugly lady hue hue, I F*CK FIRST !!!", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(77), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("._. I wanna die...", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(83), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Cat, Dog, ASS, BLINDYNOSUCIDE NO JUTSU !", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(87), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("People...i am not the smartest one...but i think we have to kill Yerti first?", 0)));

                        Thread.Sleep(83000);
                        for (int c = 0; c < 5000; c++)
                        {
                            //if every monster is dead in ic
                            if (mapinstance.Item1.Monsters.Count() <= 15)
                            {
                                mapinstance.Item1.DespawnMonster(2591);
                                Thread.Sleep(1000);
                                c = 5000;
                            }
                            Thread.Sleep(1000);
                        }
                        break;

                    case 8:
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(10), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("As they walk up the stairs", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(15), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("They hear a strong roar from the top of the stairs", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(20), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("It seems really scary and the dark magic is everywhere in this place", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(25), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("Our heroes are near the top, get ready heroes !", 0)));

                        i = 0;
                        foreach (ClientSession cli in mapinstance.Item1.Sessions.Where(s => s.Character != null).ToList())
                        {
                            session_dialogue[i] = cli;
                            i++;
                        }
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(10), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("As they walk up the stairs", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(15), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("They hear a strong roar from the top of the stairs", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(20), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("It seems really scary and the dark magic is everywhere in this place", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(25), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Our heroes are near the top, get ready heroes !", 10)));

                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(10), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("I'm tired, can we do it another day?", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(10), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("I know it's tiring, but we are almost on top", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(63), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Shut up, u guys heard that?", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(63), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("I have a bad feeling, don't let your guard down!", 0)));

                        break;

                    case 9:
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(10), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("They got to the top and what to see..", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(15), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("A big dragon with hot breath is approaching them with his red eyes", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(20), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("It won't be an easy task to kill it", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(22), new EventContainer(mapinstance.Item1, EventActionType.SPAWNMONSTERS, getStoryBattleMonster(mapinstance.Item1.Map, mapinstance.Item2, wave, mapinstance)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(25), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("As they fight they realise there is something wrong with the dragon", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(30), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("It is like...like when he uses his full power", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(35), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("He needs some time to rest and while he is resting, his defenses are all down", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(40), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("This is the chance to kill it, use water magic HEROES !", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(45), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("Watch out for the fallin meteors that he summons !", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(50), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("Our heroes are so brave...so strong...so beautiful !", 0)));

                        i = 0;
                        foreach (ClientSession cli in mapinstance.Item1.Sessions.Where(s => s.Character != null).ToList())
                        {
                            session_dialogue[i] = cli;
                            i++;
                        }
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(10), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("They got to the top and what to see..", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(15), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("A big dragon with hot breath is approaching them with his red eyes", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(20), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("It won't be an easy task to kill it", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(25), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("As they fight they realise there is something wrong with the dragon", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(30), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("It is like...like when he uses his full power", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(35), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("He needs some time to rest and while he is resting, his defenses are all down", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(40), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("This is the chance to kill it, use water magic HEROES !", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(45), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Watch out for the fallin meteors that he summons !", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(50), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Our heroes are so brave...so strong...so beautiful !", 10)));

                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(15), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("We are on top, but... Is that a dragon?!", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(20), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("This is going to be hard", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(25), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("I know is not the moment, but i'm hungry", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(25), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Oh god.. Can u stop doing stupid comments and focus?", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(30), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("But i can cook with his fire!", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(30), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("I wish he eats u...", 0)));


                        break;

                    case 10:
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(10), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("As they wake up, they look around and understand they are", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(15), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("Into the core of earth...or better say, into a volcano !", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(20), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("They start hearing some strange sounds", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(25), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("One of our heroes says: \"I've a really bad feeling 'bout this\"", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(30), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("\"But we must continue and stop whatever is going on in this strange mountain !\"", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(35), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("Our heroes decided to continue with their adventure", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(40), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("They hear rumours above them !", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(45), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("One of them says \"Watch out people !Above you !!\"", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(50), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("When everyone looks up, they see something unexpectable", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(55), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("A big Fire Demon ! That is a very rare monster to find and it was told", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(60), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("that lives only in the most ancient legends", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(65), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("The demon is going to go out by flying up", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(70), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("\"No, he is going to get out from the mountain and he might destroy everything on its way !\"", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(75), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("One of our heroes uses strong water hurricane and brings the demon down", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(80), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("A BIG fight is coming ! Everyone, get your weapons, books and skills ready !", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(85), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("THE FIRE GODS ARE ANGRY !", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(87), new EventContainer(mapinstance.Item1, EventActionType.SPAWNMONSTERS, getStoryBattleMonster(mapinstance.Item1.Map, mapinstance.Item2, wave, mapinstance)));

                        //every 4 min
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(270), new EventContainer(mapinstance.Item1, EventActionType.SPAWNMONSTERS, getStoryBattleMonster(mapinstance.Item1.Map, mapinstance.Item2, wave + 100, mapinstance)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(450), new EventContainer(mapinstance.Item1, EventActionType.SPAWNMONSTERS, getStoryBattleMonster(mapinstance.Item1.Map, mapinstance.Item2, wave + 100, mapinstance)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(630), new EventContainer(mapinstance.Item1, EventActionType.SPAWNMONSTERS, getStoryBattleMonster(mapinstance.Item1.Map, mapinstance.Item2, wave + 100, mapinstance)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(810), new EventContainer(mapinstance.Item1, EventActionType.SPAWNMONSTERS, getStoryBattleMonster(mapinstance.Item1.Map, mapinstance.Item2, wave + 100, mapinstance)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(990), new EventContainer(mapinstance.Item1, EventActionType.SPAWNMONSTERS, getStoryBattleMonster(mapinstance.Item1.Map, mapinstance.Item2, wave + 100, mapinstance)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(1170), new EventContainer(mapinstance.Item1, EventActionType.SPAWNMONSTERS, getStoryBattleMonster(mapinstance.Item1.Map, mapinstance.Item2, wave + 100, mapinstance)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(1350), new EventContainer(mapinstance.Item1, EventActionType.SPAWNMONSTERS, getStoryBattleMonster(mapinstance.Item1.Map, mapinstance.Item2, wave + 100, mapinstance)));

                        i = 0;
                        foreach (ClientSession cli in mapinstance.Item1.Sessions.Where(s => s.Character != null).ToList())
                        {
                            session_dialogue[i] = cli;
                            i++;
                        }
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(10), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("As they wake up, they look around and understand they are", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(15), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("Into the core of earth...or better say, into a volcano !", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(20), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("They start hearing some strange sounds", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(25), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("One of our heroes says: \"I've a really bad feeling 'bout this\"", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(30), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("\"But we must continue and stop whatever is going on in this strange mountain !\"", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(35), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("Our heroes decided to continue with their adventure", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(40), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("They hear rumours above them !", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(45), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("One of them says \"Watch out people !Above you !!\"", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(50), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("When everyone looks up, they see something unexpectable", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(55), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("A big Fire Demon ! That is a very rare monster to find and it was told", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(60), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("that lives only in the most ancient legends", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(65), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("The demon is going to go out by flying up", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(70), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("\"No, he is going to get out from the mountain and he might destroy everything on its way !\"", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(75), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("One of our heroes uses strong water hurricane and brings the demon down", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(80), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("A BIG fight is coming ! Everyone, get your weapons, books and skills ready !", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(85), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("THE FIRE GODS ARE ANGRY", 10)));

                        Thread.Sleep(1000);

                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(10), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Huh, my head...", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(10), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Where are we?", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(63), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("We are inside a volcano!!!  ", 0)));


                        break;
                    // BONUS DIALOGUES WHEN THE MOBS ARE DEAD

                    case 100: //round 0 dialogues after monsters are dead
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(2), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("OH NO ! The dark mage is casting a spell before his death !", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(7), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("What is happening to our heroes ! THAT'S BAD !!...............", 0)));

                        i = 0;
                        foreach (ClientSession cli in mapinstance.Item1.Sessions.Where(s => s.Character != null).ToList())
                        {
                            session_dialogue[i] = cli;
                            i++;
                        }
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(2), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("OH NO ! The dark mage is casting a spell before his death !", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(2), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("What's...this...is it...dark..magic ? Why do i feel so weak...", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(7), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("What is happening to our heroes ! THAT'S BAD !!...............", 10)));

                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(4), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Wait...this spell...it seems familiar", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(5), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("WE ARE ALL GONNA DIEEEE ~WUAAAAAAA", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(8), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Somebody...PLEASE..make him shut the F*CK UP !!!!", 0)));

                        break;

                    case 101: //round 1 dialogues after monsters are dead
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(2), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("They did it ! They've killed the white mage !!!", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(7), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("But..something's wrong...the sky...what is this ?", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(12), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("OH NO, LIGHTNINGS !!! QUICK EVERYONE, COVER YOURSELF !", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(17), new EventContainer(mapinstance.Item1, EventActionType.SPAWNMONSTERS, getStoryBattleMonster(mapinstance.Item1.Map, mapinstance.Item2, 1001, mapinstance)));

                        i = 0;
                        foreach (ClientSession cli in mapinstance.Item1.Sessions.Where(s => s.Character != null).ToList())
                        {
                            session_dialogue[i] = cli;
                            i++;
                        }
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(2), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("They did it ! They've killed the white mage !!!", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(7), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("But..something's wrong...the sky...what is this ?", 10)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(8), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("YES YES YEEEEES WE ARE ALL GETTING ISEKAIEDDDDDD ", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(10), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("ISEKAI ME MADAFAKA", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(11), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("FINALLY MY WISH WILL BECOME REAL", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(12), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("HAREM HAREM....HERE I COME TO YOUUUUU VIVA LA HENTAI !!!!!", 0)));
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(13), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Somebody has another pair of pants? I think mine got dirty...", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(12), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("OH NO, LIGHTNINGS !!! QUICK EVERYONE, COVER YOURSELF !", 10)));

                        break;
                    case 102: //round 2 dialogues after monsters are dead
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(2), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("They did it, they survived the attack.", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(7), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("Our heroes rush forward hoping to see an exit from this Icy Hell", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(12), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("It is so cold that some of them are losing their will to walk...", 0)));

                        i = 0;
                        foreach (ClientSession cli in mapinstance.Item1.Sessions.Where(s => s.Character != null).ToList())
                        {
                            session_dialogue[i] = cli;
                            i++;
                        }
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(2), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("They did it, they survived the attack.", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(7), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Our heroes rush forward hoping to see an exit from this Icy Hell", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(12), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("It is so cold that some of them are losing their will to walk...", 10)));

                        break;
                    case 103: //round 3 dialogues after monsters are dead
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(2), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("Nice work heroes ! You've defeated Byakko...but the trouble", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(7), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("has just begun...", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(12), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("He is...he is transforming? Oh NO.....", 0)));

                        i = 0;
                        foreach (ClientSession cli in mapinstance.Item1.Sessions.Where(s => s.Character != null).ToList())
                        {
                            session_dialogue[i] = cli;
                            i++;
                        }
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(2), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Nice work heroes ! You've defeated Byakko...but the trouble", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(7), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("has just begun...", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(12), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("He is...he is transforming? Oh NO.....", 10)));

                        break;
                    case 104: //round 3 dialogues after monsters are dead
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(2), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("WOW THEY ACTUALLY DID IT !!!!", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(7), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("THEY REALLY KILLED THE GUARDIAN !", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(12), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("THIS WILL CHANGE THE HISTORY !", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(17), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("Our heroes did really well, there's no doubt but many of them", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(22), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("got hurt badly, they really have to find somewhere to rest", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(27), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("Between the white snow and the blue fog they see a cave", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(32), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("They run towards it hopefuly they can get some rest from the cold and the injuries", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(37), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("They really did a great job...good work HEROES !!", 0)));

                        i = 0;
                        foreach (ClientSession cli in mapinstance.Item1.Sessions.Where(s => s.Character != null).ToList())
                        {
                            session_dialogue[i] = cli;
                            i++;
                        }
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(2), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("WOW THEY ACTUALLY DID IT !!!!", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(7), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("THEY REALLY KILLED THE GUARDIAN !", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(12), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("THIS WILL CHANGE THE HISTORY !", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(17), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("Our heroes did really well, there's no doubt but many of them", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(22), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("got hurt9+ badly, they really have to find somewhere to rest", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(27), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("Between the white snow and the blue fog they see a cave", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(32), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("They run towards it hopefuly they can get some rest from the cold and the injuries", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(37), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("They really did a great job...good work HEROES !!", 10)));


                        break;
                    case 105: //round 3 dialogues after monsters are dead
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(2), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("They survived the ambush but they start hearing more barks !", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(7), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("Our heroes spurt towards an open entrance to another room", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(12), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("And sadly they reached an end point...they see they are on a hight cliff..", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(17), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("But they start seeing the dogs, they can't handle more of them so", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(22), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("They decided to test their luck...and jumped !", 0)));

                        i = 0;
                        foreach (ClientSession cli in mapinstance.Item1.Sessions.Where(s => s.Character != null).ToList())
                        {
                            session_dialogue[i] = cli;
                            i++;
                        }
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(2), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("They survived the ambush but they start hearing more barks !", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(7), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Our heroes spurt towards an open entrance to another room", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(12), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("And sadly they reached an end point...they see they are on a hight cliff..", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(17), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("But they start seeing the dogs, they can't handle more of them so", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(22), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("They decided to test their luck...and jumped !", 10)));

                        break;
                    case 106: //round 3 dialogues after monsters are dead
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(2), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("Nice a path has opened for our heroes !", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(7), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("It seems like they are getting into a big cave, not small anymore", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(12), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("Who knows what is waiting for them there", 0)));

                        i = 0;
                        foreach (ClientSession cli in mapinstance.Item1.Sessions.Where(s => s.Character != null).ToList())
                        {
                            session_dialogue[i] = cli;
                            i++;
                        }
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(2), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Nice a path has opened for our heroes !", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(7), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("It seems like they are getting into a big cave, not small anymore", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(12), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Who knows what is waiting for them there", 10)));

                        break;
                    case 107: //round 3 dialogues after monsters are dead
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(2), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("Good work heroes! After killing the Ugly Giant Reve, they saw a crack in the wall", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(7), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("They went through it and what to see...a new room with stairs", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(12), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("They are sure something was going on so they had to check", 0)));

                        i = 0;
                        foreach (ClientSession cli in mapinstance.Item1.Sessions.Where(s => s.Character != null).ToList())
                        {
                            session_dialogue[i] = cli;
                            i++;
                        }
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(2), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Good work heroes! After killing the Ugly Giant Reve, they saw a crack in the wall", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(7), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("They went through it and what to see...a new room with stairs", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(12), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("They are sure something was going on so they had to check", 10)));

                        break;
                    case 108: //round 7
                        break;
                    case 109: // round 8
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(2), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("Our heroes were near to kill the legendary Dragon but", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(7), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("Suddenly he spreads his wings and creates a rapid hot wind", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(12), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("Our heroes can't take the oxygen to their lungs !", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(17), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("This is really dangerous ! They are starting to collapse !", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(22), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("OH NO, THEY'VE FALLEN DOWN THE TREE !", 0)));

                        i = 0;
                        foreach (ClientSession cli in mapinstance.Item1.Sessions.Where(s => s.Character != null).ToList())
                        {
                            session_dialogue[i] = cli;
                            i++;
                        }
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(2), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Our heroes were near to kill the legendary Dragon but", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(7), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Suddenly he spreads his wings and creates a rapid hot wind", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(12), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("Our heroes can't take the oxygen to their lungs !", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(17), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("This is really dangerous ! They are starting to collapse !", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(22), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("OH NO, THEY'VE FALLEN DOWN THE TREE !", 10)));

                        break;
                    case 110: // | CHAPTER 11 |
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(2), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("THIS WAS THE END OF STORY 1", 0)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(7), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("TO BE CONTINUED.......", 0)));

                        i = 0;
                        foreach (ClientSession cli in mapinstance.Item1.Sessions.Where(s => s.Character != null).ToList())
                        {
                            session_dialogue[i] = cli;
                            i++;
                        }
                        y = rnd.Next(0, i);
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(2), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("THIS WAS THE END OF STORY 1", 10)));
                        EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(7), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, session_dialogue[y].Character.GenerateSay("TO BE CONTINUED.......", 10)));

                        break;

                    default:
                        break;
                }
                return SummonParameters;
            }

            /*
            private static List<MonsterToSummon> getInstantSubWaveMonster(Map map, short instantbattletype, int wave, int subwave)
            {
                List<MonsterToSummon> SummonParameters = new List<MonsterToSummon>();
                switch (wave)
                {
                    case 0: // castra (285) hellknight (724)
                        switch (subwave)
                        {
                            case 1:
                                SummonParameters.AddRange(map.GenerateMonsters(556, 1, true, new List<EventContainer>()));
                                break;
                            case 2:
                                SummonParameters.AddRange(map.GenerateMonsters(2510, 1, true, new List<EventContainer>()));
                                break;
                        }
                        break;

                    case 1: //whiteYerti (2651) vampireMob x2 (464) swordspell x1 (2594) swordspell2 x1 (2595) meteorSmall x5(2591) largeMeteor x3 (2592)
                        SummonParameters.AddRange(map.GenerateMonsters(464, 2, true, new List<EventContainer>()));
                        SummonParameters.AddRange(map.GenerateMonsters(2651, 1, true, new List<EventContainer>()));
                        //SummonParameters.AddRange(map.GenerateMonsters(2595, 1, true, new List<EventContainer>()));
                        //SummonParameters.AddRange(map.GenerateMonsters(2591, 5, true, new List<EventContainer>()));
                        //SummonParameters.AddRange(map.GenerateMonsters(2592, 3, true, new List<EventContainer>()));
                        break;

                    case 2: //amora x10 (2641) peng x8 (2205) monkey x6 (2207)
                        SummonParameters.AddRange(map.GenerateMonsters(2641, 10, true, new List<EventContainer>()));
                        SummonParameters.AddRange(map.GenerateMonsters(2205, 8, true, new List<EventContainer>()));
                        SummonParameters.AddRange(map.GenerateMonsters(2207, 6, true, new List<EventContainer>()));
                        break;

                    case 3: // mukraju (556)  changemob(2510)
                        switch (subwave)
                        {
                            case 1:
                                SummonParameters.AddRange(map.GenerateMonsters(556, 1, true, new List<EventContainer>()));
                                break;
                            case 2:
                                SummonParameters.AddRange(map.GenerateMonsters(2510, 1, true, new List<EventContainer>()));
                                break;
                        }
                        break;

                    case 4:
                        SummonParameters.AddRange(map.GenerateMonsters(2530, 1, true, new List<EventContainer>()));
                        //SummonParameters.AddRange(map.GenerateMonsters(11015, 1, true, new List<EventContainer>()));
                        break;
                    case 5:
                        SummonParameters.AddRange(map.GenerateMonsters(2530, 1, true, new List<EventContainer>()));
                        break;
                    case 6:
                        SummonParameters.AddRange(map.GenerateMonsters(2530, 1, true, new List<EventContainer>()));
                        break;
                    case 7:
                        SummonParameters.AddRange(map.GenerateMonsters(2530, 1, true, new List<EventContainer>()));
                        break;
                    case 8:
                        SummonParameters.AddRange(map.GenerateMonsters(2530, 1, true, new List<EventContainer>()));
                        break;
                }
                return SummonParameters;
            } */
            #endregion
        }
        #endregion
    }
}