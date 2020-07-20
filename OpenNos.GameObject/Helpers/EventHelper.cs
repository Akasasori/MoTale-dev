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
using OpenNos.GameObject.Event;
using OpenNos.GameObject.Event.GAMES;
using OpenNos.GameObject.Event.ARENA;
using OpenNos.PathFinder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using OpenNos.GameObject.Networking;
using OpenNos.Master.Library.Client;
using OpenNos.Master.Library.Data;
using OpenNos.Data;
namespace OpenNos.GameObject.Helpers
{
    public class EventHelper
    {
        #region Members

        private static EventHelper _instance;

        public static MapInstance Map { get; private set; }

        public Map map { get; set; }

        #endregion

        #region Properties

        public static EventHelper Instance => _instance ?? (_instance = new EventHelper());

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

        #endregion

        #region Methods

        public static int CalculateComboPoint(int n)
        {
            int a = 4;
            int b = 7;
            for (int i = 0; i < n; i++)
            {
                int temp = a;
                a = b;
                b = temp + b;
            }
            return a;
        }

        public static void GenerateEvent(EventType type, int LvlBracket = -1)
        {
            if (type == EventType.ICEBREAKER && LvlBracket < 0)
            {
                return;
            }

            try
            {
                if (!ServerManager.Instance.StartedEvents.Contains(type))
                {
                    Task.Factory.StartNew(() =>
                    {
                        ServerManager.Instance.StartedEvents.Add(type);
                        switch (type)
                        {
                            case EventType.RANKINGREFRESH:
                                ServerManager.Instance.RefreshRanking();
                                ServerManager.Instance.StartedEvents.Remove(type);
                                break;

                            case EventType.LOD:
                                if (ServerManager.Instance.ChannelId != 51)
                                {
                                    LOD.GenerateLod();
                                }
                                else
                                {
                                    ServerManager.Instance.StartedEvents.Remove(type);
                                    return;
                                }
                                break;

                            case EventType.MINILANDREFRESHEVENT:
                                MinilandRefresh.GenerateMinilandEvent();
                                break;

                            case EventType.INSTANTBATTLE:
                                if (ServerManager.Instance.ChannelId != 51)
                                {
                                    InstantBattle.GenerateInstantBattle();
                                }
                                else
                                {
                                    ServerManager.Instance.StartedEvents.Remove(type);
                                    return;
                                }
                                break;

                            case EventType.SHEEPGAME:
                                if (ServerManager.Instance.ChannelId != 51)
                                {
                                    SheepGame.GenerateSheepGames();
                                }
                                else
                                {
                                    ServerManager.Instance.StartedEvents.Remove(type);
                                    return;
                                }
                                break;

                            case EventType.METEORITEGAME:
                                if (ServerManager.Instance.ChannelId != 51)
                                {
                                    MeteoriteGame.GenerateMeteoriteGame();
                                }
                                else
                                {
                                    ServerManager.Instance.StartedEvents.Remove(type);
                                    return;
                                }
                                break;

                            case EventType.ACT4SHIP:
                                ACT4SHIP.GenerateAct4Ship(1);
                                ACT4SHIP.GenerateAct4Ship(2);
                                break;

                            case EventType.TALENTARENA:
                                if (ServerManager.Instance.ChannelId == 1)
                                {
                                    ArenaEvent.GenerateTalentArena();
                                }
                                else
                                {
                                    ServerManager.Instance.StartedEvents.Remove(type);
                                    return;
                                }
                                break;

                            case EventType.CALIGOR:
                                if (ServerManager.Instance.ChannelId == 51)
                                {
                                    CaligorRaid.Run();
                                }
                                else
                                {
                                    ServerManager.Instance.StartedEvents.Remove(type);
                                    return;
                                }
                                break;

                            case EventType.ICEBREAKER:
                                if (ServerManager.Instance.ChannelId != 51)
                                {
                                    IceBreaker.GenerateIceBreaker(LvlBracket);
                                }
                                else
                                {
                                    ServerManager.Instance.StartedEvents.Remove(type);
                                    return;
                                }
                                break;

                            case EventType.RAINBOWBATTLE:
                                if (ServerManager.Instance.ChannelId != 51)
                                {
                                    Event.RAINBOWBATTLE.RainbowBattle.GenerateEvent();
                                }
                                else
                                {
                                    ServerManager.Instance.StartedEvents.Remove(type);
                                    return;
                                }
                                break;

                            case EventType.STORYEVENT:
                                if (ServerManager.Instance.ChannelId != 51)
                                {
                                    StoryEvent.GenerateStoryEvent();
                                }
                                else
                                {
                                    ServerManager.Instance.StartedEvents.Remove(type);
                                    return;
                                }
                                break;

                            case EventType.WORLDBOSS:
                                if (ServerManager.Instance.ChannelId == 1)
                                {
                                    WorldRad.Run();
                                }
                                break;

                            case EventType.DAILYREWARDREFRESH:
                                ServerManager.Instance.RefreshDailyReward();
                                ServerManager.Instance.StartedEvents.Remove(EventType.DAILYREWARDREFRESH);
                                break;

                            case EventType.SHOPSHIP:
                                if(ServerManager.Instance.ChannelId == 1)
                                {
                                    ShopShipev.Run();
                                }
                                break;
                        }
                    });
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"ErrorEvent" + ex);
            }

        }

