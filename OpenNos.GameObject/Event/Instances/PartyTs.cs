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
    public static class PTS
    {
        #region Methods

        public static void GeneratePTS(int Vnum, ClientSession host)
        {
            List<ClientSession> sessions = new List<ClientSession>();
            if (host.Character.Group != null)
            {
                sessions = host.Character.Group.Sessions.Where(s => s.Character.MapInstance.MapInstanceType == MapInstanceType.ExpMap);
            }
            else
            {
                sessions.Add(host);
            }
            List<Tuple<MapInstance, byte>> maps = new List<Tuple<MapInstance, byte>>();
            MapInstance map = null;
            byte instancelevel = 1;
            if (Vnum == 1805)
            {
                instancelevel = 55;
            }
            if (Vnum == 1824)
            {
                instancelevel = 65;
            }
            if (Vnum == 1825)
            {
                instancelevel = 75;
            }
            if (Vnum == 1826)
            {
                instancelevel = 80;
            }
            if (Vnum == 1827)
            {
                instancelevel = 88;
            }
            map = ServerManager.GenerateMapInstance(2100, MapInstanceType.ExpMap, new InstanceBag());
            maps.Add(new Tuple<MapInstance, byte>(map, instancelevel));

            if (map != null)
            {
                foreach (ClientSession s in sessions)
                {
                    ServerManager.Instance.TeleportOnRandomPlaceInMap(s, map.MapInstanceId);
                }
            }

            foreach (Tuple<MapInstance, byte> mapinstance in maps)
            {
                PSTTask task = new PSTTask();
                Observable.Timer(TimeSpan.FromMinutes(0)).Subscribe(X => PSTTask.Run(mapinstance));
            }
        }

        #endregion

        #region Classes

        public class PSTTask
        {
            #region Methods

            public static void Run(Tuple<MapInstance, byte> mapinstance)
            {
                long maxGold = ServerManager.Instance.Configuration.MaxGold;
                Thread.Sleep(10 * 1000);
                Observable.Timer(TimeSpan.FromMinutes(2.6)).Subscribe(X =>
                {
                    for (int d = 0; d < 60; d++)
                    {
                        if (!mapinstance.Item1.Monsters.Any(s => s.CurrentHp > 0))
                        {
                            EventHelper.Instance.ScheduleEvent(TimeSpan.FromMinutes(0), new EventContainer(mapinstance.Item1, EventActionType.SPAWNPORTAL, new Portal { SourceX = 33, SourceY = 34, DestinationMapId = 1 }));
                            mapinstance.Item1.Broadcast(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("PTS_SUCCEEDED"), 0));
                            foreach (ClientSession cli in mapinstance.Item1.Sessions.Where(s => s.Character != null).ToList())
                            {
                                cli.Character.GenerateFamilyXp(cli.Character.SwitchLevel() * 4);
                                cli.Character.SpAdditionPoint += cli.Character.SwitchLevel() * 100;
                                cli.Character.SpAdditionPoint = cli.Character.SpAdditionPoint > 20000 ? 20000 : cli.Character.SpAdditionPoint;
                                cli.SendPacket(cli.Character.GenerateSpPoint());
                                cli.SendPacket(cli.Character.GenerateGold());
                                cli.SendPacket(cli.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("WIN_FXP"), 25), 10));
                                cli.SendPacket(cli.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("WIN_SP_POINT"), cli.Character.SwitchLevel() * 100), 10));
                            }
                            break;
                        }
                        Thread.Sleep(1000);
                    }
                });

                EventHelper.Instance.ScheduleEvent(TimeSpan.FromMinutes(4), new EventContainer(mapinstance.Item1, EventActionType.DISPOSEMAP, null));
                EventHelper.Instance.ScheduleEvent(TimeSpan.FromMinutes(1), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("PTS_MINUTES_REMAINING"), 2), 0)));
                EventHelper.Instance.ScheduleEvent(TimeSpan.FromMinutes(2), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("PTS_MINUTES_REMAINING"), 1), 0)));
                EventHelper.Instance.ScheduleEvent(TimeSpan.FromMinutes(2.5), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("PTS_SECONDS_REMAINING"), 30), 0)));
                EventHelper.Instance.ScheduleEvent(TimeSpan.FromMinutes(2.5), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("PTS_SECONDS_REMAINING"), 30), 0)));
                EventHelper.Instance.ScheduleEvent(TimeSpan.FromMinutes(0), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("PTS_MONSTERS_INCOMING"), 0)));
                EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(10), new EventContainer(mapinstance.Item1, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("PTS_MONSTERS_HERE"), 0)));

                for (int wave = 0; wave < 3; wave++)
                {
                    EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(10 + (wave * 60)), new EventContainer(mapinstance.Item1, EventActionType.SPAWNMONSTERS, getInstantBattleMonster(mapinstance.Item1.Map, mapinstance.Item2, wave)));
                }
            }


            private static List<MonsterToSummon> getInstantBattleMonster(Map map, short instantbattletype, int wave)
            {
                List<MonsterToSummon> SummonParameters = new List<MonsterToSummon>();

                switch (instantbattletype)
                {
                    case 55:
                        switch (wave)
                        {
                            case 0:
                                SummonParameters.AddRange(map.GenerateMonsters(44, 10, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(242, 10, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(227, 15, true, new List<EventContainer>()));
                                break;

                            case 1:
                                SummonParameters.AddRange(map.GenerateMonsters(44, 10, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(227, 15, true, new List<EventContainer>()));
                                break;

                            case 2:
                                SummonParameters.AddRange(map.GenerateMonsters(44, 10, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(242, 10, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(227, 15, true, new List<EventContainer>()));
                                break;
                        }
                        break;
                    case 65:
                        switch (wave)
                        {
                            case 0:
                                SummonParameters.AddRange(map.GenerateMonsters(481, 10, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(472, 10, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(485, 15, true, new List<EventContainer>()));
                                break;

                            case 1:
                                SummonParameters.AddRange(map.GenerateMonsters(481, 10, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(472, 10, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(485, 15, true, new List<EventContainer>()));
                                break;

                            case 2:
                                SummonParameters.AddRange(map.GenerateMonsters(481, 10, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(472, 10, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(485, 15, true, new List<EventContainer>()));
                                break;
                        }
                        break;
                    case 75:
                        switch (wave)
                        {
                            case 0:
                                SummonParameters.AddRange(map.GenerateMonsters(1244, 10, false, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(1123, 10, false, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(1252, 15, false, new List<EventContainer>()));
                                break;

                            case 1:
                                SummonParameters.AddRange(map.GenerateMonsters(1187, 10, false, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(1191, 10, false, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(1195, 15, false, new List<EventContainer>()));
                                break;

                            case 2:
                                SummonParameters.AddRange(map.GenerateMonsters(1016, 1, false, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(1017, 1, false, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(1018, 1, false, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(1019, 1, false, new List<EventContainer>()));
                                break;
                        }
                        break;
                    case 80:
                        switch (wave)
                        {
                            case 0:
                                SummonParameters.AddRange(map.GenerateMonsters(1131, 10, false, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(1132, 10, false, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(1133, 15, false, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(1134, 10, false, new List<EventContainer>()));
                                break;

                            case 1:
                                SummonParameters.AddRange(map.GenerateMonsters(1167, 10, false, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(1168, 10, false, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(1169, 15, false, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(1170, 10, false, new List<EventContainer>()));
                                break;

                            case 2:
                                SummonParameters.AddRange(map.GenerateMonsters(1019, 10, false, new List<EventContainer>()));
                                break;
                        }
                        break;
                    case 88:
                        switch (wave)
                        {
                            case 0:
                                SummonParameters.AddRange(map.GenerateMonsters(242, 10, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(237, 10, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(246, 15, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(248, 10, true, new List<EventContainer>()));
                                break;

                            case 1:
                                SummonParameters.AddRange(map.GenerateMonsters(248, 15, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(246, 15, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(234, 15, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(221, 10, true, new List<EventContainer>()));
                                break;

                            case 2:
                                SummonParameters.AddRange(map.GenerateMonsters(225, 15, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(221, 15, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(207, 15, true, new List<EventContainer>()));
                                SummonParameters.AddRange(map.GenerateMonsters(234, 15, true, new List<EventContainer>()));
                                break;
                        }
                        break;
                }
                return SummonParameters;
            }

            #endregion
        }

        #endregion
    }
}