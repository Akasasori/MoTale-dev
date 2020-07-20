using OpenNos.Core;
using OpenNos.Domain;
using OpenNos.GameObject.Helpers;
using OpenNos.GameObject.Networking;
using OpenNos.GameObject.Event;
using OpenNos.Master.Library.Client;
using OpenNos.Master.Library.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using OpenNos.GameObject.RainbowBattle;

namespace OpenNos.GameObject.Event.RAINBOWBATTLE
{
    public class RainbowBattle
    {
        public static void GenerateEvent()
        {
            // Init The event
            Initialize();
            // Inform everyone about RBB
            SendShout();
            // ask at all players online if he want join the event :) 
            SendEvent();
        }

        public static void Initialize()
        {
            Map = ServerManager.GenerateMapInstance(2010, MapInstanceType.RainbowBattle, new InstanceBag());
        }

        public static void SendShout()
        {
            CommunicationServiceClient.Instance.SendMessageToCharacter(new SCSCharacterMessage
            {
                DestinationCharacterId = null,
                SourceCharacterId = 0,
                SourceWorldId = ServerManager.Instance.WorldId,
                Message = $"Rainbow Battle begins in 10 seconds!",
                Type = MessageType.Shout
            });
        }

        public static void SendEvent()
        {

            ServerManager.Instance.Broadcast(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("RAINBOW_SECONDS"), 10), 1));
            Thread.Sleep(10 * 1000);
            ServerManager.Instance.Broadcast(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("RAINBOW_STARTED"), 1));
            //qnaml 3 #guri^506 The Meteorite Game is starting! Join now!
            ServerManager.Instance.Broadcast("qnaml 100 #guri^506 The Rainbow Battle started! Join now!");
            ServerManager.Instance.EventInWaiting = true;
            Thread.Sleep(30 * 1000);
            ServerManager.Instance.Sessions.Where(s => s.Character?.IsWaitingForEvent == false).ToList().ForEach(s => s.SendPacket("esf"));
            ServerManager.Instance.EventInWaiting = false;
            IEnumerable<ClientSession> sessions = ServerManager.Instance.Sessions.Where(s => s.Character?.IsWaitingForEvent == true && s.Character.MapInstance.MapInstanceType == MapInstanceType.BaseMapInstance);

            MapInstance map = ServerManager.GenerateMapInstance(2004, MapInstanceType.EventGameInstance, new InstanceBag());
            if (Map != null)
            {
                foreach (ClientSession sess in sessions)
                {
                    ServerManager.Instance.TeleportOnRandomPlaceInMap(sess, Map.MapInstanceId);
                    sess.SendPacket(UserInterfaceHelper.GenerateBSInfo(2, 7, 0, 0));
                    sess.SendPacket("rsfp 0 0");
                    sess.Character.DisableBuffs(BuffType.All);
                    foreach (var mate in sess.Character.Mates.Where(s => s.IsTeamMember))
                    {
                        mate.RemoveTeamMember(true);
                    }
                }

                ServerManager.Instance.Sessions.Where(s => s.Character != null).ToList().ForEach(s => s.Character.IsWaitingForEvent = false);
                ServerManager.Instance.StartedEvents.Remove(EventType.RAINBOWBATTLE);

              
            }
           

            if (Map.Sessions.Count() < 1)
            {
                Map.Broadcast(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("RAINBOW_NOT_ENOUGH_PLAYERS"), 0));
                EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(5), new EventContainer(Map, EventActionType.DISPOSEMAP, null));
                return;
            }
            RainbowThread task = new RainbowThread();
            Observable.Timer(TimeSpan.FromSeconds(10)).Subscribe(X => task.RunEvent(Map));
        }

        public class RainbowThread
        {
            public void RunEvent(MapInstance map)
            {
                SummonNpc(Map);
                map.Broadcast("msg 0 The battle begin in 5 seconds.");
                map.SendInMapAfter(1, "msg 0 The battle begin in 4 seconds.");
                map.SendInMapAfter(2, "msg 0 The battle begin in 3 seconds.");
                map.SendInMapAfter(3, "msg 0 The battle begin in 2 seconds.");
                map.SendInMapAfter(4, "msg 0 The battle begin in 1 seconds.");
                map.SendInMapAfter(5, "msg 0 Fight !");

                Observable.Timer(TimeSpan.FromSeconds(5)).Subscribe(o =>
                {
                    CreateGroup(Map.Sessions);
                    GenerateBattleRainbowPacket(RainbowTeamBattleType.Blue);
                    GenerateBattleRainbowPacket(RainbowTeamBattleType.Red);
                    map.IsPVP = true;

                    // Observable Disposed → EndEvent()
                    RainbowBattleManager.ObservableFlag = Observable.Interval(TimeSpan.FromSeconds(7)).Subscribe(s =>
                    {
                        RainbowBattleManager.GenerateScoreForAll();
                    });

                });

                Observable.Timer(TimeSpan.FromSeconds(605)).Subscribe(o =>
                {
                    RainbowBattleManager.EndEvent(map);
                });
            }

            public void CreateGroup(IEnumerable<ClientSession> session)
            {
                int team1 = 0;
                int team2 = 0;
                var group = new Group
                {
                    GroupType = GroupType.RainbowBattleBlue
                };
                ServerManager.Instance.AddGroup(group);
                var group2 = new Group
                {
                    GroupType = GroupType.RainbowBattleRed
                };
                ServerManager.Instance.AddGroup(group2);
                ConcurrentBag<ClientSession> firstTeam = new ConcurrentBag<ClientSession>();
                ConcurrentBag<ClientSession> secondTeam = new ConcurrentBag<ClientSession>();
                foreach (ClientSession ses in session)
                {
                    if (RainbowBattleManager.AreNotInMap(ses))
                    {
                        continue;
                    }

                    ses.Character.Group?.LeaveGroup(ses);

                    var value = team1 - team2;

                    if (value == 0)
                    {
                        team1++;
                        firstTeam.Add(ses);
                        group.JoinGroup(ses);
                    }
                    else
                    {
                        team2++;
                        secondTeam.Add(ses);
                        group2.JoinGroup(ses);
                    }

                    ServerManager.Instance.UpdateGroup(ses.Character.CharacterId);
                }

                ServerManager.Instance.RainbowBattleMembers = new ConcurrentBag<RainbowBattleTeam>
                {
                    new RainbowBattleTeam(firstTeam, RainbowTeamBattleType.Blue),
                    new RainbowBattleTeam(secondTeam, RainbowTeamBattleType.Red)
                };

            }

            public void GenerateBattleRainbowPacket(RainbowTeamBattleType value)
            {
                string rndm = string.Empty;
                string rndm2 = string.Empty;
                var RainbowTeam = ServerManager.Instance.RainbowBattleMembers.First(s => s.TeamEntity == value);

                if (RainbowTeam == null)
                {
                    return;
                }

                foreach (var bb in RainbowTeam.Session)
                {
                    if (RainbowBattleManager.AreNotInMap(bb))
                    {
                        continue;
                    }

                    rndm += $"{bb.Character.CharacterId} ";
                    rndm2 +=
                        $"{bb.Character.Level}." +
                        $"{bb.Character.Morph}." +
                        $"{(byte)bb.Character.Class}." +
                        $"0." +
                        $"{bb.Character.Name}." +
                        $"{(byte)bb.Character.Gender}." +
                        $"{bb.Character.CharacterId}." +
                        $"{bb.Character.HeroLevel} ";
                }

                foreach (var bb in RainbowTeam.Session)
                {
                    if (RainbowBattleManager.AreNotInMap(bb))
                    {
                        continue;
                    }

                    bb.SendPacket("fbt 0 1");
                    bb.SendPacket($"fbt 1 {rndm}");
                    bb.SendPacket($"fblst {rndm2}");
                    bb.SendPacket($"fbt 5 1 600");
                    bb.SendPacket($"msg 0 you are the {value} team");
                    bb.SendPacket($"fbs {(byte)value} {RainbowTeam.Session.Count()} 0 0 0 0 0 {value}");
                }
                //fbs <Type> <TeamCount> <RedPts> <BluePts> <flag1> <flag2> <flag3> <TEAM>
            }

            public void SummonNpc(MapInstance map)
            {
                List<MapNpc> npc = new List<MapNpc>
                {
                    new MapNpc
                    {
                        NpcVNum = 922,
                        MapNpcId = map.GetNextNpcId(),
                        Dialog = 0,
                        MapId = map.Map.MapId,
                        MapX = 59,
                        MapY = 40,
                        IsMoving = false,
                        Position = 0,
                        IsSitting = false,
                        Effect = 3009,
                        Score = 5
                    },
                    new MapNpc
                    {
                        NpcVNum = 923,
                        MapNpcId = map.GetNextNpcId(),
                        Dialog = 0,
                        MapId = map.Map.MapId,
                        MapX = 74,
                        MapY = 53,
                        IsMoving = false,
                        Position = 0,
                        IsSitting = false,
                        Effect = 3009,
                        Score = 2
                    },
                    new MapNpc
                    {
                        NpcVNum = 923,
                        MapNpcId = map.GetNextNpcId(),
                        Dialog = 0,
                        MapId = map.Map.MapId,
                        MapX = 32,
                        MapY = 75,
                        IsMoving = false,
                        Position = 0,
                        IsSitting = false,
                        Effect = 3009,
                        Score = 2
                    },
                    new MapNpc
                    {
                        NpcVNum = 923,
                        MapNpcId = map.GetNextNpcId(),
                        Dialog = 0,
                        MapId = map.Map.MapId,
                        MapX = 85,
                        MapY = 4,
                        IsMoving = false,
                        Position = 0,
                        IsSitting = false,
                        Effect = 3009,
                        Score = 2
                    },
                    new MapNpc
                    {
                        NpcVNum = 923,
                        MapNpcId = map.GetNextNpcId(),
                        Dialog = 0,
                        MapId = map.Map.MapId,
                        MapX = 43,
                        MapY = 26,
                        IsMoving = false,
                        Position = 0,
                        IsSitting = false,
                        Effect = 3009,
                        Score = 2
                    },
                    new MapNpc
                    {
                        NpcVNum = 924,
                        MapNpcId = map.GetNextNpcId(),
                        Dialog = 0,
                        MapId = map.Map.MapId,
                        MapX = 15,
                        MapY = 40,
                        IsMoving = false,
                        Position = 0,
                        IsSitting = false,
                        Effect = 3009,
                        Score = 1
                    },
                    new MapNpc
                    {
                        NpcVNum = 924,
                        MapNpcId = map.GetNextNpcId(),
                        Dialog = 999,
                        MapId = map.Map.MapId,
                        MapX = 102,
                        MapY = 39,
                        IsMoving = false,
                        Position = 0,
                        IsSitting = false,
                        Effect = 3009,
                        Score = 1
                    },
                };

                foreach (var Stone in npc)
                {
                    Stone.Dialog = 999;
                    Stone.Initialize(map);
                    map.AddNPC(Stone);
                    map.Broadcast(Stone.GenerateIn());
                    IEnumerable<ClientSession> sess = ServerManager.Instance.Sessions.Where(s => s.Character?.CharacterId == null);
                    foreach (ClientSession ses in sess)
                    {
                        if (Stone.Dialog == 999)
                        {
                            ses.SendPacket(UserInterfaceHelper.GenerateDelay(5000, 1, $"#guri^711^{Stone.MapNpcId}"));
                            Stone.Score++;
                            RainbowBattleManager.SendFbs();
                        }
                    }
                }
            }

            //fbt 3 1.10.100 1285990.100.88
        }


        #region Method

        public static MapInstance Map { get; private set; }

        #endregion
    }

    public static class ExtensionTimer
    {
        public static void SendInMapAfter(this MapInstance map, double sec, string packet)
        {
            Observable.Timer(TimeSpan.FromSeconds(sec)).Subscribe(o => { map.Broadcast(packet); });
        }
    }
}