        public static TimeSpan GetMilisecondsBeforeTime(TimeSpan time)
        {
            TimeSpan now = TimeSpan.Parse(DateTime.Now.ToString("HH:mm"));
            TimeSpan timeLeftUntilFirstRun = time - now;
            if (timeLeftUntilFirstRun.TotalHours < 0)
            {
                timeLeftUntilFirstRun += new TimeSpan(24, 0, 0);
            }
            return timeLeftUntilFirstRun;
        }

        public void RunEvent(EventContainer evt, ClientSession session = null, MapMonster monster = null, MapNpc npc = null)
        {
            if (evt != null)
            {
                if (session != null)
                {
                    evt.MapInstance = session.CurrentMapInstance;
                    switch (evt.EventActionType)
                    {
                        #region EventForUser

                        case EventActionType.NPCDIALOG:
                            session.SendPacket(session.Character.GenerateNpcDialog((int)evt.Parameter));
                            break;

                        case EventActionType.SENDPACKET:
                            session.SendPacket((string)evt.Parameter);
                            break;

                            #endregion
                    }
                }
                if (evt.MapInstance != null)
                {
                    switch (evt.EventActionType)
                    {
                        #region EventForUser

                        case EventActionType.NPCDIALOG:
                        case EventActionType.SENDPACKET:
                            if (session == null)
                            {
                                evt.MapInstance.Sessions.ToList().ForEach(e => RunEvent(evt, e));
                            }
                            break;

                        #endregion

                        #region MapInstanceEvent

                        case EventActionType.REGISTEREVENT:
                            Tuple<string, List<EventContainer>> even = (Tuple<string, List<EventContainer>>)evt.Parameter;
                            switch (even.Item1)
                            {
                                case "OnCharacterDiscoveringMap":
                                    even.Item2.ForEach(s => evt.MapInstance.OnCharacterDiscoveringMapEvents.Add(new Tuple<EventContainer, List<long>>(s, new List<long>())));
                                    break;

                                case "OnMoveOnMap":
                                    evt.MapInstance.OnMoveOnMapEvents.AddRange(even.Item2);
                                    break;

                                case "OnMapClean":
                                    evt.MapInstance.OnMapClean.AddRange(even.Item2);
                                    break;

                                case "OnLockerOpen":
                                    evt.MapInstance.UnlockEvents.AddRange(even.Item2);
                                    break;
                            }
                            break;

                        case EventActionType.REGISTERWAVE:
                            evt.MapInstance.WaveEvents.Add((EventWave)evt.Parameter);
                            break;

                        case EventActionType.SETAREAENTRY:
                            ZoneEvent even2 = (ZoneEvent)evt.Parameter;
                            evt.MapInstance.OnAreaEntryEvents.Add(even2);
                            break;

                        case EventActionType.REMOVEMONSTERLOCKER:
                            EventContainer evt2 = (EventContainer)evt.Parameter;
                            if (evt.MapInstance.InstanceBag.MonsterLocker.Current > 0)
                            {
                                evt.MapInstance.InstanceBag.MonsterLocker.Current--;
                            }
                            if (evt.MapInstance.InstanceBag.MonsterLocker.Current == 0 && evt.MapInstance.InstanceBag.ButtonLocker.Current == 0)
                            {
                                List<EventContainer> UnlockEventsCopy = evt.MapInstance.UnlockEvents.ToList();
                                UnlockEventsCopy.ForEach(e => RunEvent(e));
                                evt.MapInstance.UnlockEvents.RemoveAll(s => s != null && UnlockEventsCopy.Contains(s));
                            }
                            break;

                        case EventActionType.REMOVEBUTTONLOCKER:
                            evt2 = (EventContainer)evt.Parameter;
                            if (evt.MapInstance.InstanceBag.ButtonLocker.Current > 0)
                            {
                                evt.MapInstance.InstanceBag.ButtonLocker.Current--;
                            }
                            if (evt.MapInstance.InstanceBag.MonsterLocker.Current == 0 && evt.MapInstance.InstanceBag.ButtonLocker.Current == 0)
                            {
                                List<EventContainer> UnlockEventsCopy = evt.MapInstance.UnlockEvents.ToList();
                                UnlockEventsCopy.ForEach(e => RunEvent(e));
                                evt.MapInstance.UnlockEvents.RemoveAll(s => s != null && UnlockEventsCopy.Contains(s));
                            }
                            break;

                        case EventActionType.EFFECT:
                            short evt3 = (short)evt.Parameter;
                            if (monster != null)
                            {
                                monster.LastEffect = DateTime.Now;
                                evt.MapInstance.Broadcast(StaticPacketHelper.GenerateEff(UserType.Monster, monster.MapMonsterId, evt3));
                            }
                            break;

                        case EventActionType.CONTROLEMONSTERINRANGE:
                            if (monster != null)
                            {
                                Tuple<short, byte, List<EventContainer>> evnt = (Tuple<short, byte, List<EventContainer>>)evt.Parameter;
                                List<MapMonster> MapMonsters = evt.MapInstance.GetMonsterInRangeList(monster.MapX, monster.MapY, evnt.Item2);
                                if (evnt.Item1 != 0)
                                {
                                    MapMonsters.RemoveAll(s => s.MonsterVNum != evnt.Item1);
                                }
                                MapMonsters.ForEach(s => evnt.Item3.ForEach(e => RunEvent(e, monster: s)));
                            }
                            break;

                        case EventActionType.ONTARGET:
                            if (monster.MoveEvent?.InZone(monster.MapX, monster.MapY) == true)
                            {
                                monster.MoveEvent = null;
                                monster.Path = new List<Node>();
                                ((List<EventContainer>)evt.Parameter).ForEach(s => RunEvent(s, monster: monster));
                            }
                            break;

                        case EventActionType.MOVE:
                            ZoneEvent evt4 = (ZoneEvent)evt.Parameter;
                            if (monster != null)
                            {
                                monster.MoveEvent = evt4;
                                monster.Path = BestFirstSearch.FindPathJagged(new Node { X = monster.MapX, Y = monster.MapY }, new Node { X = evt4.X, Y = evt4.Y }, evt.MapInstance?.Map.JaggedGrid);
                                monster.RunToX = evt4.X;
                                monster.RunToY = evt4.Y;
                            }
                            else if (npc != null)
                            {
                                //npc.MoveEvent = evt4;
                                npc.Path = BestFirstSearch.FindPathJagged(new Node { X = npc.MapX, Y = npc.MapY }, new Node { X = evt4.X, Y = evt4.Y }, evt.MapInstance?.Map.JaggedGrid);
                                npc.RunToX = evt4.X;
                                npc.RunToY = evt4.Y;
                            }
                            break;

                        case EventActionType.STARTACT4RAIDWAVES:
                            IDisposable spawnsDisposable = null;
                            spawnsDisposable = Observable.Interval(TimeSpan.FromSeconds(60)).Subscribe(s =>
                            {
                                int count = evt.MapInstance.Sessions.Count();

                                if (count <= 0)
                                {
                                    spawnsDisposable.Dispose();
                                    return;
                                }

                                if (count > 5)
                                {
                                    count = 5;
                                }
                                List<MonsterToSummon> mobWave = new List<MonsterToSummon>();
                                for (int i = 0; i < count; i++)
                                {
                                    switch (evt.MapInstance.MapInstanceType)
                                    {
                                        case MapInstanceType.Act4Morcos:
                                            mobWave.Add(new MonsterToSummon(561, evt.MapInstance.Map.GetRandomPosition(), null, true));
                                            mobWave.Add(new MonsterToSummon(561, evt.MapInstance.Map.GetRandomPosition(), null, true));
                                            mobWave.Add(new MonsterToSummon(561, evt.MapInstance.Map.GetRandomPosition(), null, true));
                                            mobWave.Add(new MonsterToSummon(562, evt.MapInstance.Map.GetRandomPosition(), null, true));
                                            mobWave.Add(new MonsterToSummon(562, evt.MapInstance.Map.GetRandomPosition(), null, true));
                                            mobWave.Add(new MonsterToSummon(562, evt.MapInstance.Map.GetRandomPosition(), null, true));
                                            mobWave.Add(new MonsterToSummon(851, evt.MapInstance.Map.GetRandomPosition(), null, false));
                                            break;

                                        case MapInstanceType.Act4Hatus:
                                            mobWave.Add(new MonsterToSummon(574, evt.MapInstance.Map.GetRandomPosition(), null, true));
                                            mobWave.Add(new MonsterToSummon(574, evt.MapInstance.Map.GetRandomPosition(), null, true));
                                            mobWave.Add(new MonsterToSummon(575, evt.MapInstance.Map.GetRandomPosition(), null, true));
                                            mobWave.Add(new MonsterToSummon(575, evt.MapInstance.Map.GetRandomPosition(), null, true));
                                            mobWave.Add(new MonsterToSummon(576, evt.MapInstance.Map.GetRandomPosition(), null, true));
                                            mobWave.Add(new MonsterToSummon(576, evt.MapInstance.Map.GetRandomPosition(), null, true));
                                            break;

                                        case MapInstanceType.Act4Calvina:
                                            mobWave.Add(new MonsterToSummon(770, evt.MapInstance.Map.GetRandomPosition(), null, true));
                                            mobWave.Add(new MonsterToSummon(770, evt.MapInstance.Map.GetRandomPosition(), null, true));
                                            mobWave.Add(new MonsterToSummon(770, evt.MapInstance.Map.GetRandomPosition(), null, true));
                                            mobWave.Add(new MonsterToSummon(771, evt.MapInstance.Map.GetRandomPosition(), null, true));
                                            mobWave.Add(new MonsterToSummon(771, evt.MapInstance.Map.GetRandomPosition(), null, true));
                                            mobWave.Add(new MonsterToSummon(771, evt.MapInstance.Map.GetRandomPosition(), null, true));
                                            break;

                                        case MapInstanceType.Act4Berios:
                                            mobWave.Add(new MonsterToSummon(780, evt.MapInstance.Map.GetRandomPosition(), null, true));
                                            mobWave.Add(new MonsterToSummon(781, evt.MapInstance.Map.GetRandomPosition(), null, true));
                                            mobWave.Add(new MonsterToSummon(782, evt.MapInstance.Map.GetRandomPosition(), null, true));
                                            mobWave.Add(new MonsterToSummon(782, evt.MapInstance.Map.GetRandomPosition(), null, true));
                                            mobWave.Add(new MonsterToSummon(783, evt.MapInstance.Map.GetRandomPosition(), null, true));
                                            mobWave.Add(new MonsterToSummon(783, evt.MapInstance.Map.GetRandomPosition(), null, true));
                                            break;
                                    }
                                }
                                evt.MapInstance.SummonMonsters(mobWave);
                            });
                            break;

                        case EventActionType.SETMONSTERLOCKERS:
                            evt.MapInstance.InstanceBag.MonsterLocker.Current = Convert.ToByte(evt.Parameter);
                            evt.MapInstance.InstanceBag.MonsterLocker.Initial = Convert.ToByte(evt.Parameter);
                            break;

                        case EventActionType.SETBUTTONLOCKERS:
                            evt.MapInstance.InstanceBag.ButtonLocker.Current = Convert.ToByte(evt.Parameter);
                            evt.MapInstance.InstanceBag.ButtonLocker.Initial = Convert.ToByte(evt.Parameter);
                            break;

                        case EventActionType.SCRIPTEND:
                            switch (evt.MapInstance.MapInstanceType)
                            {
                                case MapInstanceType.TimeSpaceInstance:
                                    evt.MapInstance.InstanceBag.Clock.StopClock();
                                    evt.MapInstance.Clock.StopClock();
                                    evt.MapInstance.InstanceBag.EndState = (byte)evt.Parameter;
                                    ClientSession client = evt.MapInstance.Sessions.ToList().Where(s => s.Character?.Timespace != null).FirstOrDefault();
                                    if (client != null && client.Character?.Timespace != null && evt.MapInstance.InstanceBag.EndState != 10)
                                    {
#warning Check EndState and monsters to kill
                                        Guid MapInstanceId = ServerManager.GetBaseMapInstanceIdByMapId(client.Character.MapId);
                                        ScriptedInstance si = ServerManager.Instance.TimeSpaces.FirstOrDefault(s => s.Id == client.Character.Timespace.Id);
                                        if (si == null)
                                        {
                                            return;
                                        }
                                        byte penalty = 0;
                                        if (penalty > (client.Character.Level - si.LevelMinimum) * 2)
                                        {
                                            penalty = penalty > 100 ? (byte)100 : penalty;
                                            client.SendPacket(client.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("TS_PENALTY"), penalty), 10));
                                        }
                                        int point = evt.MapInstance.InstanceBag.Point * (100 - penalty) / 100;
                                        string perfection = "";
                                        perfection += evt.MapInstance.InstanceBag.MonstersKilled >= si.MonsterAmount ? 1 : 0;
                                        perfection += evt.MapInstance.InstanceBag.NpcsKilled == 0 ? 1 : 0;
                                        perfection += evt.MapInstance.InstanceBag.RoomsVisited >= si.RoomAmount ? 1 : 0;
                                        foreach (MapInstance mapInstance in client.Character.Timespace._mapInstanceDictionary.Values)
                                        {
                                            mapInstance.Broadcast($"score  {evt.MapInstance.InstanceBag.EndState} {point} 27 47 18 {si.DrawItems?.Count ?? 0} {evt.MapInstance.InstanceBag.MonstersKilled} {si.NpcAmount - evt.MapInstance.InstanceBag.NpcsKilled} {evt.MapInstance.InstanceBag.RoomsVisited} {perfection} 1 1");
                                        }

                                        if (evt.MapInstance.InstanceBag.EndState == 5)
                                        {
                                            if (client.Character.Inventory.GetAllItems().FirstOrDefault(s => s.Item.ItemType == ItemType.Special && s.Item.Effect == 140 && s.Item.EffectValue == si.Id) is ItemInstance tsStone)
                                            {
                                                client.Character.Inventory.RemoveItemFromInventory(tsStone.Id);
                                            }
                                            ClientSession[] tsmembers = new ClientSession[40];
                                            client.Character.Timespace._mapInstanceDictionary.SelectMany(s => s.Value?.Sessions).ToList().CopyTo(tsmembers);
                                            foreach (ClientSession targetSession in tsmembers)
                                            {
                                                if (targetSession != null)
                                                {
                                                    targetSession.Character.IncrementQuests(QuestType.TimesSpace, si.QuestTimeSpaceId);
                                                }
                                            }
                                        }

                                        ScriptedInstance ClientTimeSpace = client.Character.Timespace;
                                        Observable.Timer(TimeSpan.FromSeconds(30)).Subscribe(o =>
                                        {
                                            ClientSession[] tsmembers = new ClientSession[40];
                                            ClientTimeSpace._mapInstanceDictionary.SelectMany(s => s.Value?.Sessions).ToList().CopyTo(tsmembers);
                                            foreach (ClientSession targetSession in tsmembers)
                                            {
                                                if (targetSession != null)
                                                {
                                                    if (targetSession.Character.Hp <= 0)
                                                    {
                                                        targetSession.Character.Hp = 1;
                                                        targetSession.Character.Mp = 1;
                                                    }
                                                }
                                            }
                                            ClientTimeSpace._mapInstanceDictionary.Values.ToList().ForEach(m => m.Dispose());
                                        });
                                    }
                                    break;

                                case MapInstanceType.RaidInstance:
                                    {
                                        evt.MapInstance.InstanceBag.EndState = (byte)evt.Parameter;

                                        Character owner = evt.MapInstance.Sessions.FirstOrDefault(s => s.Character.Group?.Raid?.InstanceBag.CreatorId == s.Character.CharacterId)?.Character;
                                        if (owner == null) owner = evt.MapInstance.Sessions.FirstOrDefault(s => s.Character.Group?.Raid != null)?.Character;

                                        Group group = owner?.Group;

                                        if (group?.Raid == null)
                                        {
                                            break;
                                        }

                                        short teamSize = group.Raid.InstanceBag.Lives;

                                        if (evt.MapInstance.InstanceBag.EndState == 1 && evt.MapInstance.Monsters.Any(s => s.IsBoss))
                                        {
                                            Parallel.ForEach(group.Sessions.Where(s => s?.Character?.MapInstance?.Monsters.Any(e => e.IsBoss) == true), s =>
                                            {
                                                foreach (Gift gift in group.Raid.GiftItems)
                                                {
                                                    byte rare = 0;

                                                    for (int i = ItemHelper.RareRate.Length - 1, t = ServerManager.RandomNumber(); i >= 0; i--)
                                                    {
                                                        if (t < ItemHelper.RareRate[i])
                                                        {
                                                            rare = (byte)i;
                                                            break;
                                                        }
                                                    }

                                                    if (rare < 1)
                                                    {
                                                        rare = 1;
                                                    }

                                                    if (s.Character.Level >= group.Raid.LevelMinimum)
                                                    {
                                                        if ((gift.MinTeamSize == 0 && gift.MaxTeamSize == 0) || (teamSize >= gift.MinTeamSize && teamSize <= gift.MaxTeamSize))
                                                        {
                                                            if (ServerManager.Instance.Configuration.DoubleRaidBox == true)
                                                            {
                                                                s.Character.GiftAdd(gift.VNum, gift.Amount, rare, 0, gift.Design, gift.IsRandomRare);
                                                                s.Character.GiftAdd(gift.VNum, gift.Amount, rare, 0, gift.Design, gift.IsRandomRare);
                                                            }
                                                            else
                                                            {
                                                                s.Character.GiftAdd(gift.VNum, gift.Amount, rare, 0, gift.Design, gift.IsRandomRare);
                                                            }
                                                        }
                                                    }
                                                }

                                                s.Character.GetReputation(group.Raid.Reputation);

                                                if (s.Character.GenerateFamilyXp(group.Raid.FamExp, group.Raid.Id))
                                                {
                                                    s.SendPacket(s.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("WIN_FXP"), group.Raid.FamExp), 10));
                                                }

                                                s.Character.IncrementQuests(QuestType.WinRaid, group.Raid.Id);
                                            });

                                            foreach (MapMonster mapMonster in evt.MapInstance.Monsters)
                                            {
                                                if (mapMonster != null)
                                                {
                                                    mapMonster.SetDeathStatement();
                                                    evt.MapInstance.Broadcast(StaticPacketHelper.Out(UserType.Monster, mapMonster.MapMonsterId));
                                                    evt.MapInstance.RemoveMonster(mapMonster);
                                                }
                                            }

                                            Logger.LogUserEvent("RAID_SUCCESS", owner.Name, $"RaidId: {group.GroupId}");

                                            ServerManager.Instance.Broadcast(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("RAID_SUCCEED"), group.Raid.Label, owner.Name), 0));

                                            Parallel.ForEach(group.Sessions.GetAllItems(), s =>
                                            {
                                                if (s.Account != null && s.Character?.Group?.Raid != null)
                                                {
                                                    s.Character.GeneralLogs?.Add(new GeneralLogDTO
                                                    {
                                                        AccountId = s.Account.AccountId,
                                                        CharacterId = s.Character.CharacterId,
                                                        IpAddress = s.IpAddress,
                                                        LogData = $"{s.Character.Group.Raid.Id}",
                                                        LogType = "InstanceEntry",
                                                        Timestamp = DateTime.Now
                                                    });
                                                }
                                            });
                                        }

