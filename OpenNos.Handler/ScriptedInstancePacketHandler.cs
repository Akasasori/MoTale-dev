using OpenNos.Core;
using OpenNos.Core.Extensions;
using OpenNos.Domain;
using OpenNos.GameObject;
using OpenNos.GameObject.Helpers;
using OpenNos.GameObject.Packets.ServerPackets;
using System;
using System.Linq;
using OpenNos.GameObject.Networking;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading;
using OpenNos.Data;

namespace OpenNos.Handler
{
    internal class ScriptedInstancePacketHandler : IPacketHandler
    {
        #region Instantiation

        public ScriptedInstancePacketHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        private ClientSession Session { get; }

        #endregion

        #region Methods
        public void ButtonCancel(BscPacket packet)
        {
            switch (packet.Type)
            {
                case 2:
                    var arenamember = ServerManager.Instance.ArenaMembers.ToList().FirstOrDefault(s => s.Session == Session);
                    if (arenamember?.GroupId != null)
                    {
                        if (packet.Option != 1)
                        {
                            Session.SendPacket($"qna #bsc^2^1 {Language.Instance.GetMessageFromKey("ARENA_PENALTY_NOTICE")}");
                            return;
                        }
                    }

                    Session.Character.LeaveTalentArena(false);
                    break;
            }
        }