                                        TimeSpan dueTime = TimeSpan.FromSeconds(evt.MapInstance.InstanceBag.EndState == 1 ? 15 : 0);

                                        evt.MapInstance.Broadcast(Character.GenerateRaidBf(evt.MapInstance.InstanceBag.EndState));

                                        Observable.Timer(dueTime).Subscribe(o =>
                                        {
                                            evt.MapInstance.Sessions.Where(s => s.Character != null && s.Character.HasBuff(BCardType.CardType.FrozenDebuff, (byte)AdditionalTypes.FrozenDebuff.EternalIce))
                                                .Select(s => s.Character).ToList().ForEach(c =>
                                                {
                                                    c.RemoveBuff(569);
                                                });

                                            ClientSession[] groupMembers = new ClientSession[group.SessionCount];
                                            group.Sessions.CopyTo(groupMembers);

                                            foreach (ClientSession groupMember in groupMembers)
                                            {
                                                if (groupMember.Character.Hp < 1)
                                                {
                                                    groupMember.Character.Hp = 1;
                                                    groupMember.Character.Mp = 1;
                                                }

                                                groupMember.SendPacket(groupMember.Character.GenerateRaid(1, true));
                                                groupMember.SendPacket(groupMember.Character.GenerateRaid(2, true));
                                                group.LeaveGroup(groupMember);
                                            }

                                            ServerManager.Instance.GroupList.RemoveAll(s => s.GroupId == group.GroupId);
                                            ServerManager.Instance.ThreadSafeGroupList.Remove(group.GroupId);

                                            group.Raid.Dispose();
                                        });
                                    }
                                    break;


                                case MapInstanceType.Act4Morcos:
                                case MapInstanceType.Act4Hatus:
                                case MapInstanceType.Act4Calvina:
                                case MapInstanceType.Act4Berios:
                                    client = evt.MapInstance.Sessions.FirstOrDefault(s => s.Character?.Family?.Act4RaidBossMap == evt.MapInstance);
                                    if (client != null)
                                    {
                                        Family fam = client.Character.Family;
                                        if (fam != null)
                                        {
                                            short destX = 38;
                                            short destY = 179;
                                            short rewardVNum = 882;
                                            switch (evt.MapInstance.MapInstanceType)
                                            {
                                                //Morcos is default
                                                case MapInstanceType.Act4Hatus:
                                                    destX = 18;
                                                    destY = 10;
                                                    rewardVNum = 185;
                                                    break;

                                                case MapInstanceType.Act4Calvina:
                                                    destX = 25;
                                                    destY = 7;
                                                    rewardVNum = 942;
                                                    break;

                                                case MapInstanceType.Act4Berios:
                                                    destX = 16;
                                                    destY = 25;
                                                    rewardVNum = 999;
                                                    break;
                                            }
                                            int count = evt.MapInstance.Sessions.Count(s => s?.Character != null);
                                            foreach (ClientSession sess in evt.MapInstance.Sessions)
                                            {
                                                if (sess?.Character != null)
                                                {
                                                    sess.Character.GiftAdd(rewardVNum, 1, forceRandom: true, minRare: 4, design: 255);
                                                    sess.Character.GiftAdd(2361, 5);
                                                    if (sess.Character.GenerateFamilyXp(10000 / count))
                                                    {
                                                        sess.SendPacket(sess.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("WIN_FXP"), 10000 / count), 10));
                                                    }
                                                }
                                            }
                                            evt.MapInstance.Broadcast("dance 2");