        public void Call(TaCallPacket packet)
        {
            try
            {
                ConcurrentBag<ArenaTeamMember> arenateam = ServerManager.Instance.ArenaTeams.ToList().FirstOrDefault(s => s.Any(o => o.Session == Session));
                if (arenateam == null || Session.CurrentMapInstance.MapInstanceType != MapInstanceType.TalentArenaMapInstance)
                {
                    return;
                }

                IEnumerable<ArenaTeamMember> ownteam = arenateam.Replace(s => s.ArenaTeamType == arenateam?.FirstOrDefault(e => e.Session == Session)?.ArenaTeamType);
                var client = ownteam.Where(s => s.Session != Session).OrderBy(s => s.Order).Skip(packet.CalledIndex).FirstOrDefault()?.Session;
                var memb = arenateam.FirstOrDefault(s => s.Session == client);
                if (client == null || client.CurrentMapInstance != Session.CurrentMapInstance || memb == null || memb.LastSummoned != null || ownteam.Sum(s => s.SummonCount) >= 5)
                {
                    return;
                }

                memb.SummonCount++;
                arenateam.ToList().ForEach(arenauser => { arenauser.Session.SendPacket(arenauser.Session.Character.GenerateTaP(2, true)); });
                var arenaTeamMember = arenateam.FirstOrDefault(s => s.Session == client);
                if (arenaTeamMember != null)
                {
                    arenaTeamMember.LastSummoned = DateTime.Now;
                }

                Session.CurrentMapInstance.Broadcast(Session.Character.GenerateEff(4432));

                Observable.Timer(TimeSpan.FromSeconds(0)).Subscribe(o =>
                {
                    client.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("ARENA_CALLED"), 3), 0));
                    client.SendPacket(client.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("ARENA_CALLED"), 3), 10));
                });

                Observable.Timer(TimeSpan.FromSeconds(1)).Subscribe(o =>
                {
                    client.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("ARENA_CALLED"), 2), 0));
                    client.SendPacket(client.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("ARENA_CALLED"), 2), 10));
                });

                Observable.Timer(TimeSpan.FromSeconds(2)).Subscribe(o =>
                {
                    client.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("ARENA_CALLED"), 1), 0));
                    client.SendPacket(client.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("ARENA_CALLED"), 1), 10));
                });

                var x = Session.Character.PositionX;
                var y = Session.Character.PositionY;
                const byte TIMER = 30;
                Observable.Timer(TimeSpan.FromSeconds(3)).Subscribe(o =>
                {
                    Session.CurrentMapInstance.Broadcast($"ta_t 0 {client.Character.CharacterId} {TIMER}");
                    client.Character.PositionX = x;
                    client.Character.PositionY = y;
                    Session.CurrentMapInstance.Broadcast(client.Character.GenerateTp());

                    client.SendPacket(UserInterfaceHelper.Instance.GenerateTaSt(TalentArenaOptionType.Nothing));
                });

                Observable.Timer(TimeSpan.FromSeconds(TIMER + 3)).Subscribe(o =>
                {
                    DateTime? lastsummoned = arenateam.FirstOrDefault(s => s.Session == client)?.LastSummoned;
                    if (lastsummoned == null || ((DateTime)lastsummoned).AddSeconds(TIMER) >= DateTime.Now)
                    {
                        return;
                    }

                    var firstOrDefault = arenateam.FirstOrDefault(s => s.Session == client);
                    if (firstOrDefault != null)
                    {
                        firstOrDefault.LastSummoned = null;
                    }

                    List<BuffType> bufftodisable = new List<BuffType> { BuffType.Bad };
                    client.Character.DisableBuffs(bufftodisable);
                    client.Character.Hp = (int)client.Character.HPLoad();
                    client.Character.Mp = (int)client.Character.MPLoad();
                    client.SendPacket(client.Character.GenerateStat());

                    client.Character.PositionX = memb.ArenaTeamType == ArenaTeamType.ERENIA ? (short)120 : (short)19;
                    client.Character.PositionY = memb.ArenaTeamType == ArenaTeamType.ERENIA ? (short)39 : (short)40;
                    Session?.CurrentMapInstance?.Broadcast(client?.Character?.GenerateTp());
                    client.SendPacket(UserInterfaceHelper.Instance.GenerateTaSt(TalentArenaOptionType.Watch));
                });
            }
            catch
            {
            }
        }

        /// <summary>
        /// RSelPacket packet
        /// </summary>
        /// <param name="escapePacket"></param>
        public void Escape(EscapePacket escapePacket)
        {
            if (Session.CurrentMapInstance.MapInstanceType == MapInstanceType.TimeSpaceInstance)
            {
                ServerManager.Instance.ChangeMap(Session.Character.CharacterId, Session.Character.MapId,
                    Session.Character.MapX, Session.Character.MapY);
                Session.Character.Timespace = null;
            }
            else if (Session.CurrentMapInstance.MapInstanceType == MapInstanceType.RaidInstance)
            {
                ServerManager.Instance.ChangeMap(Session.Character.CharacterId, Session.Character.MapId,
                    Session.Character.MapX, Session.Character.MapY);
                ServerManager.Instance.GroupLeave(Session);
            }
            else if  (Session.CurrentMapInstance.MapInstanceType == MapInstanceType.SheepGameInstance)
            {
                int miniscore = 50; // your score
                if (Session.Character.SheepScore1 > miniscore && Session.Character.IsWaitingForGift == true) // Anti Afk to get Reward
                {
                    short[] random1 = { 1, 2, 3 };
                    short[] random = { 2, 4, 6 };
                    short[] acorn = { 5947, 5948, 5949, 5950 };
                    Session.Character.GiftAdd(5951, random1[ServerManager.RandomNumber(0, random1.Length)]);
                    int rnd = ServerManager.RandomNumber(0, 5);
                    switch (rnd)
                    {
                        case 2:
                            Session.Character.GiftAdd(acorn[ServerManager.RandomNumber(0, acorn.Length)], random[ServerManager.RandomNumber(0, random.Length)]);
                            break;
                        default:
                            break;
                    }
                    ServerManager.Instance.ChangeMap(Session.Character.CharacterId, Session.Character.MapId, Session.Character.MapX, Session.Character.MapY);
                    Session.Character.IsWaitingForGift = false;
                }
            }
        }

        /// <summary>
        /// mkraid packet
        /// </summary>
        /// <param name="mkRaidPacket"></param>
        public void GenerateRaid(MkRaidPacket mkRaidPacket)
        {
            if (Session.Character.Group?.Raid != null && Session.Character.Group.IsLeader(Session))
            {
                if (Session.Character.MapId == Session.Character.Group.Raid.MapId
                    && Map.GetDistance(
                        new MapCell { X = Session.Character.PositionX, Y = Session.Character.PositionY }, 
                        new MapCell { X = Session.Character.Group.Raid.PositionX, Y = Session.Character.Group.Raid.PositionY }) < 2)
                {
                    if ((Session.Character.Group.SessionCount > 0 || Session.Character.Authority >= AuthorityType.TGM)
                    && Session.Character.Group.Sessions.All(s => s.CurrentMapInstance == Session.CurrentMapInstance))
                    {
                        if (Session.Character.Group.Raid.FirstMap == null)
                        {
                            Session.Character.Group.Raid.LoadScript(MapInstanceType.RaidInstance, Session.Character);
                        }

                        if (Session.Character.Group.Raid.FirstMap == null)
                        {
                            return;
                        }

                        Session.Character.Group.Raid.InstanceBag.Lock = true;

                        /*Session.Character.Group.Characters.Where(s => s.CurrentMapInstance != Session.CurrentMapInstance).ToList().ForEach(
                        session =>
                        {
                            Session.Character.Group.LeaveGroup(session);
                            session.SendPacket(session.Character.GenerateRaid(1, true));
                            session.SendPacket(session.Character.GenerateRaid(2, true));
                        });*/

                        Session.Character.Group.Raid.InstanceBag.Lives = (short)Session.Character.Group.SessionCount;

                        foreach (ClientSession session in Session.Character.Group.Sessions.GetAllItems())
                        {
                            if (session != null)
                            {
                                ServerManager.Instance.ChangeMapInstance(session.Character.CharacterId,
                                    session.Character.Group.Raid.FirstMap.MapInstanceId,
                                    session.Character.Group.Raid.StartX, session.Character.Group.Raid.StartY);
                                session.SendPacket("raidbf 0 0 25");
                                session.SendPacket(session.Character.Group.GeneraterRaidmbf(session));
                                session.SendPacket(session.Character.GenerateRaid(5));
                                session.SendPacket(session.Character.GenerateRaid(4));
                                session.SendPacket(session.Character.GenerateRaid(3));
                                if (session.Character.Group.Raid.DailyEntries > 0)
                                {
                                    var entries = session.Character.Group.Raid.DailyEntries - session.Character.GeneralLogs.CountLinq(s => s.LogType == "InstanceEntry" && short.Parse(s.LogData) == session.Character.Group.Raid.Id && s.Timestamp.Date == DateTime.Today);
                                    session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("INSTANCE_ENTRIES"), entries), 10));
                                }
                            }
                        }

                        ServerManager.Instance.GroupList.Remove(Session.Character.Group);

                        Logger.LogUserEvent("RAID_START", Session.GenerateIdentity(),
                            $"RaidId: {Session.Character.Group.GroupId}");
                    }
                    else
                    {
                        Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("RAID_TEAM_NOT_READY"), 0));
                    }
                }
                else
                {
                    if(Session.Character.Group.Raid.IsSinglePortal == true)
                    {
                        if ((Session.Character.Group.SessionCount > 0 || Session.Character.Authority >= AuthorityType.TGM)
                        && Session.Character.Group.Sessions.All(s => s.CurrentMapInstance == Session.CurrentMapInstance))
                        {
                            if (Session.Character.Group.Raid.FirstMap == null)
                            {
                                Session.Character.Group.Raid.LoadScript(MapInstanceType.RaidInstance, Session.Character);
                            }

                            if (Session.Character.Group.Raid.FirstMap == null)
                            {
                                return;
                            }

                            Session.Character.Group.Raid.InstanceBag.Lock = true;

                            /*Session.Character.Group.Characters.Where(s => s.CurrentMapInstance != Session.CurrentMapInstance).ToList().ForEach(
                            session =>
                            {
                                Session.Character.Group.LeaveGroup(session);
                                session.SendPacket(session.Character.GenerateRaid(1, true));
                                session.SendPacket(session.Character.GenerateRaid(2, true));
                            });*/

                            Session.Character.Group.Raid.InstanceBag.Lives = (short)Session.Character.Group.SessionCount;

                            foreach (ClientSession session in Session.Character.Group.Sessions.GetAllItems())
                            {
                                if (session != null)
                                {
                                    ServerManager.Instance.ChangeMapInstance(session.Character.CharacterId,
                                        session.Character.Group.Raid.FirstMap.MapInstanceId,
                                        session.Character.Group.Raid.StartX, session.Character.Group.Raid.StartY);
                                    session.SendPacket("raidbf 0 0 25");
                                    session.SendPacket(session.Character.Group.GeneraterRaidmbf(session));
                                    session.SendPacket(session.Character.GenerateRaid(5));
                                    session.SendPacket(session.Character.GenerateRaid(4));
                                    session.SendPacket(session.Character.GenerateRaid(3));
                                    if (session.Character.Group.Raid.DailyEntries > 0)
                                    {
                                        var entries = session.Character.Group.Raid.DailyEntries - session.Character.GeneralLogs.CountLinq(s => s.LogType == "InstanceEntry" && short.Parse(s.LogData) == session.Character.Group.Raid.Id && s.Timestamp.Date == DateTime.Today);
                                        session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("INSTANCE_ENTRIES"), entries), 10));
                                    }
                                }
                            }

                            ServerManager.Instance.GroupList.Remove(Session.Character.Group);

                            Logger.LogUserEvent("RAID_START", Session.GenerateIdentity(),
                                $"RaidId: {Session.Character.Group.GroupId}");
                        }
                        else
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("RAID_TEAM_NOT_READY"), 0));
                        }
                    }
                    else
                    {
                        Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("WRONG_PORTAL"), 0));
                    }

                }
            }
        }

        /// <summary>
        /// RSelPacket packet
        /// </summary>
        /// <param name="rSelPacket"></param>
        public void GetGift(RSelPacket rSelPacket)
        {
            if (Session.Character.Timespace?.FirstMap?.MapInstanceType == MapInstanceType.TimeSpaceInstance)
            {
                ServerManager.GetBaseMapInstanceIdByMapId(Session.Character.MapId);
                if (Session.Character.Timespace?.FirstMap.InstanceBag.EndState == 5 || Session.Character.Timespace?.FirstMap.InstanceBag.EndState == 6)
                {
                    if (!Session.Character.TimespaceRewardGotten)
                    {
                        Session.Character.TimespaceRewardGotten = true;
                        Session.Character.GetReputation(Session.Character.Timespace.Reputation);

                        Session.Character.Gold =
                            Session.Character.Gold + Session.Character.Timespace.Gold
                            > ServerManager.Instance.Configuration.MaxGold
                                ? ServerManager.Instance.Configuration.MaxGold
                                : Session.Character.Gold + Session.Character.Timespace.Gold;
                        Session.SendPacket(Session.Character.GenerateGold());
                        Session.SendPacket(Session.Character.GenerateSay(
                            string.Format(Language.Instance.GetMessageFromKey("GOLD_TS_END"),
                                Session.Character.Timespace.Gold), 10));

                        int rand = new Random().Next(Session.Character.Timespace.DrawItems.Count);
                        string repay = "repay ";
                        if (Session.Character.Timespace.DrawItems.Count > 0)
                        {
                            Session.Character.GiftAdd(Session.Character.Timespace.DrawItems[rand].VNum,
                                Session.Character.Timespace.DrawItems[rand].Amount,
                                design: Session.Character.Timespace.DrawItems[rand].Design,
                                forceRandom: Session.Character.Timespace.DrawItems[rand].IsRandomRare);
                        }

                        for (int i = 0; i < 3; i++)
                        {
                            Gift gift = Session.Character.Timespace.GiftItems.ElementAtOrDefault(i);
                            repay += gift == null ? "-1.0.0 " : $"{gift.VNum}.0.{gift.Amount} ";
                            if (gift != null)
                            {
                                Session.Character.GiftAdd(gift.VNum, gift.Amount, design: gift.Design, forceRandom: gift.IsRandomRare);
                            }
                        }

                        // TODO: Add HasAlreadyDone
                        for (int i = 0; i < 2; i++)
                        {
                            Gift gift = Session.Character.Timespace.SpecialItems.ElementAtOrDefault(i);
                            repay += gift == null ? "-1.0.0 " : $"{gift.VNum}.0.{gift.Amount} ";
                            if (gift != null)
                            {
                                Session.Character.GiftAdd(gift.VNum, gift.Amount, design: gift.Design, forceRandom: gift.IsRandomRare);
                            }
                        }

                        if (Session.Character.Timespace.DrawItems.Count > 0)
                        {
                            repay +=
                                $"{Session.Character.Timespace.DrawItems[rand].VNum}.0.{Session.Character.Timespace.DrawItems[rand].Amount}";
                        }
                        else
                        {
                            repay +=
                                $"-1.0.0";
                        }
                        Session.SendPacket(repay);
                        Session.Character.Timespace.FirstMap.InstanceBag.EndState = 6;
                    }
                }
            }
        }

        /// <summary>
        /// treq packet
        /// </summary>
        /// <param name="treqPacket"></param>
        public void GetTreq(TreqPacket treqPacket)
        {
            ScriptedInstance timespace = Session.CurrentMapInstance.ScriptedInstances
                .Find(s => treqPacket.X == s.PositionX && treqPacket.Y == s.PositionY).Copy();

            if (timespace != null)
            {
                if (treqPacket.StartPress == 1 || treqPacket.RecordPress == 1)
                {
                    Session.Character.EnterInstance(timespace);
                }
                else
                {
                    Session.SendPacket(timespace.GenerateRbr());
                }
            }
        }
        
        /// <summary>
        /// wreq packet
        /// </summary>
        /// <param name="packet"></param>
        public void GetWreq(WreqPacket packet)
        {
            short CharPositionX = Session.Character.PositionX;
            short CharPositionY = Session.Character.PositionY;
            foreach (ScriptedInstance portal in Session.CurrentMapInstance.ScriptedInstances)
            {
                if (CharPositionY >= portal.PositionY - 1 && CharPositionY
                                                                        <= portal.PositionY + 1
                                                                        && CharPositionX
                                                                        >= portal.PositionX - 1
                                                                        && CharPositionX
                                                                        <= portal.PositionX + 1)
                {
                    switch (packet.Value)
                    {
                        case 0:
                            if (packet.Param != 1 
                                && Session.Character.Group?.Sessions.Find(s =>
                                    s.CurrentMapInstance.InstanceBag?.Lock == false
                                    && s.CurrentMapInstance.MapInstanceType == MapInstanceType.TimeSpaceInstance
                                    && s.Character.CharacterId != Session.Character.CharacterId
                                    && s.Character.Timespace?.Id == portal.Id
                                    && !s.Character.Timespace.IsIndividual) is ClientSession TeamMemberInInstance)
                            {
                                if (portal.DailyEntries > 0)
                                {
                                    var entries = portal.DailyEntries - Session.Character.GeneralLogs.CountLinq(s => s.LogType == "InstanceEntry" && short.Parse(s.LogData) == portal.Id && s.Timestamp.Date == DateTime.Today);
                                    Session.SendPacket(Session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("INSTANCE_ENTRIES"), entries), 10));
                                }
                                Session.SendPacket(UserInterfaceHelper.GenerateDialog(
                                    $"#wreq^3^{TeamMemberInInstance.Character.CharacterId} #wreq^0^1 {Language.Instance.GetMessageFromKey("ASK_JOIN_TEAM_TS")}"));
                            }
                            else
                            {
                                if (portal.DailyEntries > 0)
                                {
                                    var entries = portal.DailyEntries - Session.Character.GeneralLogs.CountLinq(s => s.LogType == "InstanceEntry" && short.Parse(s.LogData) == portal.Id && s.Timestamp.Date == DateTime.Today);
                                    Session.SendPacket(Session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("INSTANCE_ENTRIES"), entries), 10));
                                }
                                Session.SendPacket(portal.GenerateRbr());
                            }

                            break;

                        case 1:
                            if (!packet.Param.HasValue)
                            {
                                Session.Character.EnterInstance(portal);
                            }
                            break;

                        case 3:
                            ClientSession clientSession =
                                Session.Character.Group?.Sessions.Find(s => s.Character.CharacterId == packet.Param);
                            if (clientSession != null && clientSession.CurrentMapInstance.InstanceBag?.Lock == false && clientSession.Character?.Timespace is ScriptedInstance TeamTimeSpace && !TeamTimeSpace.IsIndividual)
                            {
                                if (portal.Id == TeamTimeSpace.Id)
                                {
                                    if (Session.Character.Level < TeamTimeSpace.LevelMinimum)
                                    {
                                        Session.SendPacket(
                                            UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("TOO_LOW_LVL"), 0));
                                        return;
                                    }
                                    if (Session.Character.Level > TeamTimeSpace.LevelMaximum)
                                    {
                                        Session.SendPacket(
                                            UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("TOO_HIGH_LVL"), 0));
                                        return;
                                    }

                                    var entries = TeamTimeSpace.DailyEntries - Session.Character.GeneralLogs.CountLinq(s => s.LogType == "InstanceEntry" && short.Parse(s.LogData) == TeamTimeSpace.Id && s.Timestamp.Date == DateTime.Today);
                                    if (TeamTimeSpace.DailyEntries == 0 || entries > 0)
                                    {
                                        foreach (Gift gift in TeamTimeSpace.RequiredItems)
                                        {
                                            if (Session.Character.Inventory.CountItem(gift.VNum) < gift.Amount)
                                            {
                                                Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                                    string.Format(Language.Instance.GetMessageFromKey("NO_ITEM_REQUIRED"),
                                                        ServerManager.GetItem(gift.VNum).Name), 0));
                                                return;
                                            }

                                            Session.Character.Inventory.RemoveItemAmount(gift.VNum, gift.Amount);
                                        }
                                        Session?.SendPackets(TeamTimeSpace.GenerateMinimap());
                                        Session?.SendPacket(TeamTimeSpace.GenerateMainInfo());
                                        Session?.SendPacket(TeamTimeSpace.FirstMap.InstanceBag.GenerateScore());
                                        if (TeamTimeSpace.StartX != 0 || TeamTimeSpace.StartY != 0)
                                        {
                                            ServerManager.Instance.ChangeMapInstance(Session.Character.CharacterId,
                                                clientSession.CurrentMapInstance.MapInstanceId, TeamTimeSpace.StartX, TeamTimeSpace.StartY);
                                        }
                                        else
                                        {
                                            ServerManager.Instance.TeleportOnRandomPlaceInMap(Session, clientSession.CurrentMapInstance.MapInstanceId);
                                        }
                                        Session.Character.Timespace = TeamTimeSpace;
                                    }
                                    else
                                    {
                                        Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("INSTANCE_NO_MORE_ENTRIES"), 0));
                                        Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("INSTANCE_NO_MORE_ENTRIES"), 10));
                                    }
                                }
                            }
                            else
                            {
                                GetWreq(new WreqPacket { Value = 0, Param = 1 });
                            }

                            // TODO: Implement
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// GitPacket packet
        /// </summary>
        /// <param name="packet"></param>
        public void Git(GitPacket packet)
        {
            MapButton button = Session.CurrentMapInstance.Buttons.Find(s => s.MapButtonId == packet.ButtonId);
            if (button != null)
            {
                if (Session.Character.IsVehicled)
                {
                    Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("CANT_DO_VEHICLED"), 10));
                    return;
                }

                Session.CurrentMapInstance.Broadcast(StaticPacketHelper.Out(UserType.Object, button.MapButtonId));
                button.RunAction();
                Session.CurrentMapInstance.Broadcast(button.GenerateIn());
            }
        }

        /// <summary>
        /// rxitPacket packet
        /// </summary>
        /// <param name="rxitPacket"></param>
        public void InstanceExit(RaidExitPacket rxitPacket)
        {
            if (rxitPacket?.State == 1)
            {
                if (Session.CurrentMapInstance?.MapInstanceType == MapInstanceType.TimeSpaceInstance && Session.Character.Timespace != null)
                {
                    if (Session.CurrentMapInstance.InstanceBag.Lock)
                    {
                        //5seed
                        if (Session.Character.Inventory.CountItem(1012) >= 5)
                        {
                            Session.CurrentMapInstance.InstanceBag.DeadList.Add(Session.Character.CharacterId);
                            Session.Character.Dignity -= 20;
                            if (Session.Character.Dignity < -1000)
                            {
                                Session.Character.Dignity = -1000;
                            }
                            Session.SendPacket(
                                Session.Character.GenerateSay(
                                    string.Format(Language.Instance.GetMessageFromKey("DIGNITY_LOST"), 20), 11));
                            Session.Character.Inventory.RemoveItemAmount(1012, 5);
                        }
                        else
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                string.Format(Language.Instance.GetMessageFromKey("NO_ITEM_REQUIRED"),
                                    ServerManager.GetItem(1012).Name), 0));
                            return;
                        }

                    }
                    else
                    {
                        //1seed
                    }
                    ServerManager.Instance.ChangeMap(Session.Character.CharacterId, Session.Character.MapId, Session.Character.MapX, Session.Character.MapY);
                }
                if (Session.CurrentMapInstance?.MapInstanceType == MapInstanceType.RaidInstance)
                {
                    /*if (Session.CurrentMapInstance.InstanceBag.Lock)
                    {
                        //5seed
                        if (Session.Character.Inventory.CountItem(1012) >= 5)
                        {
                            Session.CurrentMapInstance.InstanceBag.DeadList.Add(Session.Character.CharacterId);
                            Session.Character.Dignity -= 20;
                            if (Session.Character.Dignity < -1000)
                            {
                                Session.Character.Dignity = -1000;
                            }
                            Session.SendPacket(
                                Session.Character.GenerateSay(
                                    string.Format(Language.Instance.GetMessageFromKey("DIGNITY_LOST"), 20), 11));
                            Session.Character.Inventory.RemoveItemAmount(1012, 5);
                        }
                        else
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                string.Format(Language.Instance.GetMessageFromKey("NO_ITEM_REQUIRED"),
                                    ServerManager.GetItem(1012).Name), 0));
                            return;
                        }

                    }
                    else
                    {
                        //1seed
                    }*/
                    ServerManager.Instance.GroupLeave(Session);
                }
                else if (Session.CurrentMapInstance?.MapInstanceType == MapInstanceType.TalentArenaMapInstance)
                {
                    Session.Character.LeaveTalentArena(true);
                    ServerManager.Instance.TeleportOnRandomPlaceInMap(Session, ServerManager.Instance.ArenaInstance.MapInstanceId);
                }
            }
        }

        public void SearchName(TawPacket packet)
        {
            ConcurrentBag<ArenaTeamMember> at = ServerManager.Instance.ArenaTeams.ToList().FirstOrDefault(s => s.Any(o => o.Session?.Character?.Name == packet.Username && Session.CurrentMapInstance != null));
            if (at != null)
            {
                ServerManager.Instance.ChangeMapInstance(Session.Character.CharacterId, at.FirstOrDefault(s => s.Session != null).Session.CurrentMapInstance.MapInstanceId, 69, 100);

                var zenas = at.OrderBy(s => s.Order).FirstOrDefault(s => s.Session != null && !s.Dead && s.ArenaTeamType == ArenaTeamType.ZENAS);
                var erenia = at.OrderBy(s => s.Order).FirstOrDefault(s => s.Session != null && !s.Dead && s.ArenaTeamType == ArenaTeamType.ERENIA);
                Session.SendPacket(Session.Character.GenerateTaM(0));
                Session.SendPacket(Session.Character.GenerateTaM(3));
                Session.SendPacket("taw_sv 0");
                Session.SendPacket(zenas?.Session.Character.GenerateTaP(0, true));
                Session.SendPacket(erenia?.Session.Character.GenerateTaP(2, true));
                Session.SendPacket(zenas?.Session.Character.GenerateTaFc(0));
                Session.SendPacket(erenia?.Session.Character.GenerateTaFc(1));
            }
            else
            {
                Session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("USER_NOT_FOUND_IN_ARENA")));
            }
        }
        #endregion
    }
}