                                            Logger.LogEvent("FAMILYRAID_SUCCESS", $"[fam.Name]FamilyRaidId: {evt.MapInstance.MapInstanceType.ToString()}");

                                            CommunicationServiceClient.Instance.SendMessageToCharacter(new SCSCharacterMessage
                                            {
                                                DestinationCharacterId = fam.FamilyId,
                                                SourceCharacterId = client.Character.CharacterId,
                                                SourceWorldId = ServerManager.Instance.WorldId,
                                                Message = UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("FAMILYRAID_SUCCESS"), 0),
                                                Type = MessageType.Family
                                            });
                                            //ServerManager.Instance.Broadcast(UserInterfaceHelper.Instance.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("FAMILYRAID_SUCCESS"), group?.Raid?.Label, group.Characters.ElementAt(0).Character.Name), 0));

                                            Observable.Timer(TimeSpan.FromSeconds(30)).Subscribe(o =>
                                            {
                                                foreach (ClientSession targetSession in evt.MapInstance.Sessions.ToArray())
                                                {
                                                    if (targetSession != null)
                                                    {
                                                        if (targetSession.Character.Hp <= 0)
                                                        {
                                                            targetSession.Character.Hp = 1;
                                                            targetSession.Character.Mp = 1;
                                                        }

                                                        ServerManager.Instance.ChangeMapInstance(targetSession.Character.CharacterId, fam.Act4Raid.MapInstanceId, destX, destY);

                                                        targetSession.SendPacket("dance");
                                                    }
                                                }
                                                evt.MapInstance.Dispose();
                                            });

                                            fam.InsertFamilyLog(FamilyLogType.RaidWon, raidType: (int)evt.MapInstance.MapInstanceType - 7);
                                        }
                                    }
                                    break;
                                case MapInstanceType.CaligorInstance:

                                    FactionType winningFaction = CaligorRaid.AngelDamage > CaligorRaid.DemonDamage ? FactionType.Angel : FactionType.Demon;

                                    foreach (ClientSession sess in evt.MapInstance.Sessions)
                                    {
                                        if (sess?.Character != null)
                                        {
                                            if (CaligorRaid.RemainingTime > 2400)
                                            {
                                                if (sess.Character.Faction == winningFaction)
                                                {
                                                    sess.Character.GiftAdd(5960, 1);
                                                }
                                                else
                                                {
                                                    sess.Character.GiftAdd(5961, 1);
                                                }
                                            }
                                            else
                                            {
                                                if (sess.Character.Faction == winningFaction)
                                                {
                                                    sess.Character.GiftAdd(5961, 1);
                                                }
                                                else
                                                {
                                                    sess.Character.GiftAdd(5958, 1);
                                                }
                                            }
                                            sess.Character.GiftAdd(5959, 1);
                                            sess.Character.GenerateFamilyXp(500);
                                        }
                                    }
                                    evt.MapInstance.Broadcast(UserInterfaceHelper.GenerateCHDM(ServerManager.GetNpcMonster(2305).MaxHP, CaligorRaid.AngelDamage, CaligorRaid.DemonDamage, CaligorRaid.RemainingTime));
                                    break;

                                case MapInstanceType.WorldBossInstance:
                                    foreach (ClientSession sess in evt.MapInstance.Sessions)
                                    {
                                        if (sess?.Character != null)
                                        {
                                            if (WorldRad.RemainingTime > 0)
                                            {
                                                sess.Character.GetReputation(50000, false);
                                                sess.Character.GiftAdd(1363, 6);
                                                sess.Character.GiftAdd(1364, 4);
                                                sess.Character.GiftAdd(5462, 1);
                                                sess.Character.Gold += 10000000;
                                                sess.SendPacket(sess.Character.GenerateGold());
                                                try
                                                {
                                                    Observable.Timer(TimeSpan.FromSeconds(15))
                                                    .Subscribe(observer =>
                                                    {
                                                        //SomeTime
                                                        ServerManager.Instance.ChangeMap(sess.Character.CharacterId, 2628, 56, 39);                                                     
                                                        ServerManager.Instance.StartedEvents.Remove(EventType.WORLDBOSS);

                                                    });
                                                    foreach (Portal p in WorldRad.UnknownLandMapInstance.Portals.Where(s => s.DestinationMapInstanceId == WorldRad.WorldMapinstance.MapInstanceId).ToList())
                                                    {
                                                        WorldRad.UnknownLandMapInstance.Portals.Remove(p);
                                                        WorldRad.UnknownLandMapInstance.Broadcast(p.GenerateGp());
                                                        ServerManager.Shout(Language.Instance.GetMessageFromKey("WORDLBOSS_END"), false);
                                                    }



                                                }
                                                catch
                                                {

                                                }


                                            }
                                        }
                                        sess.Character.GenerateFamilyXp(100);


                                    }
                                    break;
                            }
                            break;

                        case EventActionType.CLOCK:
                            evt.MapInstance.InstanceBag.Clock.TotalSecondsAmount = Convert.ToInt32(evt.Parameter);
                            evt.MapInstance.InstanceBag.Clock.SecondsRemaining = Convert.ToInt32(evt.Parameter);
                            break;

                        case EventActionType.MAPCLOCK:
                            evt.MapInstance.Clock.TotalSecondsAmount = Convert.ToInt32(evt.Parameter);
                            evt.MapInstance.Clock.SecondsRemaining = Convert.ToInt32(evt.Parameter);
                            break;

                        case EventActionType.STARTCLOCK:
                            Tuple<List<EventContainer>, List<EventContainer>> eve = (Tuple<List<EventContainer>, List<EventContainer>>)evt.Parameter;
                            evt.MapInstance.InstanceBag.Clock.StopEvents = eve.Item1;
                            evt.MapInstance.InstanceBag.Clock.TimeoutEvents = eve.Item2;
                            evt.MapInstance.InstanceBag.Clock.StartClock();
                            evt.MapInstance.Broadcast(evt.MapInstance.InstanceBag.Clock.GetClock());
                            break;

                        case EventActionType.STARTMAPCLOCK:
                            eve = (Tuple<List<EventContainer>, List<EventContainer>>)evt.Parameter;
                            evt.MapInstance.Clock.StopEvents = eve.Item1;
                            evt.MapInstance.Clock.TimeoutEvents = eve.Item2;
                            evt.MapInstance.Clock.StartClock();
                            evt.MapInstance.Broadcast(evt.MapInstance.Clock.GetClock());
                            break;

                        case EventActionType.STOPCLOCK:
                            evt.MapInstance.InstanceBag.Clock.StopClock();
                            evt.MapInstance.Broadcast(evt.MapInstance.InstanceBag.Clock.GetClock());
                            break;

                        case EventActionType.STOPMAPCLOCK:
                            evt.MapInstance.Clock.StopClock();
                            evt.MapInstance.Broadcast(evt.MapInstance.Clock.GetClock());
                            break;

                        case EventActionType.ADDCLOCKTIME:
                            evt.MapInstance.InstanceBag.Clock.AddTime((int)evt.Parameter);
                            evt.MapInstance.Broadcast(evt.MapInstance.InstanceBag.Clock.GetClock());
                            break;

                        case EventActionType.ADDMAPCLOCKTIME:
                            evt.MapInstance.Clock.AddTime((int)evt.Parameter);
                            evt.MapInstance.Broadcast(evt.MapInstance.Clock.GetClock());
                            break;

                        case EventActionType.TELEPORT:
                            Tuple<short, short, short, short> tp = (Tuple<short, short, short, short>)evt.Parameter;
                            List<Character> characters = evt.MapInstance.GetCharactersInRange(tp.Item1, tp.Item2, 5).ToList();
                            characters.ForEach(s =>
                            {
                                s.PositionX = tp.Item3;
                                s.PositionY = tp.Item4;
                                evt.MapInstance?.Broadcast(s.Session, s.GenerateTp());
                                foreach (Mate mate in s.Mates.Where(m => m.IsTeamMember && m.IsAlive))
                                {
                                    mate.PositionX = tp.Item3;
                                    mate.PositionY = tp.Item4;
                                    evt.MapInstance?.Broadcast(s.Session, mate.GenerateTp());
                                }
                            });
                            break;

                        case EventActionType.SPAWNPORTAL:
                            evt.MapInstance.CreatePortal((Portal)evt.Parameter);
                            break;

                        case EventActionType.REFRESHMAPITEMS:
                            evt.MapInstance.MapClear();
                            break;

                        case EventActionType.STOPMAPWAVES:
                            evt.MapInstance.WaveEvents.Clear();
                            break;

                        case EventActionType.NPCSEFFECTCHANGESTATE:
                            evt.MapInstance.Npcs.ForEach(s => s.EffectActivated = (bool)evt.Parameter);
                            break;

                        case EventActionType.CHANGEPORTALTYPE:
                            Tuple<int, PortalType> param = (Tuple<int, PortalType>)evt.Parameter;
                            Portal portal = evt.MapInstance.Portals.Find(s => s.PortalId == param.Item1);
                            if (portal != null)
                            {
                                portal.IsDisabled = true;
                                evt.MapInstance.Broadcast(portal.GenerateGp());
                                portal.IsDisabled = false;

                                portal.Type = (short)param.Item2;
                                if ((PortalType)portal.Type == PortalType.Closed
                                && (evt.MapInstance.MapInstanceType.Equals(MapInstanceType.Act4Berios)
                                 || evt.MapInstance.MapInstanceType.Equals(MapInstanceType.Act4Calvina)
                                 || evt.MapInstance.MapInstanceType.Equals(MapInstanceType.Act4Hatus)
                                 || evt.MapInstance.MapInstanceType.Equals(MapInstanceType.Act4Morcos)))
                                {
                                    portal.IsDisabled = true;
                                }
                                evt.MapInstance.Broadcast(portal.GenerateGp());
                            }
                            break;

                        case EventActionType.CHANGEDROPRATE:
                            evt.MapInstance.DropRate = (int)evt.Parameter;
                            break;

                        case EventActionType.CHANGEXPRATE:
                            evt.MapInstance.XpRate = (int)evt.Parameter;
                            break;

                        case EventActionType.CLEARMAPMONSTERS:
                            Parallel.ForEach(evt.MapInstance.Monsters.ToList().Where(s => s.Owner?.Character == null && s.Owner?.Mate == null), mapMonster =>
                            {
                                evt.MapInstance.Broadcast(StaticPacketHelper.Out(UserType.Monster, mapMonster.MapMonsterId));
                                mapMonster.SetDeathStatement();
                                evt.MapInstance.RemoveMonster(mapMonster);
                            });
                            break;

                        case EventActionType.DISPOSEMAP:
                            evt.MapInstance.Dispose();
                            break;

                        case EventActionType.SPAWNBUTTON:
                            evt.MapInstance.SpawnButton((MapButton)evt.Parameter);
                            break;

                        case EventActionType.UNSPAWNMONSTERS:
                            evt.MapInstance.DespawnMonster((int)evt.Parameter);
                            break;

                        case EventActionType.SPAWNMONSTER:
                            evt.MapInstance.SummonMonster((MonsterToSummon)evt.Parameter);
                            break;

                        case EventActionType.SPAWNMONSTERS:
                            evt.MapInstance.SummonMonsters((List<MonsterToSummon>)evt.Parameter);
                            break;

                        case EventActionType.REFRESHRAIDGOAL:
                            ClientSession cl = evt.MapInstance.Sessions.FirstOrDefault();
                            if (cl?.Character != null)
                            {
                                ServerManager.Instance.Broadcast(cl, cl.Character?.Group?.GeneraterRaidmbf(cl), ReceiverType.Group);
                                ServerManager.Instance.Broadcast(cl, UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NEW_MISSION"), 0), ReceiverType.Group);
                            }
                            break;

                        case EventActionType.SPAWNNPC:
                            evt.MapInstance.SummonNpc((NpcToSummon)evt.Parameter);
                            break;

                        case EventActionType.SPAWNNPCS:
                            evt.MapInstance.SummonNpcs((List<NpcToSummon>)evt.Parameter);
                            break;

                        case EventActionType.DROPITEMS:
                            evt.MapInstance.DropItems((List<Tuple<short, int, short, short>>)evt.Parameter);
                            break;

                        case EventActionType.THROWITEMS:
                            Tuple<int, short, byte, int, int, short> parameters = (Tuple<int, short, byte, int, int, short>)evt.Parameter;
                            if (monster != null)
                            {
                                parameters = new Tuple<int, short, byte, int, int, short>(monster.MapMonsterId, parameters.Item2, parameters.Item3, parameters.Item4, parameters.Item5, parameters.Item6);
                            }
                            evt.MapInstance.ThrowItems(parameters);
                            break;

                        case EventActionType.SPAWNONLASTENTRY:
                            Character lastincharacter = evt.MapInstance.Sessions.OrderByDescending(s => s.RegisterTime).FirstOrDefault()?.Character;
                            List<MonsterToSummon> summonParameters = new List<MonsterToSummon>();
                            MapCell hornSpawn = new MapCell
                            {
                                X = lastincharacter?.PositionX ?? 154,
                                Y = lastincharacter?.PositionY ?? 140
                            };
                            BattleEntity hornTarget = lastincharacter?.BattleEntity ?? null;
                            summonParameters.Add(new MonsterToSummon(Convert.ToInt16(evt.Parameter), hornSpawn, hornTarget, true));
                            evt.MapInstance.SummonMonsters(summonParameters);
                            break;

                        case EventActionType.REMOVEAFTER:
                            {
                                Observable.Timer(TimeSpan.FromSeconds(Convert.ToInt16(evt.Parameter)))
                                    .Subscribe(o =>
                                    {
                                        if (monster != null)
                                        {
                                            monster.SetDeathStatement();
                                            evt.MapInstance.RemoveMonster(monster);
                                            evt.MapInstance.Broadcast(StaticPacketHelper.Out(UserType.Monster, monster.MapMonsterId));
                                        }
                                    });
                            }
                            break;

                        case EventActionType.REMOVELAURENABUFF:
                            {
                                Observable.Timer(TimeSpan.FromSeconds(1))
                                    .Subscribe(observer =>
                                    {
                                        if (evt.Parameter is BattleEntity battleEntity
                                            && evt.MapInstance?.Monsters != null
                                            && !evt.MapInstance.Monsters.ToList().Any(s => s.MonsterVNum == 2327))
                                        {
                                            battleEntity.RemoveBuff(475);
                                        }
                                    });
                            }
                            break;

                            #endregion
                    }
                }
            }
        }

        public void ScheduleEvent(TimeSpan timeSpan, EventContainer evt) => Observable.Timer(timeSpan).Subscribe(x => RunEvent(evt));

        #endregion
    }
}