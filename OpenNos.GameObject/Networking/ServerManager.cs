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

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using OpenNos.Core;
using OpenNos.Data;
using OpenNos.DAL;
using OpenNos.Domain;
using OpenNos.GameObject.Helpers;
using OpenNos.Master.Library.Client;
using OpenNos.Master.Library.Data;
using OpenNos.XMLModel.Models.Quest;
using OpenNos.GameObject.Event;
using System.Net.Sockets;
using OpenNos.GameObject.Event.ACT6;
using OpenNos.GameObject.RainbowBattle;

namespace OpenNos.GameObject.Networking
{
    public class ServerManager : BroadcastableBase
    {
        #region Members

        public ThreadSafeSortedList<long, Group> ThreadSafeGroupList;

        public bool InShutdown;

        public bool IsReboot { get; set; }

        public bool ShutdownStop;

        public PercentBar Act6Zenas { get; set; }

        public PercentBar Act6Erenia { get; set; }

        private static readonly ConcurrentBag<Card> _cards = new ConcurrentBag<Card>();

        private static readonly ConcurrentBag<Item> _items = new ConcurrentBag<Item>();

        private static readonly ConcurrentDictionary<Guid, MapInstance> _mapinstances = new ConcurrentDictionary<Guid, MapInstance>();

        private static readonly ConcurrentBag<Map> _maps = new ConcurrentBag<Map>();

        private static readonly ConcurrentBag<NpcMonster> _npcmonsters = new ConcurrentBag<NpcMonster>();

        private static readonly CryptoRandom _random = new CryptoRandom();

        private static readonly ConcurrentBag<Skill> _skills = new ConcurrentBag<Skill>();

        private static ServerManager _instance;

        private List<DropDTO> _generalDrops;

        private bool _inRelationRefreshMode;

        private long _lastGroupId;

        private ThreadSafeSortedList<short, List<MapNpc>> _mapNpcs;

        private ThreadSafeSortedList<short, List<DropDTO>> _monsterDrops;

        private ThreadSafeSortedList<short, List<NpcMonsterSkill>> _monsterSkills;

        private ThreadSafeSortedList<int, RecipeListDTO> _recipeLists;

        private ThreadSafeSortedList<short, Recipe> _recipes;

        private ThreadSafeSortedList<int, List<ShopItemDTO>> _shopItems;

        private ThreadSafeSortedList<int, Shop> _shops;

        private ThreadSafeSortedList<int, List<ShopSkillDTO>> _shopSkills;

        private ThreadSafeSortedList<int, List<TeleporterDTO>> _teleporters;

        public static ScriptedInstance RaidInstance;

        public static MapInstance EntryMap;

        #endregion

        #region Instantiation

        private ServerManager()
        {
        }

        #endregion

        #region Properties

        public static ServerManager Instance => _instance ?? (_instance = new ServerManager());

        public List<BoxItemDTO> BoxItems { get; set; } = new List<BoxItemDTO>();

        public Act4Stat Act4AngelStat { get; set; }

        public Act4Stat Act4DemonStat { get; set; }
        
        public DateTime Act4RaidStart { get; set; }

        public ConcurrentBag<ScriptedInstance> Act6Raids { get; set; }

        public MapInstance ArenaInstance { get; private set; }

        public List<ArenaMember> ArenaMembers { get; set; } = new List<ArenaMember>();

        public ConcurrentBag<RainbowBattleTeam> RainbowBattleMembers { get; set; } = new ConcurrentBag<RainbowBattleTeam>();

        public List<ConcurrentBag<ArenaTeamMember>> ArenaTeams { get; set; } = new List<ConcurrentBag<ArenaTeamMember>>();

        public List<RainbowBattleMember> RainbowBattleMembersRegistered { get; set; } = new List<RainbowBattleMember>();

        public List<long> BannedCharacters { get; set; } = new List<long>();

        public ThreadSafeGenericList<BazaarItemLink> BazaarList { get; set; }

        public List<short> BossVNums { get; set; }

        public List<short> MapBossVNums { get; set; }

        public int ChannelId { get; set; }

        public bool CanRegisterRainbowBattle { get; set; }

        public List<CharacterRelationDTO> CharacterRelations { get; set; }

        public ConfigurationObject Configuration { get; set; }

        public bool EventInWaiting { get; set; }

        public MapInstance FamilyArenaInstance { get; private set; }

        public ThreadSafeSortedList<long, Family> FamilyList { get; set; }

        public ThreadSafeGenericLockedList<Group> GroupList { get; set; } = new ThreadSafeGenericLockedList<Group>();

        public List<Group> Groups => ThreadSafeGroupList.GetAllItems();

        public bool IceBreakerInWaiting { get; set; }

        public bool InBazaarRefreshMode { get; set; }

        public DateTime LastFCSent { get; set; }

        private DateTime LastMaintenanceAdvert { get; set; }

        public MallAPIHelper MallApi { get; set; }

        public List<int> MateIds { get; internal set; } = new List<int>();

        public long MaxBankGold { get; set; }

        public List<PenaltyLogDTO> PenaltyLogs { get; set; }

        public ThreadSafeSortedList<long, QuestModel> QuestList { get; set; }

        public ConcurrentBag<ScriptedInstance> Raids { get; set; }

        public ConcurrentBag<ScriptedInstance> TimeSpaces { get; set; }

        public List<Schedule> Schedules { get; set; }

        public string ServerGroup { get; set; }

        public List<MapInstance> SpecialistGemMapInstances { get; set; } = new List<MapInstance>();

        public List<EventType> StartedEvents { get; set; }

        public Task TaskShutdown { get; set; }

        public List<CharacterDTO> TopComplimented { get; set; }

        public List<CharacterDTO> TopPoints { get; set; }

        public List<CharacterDTO> TopReputation { get; set; }

        public Guid WorldId { get; private set; }
        
        public ThreadSafeSortedList<long, ClientSession> CharacterScreenSessions { get; set; }

        public short DailyRewardVNum = (short)Convert.ToInt32(ConfigurationManager.AppSettings["DailyRewardVNum"]);
        private FactionType raidType;

        public List<Quest> Quests { get; set; }

        public long? FlowerQuestId { get; set; }

        public bool IsWorldServer => WorldId != Guid.Empty;

        #endregion

        #region Methods

        public List<MapNpc> GetMapNpcsByVNum(short npcVNum)
        {
            return GetAllMapInstances().Where(mapInstance => mapInstance != null && !mapInstance.IsScriptedInstance)
                .SelectMany(mapInstance => mapInstance.Npcs.Where(mapNpc => mapNpc?.NpcVNum == npcVNum)).ToList();
        }

        public void AddGroup(Group group) => ThreadSafeGroupList[group.GroupId] = group;


        public void AskPvpRevive(long characterId)
        {
            ClientSession session = GetSessionByCharacterId(characterId);
            if (session?.HasSelectedCharacter == true)
            {
                if (session.Character.IsVehicled)
                {
                    session.Character.RemoveVehicle();
                }
                session.Character.DisableBuffs(BuffType.All);
                session.SendPacket(session.Character.GenerateStat());
                session.SendPacket(session.Character.GenerateCond());
                session.SendPackets(UserInterfaceHelper.GenerateVb());

                switch (session.CurrentMapInstance.MapInstanceType)
                {
                    case MapInstanceType.TalentArenaMapInstance:
                        ConcurrentBag<ArenaTeamMember> team = Instance.ArenaTeams.ToList().FirstOrDefault(s => s.Any(o => o.Session == session));
                        var member = team?.FirstOrDefault(s => s.Session == session);
                        if (member != null)
                        {
                            if (member.LastSummoned == null && team.OrderBy(tm3 => tm3.Order).FirstOrDefault(tm3 => tm3.ArenaTeamType == member.ArenaTeamType && !tm3.Dead)?.Session == session)
                            {
                                session.CurrentMapInstance.InstanceBag.DeadList.Add(session.Character.CharacterId);
                                member.Dead = true;
                                team.ToList().Where(s => s.LastSummoned != null).ToList().ForEach(s =>
                                {
                                    s.LastSummoned = null;
                                    s.Session.Character.PositionX = s.ArenaTeamType == ArenaTeamType.ERENIA ? (short)120 : (short)19;
                                    s.Session.Character.PositionY = s.ArenaTeamType == ArenaTeamType.ERENIA ? (short)39 : (short)40;
                                    session.CurrentMapInstance.Broadcast(s.Session.Character.GenerateTp());
                                    s.Session.SendPacket(UserInterfaceHelper.Instance.GenerateTaSt(TalentArenaOptionType.Watch));

                                    List<BuffType> bufftodisable = new List<BuffType> { BuffType.Bad };
                                    s.Session.Character.DisableBuffs(BuffType.All);
                                    s.Session.Character.Hp = (int)s.Session.Character.HPLoad();
                                    s.Session.Character.Mp = (int)s.Session.Character.MPLoad();
                                });
                                var killer = team.OrderBy(s => s.Order).FirstOrDefault(s => !s.Dead && s.ArenaTeamType != member.ArenaTeamType);
                                session.CurrentMapInstance.Broadcast(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("TEAM_WINNER_ARENA_ROUND"), killer?.Session.Character.Name, killer?.ArenaTeamType), 10));
                                session.CurrentMapInstance.Broadcast(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("TEAM_WINNER_ARENA_ROUND"), killer?.Session.Character.Name, killer?.ArenaTeamType), 0));
                                session.CurrentMapInstance.Sessions.Except(team.Where(s => s.ArenaTeamType == killer?.ArenaTeamType).Select(s => s.Session)).ToList().ForEach(o =>
                                {
                                    if (killer?.ArenaTeamType == ArenaTeamType.ERENIA)
                                    {
                                        o.SendPacket(killer.Session.Character.GenerateTaM(2));
                                        o.SendPacket(killer.Session.Character.GenerateTaP(2, true));
                                    }
                                    else
                                    {
                                        o.SendPacket(member.Session.Character.GenerateTaM(2));
                                        o.SendPacket(member.Session.Character.GenerateTaP(2, true));
                                    }

                                    o.SendPacket($"taw_d {member.Session.Character.CharacterId}");
                                    o.SendPacket(member.Session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("WINNER_ARENA_ROUND"), killer?.Session.Character.Name/*, killer?.ArenaTeamType*/, member.Session.Character.Name), 10));
                                    o.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("WINNER_ARENA_ROUND"), killer?.Session.Character.Name/*, killer?.ArenaTeamType*/, member.Session.Character.Name), 0));
                                });
                                team.Replace(friends => friends.ArenaTeamType == member.ArenaTeamType).ToList().ForEach(friends => { friends.Session.SendPacket(friends.Session.Character.GenerateTaFc(0)); });
                            }
                            else
                            {
                                member.LastSummoned = null;
                                ArenaTeamMember tm = team.OrderBy(tm3 => tm3.Order).FirstOrDefault(tm3 => tm3.ArenaTeamType == member.ArenaTeamType && !tm3.Dead);
                                team.Replace(friends => friends.ArenaTeamType == member.ArenaTeamType).ToList().ForEach(friends => { friends.Session.SendPacket(tm.Session.Character.GenerateTaFc(0)); });
                            }

                            team.ToList().ForEach(arenauser =>
                            {
                                if (arenauser?.Session?.Character != null)
                                {
                                    arenauser.Session.SendPacket(arenauser.Session.Character.GenerateTaP(2, true));
                                    arenauser.Session.SendPacket(arenauser.Session.Character.GenerateTaM(2));
                                }
                            });

                            Observable.Timer(TimeSpan.FromSeconds(3)).Subscribe(s =>
                            {
                                if (member?.Session != null)
                                {
                                    member.Session.Character.PositionX = member.ArenaTeamType == ArenaTeamType.ERENIA ? (short)120 : (short)19;
                                    member.Session.Character.PositionY = member.ArenaTeamType == ArenaTeamType.ERENIA ? (short)39 : (short)40;
                                    member.Session.CurrentMapInstance.Broadcast(member.Session, member.Session.Character.GenerateTp());
                                    member.Session.SendPacket(UserInterfaceHelper.Instance.GenerateTaSt(TalentArenaOptionType.Watch));
                                }
                            });

                            Observable.Timer(TimeSpan.FromSeconds(4)).Subscribe(s =>
                            {
                                if (session != null)
                                {
                                    session.Character.Hp = (int)session.Character.HPLoad();
                                    session.Character.Mp = (int)session.Character.MPLoad();
                                    session.CurrentMapInstance?.Broadcast(session, session.Character.GenerateRevive());
                                    session.SendPacket(session.Character.GenerateStat());
                                }
                            });
                        }

                        break;

                    case MapInstanceType.RainbowBattle:
                        IDisposable obs = Observable.Interval(TimeSpan.FromSeconds(1)).Subscribe(s =>
                        {
                            session.CurrentMapInstance?.Broadcast(session.Character?.GenerateEff(35));
                        });

                        session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("RESPAWN_Rainbow_BATTLE")));

                        session.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("PLAYER_DIE_RBB"), $"{session.Character.Name}"), 0));

                        session.CurrentMapInstance?.Broadcast(session, session.Character.GenerateTp());
                        Observable.Timer(TimeSpan.FromMilliseconds(5000)).Subscribe(o =>
                        {
                            session.Character.Hp = (int)session.Character.HPLoad();
                            session.Character.Mp = (int)session.Character.MPLoad();

                            if (Event.RAINBOWBATTLE.RainbowBattle.Map != null)
                            {
                                Instance.ChangeMapInstance(
                                    session.Character.CharacterId,
                                    Event.RAINBOWBATTLE.RainbowBattle.Map.MapInstanceId,
                                    60, 35);

                                //fbt 6 2 1 1
                            }

                            session.CurrentMapInstance?.Broadcast(session, session.Character.GenerateTp());
                            session.CurrentMapInstance?.Broadcast(session.Character.GenerateRevive());
                            session.SendPacket(session.Character.GenerateStat());
                            obs?.Dispose();
                        });
                        break;

                    default:
                        if (session.CurrentMapInstance == ArenaInstance || session.CurrentMapInstance == FamilyArenaInstance)
                        {
                            session.Character.LeaveTalentArena(true);
                            session.SendPacket(UserInterfaceHelper.GenerateDialog($"#revival^2 #revival^1 {Language.Instance.GetMessageFromKey("ASK_REVIVE_PVP")}"));
                            Task.Factory.StartNew(async () =>
                            {
                                var revive = true;
                                for (int i = 1; i <= 30; i++)
                                {
                                    await Task.Delay(1000);
                                    if (session.Character.Hp <= 0)
                                    {
                                        continue;
                                    }

                                    revive = false;
                                    break;
                                }

                                if (revive)
                                {
                                    ReviveTask(session);
                                }
                            });
                        }
                        else
                        {
                            AskRevive(characterId);
                        }
                        break;
                }
            }
        }

        // PacketHandler -> with Callback?
        public void AskRevive(long characterId)
        {
            ClientSession session = GetSessionByCharacterId(characterId);
            if (session?.HasSelectedCharacter == true && session.HasCurrentMapInstance)
            {
                if (session.Character.IsVehicled)
                {
                    session.Character.RemoveVehicle();
                }

                session.Character.ClearLaurena();

                session.Character.DisableBuffs(BuffType.All);
                session.Character.BattleEntity.AdditionalHp = 0;
                session.Character.BattleEntity.AdditionalMp = 0;
                session.SendPacket(session.Character.GenerateAdditionalHpMp());
                session.SendPacket(session.Character.GenerateStat());
                session.SendPacket(session.Character.GenerateCond());
                session.SendPackets(UserInterfaceHelper.GenerateVb());

                switch (session.CurrentMapInstance.MapInstanceType)
                {
                    case MapInstanceType.BaseMapInstance:
                        if(ChannelId == 51)
                        {
                            session.Character.Dignity -= (short)(session.Character.Level < 50 ? session.Character.Level : 50);
                            if (session.Character.Dignity < -1000)
                            {
                                session.Character.Dignity = -1000;
                                session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("LOSE_DIGNITY"), (short)(session.Character.Level < 50 ? session.Character.Level : 50)), 11));
                            }
                            session.SendPacket(session.Character.GenerateFd());
                            session.CurrentMapInstance?.Broadcast(session, session.Character.GenerateIn(InEffect: 1), ReceiverType.AllExceptMe);
                            session.CurrentMapInstance?.Broadcast(session, session.Character.GenerateGidx(), ReceiverType.AllExceptMe);
                            session.SendPacket(UserInterfaceHelper.GenerateDialog($"#revival^0 #revival^1 {(session.Character.Level > 20 ? Language.Instance.GetMessageFromKey("ASK_REVIVE") : Language.Instance.GetMessageFromKey("ASK_REVIVE_FREE"))}"));
                            session.Character.Hp = (int)session.Character.HPLoad();
                            session.Character.Mp = (int)session.Character.MPLoad();
                            short x = (short)(39 + RandomNumber(-2, 3));
                            short y = (short)(42 + RandomNumber(-2, 3));
                            if (session.Character.Faction == FactionType.Angel)
                            {
                                ChangeMap(session.Character.CharacterId, 130, x, y);
                            }
                            else if (session.Character.Faction == FactionType.Demon)
                            {
                                ChangeMap(session.Character.CharacterId, 131, x, y);
                            }
                        }

                        if (ChannelId != 51)
                        {
                            if(session.CurrentMapInstance.Map.MapId == 150)
                            {
                                const int savior = 1211;
                                if (session.Character.Inventory.CountItem(savior) >= 1)
                                {
                                    session.SendPacket(UserInterfaceHelper.GenerateDialog($"#revival^0 #revival^1 {Language.Instance.GetMessageFromKey("ASK_REVIVE_LOD")}"));
                                    ReviveTask(session);
                                }
                                else
                                {
                                    Observable.Timer(TimeSpan.FromSeconds(10)).Subscribe(o => ServerManager.Instance.ReviveFirstPosition(session.Character.CharacterId));
                                }
                            }
                            else
                            {
                                session.Character.Dignity -= (short)(session.Character.Level < 50 ? session.Character.Level : 50);
                                if (session.Character.Dignity < -1000)
                                {
                                    session.Character.Dignity = -1000;
                                    session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("LOSE_DIGNITY"), (short)(session.Character.Level < 50 ? session.Character.Level : 50)), 11));
                                }
                                session.SendPacket(session.Character.GenerateFd());
                                session.CurrentMapInstance?.Broadcast(session, session.Character.GenerateIn(InEffect: 1), ReceiverType.AllExceptMe);
                                session.CurrentMapInstance?.Broadcast(session, session.Character.GenerateGidx(), ReceiverType.AllExceptMe);
                                session.SendPacket(UserInterfaceHelper.GenerateDialog($"#revival^0 #revival^1 {(session.Character.Level > 20 ? Language.Instance.GetMessageFromKey("ASK_REVIVE") : Language.Instance.GetMessageFromKey("ASK_REVIVE_FREE"))}"));
                                ReviveTask(session);
                            }

                        }
                        break;

                    case MapInstanceType.TimeSpaceInstance:
                        lock (session.CurrentMapInstance.InstanceBag.DeadList)
                        {
                            if (session.CurrentMapInstance.InstanceBag.Lives - session.CurrentMapInstance.InstanceBag.DeadList.ToList().Count(s => s == session.Character.CharacterId) < 0)
                            {
                                session.Character.Hp = 1;
                                session.Character.Mp = 1;
                            }
                            else
                            {
                                session.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("YOU_HAVE_LIFE"), session.CurrentMapInstance.InstanceBag.Lives - session.CurrentMapInstance.InstanceBag.DeadList.Count(e => e == session.Character.CharacterId)), 0));
                                session.SendPacket(UserInterfaceHelper.GenerateDialog($"#revival^1 #revival^1 {Language.Instance.GetMessageFromKey("ASK_REVIVE_TS")}"));
                                ReviveTask(session);
                            }
                        }
                        break;

                    case MapInstanceType.RaidInstance:
                        List<long> save = session.CurrentMapInstance.InstanceBag.DeadList.ToList();
                        if (session.CurrentMapInstance.InstanceBag.Lives - save.Count < 0)
                        {
                            session.Character.Hp = 1;
                            session.Character.Mp = 1;
                            session.Character.Group?.Raid.End();
                        }
                        else if (3 - save.Count(s => s == session.Character.CharacterId) > 0)
                        {
                            session.SendPacket(UserInterfaceHelper.GenerateInfo(string.Format(Language.Instance.GetMessageFromKey("YOU_HAVE_LIFE"), 2 - session.CurrentMapInstance.InstanceBag.DeadList.Count(s => s == session.Character.CharacterId))));
                            
                            session.Character.Group?.Sessions.ForEach(grpSession =>
                            {
                                grpSession?.SendPacket(grpSession.Character.Group?.GeneraterRaidmbf(grpSession));
                                grpSession?.SendPacket(grpSession.Character.Group?.GenerateRdlst());
                            });
                            Task.Factory.StartNew(async () =>
                            {
                                await Task.Delay(20000).ConfigureAwait(false);
                                Instance.ReviveFirstPosition(session.Character.CharacterId);
                            });
                        }
                        else
                        {
                            Group grp = session.Character?.Group;
                            session.Character.Hp = 1;
                            session.Character.Mp = 1;
                            ChangeMap(session.Character.CharacterId, session.Character.MapId, session.Character.MapX, session.Character.MapY);
                            session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("KICK_RAID"), 0));
                            if (grp != null)
                            {
                                grp.LeaveGroup(session);
                                grp.Sessions.ForEach(s =>
                                {
                                    s.SendPacket(grp.GenerateRdlst());
                                    s.SendPacket(s.Character.Group?.GeneraterRaidmbf(s));
                                    s.SendPacket(s.Character.GenerateRaid(0));
                                });
                            }
                            session.SendPacket(session.Character.GenerateRaid(1, true));
                            session.SendPacket(session.Character.GenerateRaid(2, true));
                        }
                        break;

                    case MapInstanceType.LodInstance:
                        const int saver = 1211;
                        if (session.Character.Inventory.CountItem(saver) >= 1)
                        {
                            session.SendPacket(UserInterfaceHelper.GenerateDialog($"#revival^0 #revival^1 {Language.Instance.GetMessageFromKey("ASK_REVIVE_LOD")}"));
                            ReviveTask(session);
                        }
                        else
                        {
                            Observable.Timer(TimeSpan.FromSeconds(10)).Subscribe(o => ServerManager.Instance.ReviveFirstPosition(session.Character.CharacterId));
                        }
                        break;

                    case MapInstanceType.Act4Berios:
                    case MapInstanceType.Act4Calvina:
                    case MapInstanceType.Act4Hatus:
                    case MapInstanceType.Act4Morcos:
                        session.SendPacket(UserInterfaceHelper.GenerateDialog($"#revival^0 #revival^1 {string.Format(Language.Instance.GetMessageFromKey("ASK_REVIVE_ACT4RAID"), session.Character.Level * 10)}"));
                        ReviveTask(session);
                        break;

                    case MapInstanceType.CaligorInstance:
                        session.SendPacket(UserInterfaceHelper.GenerateDialog($"#revival^0 #revival^1 {Language.Instance.GetMessageFromKey("ASK_REVIVE_CALIGOR")}"));
                        ReviveTask(session);
                        break;

                    default:
                        Instance.ReviveFirstPosition(session.Character.CharacterId);
                        break;
                }
            }
        }

        public void BazaarRefresh(long bazaarItemId)
        {
            InBazaarRefreshMode = true;
            CommunicationServiceClient.Instance.UpdateBazaar(ServerGroup, bazaarItemId);
            SpinWait.SpinUntil(() => !InBazaarRefreshMode);
        }

        public void ChangeMap(long id, short? mapId = null, short? mapX = null, short? mapY = null)
        {
            ClientSession session = GetSessionByCharacterId(id);
            if (session?.Character != null)
            {
                if (mapId != null)
                {
                    MapInstance gotoMapInstance = GetMapInstanceByMapId(mapId.Value);
                    if (session.Character.Level < gotoMapInstance.MinLevel || session.Character.Level > gotoMapInstance.MaxLevel)
                    {
                        session.SendPacket(UserInterfaceHelper.GenerateInfo(string.Format(Language.Instance.GetMessageFromKey("LOW_LVL_MAP"), gotoMapInstance.MinLevel, gotoMapInstance.MaxLevel)));
                        return;
                    }

                    session.Character.MapInstanceId = GetBaseMapInstanceIdByMapId((short)mapId);
                }
                ChangeMapInstance(id, session.Character.MapInstanceId, mapX, mapY);
            }
        }

        public void ChangeMapInstance(long characterId, Guid mapInstanceId, int? mapX = null, int? mapY = null, bool noAggroLoss = false)
        {
            ClientSession session = GetSessionByCharacterId(characterId);
            if (session?.Character != null && !session.Character.IsChangingMapInstance)
            {
                session.Character.IsChangingMapInstance = true;

                session.Character.RemoveBuff(620);

                session.Character.WalkDisposable?.Dispose();
                SpinWait.SpinUntil(() => session.Character.LastSkillUse.AddMilliseconds(500) <= DateTime.Now);
                try
                {
                    MapInstance gotoMapInstance = GetMapInstance(mapInstanceId);

                    session.SendPacket(StaticPacketHelper.Cancel(2, characterId));

                    if (session.Character.InExchangeOrTrade)
                    {
                        session.Character.CloseExchangeOrTrade();
                    }

                    if (session.Character.HasShopOpened)
                    {
                        session.Character.CloseShop();
                    }

                    session.Character.BattleEntity.ClearOwnFalcon();
                    session.Character.BattleEntity.ClearEnemyFalcon();

                    if (!noAggroLoss)
                    {
                        session.CurrentMapInstance.RemoveMonstersTarget(session.Character.CharacterId);
                    }

                    session.Character.BattleEntity.RemoveOwnedMonsters();

                    if (gotoMapInstance == CaligorRaid.CaligorMapInstance && session.Character.MapInstance != CaligorRaid.CaligorMapInstance)
                    {
                        session.Character.OriginalFaction = (byte)session.Character.Faction;
                        if (CaligorRaid.CaligorMapInstance.Sessions.ToList().Count(s => s.Character?.Faction == FactionType.Angel) >
                            CaligorRaid.CaligorMapInstance.Sessions.ToList().Count(s => s.Character?.Faction == FactionType.Demon)
                            && session.Character.Faction != FactionType.Demon)
                        {
                            session.Character.Faction = FactionType.Demon;
                            session.SendPacket(session.Character.GenerateFaction());
                        }
                        else if (CaligorRaid.CaligorMapInstance.Sessions.ToList().Count(s => s.Character?.Faction == FactionType.Demon) >
                                 CaligorRaid.CaligorMapInstance.Sessions.ToList().Count(s => s.Character?.Faction == FactionType.Angel)
                                 && session.Character.Faction != FactionType.Angel)
                        {
                            session.Character.Faction = FactionType.Angel;
                            session.SendPacket(session.Character.GenerateFaction());
                        }
                        if (mapX <= 0 && mapY <= 0)
                        {
                            switch (session.Character.Faction)
                            {
                                case FactionType.Angel:
                                    mapX = 58;
                                    mapY = 164;
                                    break;
                                case FactionType.Demon:
                                    mapX = 121;
                                    mapY = 164;
                                    break;
                            }
                        }
                    }
                    else if (gotoMapInstance != CaligorRaid.CaligorMapInstance && session.Character.MapInstance == CaligorRaid.CaligorMapInstance)
                    {
                        if (session.Character.OriginalFaction != -1 && (byte)session.Character.Faction != session.Character.OriginalFaction)
                        {
                            session.Character.Faction = (FactionType)session.Character.OriginalFaction;
                            session.SendPacket(session.Character.GenerateFaction());
                        }
                    }

                    session.CurrentMapInstance.UnregisterSession(session.Character.CharacterId);
                    LeaveMap(session.Character.CharacterId);

                    // cleanup sending queue to avoid sending uneccessary packets to it
                    session.ClearLowPriorityQueue();

                    session.Character.IsSitting = false;
                    session.Character.MapInstanceId = mapInstanceId;
                    session.CurrentMapInstance = session.Character.MapInstance;

                    if (!session.Character.MapInstance.MapInstanceType.Equals(MapInstanceType.TimeSpaceInstance) && session.Character.Timespace != null)
                    {
                        session.Character.TimespaceRewardGotten = false;
                        session.Character.RemoveTemporalMates();
                        if (session.Character.Timespace.SpNeeded?[(byte)session.Character.Class] != 0)
                        {
                            ItemInstance specialist = session.Character.Inventory?.LoadBySlotAndType((byte)EquipmentType.Sp, InventoryType.Wear);
                            if (specialist != null || specialist.ItemVNum == session.Character.Timespace.SpNeeded?[(byte)session.Character.Class])
                            {
                                Observable.Timer(TimeSpan.FromMilliseconds(300)).Subscribe(s => session.Character.RemoveSp(specialist.ItemVNum, true));
                            }
                        }
                        session.Character.Timespace = null;
                    }

                    if (session.Character.Hp <= 0 && !session.Character.IsSeal)
                    {
                        session.Character.Hp = 1;
                        session.Character.Mp = 1;
                    }

                    session.Character.LeaveTalentArena(false);

                    if (session.Character.MapInstance.MapInstanceType == MapInstanceType.BaseMapInstance)
                    {
                        session.Character.MapId = session.Character.MapInstance.Map.MapId;
                        if (mapX != null && mapY != null)
                        {
                            session.Character.MapX = (short)mapX.Value;
                            session.Character.MapY = (short)mapY.Value;
                        }
                    }

                    if (mapX != null && mapY != null)
                    {
                        session.Character.PositionX = (short)mapX.Value;
                        session.Character.PositionY = (short)mapY.Value;
                    }

                    foreach (Mate mate in session.Character.Mates.Where(m => (m.IsTeamMember && !session.Character.IsVehicled) || m.IsTemporalMate))
                    {
                        mate.PositionX =
                            (short)(session.Character.PositionX + (mate.MateType == MateType.Partner ? -1 : 1));
                        mate.PositionY = (short)(session.Character.PositionY + 1);
                        if (session.Character.MapInstance.Map.IsBlockedZone(mate.PositionX, mate.PositionY))
                        {
                            mate.PositionX = session.Character.PositionX;
                            mate.PositionY = session.Character.PositionY;
                        }
                        mate.UpdateBushFire();
                    }

                    session.Character.UpdateBushFire();
                    session.CurrentMapInstance.RegisterSession(session);
                    session.Character.LoadSpeed();

                    /* if (gotoMapInstance.Map?.MapId != 2514)
                     {
                         session.Character.ClearLaurena();
                     }*/

                    session.SendPacket(session.Character.GenerateTitInfo());
                    session.SendPacket(session.Character.GenerateCInfo());
                    session.SendPacket(session.Character.GenerateCMode());
                    session.SendPacket(session.Character.GenerateEq());
                    session.SendPacket(session.Character.GenerateEquipment());
                    session.SendPacket(session.Character.GenerateLev());
                    session.SendPacket(session.Character.GenerateStat());
                    session.SendPacket(session.Character.GenerateAt());
                    session.SendPacket(session.Character.GenerateCond());
                    session.SendPacket(session.Character.GenerateCMap());
                    session.SendPackets(session.Character.GenerateStatChar());
                    session.SendPacket(session.Character.GeneratePairy());
                    session.SendPacket(Character.GenerateAct());
                    session.SendPacket(session.Character.GenerateScpStc());
                    session.Character.GenerateAscrPacket();

                    if (session.CurrentMapInstance.OnSpawnEvents.Any())
                    {
                        session.CurrentMapInstance.OnSpawnEvents.ForEach(e => EventHelper.Instance.RunEvent(e, session: session));
                    }

                    if (ChannelId == 51)
                    {
                        session.SendPacket(session.Character.GenerateFc());

                        if (mapInstanceId == session.Character.Family?.Act4Raid?.MapInstanceId ||
                            mapInstanceId == session.Character.Family?.Act4RaidBossMap?.MapInstanceId)
                        {
                            session.SendPacket(session.Character.GenerateDG());
                        }
                    }

                    if (session.Character.Group?.Raid?.InstanceBag?.Lock == true)
                    {
                        session.SendPacket(session.Character.Group.GeneraterRaidmbf(session));

                        if (session.CurrentMapInstance.Monsters.Any(s => s.IsBoss))
                        {
                            session.Character.Group.Sessions?.Where(s => s?.Character != null).ForEach(s =>
                            {
                                if (!s.Character.IsChangingMapInstance && s.CurrentMapInstance != session.CurrentMapInstance)
                                {
                                    ChangeMapInstance(s.Character.CharacterId, session.CurrentMapInstance.MapInstanceId, mapX, mapY);
                                }
                            });
                        }
                    }
                    if (session.Character.MapInstance == session.Character.Family?.Act4RaidBossMap)
                    {
                        session.Character.Family.Act4Raid.Sessions.Where(s => !s.Character.IsChangingMapInstance).ToList().ForEach(s =>
                        {
                            ChangeMapInstance(s.Character.CharacterId, session.CurrentMapInstance.MapInstanceId, mapX, mapY);
                        });
                    }

                    Parallel.ForEach(
                         session.CurrentMapInstance.Sessions.Where(s =>
                             s.Character?.InvisibleGm == false &&
                             s.Character.CharacterId != session.Character.CharacterId), visibleSession =>
                             {
                                 if (ChannelId != 51 || session.Character.Faction == visibleSession.Character.Faction)
                                 {
                                     session.SendPacket(visibleSession.Character.GenerateIn());
                                     session.SendPacket(visibleSession.Character.GenerateGidx());
                                     visibleSession.Character.Mates
                                        .Where(m => (m.IsTeamMember || m.IsTemporalMate) && m.CharacterId != session.Character.CharacterId)
                                        .ToList().ForEach(m => session.SendPacket(m.GenerateIn()));
                                 }
                                 else
                                 {
                                     session.SendPacket(visibleSession.Character.GenerateIn(true, session.Account.Authority));
                                     visibleSession.Character.Mates
                                        .Where(m => (m.IsTeamMember || m.IsTemporalMate) && m.CharacterId != session.Character.CharacterId)
                                        .ToList().ForEach(m => session.SendPacket(m.GenerateIn(true, ChannelId == 51, session.Account.Authority)));
                                 }
                             });
                    session.SendPacket(session.CurrentMapInstance.GenerateMapDesignObjects());
                    session.SendPackets(session.CurrentMapInstance.GetMapDesignObjectEffects());

                    session.CurrentMapInstance.Npcs.Where(npc => npc != null).ToList().ForEach(npc =>
                    {
                        if (npc.IsMate)
                        {
                            Mate npcPartner = new Mate(session.Character, npc.Npc, npc.Npc.Level, MateType.Partner, true, npc.IsTsReward, npc.IsProtected);
                            session.Character.AddPet(npcPartner);
                            session.CurrentMapInstance.RemoveNpc(npc);
                        }
                    });

                    session.SendPackets(session.CurrentMapInstance.GetMapItems());

                    MapInstancePortalHandler
                        .GenerateMinilandEntryPortals(session.CurrentMapInstance.Map.MapId,
                            session.Character.Miniland.MapInstanceId).ForEach(p => session.SendPacket(p.GenerateGp()));
                    MapInstancePortalHandler
                        .GenerateAct4EntryPortals(session.CurrentMapInstance.Map.MapId).ForEach(p => session.SendPacket(p.GenerateGp()));

                    if (session.CurrentMapInstance.InstanceBag?.Clock?.Enabled == true)
                    {
                        session.SendPacket(session.CurrentMapInstance.InstanceBag.Clock.GetClock());
                    }

                    if (session.CurrentMapInstance.Clock.Enabled)
                    {
                        session.SendPacket(session.CurrentMapInstance.Clock.GetClock());
                    }

                    // TODO: fix this
                    if (session.Character.MapInstance.Map.MapTypes.Any(m =>
                        m.MapTypeId == (short)MapTypeEnum.CleftOfDarkness))
                    {
                        session.SendPacket("bc 0 0 0");
                    }

                    if (!session.Character.InvisibleGm)
                    {
                        Parallel.ForEach(session.CurrentMapInstance.Sessions.Where(s => s.Character != null), s =>
                        {
                            if (ChannelId != 51 || session.Character.Faction == s.Character.Faction)
                            {
                                s.SendPacket(session.Character.GenerateIn());
                                s.SendPacket(session.Character.GenerateGidx());
                                session.Character.Mates.Where(m => m.IsTeamMember || m.IsTemporalMate).ToList()
                                    .ForEach(m => s.SendPacket(m.GenerateIn(false, ChannelId == 51)));
                            }
                            else
                            {
                                s.SendPacket(session.Character.GenerateIn(true, s.Account.Authority));
                                session.Character.Mates.Where(m => m.IsTeamMember || m.IsTemporalMate).ToList()
                                    .ForEach(m => s.SendPacket(m.GenerateIn(true, ChannelId == 51, s.Account.Authority)));
                            }

                            if (session.Character.GetBuff(BCardType.CardType.SpecialEffects, (byte)AdditionalTypes.SpecialEffects.ShadowAppears) is int[] EffectData && EffectData[0] != 0 && EffectData[1] != 0)
                            {
                                s.CurrentMapInstance.Broadcast($"guri 0 {(short)UserType.Player} {session.Character.CharacterId} {EffectData[0]} {EffectData[1]}");
                            }

                            session.Character.Mates.Where(m => m.IsTeamMember || m.IsTemporalMate).ToList()
                                .ForEach(m =>
                                {
                                    if (session.Character.IsVehicled)
                                    {
                                        m.PositionX = session.Character.PositionX;
                                        m.PositionY = session.Character.PositionY;
                                    }

                                    if (m.GetBuff(BCardType.CardType.SpecialEffects, (byte)AdditionalTypes.SpecialEffects.ShadowAppears) is int[] MateEffectData && MateEffectData[0] != 0 && MateEffectData[1] != 0)
                                    {
                                        s.CurrentMapInstance.Broadcast($"guri 0 {(short)UserType.Monster} {m.MateTransportId} {MateEffectData[0]} {MateEffectData[1]}");
                                    }
                                });
                        });
                    }

                    session.SendPacket(session.Character.GeneratePinit());

                    if (session.Character.Mates.FirstOrDefault(s => (s.IsTeamMember || s.IsTemporalMate) && s.MateType == MateType.Partner && s.IsUsingSp) is Mate partner)
                    {
                        session.SendPacket(partner.Sp.GeneratePski());
                    }

                    session.Character.Mates.ForEach(s => session.SendPacket(s.GenerateScPacket()));
                    session.SendPackets(session.Character.GeneratePst());

                    if (session.Character.Size != 10)
                    {
                        session.SendPacket(session.Character.GenerateScal());
                    }

                    if (session.Character.Size != 10)
                    {
                        session.SendPacket(session.Character.GenerateScal());
                    }

                    if (session.CurrentMapInstance?.IsDancing == true && !session.Character.IsDancing)
                    {
                        session.CurrentMapInstance?.Broadcast("dance 2");
                    }
                    else if (session.CurrentMapInstance?.IsDancing == false && session.Character.IsDancing)
                    {
                        session.Character.IsDancing = false;
                        session.CurrentMapInstance?.Broadcast("dance");
                    }

                    if (Groups != null)
                    {
                        Parallel.ForEach(Groups, group =>
                        {
                            foreach (ClientSession groupSession in group.Sessions.GetAllItems())
                            {
                                ClientSession groupCharacterSession = Sessions.FirstOrDefault(s =>
                                    s.Character != null &&
                                    s.Character.CharacterId == groupSession.Character.CharacterId &&
                                    s.CurrentMapInstance == groupSession.CurrentMapInstance);
                                if (groupCharacterSession == null)
                                {
                                    continue;
                                }

                                groupSession.SendPacket(groupSession.Character.GeneratePinit());
                                groupSession.SendPackets(groupSession.Character.GeneratePst());
                            }
                        });
                    }

                    if (session.Character.Group?.GroupType == GroupType.Group)
                    {
                        session.CurrentMapInstance?.Broadcast(session, session.Character.GeneratePidx(),
                            ReceiverType.AllExceptMe);
                    }                  
                    if (session.CurrentMapInstance?.Map.MapTypes.All(s => s.MapTypeId != (short)MapTypeEnum.Act52) == true && session.Character.Buff.Any(s => s.Card.CardId == 339)) //Act5.2 debuff
                    {
                        session.Character.RemoveBuff(339);
                    }
                    else if (session.CurrentMapInstance?.Map.MapTypes.Any(s => s.MapTypeId == (short)MapTypeEnum.Act52) == true && session.CurrentMapInstance.Map?.MapId != 2640 && session.Character.Buff.All(s => s.Card.CardId != 339 && s.Card.CardId != 340))
                    {
                        session.Character.AddStaticBuff(new StaticBuffDTO
                        {
                            CardId = 339,
                            CharacterId = session.Character.CharacterId,
                            RemainingTime = -1
                        });
                        session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("ENCASED_BURNING_SWORD")));
                    }

                    if (session.CurrentMapInstance?.Map.MapId != 2640 == true && session.Character.Buff.Any(s => s.Card.CardId == 634)) //Act5.2 debuff
                    {
                        session.Character.RemoveBuff(634);
                    }
                    else if (session.CurrentMapInstance?.Map.MapId == 2640 == true && session.CurrentMapInstance.Map?.MapId != 2640 && session.Character.Buff.All(s => s.Card.CardId != 634 && s.Card.CardId != 340))
                    {
                        session.Character.AddStaticBuff(new StaticBuffDTO
                        {
                            CardId = 634,
                            CharacterId = session.Character.CharacterId,
                            RemainingTime = -1
                        });
                        session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("ENCASED_BURNING_SWORD")));
                    }


                    session.SendPacket(session.Character.GenerateMinimapPosition());
                    session.CurrentMapInstance.OnCharacterDiscoveringMapEvents.ForEach(e =>
                    {
                        if (!e.Item2.Contains(session.Character.CharacterId))
                        {
                            e.Item2.Add(session.Character.CharacterId);
                            EventHelper.Instance.RunEvent(e.Item1, session);
                        }
                    });
                    session.CurrentMapInstance.OnCharacterDiscoveringMapEvents = session.CurrentMapInstance.OnCharacterDiscoveringMapEvents.Where(s => s.Item1.EventActionType == EventActionType.SENDPACKET).ToList();
                    session.Character.LeaveIceBreaker();

                    session.Character.IsChangingMapInstance = false;
                }
                catch (Exception ex)
                {
                    Logger.Warn("Character changed while changing map. Do not abuse Commands.", ex);
                    session.Character.IsChangingMapInstance = false;
                }
            }
        }

        public void FamilyRefresh(long familyId, bool changeFaction = false) => CommunicationServiceClient.Instance.UpdateFamily(ServerGroup, familyId, changeFaction);

        public static MapInstance GenerateMapInstance(short mapId, MapInstanceType type, InstanceBag mapclock, bool dropAllowed = false, bool isScriptedInstance = false)
        {
            Map map = _maps.FirstOrDefault(m => m.MapId.Equals(mapId));
            if (map != null)
            {
                Guid guid = Guid.NewGuid();
                MapInstance mapInstance = new MapInstance(map, guid, false, type, mapclock, dropAllowed);
                if (!isScriptedInstance)
                {
                    mapInstance.LoadMonsters();
                    mapInstance.LoadNpcs();
                    mapInstance.LoadPortals();
                    Parallel.ForEach(mapInstance.Monsters, mapMonster =>
                    {
                        mapMonster.MapInstance = mapInstance;
                        mapInstance.AddMonster(mapMonster);
                    });
                    Parallel.ForEach(mapInstance.Npcs, mapNpc =>
                    {
                        mapNpc.MapInstance = mapInstance;
                        mapInstance.AddNPC(mapNpc);
                    });
                }
                _mapinstances.TryAdd(guid, mapInstance);
                return mapInstance;
            }
            return null;
        }

        public static MapInstance ResetMapInstance(MapInstance baseMapInstance)
        {
            if (baseMapInstance != null)
            {
                Map mapinfo = new Map(baseMapInstance.Map.MapId, baseMapInstance.Map.SecondMapId, baseMapInstance.Map.Data)
                {
                    Music = baseMapInstance.Map.Music,
                    Name = baseMapInstance.Map.Name,
                    ShopAllowed = baseMapInstance.Map.ShopAllowed,
                    XpRate = baseMapInstance.Map.XpRate
                };
                MapInstance mapInstance = new MapInstance(mapinfo, baseMapInstance.MapInstanceId, baseMapInstance.ShopAllowed, baseMapInstance.MapInstanceType, new InstanceBag(), baseMapInstance.DropAllowed);
                mapInstance.LoadMonsters();
                mapInstance.LoadNpcs();
                mapInstance.LoadPortals();
                foreach (ScriptedInstanceDTO si in DAOFactory.ScriptedInstanceDAO.LoadByMap(mapInstance.Map.MapId).ToList())
                {
                    ScriptedInstance siObj = new ScriptedInstance(si);
                    if (siObj.Type == ScriptedInstanceType.TimeSpace)
                    {
                        mapInstance.ScriptedInstances.Add(siObj);
                    }
                    else if (siObj.Type == ScriptedInstanceType.Raid)
                    {
                        Portal port = new Portal
                        {
                            Type = (byte)PortalType.Raid,
                            SourceMapId = siObj.MapId,
                            SourceX = siObj.PositionX,
                            SourceY = siObj.PositionY
                        };
                        mapInstance.Portals.Add(port);
                    }
                }
                Parallel.ForEach(mapInstance.Monsters, mapMonster =>
                {
                    mapMonster.MapInstance = mapInstance;
                    mapInstance.AddMonster(mapMonster);
                });
                Parallel.ForEach(mapInstance.Npcs, mapNpc =>
                {
                    mapNpc.MapInstance = mapInstance;
                    mapInstance.AddNPC(mapNpc);
                });
                RemoveMapInstance(baseMapInstance.MapInstanceId);
                _mapinstances.TryAdd(baseMapInstance.MapInstanceId, mapInstance);
                return mapInstance;
            }
            return null;
        }

        public static IEnumerable<Card> GetAllCard() => _cards;

        public Card GetCardByCardId(short cardId) => _cards.FirstOrDefault(s => s.CardId == cardId);

        public static List<MapInstance> GetAllMapInstances() => _mapinstances.Values.ToList();

        public List<Recipe> GetAllRecipes() => _recipes.GetAllItems();

        public static IEnumerable<Skill> GetAllSkill() => _skills;

        public static Guid GetBaseMapInstanceIdByMapId(short mapId) => _mapinstances.FirstOrDefault(s => s.Value?.Map.MapId == mapId && s.Value.MapInstanceType == MapInstanceType.BaseMapInstance).Key;

        public static Card GetCard(short? cardId) => _cards.FirstOrDefault(m => m.CardId.Equals(cardId));

        public List<DropDTO> GetDropsByMonsterVNum(short monsterVNum)
        {
            if (_monsterDrops != null && _generalDrops != null)
                return _monsterDrops.ContainsKey(monsterVNum) ? _generalDrops.Concat(_monsterDrops[monsterVNum]).ToList() : _generalDrops.ToList();
            else
                return new List<DropDTO>();
        }

        public Group GetGroupByCharacterId(long characterId) => Groups?.SingleOrDefault(g => g.IsMemberOfGroup(characterId));

        public static Item GetItem(short vnum) => _items.FirstOrDefault(m => m.VNum.Equals(vnum));

        public static MapInstance GetMapInstance(Guid id) => _mapinstances.ContainsKey(id) ? _mapinstances[id] : null;

        public static MapInstance GetMapInstanceByMapId(short mapId) => _mapinstances.Values.FirstOrDefault(s => s.Map.MapId == mapId);

        public static List<MapInstance> GetMapInstances(Func<MapInstance, bool> predicate) => _mapinstances.Values.Where(predicate).ToList();

        public long GetNextGroupId() => ++_lastGroupId;

        public int GetNextMobId()
        {
            int maxMobId = 0;
            foreach (MapInstance map in _mapinstances.Values.ToList())
            {
                if (map.Monsters.Count > 0 && maxMobId < map.Monsters.Max(m => m.MapMonsterId))
                {
                    maxMobId = map.Monsters.Max(m => m.MapMonsterId);
                }
            }
            return ++maxMobId;
        }

        public int GetNextNpcId()
        {
            int mapNpcId = 0;
            foreach (MapInstance map in _mapinstances.Values.ToList())
            {
                if (map.Npcs.Count > 0 && mapNpcId < map.Npcs.Max(m => m.MapNpcId))
                {
                    mapNpcId = map.Npcs.Max(m => m.MapNpcId);
                }
            }
            return ++mapNpcId;
        }

        public static NpcMonster GetNpcMonster(short npcVNum) => _npcmonsters.FirstOrDefault(m => m.NpcMonsterVNum.Equals(npcVNum));

        public List<Recipe> GetRecipesByItemVNum(short itemVNum)
        {
            List<Recipe> recipes = new List<Recipe>();
            foreach (RecipeListDTO recipeList in _recipeLists.Where(r => r.ItemVNum == itemVNum))
            {
                recipes.Add(_recipes[recipeList.RecipeId]);
            }
            return recipes;
        }

        public List<Recipe> GetRecipesByMapNpcId(int mapNpcId)
        {
            List<Recipe> recipes = new List<Recipe>();
            foreach (RecipeListDTO recipeList in _recipeLists.Where(r => r.MapNpcId == mapNpcId))
            {
                recipes.Add(_recipes[recipeList.RecipeId]);
            }
            return recipes;
        }

        public ClientSession GetSessionByCharacterName(string name) => Sessions.SingleOrDefault(s => s.Character.Name == name);

        public ClientSession GetSessionBySessionId(int sessionId) => Sessions.SingleOrDefault(s => s.SessionId == sessionId);

        public static Skill GetSkill(short skillVNum) => _skills.FirstOrDefault(m => m.SkillVNum.Equals(skillVNum));

        public Quest GetQuest(long questId) => Quests.FirstOrDefault(m => m.QuestId.Equals(questId));

        public void GroupLeave(ClientSession session)
        {
            if (Groups != null)
            {
                Group grp = Instance.Groups.Find(s => s.IsMemberOfGroup(session.Character.CharacterId));
                if (grp != null)
                {
                    switch (grp.GroupType)
                    {
                        case GroupType.BigTeam:
                        case GroupType.GiantTeam:
                        case GroupType.Team:
                            if (grp.Raid?.InstanceBag.Lock == true)
                            {
                                grp.Raid.InstanceBag.DeadList.Add(session.Character.CharacterId);
                            }
                            if (grp.Sessions.ElementAt(0) == session && grp.SessionCount > 1)
                            {
                                Broadcast(session,
                                    UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("NEW_LEADER")),
                                    ReceiverType.OnlySomeone, "",
                                    grp.Sessions.ElementAt(1)?.Character.CharacterId ?? 0);
                            }
                            grp.LeaveGroup(session);
                            session.SendPacket(session.Character.GenerateRaid(1, true));
                            session.SendPacket(session.Character.GenerateRaid(2, true));
                            foreach (ClientSession groupSession in grp.Sessions.GetAllItems())
                            {
                                groupSession.SendPacket(grp.GenerateRdlst());
                                groupSession.SendPacket(grp.GeneraterRaidmbf(groupSession));
                                groupSession.SendPacket(groupSession.Character.GenerateRaid(0));
                            }
                            if (session.CurrentMapInstance?.MapInstanceType == MapInstanceType.RaidInstance)
                            {
                                ChangeMap(session.Character.CharacterId, session.Character.MapId, session.Character.MapX, session.Character.MapY);
                            }
                            session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("LEFT_RAID"), 0));
                            break;

                        /*case GroupType.GiantTeam:
                            ClientSession[] grpmembers = new ClientSession[40];
                            grp.Sessions.CopyTo(grpmembers);
                            foreach (ClientSession targetSession in grpmembers)
                            {
                                if (targetSession != null)
                                {
                                    targetSession.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("GROUP_CLOSED"), 0));
                                    Broadcast(targetSession.Character.GeneratePidx(true));
                                    grp.LeaveGroup(targetSession);
                                    targetSession.SendPacket(targetSession.Character.GeneratePinit());
                                    targetSession.SendPackets(targetSession.Character.GeneratePst());
                                }
                            }
                            GroupList.RemoveAll(s => s.GroupId == grp.GroupId);
                            ThreadSafeGroupList.Remove(grp.GroupId);
                            break;*/

                        case GroupType.Group:
                            if (grp.Sessions.ElementAt(0) == session && grp.SessionCount > 1)
                            {
                                Broadcast(session, UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("NEW_LEADER")), ReceiverType.OnlySomeone, "", grp.Sessions.ElementAt(1).Character.CharacterId);
                            }
                            grp.LeaveGroup(session);
                            if (grp.SessionCount == 1)
                            {
                                ClientSession targetSession = grp.Sessions.ElementAt(0);
                                if (targetSession != null)
                                {
                                    targetSession.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("GROUP_CLOSED"), 0));
                                    Broadcast(targetSession.Character.GeneratePidx(true));
                                    grp.LeaveGroup(targetSession);
                                    targetSession.SendPacket(targetSession.Character.GeneratePinit());
                                    targetSession.SendPackets(targetSession.Character.GeneratePst());
                                }
                            }
                            else
                            {
                                foreach (ClientSession groupSession in grp.Sessions.GetAllItems())
                                {
                                    groupSession.SendPacket(groupSession.Character.GeneratePinit());
                                    groupSession.SendPackets(session.Character.GeneratePst());
                                    groupSession.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("LEAVE_GROUP"), session.Character.Name), 0));
                                }
                            }
                            session.SendPacket(session.Character.GeneratePinit());
                            session.SendPackets(session.Character.GeneratePst());
                            Broadcast(session.Character.GeneratePidx(true));
                            session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("GROUP_LEFT"), 0));
                            break;

                        default:
                            return;
                    }
                    session.Character.Group = null;
                }
            }
        }

        public bool IsAct4Online() => CommunicationServiceClient.Instance.IsAct4Online(ServerGroup);

        public void Initialize()
        {
            MaxBankGold = long.Parse(ConfigurationManager.AppSettings["MaxBankGold"]);

            Act4RaidStart = DateTime.Now;
            Act4AngelStat = new Act4Stat();
            Act4DemonStat = new Act4Stat();
            Act6Erenia = new PercentBar();
            Act6Zenas = new PercentBar();
            LastFCSent = DateTime.Now;
            LoadBossEntities();

            CharacterScreenSessions = new ThreadSafeSortedList<long, ClientSession>();

            // Load Configuration

            Schedules = ConfigurationManager.GetSection("eventScheduler") as List<Schedule>;

            OrderablePartitioner<ItemDTO> itemPartitioner = Partitioner.Create(DAOFactory.ItemDAO.LoadAll(), EnumerablePartitionerOptions.NoBuffering);
            Parallel.ForEach(itemPartitioner, new ParallelOptions { MaxDegreeOfParallelism = 4 }, itemDto =>
            {
                switch (itemDto.ItemType)
                {
                    case ItemType.Armor:
                    case ItemType.Jewelery:
                    case ItemType.Fashion:
                    case ItemType.Specialist:
                    case ItemType.Weapon:
                        _items.Add(new WearableItem(itemDto));
                        break;

                    case ItemType.Box:
                        _items.Add(new BoxItem(itemDto));
                        break;

                    case ItemType.Shell:
                    case ItemType.Magical:
                    case ItemType.Event:
                        _items.Add(new MagicalItem(itemDto));
                        break;

                    case ItemType.Food:
                        _items.Add(new FoodItem(itemDto));
                        break;

                    case ItemType.Potion:
                        _items.Add(new PotionItem(itemDto));
                        break;

                    case ItemType.Production:
                        _items.Add(new ProduceItem(itemDto));
                        break;

                    case ItemType.Snack:
                        _items.Add(new SnackItem(itemDto));
                        break;

                    case ItemType.Special:
                        _items.Add(new SpecialItem(itemDto));
                        break;

                    case ItemType.Teacher:
                        _items.Add(new TeacherItem(itemDto));
                        break;

                    case ItemType.Upgrade:
                        _items.Add(new UpgradeItem(itemDto));
                        break;

                    default:
                        _items.Add(new NoFunctionItem(itemDto));
                        break;
                }
            });
            Logger.Info(string.Format(Language.Instance.GetMessageFromKey("ITEMS_LOADED"), _items.Count));

            #region BoxItem

            BoxItems = DAOFactory.BoxItemDAO.LoadAll();

            Logger.Info(string.Format(Language.Instance.GetMessageFromKey("BOX_ITEMS_LOADED"), BoxItems.Count));

            #endregion

            // intialize monsterdrops
            _monsterDrops = new ThreadSafeSortedList<short, List<DropDTO>>();
            Parallel.ForEach(DAOFactory.DropDAO.LoadAll().GroupBy(d => d.MonsterVNum), monsterDropGrouping =>
            {
                if (monsterDropGrouping.Key.HasValue)
                {
                    _monsterDrops[monsterDropGrouping.Key.Value] = monsterDropGrouping.OrderBy(d => d.DropChance).ToList();
                }
                else
                {
                    _generalDrops = monsterDropGrouping.ToList();
                }
            });
            Logger.Info(string.Format(Language.Instance.GetMessageFromKey("DROPS_LOADED"), _monsterDrops.Sum(i => i.Count)));

            // initialize monsterskills
            _monsterSkills = new ThreadSafeSortedList<short, List<NpcMonsterSkill>>();
            Parallel.ForEach(DAOFactory.NpcMonsterSkillDAO.LoadAll().GroupBy(n => n.NpcMonsterVNum), monsterSkillGrouping => _monsterSkills[monsterSkillGrouping.Key] = monsterSkillGrouping.Select(n => new NpcMonsterSkill(n)).ToList());
            Logger.Info(string.Format(Language.Instance.GetMessageFromKey("MONSTERSKILLS_LOADED"), _monsterSkills.Sum(i => i.Count)));

            // initialize bazaar
            BazaarList = new ThreadSafeGenericList<BazaarItemLink>();
            OrderablePartitioner<BazaarItemDTO> bazaarPartitioner = Partitioner.Create(DAOFactory.BazaarItemDAO.LoadAll(), EnumerablePartitionerOptions.NoBuffering);
            Parallel.ForEach(bazaarPartitioner, new ParallelOptions { MaxDegreeOfParallelism = 8 }, bazaarItem =>
            {
                BazaarItemLink item = new BazaarItemLink
                {
                    BazaarItem = bazaarItem
                };
                CharacterDTO chara = DAOFactory.CharacterDAO.LoadById(bazaarItem.SellerId);
                if (chara != null)
                {
                    item.Owner = chara.Name;
                    item.Item = new ItemInstance(DAOFactory.ItemInstanceDAO.LoadById(bazaarItem.ItemInstanceId));
                }
                BazaarList.Add(item);
            });
            Logger.Info(string.Format(Language.Instance.GetMessageFromKey("BAZAAR_LOADED"), BazaarList.Count));

            // initialize npcmonsters
            Parallel.ForEach(DAOFactory.NpcMonsterDAO.LoadAll(), npcMonster =>
            {
                NpcMonster npcMonsterObj = new NpcMonster(npcMonster);
                npcMonsterObj.Initialize();
                npcMonsterObj.BCards = new List<BCard>();
                DAOFactory.BCardDAO.LoadByNpcMonsterVNum(npcMonster.OriginalNpcMonsterVNum > 0 ? npcMonster.OriginalNpcMonsterVNum : npcMonster.NpcMonsterVNum).ToList().ForEach(s => npcMonsterObj.BCards.Add(new BCard(s)));
                _npcmonsters.Add(npcMonsterObj);
            });
            Logger.Info(string.Format(Language.Instance.GetMessageFromKey("NPCMONSTERS_LOADED"), _npcmonsters.Count));

            // intialize recipes
            _recipes = new ThreadSafeSortedList<short, Recipe>();
            Parallel.ForEach(DAOFactory.RecipeDAO.LoadAll(), recipeGrouping =>
            {
                Recipe recipe = new Recipe(recipeGrouping);
                _recipes[recipeGrouping.RecipeId] = recipe;
                recipe.Initialize();
            });
            Logger.Info(string.Format(Language.Instance.GetMessageFromKey("RECIPES_LOADED"), _recipes.Count));

            // initialize recipelist
            _recipeLists = new ThreadSafeSortedList<int, RecipeListDTO>();
            Parallel.ForEach(DAOFactory.RecipeListDAO.LoadAll(), recipeListGrouping => _recipeLists[recipeListGrouping.RecipeListId] = recipeListGrouping);
            Logger.Info(string.Format(Language.Instance.GetMessageFromKey("RECIPELISTS_LOADED"), _recipeLists.Count));

            // initialize shopitems
            _shopItems = new ThreadSafeSortedList<int, List<ShopItemDTO>>();
            Parallel.ForEach(DAOFactory.ShopItemDAO.LoadAll().GroupBy(s => s.ShopId), shopItemGrouping => _shopItems[shopItemGrouping.Key] = shopItemGrouping.ToList());
            Logger.Info(string.Format(Language.Instance.GetMessageFromKey("SHOPITEMS_LOADED"), _shopItems.Sum(i => i.Count)));

            // initialize shopskills
            _shopSkills = new ThreadSafeSortedList<int, List<ShopSkillDTO>>();
            Parallel.ForEach(DAOFactory.ShopSkillDAO.LoadAll().GroupBy(s => s.ShopId), shopSkillGrouping => _shopSkills[shopSkillGrouping.Key] = shopSkillGrouping.ToList());
            Logger.Info(string.Format(Language.Instance.GetMessageFromKey("SHOPSKILLS_LOADED"), _shopSkills.Sum(i => i.Count)));

            // initialize shops
            _shops = new ThreadSafeSortedList<int, Shop>();
            Parallel.ForEach(DAOFactory.ShopDAO.LoadAll(), shopGrouping =>
            {
                Shop shop = new Shop(shopGrouping);
                _shops[shopGrouping.MapNpcId] = shop;
                shop.Initialize();
            });
            Logger.Info(string.Format(Language.Instance.GetMessageFromKey("SHOPS_LOADED"), _shops.Count));

            // initialize teleporters
            _teleporters = new ThreadSafeSortedList<int, List<TeleporterDTO>>();
            Parallel.ForEach(DAOFactory.TeleporterDAO.LoadAll().GroupBy(t => t.MapNpcId), teleporterGrouping => _teleporters[teleporterGrouping.Key] = teleporterGrouping.Select(t => t).ToList());
            Logger.Info(string.Format(Language.Instance.GetMessageFromKey("TELEPORTERS_LOADED"), _teleporters.Sum(i => i.Count)));

            // initialize skills
            Parallel.ForEach(DAOFactory.SkillDAO.LoadAll(), skill =>
            {
                Skill skillObj = new Skill(skill);
                skillObj.Combos.AddRange(DAOFactory.ComboDAO.LoadBySkillVnum(skillObj.SkillVNum).ToList());
                skillObj.BCards = new List<BCard>();
                DAOFactory.BCardDAO.LoadBySkillVNum(skillObj.SkillVNum).ToList().ForEach(o => skillObj.BCards.Add(new BCard(o)));
                _skills.Add(skillObj);
            });
            Logger.Info(string.Format(Language.Instance.GetMessageFromKey("SKILLS_LOADED"), _skills.Count));

            // initialize cards
            Parallel.ForEach(DAOFactory.CardDAO.LoadAll(), card =>
            {
                Card cardObj = new Card(card)
                {
                    BCards = new List<BCard>()
                };
                DAOFactory.BCardDAO.LoadByCardId(cardObj.CardId).ToList().ForEach(o => cardObj.BCards.Add(new BCard(o)));
                _cards.Add(cardObj);
            });
            Logger.Info(string.Format(Language.Instance.GetMessageFromKey("CARDS_LOADED"), _cards.Count));

            // initialize quests
            Quests = new List<Quest>();
            foreach (QuestDTO questdto in DAOFactory.QuestDAO.LoadAll())
            {
                Quest quest = new Quest(questdto);
                quest.QuestRewards = DAOFactory.QuestRewardDAO.LoadByQuestId(quest.QuestId).ToList();
                quest.QuestObjectives = DAOFactory.QuestObjectiveDAO.LoadByQuestId(quest.QuestId).ToList();
                Quests.Add(quest);
            }
            FlowerQuestId = Quests.FirstOrDefault(q => q.QuestType == (byte)QuestType.FlowerQuest)?.QuestId;

            Logger.Info(string.Format(Language.Instance.GetMessageFromKey("QUESTS_LOADED"), Quests.Count));

            // intialize mapnpcs
            _mapNpcs = new ThreadSafeSortedList<short, List<MapNpc>>();
            Parallel.ForEach(DAOFactory.MapNpcDAO.LoadAll().GroupBy(t => t.MapId), mapNpcGrouping => _mapNpcs[mapNpcGrouping.Key] = mapNpcGrouping.Select(t => t as MapNpc).ToList());
            Logger.Info(string.Format(Language.Instance.GetMessageFromKey("MAPNPCS_LOADED"), _mapNpcs.Sum(i => i.Count)));

            try
            {
                int i = 0;
                int monstercount = 0;
                OrderablePartitioner<MapDTO> mapPartitioner = Partitioner.Create(DAOFactory.MapDAO.LoadAll(), EnumerablePartitionerOptions.NoBuffering);
                Parallel.ForEach(mapPartitioner, new ParallelOptions { MaxDegreeOfParallelism = 8 }, map =>
                {
                    Guid guid = Guid.NewGuid();
                    Map mapinfo = new Map(map.MapId, map.SecondMapId, map.Data)
                    {
                        Music = map.Music,
                        Name = map.Name,
                        ShopAllowed = map.ShopAllowed,
                        XpRate = map.XpRate
                    };
                    _maps.Add(mapinfo);
                    MapInstance newMap = new MapInstance(mapinfo, guid, map.ShopAllowed, MapInstanceType.BaseMapInstance, new InstanceBag(), true);
                    _mapinstances.TryAdd(guid, newMap);

                    Task.Run((Action)newMap.LoadPortals);
                    newMap.LoadNpcs();
                    newMap.LoadMonsters();

                    Parallel.ForEach(newMap.Npcs, mapNpc =>
                    {
                        mapNpc.MapInstance = newMap;
                        newMap.AddNPC(mapNpc);
                    });
                    Parallel.ForEach(newMap.Monsters, mapMonster =>
                    {
                        mapMonster.MapInstance = newMap;
                        newMap.AddMonster(mapMonster);
                    });
                    monstercount += newMap.Monsters.Count;
                    i++;
                });
                if (i != 0)
                {
                    Logger.Info(string.Format(Language.Instance.GetMessageFromKey("MAPS_LOADED"), i));
                }
                else
                {
                    Logger.Error(Language.Instance.GetMessageFromKey("NO_MAP"));
                }

                Logger.Info(string.Format(Language.Instance.GetMessageFromKey("MAPMONSTERS_LOADED"), monstercount));
                StartedEvents = new List<EventType>();

                LoadFamilies();
                LaunchEvents();
                RefreshRanking();

                CharacterRelations = DAOFactory.CharacterRelationDAO.LoadAll().ToList();
                PenaltyLogs = DAOFactory.PenaltyLogDAO.LoadAll().ToList();

                #region Normal Arena

                if (DAOFactory.MapDAO.LoadById(2006) != null)
                {
                    ArenaInstance = GenerateMapInstance(2006, MapInstanceType.NormalInstance, new InstanceBag());
                    ArenaInstance.IsPVP = true;

                    Portal portal = new Portal
                    {
                        SourceMapId = 2006,
                        SourceX = 37,
                        SourceY = 15,
                        DestinationMapId = 1,
                        DestinationX = 0,
                        DestinationY = 0,
                        Type = -1
                    };

                    ArenaInstance.CreatePortal(portal);
                }

                #endregion

                #region Family Arena

                if (DAOFactory.MapDAO.LoadById(2106) != null)
                {
                    FamilyArenaInstance = GenerateMapInstance(2106, MapInstanceType.NormalInstance, new InstanceBag());
                    FamilyArenaInstance.IsPVP = true;

                    Portal portal = new Portal
                    {
                        SourceMapId = 2106,
                        SourceX = 38,
                        SourceY = 3,
                        DestinationMapId = 1,
                        DestinationX = 0,
                        DestinationY = 0,
                        Type = -1
                    };

                    FamilyArenaInstance.CreatePortal(portal);
                }

                #endregion

                #region Specialist Gem Map

                if (DAOFactory.MapDAO.LoadById(2107) != null)
                {
                    Portal portal = new Portal
                    {
                        SourceMapId = 2107,
                        SourceX = 10,
                        SourceY = 5,
                        DestinationMapId = 1,
                        DestinationX = 0,
                        DestinationY = 0,
                        Type = -1
                    };
                    //MapInstance angelMapInstance = GetMapInstance(GetBaseMapInstanceIdByMapId(132));
                    void loadSpecialistGemMap(short npcVNum)
                    {
                        MapInstance specialistGemMapInstance;
                        specialistGemMapInstance = GenerateMapInstance(2107, MapInstanceType.NormalInstance, new InstanceBag());
                        specialistGemMapInstance.Npcs.Where(s => s.NpcVNum != npcVNum).ToList().ForEach(s => specialistGemMapInstance.RemoveNpc(s));
                        specialistGemMapInstance.CreatePortal(portal);
                        SpecialistGemMapInstances.Add(specialistGemMapInstance);
                    }

                    loadSpecialistGemMap(932); // Pajama
                    loadSpecialistGemMap(933); // SP 1
                    loadSpecialistGemMap(934); // SP 2
                    loadSpecialistGemMap(948); // SP 3
                    loadSpecialistGemMap(954); // SP 4
                }

                #endregion

                LoadScriptedInstances();
                LoadBannedCharacters();
            }
            catch (Exception ex)
            {
                Logger.Error("General Error", ex);
            }

            WorldId = Guid.NewGuid();
        }

        public bool IsCharacterMemberOfGroup(long characterId) => Groups?.Any(g => g.IsMemberOfGroup(characterId)) == true;

        public bool IsCharactersGroupFull(long characterId) => Groups?.Any(g => g.IsMemberOfGroup(characterId) && (g.SessionCount == (byte)g.GroupType || g.GroupType == GroupType.TalentArena)) == true;

        public bool ItemHasRecipe(short itemVNum) => _recipeLists.Any(r => r.ItemVNum == itemVNum);

         public void JoinMiniland(ClientSession session, ClientSession minilandSession)
        {         
            if (session.Character.Miniland.MapInstanceId == minilandSession.Character.Miniland.MapInstanceId)
            {
                foreach (Mate mate in session.Character.Mates)
                {
                    if (session.Character.Miniland.Map.IsBlockedZone(mate.PositionX, mate.PositionY))
                    {
                        MapCell newPos = MinilandRandomPos();
                        mate.MapX = newPos.X;
                        mate.MapY = newPos.Y;
                        mate.PositionX = mate.MapX;
                        mate.PositionY = mate.MapY;
                    }

                    if (!mate.IsAlive || mate.Hp <= 0)
                    {
                        mate.Hp = mate.MaxHp / 2;
                        mate.Mp = mate.MaxMp / 2;
                        mate.IsAlive = true;
                        mate.ReviveDisposable?.Dispose();
                    }
                }
            }
            ChangeMapInstance(session.Character.CharacterId, minilandSession.Character.Miniland.MapInstanceId, 5, 8);
            if (session.Character.Miniland.MapInstanceId != minilandSession.Character.Miniland.MapInstanceId)
            {
                session.SendPacket(UserInterfaceHelper.GenerateMsg(minilandSession.Character.MinilandMessage.Replace(' ', '^'), 0));
                session.SendPacket(minilandSession.Character.GenerateMlinfobr());
                minilandSession.Character.GeneralLogs.Add(new GeneralLogDTO { AccountId = session.Account.AccountId, CharacterId = session.Character.CharacterId, IpAddress = session.IpAddress, LogData = "Miniland", LogType = "World", Timestamp = DateTime.Now });
                session.SendPacket(minilandSession.Character.GenerateMinilandObjectForFriends());
            }
            else
            {
                session.SendPacket(session.Character.GenerateMlinfo());
                session.SendPacket(session.Character.GetMinilandObjectList());
            }
            if (minilandSession.Character.IsVehicled)
            {
                minilandSession.Character.Mates.ForEach(s => session.SendPacket(s.GenerateIn()));
            }
            else
            {
                minilandSession.Character.Mates.Where(s => !s.IsTeamMember).ToList().ForEach(s => session.SendPacket(s.GenerateIn()));
            }
            session.SendPackets(minilandSession.Character.GetMinilandEffects());
            session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("MINILAND_VISITOR"), minilandSession.Character.GeneralLogs.CountLinq(s => s.LogData == "Miniland" && s.Timestamp.Day == DateTime.Now.Day), minilandSession.Character.GeneralLogs.CountLinq(s => s.LogData == "Miniland")), 10));
        }

        // Server
        public void Kick(string characterName)
        {
            ClientSession session = Sessions.FirstOrDefault(s => s.Character?.Name.Equals(characterName) == true);
            session?.Disconnect();
        }

        // Map
        public void LeaveMap(long id)
        {
            ClientSession session = GetSessionByCharacterId(id);
            if (session == null)
            {
                return;
            }
            session.SendPacket(UserInterfaceHelper.GenerateMapOut());
            if (!session.Character.InvisibleGm)
            {
                session.Character.Mates.Where(s => s.IsTeamMember).ToList().ForEach(s => session.CurrentMapInstance?.Broadcast(session, StaticPacketHelper.Out(UserType.Npc, s.MateTransportId), ReceiverType.AllExceptMe));
                session.CurrentMapInstance?.Broadcast(session, StaticPacketHelper.Out(UserType.Player, session.Character.CharacterId), ReceiverType.AllExceptMe);
            }
        }

        public bool MapNpcHasRecipe(int mapNpcId) => _recipeLists.Any(r => r.MapNpcId == mapNpcId);
        
        //Function to get a random number 
        private static readonly Random random = new Random();
        private static readonly object syncLock = new object();
        public static int RandomNumber(int min = 0, int max = 100)
        {
            lock (syncLock)
            { // synchronize
                return random.Next(min, max);
            }
        }

        public static T RandomNumber<T>(int min = 0, int max = 100)
        {
            return (T)Convert.ChangeType(RandomNumber(min, max), typeof(T));
        }

        public static MapCell MinilandRandomPos() => new MapCell { X = (short)RandomNumber(5, 16), Y = (short)RandomNumber(3, 14) };

        public void RefreshDailyReward()
        {
            DAOFactory.AccountDAO.ResetDailyRewards();
            Parallel.ForEach(Sessions, sess => sess.Account.DailyRewardSent = false);
        }
        public void RefreshRanking()
        {
            TopComplimented = DAOFactory.CharacterDAO.GetTopCompliment();
            TopPoints = DAOFactory.CharacterDAO.GetTopPoints();
            TopReputation = DAOFactory.CharacterDAO.GetTopReputation();
        }

        public void RelationRefresh(long relationId)
        {
            _inRelationRefreshMode = true;
            CommunicationServiceClient.Instance.UpdateRelation(ServerGroup, relationId);
            SpinWait.SpinUntil(() => !_inRelationRefreshMode);
        }

        public static void RemoveMapInstance(Guid mapId)
        {
            if (_mapinstances == null)
            {
                return;
            }
            
            if (_mapinstances.FirstOrDefault(s => s.Key == mapId) is KeyValuePair<Guid, MapInstance> map && !map.Equals(default))
            {
                map.Value.Dispose();
                ((IDictionary)_mapinstances).Remove(map.Key);
            }
        }

        // Map
        public void ReviveFirstPosition(long characterId)
        {
            ClientSession session = GetSessionByCharacterId(characterId);
            if (session?.Character.Hp <= 0)
            {
                if (session.CurrentMapInstance.MapInstanceType == MapInstanceType.TimeSpaceInstance || session.CurrentMapInstance.MapInstanceType == MapInstanceType.RaidInstance)
                {
                    session.Character.Hp = (int)session.Character.HPLoad();
                    session.Character.Mp = (int)session.Character.MPLoad();
                    session.CurrentMapInstance?.Broadcast(session.Character.GenerateRevive());
                    session.SendPacket(session.Character.GenerateStat());
                }
                else
                {
                    if (ChannelId == 51)
                    {
                        if (session.CurrentMapInstance.MapInstanceId == session.Character.Family?.Act4RaidBossMap?.MapInstanceId)
                        {
                            session.Character.Hp = 1;
                            session.Character.Mp = 1;

                            switch (session.Character.Family.Act4Raid.MapInstanceType)
                            {
                                case MapInstanceType.Act4Morcos:
                                    ServerManager.Instance.ChangeMapInstance(session.Character.CharacterId,
                                        session.Character.Family.Act4Raid.MapInstanceId, 43, 179);
                                    break;

                                case MapInstanceType.Act4Hatus:
                                    ServerManager.Instance.ChangeMapInstance(session.Character.CharacterId,
                                        session.Character.Family.Act4Raid.MapInstanceId, 15, 9);
                                    break;

                                case MapInstanceType.Act4Calvina:
                                    ServerManager.Instance.ChangeMapInstance(session.Character.CharacterId,
                                        session.Character.Family.Act4Raid.MapInstanceId, 24, 6);
                                    break;

                                case MapInstanceType.Act4Berios:
                                    ServerManager.Instance.ChangeMapInstance(session.Character.CharacterId,
                                        session.Character.Family.Act4Raid.MapInstanceId, 20, 20);
                                    break;
                            }
                        }
                        else
                        {
                            session.Character.Hp = (int)session.Character.HPLoad();
                            session.Character.Mp = (int)session.Character.MPLoad();
                            short x = (short)(39 + RandomNumber(-2, 3));
                            short y = (short)(42 + RandomNumber(-2, 3));
                            if (session.Character.Faction == FactionType.Angel)
                            {
                                ChangeMap(session.Character.CharacterId, 130, x, y);
                            }
                            else if (session.Character.Faction == FactionType.Demon)
                            {
                                ChangeMap(session.Character.CharacterId, 131, x, y);
                            }
                        }
                    }
                    else
                    {
                        session.Character.Hp = 1;
                        session.Character.Mp = 1;
                        if (session.CurrentMapInstance.MapInstanceType == MapInstanceType.BaseMapInstance)
                        {
                            RespawnMapTypeDTO resp = session.Character.Respawn;
                            short x = (short)(resp.DefaultX + RandomNumber(-3, 3));
                            short y = (short)(resp.DefaultY + RandomNumber(-3, 3));
                            ChangeMap(session.Character.CharacterId, resp.DefaultMapId, x, y);
                        }
                        else
                        {
                            Instance.ChangeMap(session.Character.CharacterId, session.Character.MapId, session.Character.MapX, session.Character.MapY);
                        }
                    }
                    session.CurrentMapInstance?.Broadcast(session, session.Character.GenerateTp());
                    session.CurrentMapInstance?.Broadcast(session.Character.GenerateRevive());
                    session.SendPacket(session.Character.GenerateStat());
                }
            }
        }

        public void SaveAll()
        {
            CommunicationServiceClient.Instance.CleanupOutdatedSession();
            foreach (ClientSession sess in Sessions)
            {
                sess.Character?.Save();
            }
            DAOFactory.BazaarItemDAO.RemoveOutDated();
        }

        public static void Shout(string message, bool noAdminTag = false)
        {
            Instance.Broadcast(UserInterfaceHelper.GenerateSay((noAdminTag ? "" : $"({Language.Instance.GetMessageFromKey("ADMINISTRATOR")})") + message, 10));
            Instance.Broadcast(UserInterfaceHelper.GenerateMsg(message, 2));
        }

        public async Task ShutdownTaskAsync(int Time = 5)
        {
            Instance.SaveAll();
            Shout(string.Format(Language.Instance.GetMessageFromKey("SHUTDOWN_MIN"), Time));
            if (Time > 1)
            {
                for (int i = 0; i < 60 * (Time - 1); i++)
                {
                    await Task.Delay(1000).ConfigureAwait(false);
                    if (Instance.ShutdownStop)
                    {
                        Instance.ShutdownStop = false;
                        return;
                    }
                }
                Shout(string.Format(Language.Instance.GetMessageFromKey("SHUTDOWN_MIN"), 1));
            }
            for (int i = 0; i < 30; i++)
            {
                await Task.Delay(1000).ConfigureAwait(false);
                if (Instance.ShutdownStop)
                {
                    Instance.ShutdownStop = false;
                    return;
                }
            }
            Shout(string.Format(Language.Instance.GetMessageFromKey("SHUTDOWN_SEC"), 30));
            for (int i = 0; i < 30; i++)
            {
                await Task.Delay(1000).ConfigureAwait(false);
                if (Instance.ShutdownStop)
                {
                    Instance.ShutdownStop = false;
                    return;
                }
            }
            Shout(string.Format(Language.Instance.GetMessageFromKey("SHUTDOWN_SEC"), 10));
            for (int i = 0; i < 10; i++)
            {
                await Task.Delay(1000).ConfigureAwait(false);
                if (Instance.ShutdownStop)
                {
                    Instance.ShutdownStop = false;
                    return;
                }
            }
            InShutdown = true;
            foreach (ClientSession sess in Sessions)
            {
                sess.Character?.Dispose();
            }
            Instance.SaveAll();
            CommunicationServiceClient.Instance.UnregisterWorldServer(WorldId);
            if (IsReboot)
            {
                if (ChannelId == 51)
                {
                    await Task.Delay(16000).ConfigureAwait(false);
                }
                else
                {
                    await Task.Delay((ChannelId - 1) * 2000).ConfigureAwait(false);
                }
                Process.Start("OpenNos.World.exe", $"--nomsg{( ChannelId == 51 ? $" --port {Configuration.Act4Port}" : "")}");
            }
            Environment.Exit(0);
        }

        public void TeleportOnRandomPlaceInMap(ClientSession session, Guid guid)
        {
            MapInstance map = GetMapInstance(guid);
            if (guid != default)
            {
                MapCell pos = map.Map.GetRandomPosition();
                if (pos == null)
                {
                    return;
                }
                ChangeMapInstance(session.Character.CharacterId, guid, pos.X, pos.Y);
            }
        }

        // Server
        public void UpdateGroup(long charId)
        {
            try
            {
                if (Groups != null)
                {
                    Group myGroup = Groups.Find(s => s.IsMemberOfGroup(charId));
                    if (myGroup == null)
                    {
                        return;
                    }
                    ThreadSafeGenericList<ClientSession> groupMembers = Groups.Find(s => s.IsMemberOfGroup(charId))?.Sessions;
                    if (groupMembers != null)
                    {
                        foreach (ClientSession session in groupMembers.GetAllItems())
                        {
                            session.SendPacket(session.Character.GeneratePinit());
                            session.SendPackets(session.Character.GeneratePst());
                            session.SendPacket(session.Character.GenerateStat());
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        internal List<NpcMonsterSkill> GetNpcMonsterSkillsByMonsterVNum(short npcMonsterVNum) => _monsterSkills.ContainsKey(npcMonsterVNum) ? _monsterSkills[npcMonsterVNum] : new List<NpcMonsterSkill>();

        internal Shop GetShopByMapNpcId(int mapNpcId) => _shops.ContainsKey(mapNpcId) ? _shops[mapNpcId] : null;

        internal List<ShopItemDTO> GetShopItemsByShopId(int shopId) => _shopItems.ContainsKey(shopId) ? _shopItems[shopId] : new List<ShopItemDTO>();

        internal List<ShopSkillDTO> GetShopSkillsByShopId(int shopId) => _shopSkills.ContainsKey(shopId) ? _shopSkills[shopId] : new List<ShopSkillDTO>();

        internal List<TeleporterDTO> GetTeleportersByNpcVNum(int npcMonsterVNum)
        {
            if (_teleporters?.ContainsKey(npcMonsterVNum) == true)
            {
                return _teleporters[npcMonsterVNum];
            }
            return new List<TeleporterDTO>();
        }

        internal static void StopServer()
        {
            Instance.ShutdownStop = true;
            Instance.TaskShutdown = null;
            Instance.SaveAll();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _monsterDrops.Dispose();
                ThreadSafeGroupList.Dispose();
                _monsterSkills.Dispose();
                _shopSkills.Dispose();
                _shopItems.Dispose();
                _shops.Dispose();
                _recipes.Dispose();
                _mapNpcs.Dispose();
                _teleporters.Dispose();
                GC.SuppressFinalize(this);
            }
        }

        private void Act4FlowerProcess()
        {
            foreach (MapInstance map in GetAllMapInstances().Where(s => s.Map.MapTypes.Any(m => m.MapTypeId == (short) MapTypeEnum.Act4) && s.Npcs.Count(o => o.NpcVNum == 2004 && o.IsOut) < s.Npcs.Count(n => n.NpcVNum == 2004)))
            {
                foreach (MapNpc i in map.Npcs.Where(s => s.IsOut && s.NpcVNum == 2004))
                {
                    MapCell randomPos = map.Map.GetRandomPosition();
                    i.MapX = randomPos.X;
                    i.MapY = randomPos.Y;
                    i.MapInstance.Broadcast(i.GenerateIn());
                }
            }
        }

        private static void Act4ShoutProcess()
        {
            if (Instance.ChannelId == 51)
            {
                CommunicationServiceClient.Instance.SendMessageToCharacter(new SCSCharacterMessage
                {
                    DestinationCharacterId = null,
                    SourceWorldId = Instance.WorldId,
                    Message = "[Act4 Stat] Angel: " + Instance.Act4AngelStat.Percentage / 100 + " % VS Demon: " + Instance.Act4DemonStat.Percentage / 100 + " %",
                    Type = MessageType.Shout
                });
            }
        }

        private void Act4Process()
        {
            if (ChannelId != 51)
            {
                return;
            }

            MapInstance angelMapInstance = GetMapInstance(GetBaseMapInstanceIdByMapId(132));
            MapInstance demonMapInstance = GetMapInstance(GetBaseMapInstanceIdByMapId(133));

            void SummonMukraju(MapInstance instance, byte faction)
            {
                MapMonster monster = new MapMonster
                {
                    MonsterVNum = 556,
                    MapY = (faction == 1 ? (short)92 : (short)95),
                    MapX = (faction == 1 ? (short)114 : (short)20),
                    MapId = (short)(131 + faction),
                    IsMoving = true,
                    MapMonsterId = instance.GetNextMonsterId(),
                    ShouldRespawn = false
                };
                monster.Initialize(instance);
                monster.Faction = (FactionType)faction == FactionType.Angel ? FactionType.Demon : FactionType.Angel;
                instance.AddMonster(monster);
                instance.Broadcast(monster.GenerateIn());

                Observable.Timer(TimeSpan.FromSeconds(faction == 1 ? Act4AngelStat.TotalTime : Act4DemonStat.TotalTime)).Subscribe(s =>
                {
                    if (instance.Monsters.ToList().Any(m => m.MonsterVNum == monster.MonsterVNum))
                    {
                        if (faction == 1)
                        {
                            Act4AngelStat.Mode = 0;
                        }
                        else
                        {
                            Act4DemonStat.Mode = 0;
                        }
                        instance.DespawnMonster(monster.MonsterVNum);
                        Parallel.ForEach(Sessions, sess => sess.SendPacket(sess.Character.GenerateFc()));
                    }
                });
            }

            int CreateRaid(byte faction)
            {
                MapInstanceType raidType = MapInstanceType.Act4Morcos;
                int rng = RandomNumber(1, 5);
                switch (rng)
                {
                    case 2:
                        raidType = MapInstanceType.Act4Hatus;
                        break;

                    case 3:
                        raidType = MapInstanceType.Act4Calvina;
                        break;

                    case 4:
                        raidType = MapInstanceType.Act4Berios;
                        break;
                }
                Event.Act4Raid.GenerateRaid(raidType, faction);
                return rng;
            }

            if (Act4AngelStat.Percentage >= 10000)
            {
                Act4AngelStat.Mode = 1;
                Act4AngelStat.Percentage = 0;
                Act4AngelStat.TotalTime = 300;
                SummonMukraju(angelMapInstance, 1);
                Parallel.ForEach(Sessions, sess => sess.SendPacket(sess.Character.GenerateFc()));
            }

            if (Act4AngelStat.Mode == 1 && !angelMapInstance.Monsters.Any(s => s.MonsterVNum == 556))
            {
                Act4AngelStat.Mode = 3;
                Act4AngelStat.TotalTime = 3600;

                switch (CreateRaid(1))
                {
                    case 1:
                        Act4AngelStat.IsMorcos = true;
                        break;

                    case 2:
                        Act4AngelStat.IsHatus = true;
                        break;

                    case 3:
                        Act4AngelStat.IsCalvina = true;
                        break;

                    case 4:
                        Act4AngelStat.IsBerios = true;
                        break;
                }

                Parallel.ForEach(Sessions, sess => sess.SendPacket(sess.Character.GenerateFc()));
            }

            if (Act4DemonStat.Percentage >= 10000)
            {
                Act4DemonStat.Mode = 1;
                Act4DemonStat.Percentage = 0;
                Act4DemonStat.TotalTime = 300;
                SummonMukraju(demonMapInstance, 2);
                Parallel.ForEach(Sessions, sess => sess.SendPacket(sess.Character.GenerateFc()));
            }

            if (Act4DemonStat.Mode == 1 && !demonMapInstance.Monsters.Any(s => s.MonsterVNum == 556))
            {
                Act4DemonStat.Mode = 3;
                Act4DemonStat.TotalTime = 3600;

                switch (CreateRaid(2))
                {
                    case 1:
                        Act4DemonStat.IsMorcos = true;
                        break;

                    case 2:
                        Act4DemonStat.IsHatus = true;
                        break;

                    case 3:
                        Act4DemonStat.IsCalvina = true;
                        break;

                    case 4:
                        Act4DemonStat.IsBerios = true;
                        break;
                }

                Parallel.ForEach(Sessions, sess => sess.SendPacket(sess.Character.GenerateFc()));
            }

            if (DateTime.Now >= LastFCSent.AddMinutes(1))
            {
                Parallel.ForEach(Sessions, sess => sess.SendPacket(sess.Character.GenerateFc()));
                LastFCSent = DateTime.Now;
            }
        }

        public void Act6Process()
        {
            if (Act6Zenas.Percentage >= 10000 && Act6Zenas?.Mode == 0)
            {
                Act6Raid.GenerateRaid(FactionType.Angel);
                Act6Zenas.TotalTime = 3600;
                Act6Zenas.Mode = 1;
                Act6Zenas.Percentage = 0;
                Shout($"You can do now the Zenas Raid for 1 hour!", true);
            }
            else if (Act6Erenia.Percentage >= 10000 && Act6Erenia?.Mode == 0)
            {
                Act6Raid.GenerateRaid(FactionType.Demon);
                Act6Erenia.TotalTime = 3600;
                Act6Erenia.Mode = 1;
                Act6Erenia.Percentage = 0;
                Shout($"You can do now the Erenia Raid for 1 hour!", true);
            }

            if (Act6Erenia.CurrentTime <= 0 && Act6Erenia.Mode != 0)
            {
                Act6Erenia.KilledMonsters = 0;
                Act6Erenia.Mode = 0;
                Act6Zenas.Mode = 0;
                Act6Erenia.TotalTime = 0;
            }
            else if (Act6Zenas.CurrentTime <= 0 && Act6Zenas.Mode != 0)
            {
                Act6Zenas.KilledMonsters = 0;
                Act6Zenas.Mode = 0;
                Act6Erenia.Mode = 0;
                Act6Zenas.TotalTime = 0;
            }

            Parallel.ForEach(
                Sessions.Where(s =>
                    s?.Character != null && s.CurrentMapInstance?.Map.MapId >= 228 &&
                    s.CurrentMapInstance?.Map.MapId < 238 || s?.CurrentMapInstance?.Map.MapId == 2604),
                sess => sess.SendPacket(sess.Character.GenerateAct6()));
        }


        // Server
        //   private static void BotProcess()
        //   {
        //     try
        //   {
        // Shout(Language.Instance.GetMessageFromKey($"BOT_MESSAGE_{RandomNumber(0, 5)}"));
        //}
        //      catch (Exception e)
        //    {
        //Logger.Error(e);
        // }
        // }

        private void GroupProcess()
        {
            try
            {
                if (Groups != null)
                {
                    Parallel.ForEach(Groups, grp =>
                    {
                        foreach (ClientSession session in grp.Sessions.GetAllItems())
                        {
                            if (grp.GroupType == GroupType.Group)
                            {
                                session.SendPackets(grp.GeneratePst(session));
                            }
                            else
                            {
                                session.SendPacket(grp.GenerateRdlst());
                            }
                        }
                    });
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        private void LaunchEvents()
        {
            ThreadSafeGroupList = new ThreadSafeSortedList<long, Group>();

            Observable.Interval(TimeSpan.FromMinutes(2)).Subscribe(x => SaveAllProcess());
            Observable.Interval(TimeSpan.FromSeconds(1)).Subscribe(x => Act4Process());
            Observable.Interval(TimeSpan.FromSeconds(2)).Subscribe(x => GroupProcess());
            Observable.Interval(TimeSpan.FromMinutes(1)).Subscribe(x => Act4FlowerProcess());
            Observable.Interval(TimeSpan.FromSeconds(1)).Subscribe(x => Act6Process());
            // Observable.Interval(TimeSpan.FromHours(3)).Subscribe(x => BotProcess());
            Observable.Interval(TimeSpan.FromMinutes(20)).Subscribe(x => Act4ShoutProcess());
            Observable.Interval(TimeSpan.FromSeconds(5)).Subscribe(x => MaintenanceProcess());

            EventHelper.Instance.RunEvent(new EventContainer(GetMapInstance(GetBaseMapInstanceIdByMapId(98)), EventActionType.NPCSEFFECTCHANGESTATE, true));
            Parallel.ForEach(Schedules, schedule => Observable.Timer(TimeSpan.FromSeconds(EventHelper.GetMilisecondsBeforeTime(schedule.Time).TotalSeconds), TimeSpan.FromDays(1)).Subscribe(e =>
            {
                if (schedule.DayOfWeek == "" || schedule.DayOfWeek == DateTime.Now.DayOfWeek.ToString())
                {
                    EventHelper.GenerateEvent(schedule.Event, schedule.LvlBracket);
                }
            }));
            EventHelper.GenerateEvent(EventType.ACT4SHIP);

            Observable.Interval(TimeSpan.FromSeconds(1)).Subscribe(x => RemoveItemProcess());
            Observable.Interval(TimeSpan.FromMilliseconds(400)).Subscribe(x =>
            {
                Parallel.ForEach(_mapinstances, map =>
                {
                    Parallel.ForEach(map.Value.Npcs, npc => npc.StartLife());
                    Parallel.ForEach(map.Value.Monsters, monster => monster.StartLife());
                });
            });

            CommunicationServiceClient.Instance.SessionKickedEvent += OnSessionKicked;
            CommunicationServiceClient.Instance.MessageSentToCharacter += OnMessageSentToCharacter;
            CommunicationServiceClient.Instance.FamilyRefresh += OnFamilyRefresh;
            CommunicationServiceClient.Instance.RelationRefresh += OnRelationRefresh;
            CommunicationServiceClient.Instance.StaticBonusRefresh += OnStaticBonusRefresh;
            CommunicationServiceClient.Instance.BazaarRefresh += OnBazaarRefresh;
            CommunicationServiceClient.Instance.PenaltyLogRefresh += OnPenaltyLogRefresh;
            CommunicationServiceClient.Instance.GlobalEvent += OnGlobalEvent;
            CommunicationServiceClient.Instance.ShutdownEvent += OnShutdown;
            CommunicationServiceClient.Instance.RestartEvent += OnRestart;
            ConfigurationServiceClient.Instance.ConfigurationUpdate += OnConfiguratinEvent;
            MailServiceClient.Instance.MailSent += OnMailSent;
            _lastGroupId = 1;
        }

        public void SynchronizeSheduling()
        {
            if (Schedules.FirstOrDefault(s => s.Event == EventType.TALENTARENA)?.Time is TimeSpan arenaOfTalentsTime
                && IsTimeBetween(DateTime.Now, arenaOfTalentsTime, arenaOfTalentsTime.Add(new TimeSpan(4, 0, 0))))
            {
                EventHelper.GenerateEvent(EventType.TALENTARENA);
            }
            Schedules.Where(s => s.Event == EventType.LOD).ToList().ForEach(lodSchedule =>
            {
                if (IsTimeBetween(DateTime.Now, lodSchedule.Time, lodSchedule.Time.Add(new TimeSpan(2, 0, 0))))
                {
                    EventHelper.GenerateEvent(EventType.LOD);
                }
            });
        }

        private bool IsTimeBetween(DateTime dateTime, TimeSpan start, TimeSpan end)
        {
            TimeSpan now = dateTime.TimeOfDay;

            return start < end ? (start <= now && now <= end) : !(end < now && now < start);
        }

        private void OnStaticBonusRefresh(object sender, EventArgs e)
        {
            long characterId = (long)sender;

            ClientSession sess = GetSessionByCharacterId(characterId);
            if (sess != null)
            {
                sess.Character.StaticBonusList = DAOFactory.StaticBonusDAO.LoadByCharacterId(characterId).ToList();
            }
        }

        private void OnMailSent(object sender, EventArgs e)
        {
            MailDTO mail = (MailDTO)sender;

            ClientSession session = GetSessionByCharacterId(mail.IsSenderCopy ? mail.SenderId : mail.ReceiverId);
            if (session != null)
            {
                if (mail.AttachmentVNum != null)
                {
                        session.Character.MailList.Add((session.Character.MailList.Count > 0 ? session.Character.MailList.OrderBy(s => s.Key).Last().Key : 0) + 1, mail);
                        session.SendPacket(session.Character.GenerateParcel(mail));
                        //session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("ITEM_GIFTED"), GetItem(mail.AttachmentVNum.Value)?.Name, mail.AttachmentAmount), 12));
                 
                }              
                else
                {
                    session.Character.MailList.Add((session.Character.MailList.Count > 0 ? session.Character.MailList.OrderBy(s => s.Key).Last().Key : 0) + 1, mail);
                    session.SendPacket(session.Character.GeneratePost(mail, mail.IsSenderCopy ? (byte)2 : (byte)1));
                }
            }
        }

        private void OnConfiguratinEvent(object sender, EventArgs e) => Configuration = (ConfigurationObject)sender;

        private void LoadFamilies()
        {
            FamilyList = new ThreadSafeSortedList<long, Family>();
            Parallel.ForEach(DAOFactory.FamilyDAO.LoadAll(), familyDto =>
            {
                Family family = new Family(familyDto)
                {
                    FamilyCharacters = new List<FamilyCharacter>()
                };
                foreach (FamilyCharacterDTO famchar in DAOFactory.FamilyCharacterDAO.LoadByFamilyId(family.FamilyId).ToList())
                {
                    family.FamilyCharacters.Add(new FamilyCharacter(famchar));
                }
                FamilyCharacter familyCharacter = family.FamilyCharacters.Find(s => s.Authority == FamilyAuthority.Head);
                if (familyCharacter != null)
                {
                    family.Warehouse = new Inventory(new Character(familyCharacter.Character));
                    foreach (ItemInstanceDTO inventory in DAOFactory.ItemInstanceDAO.LoadByCharacterId(familyCharacter.CharacterId).Where(s => s.Type == InventoryType.FamilyWareHouse).ToList())
                    {
                        inventory.CharacterId = familyCharacter.CharacterId;
                        family.Warehouse[inventory.Id] = new ItemInstance(inventory);
                    }
                }
                family.FamilyLogs = DAOFactory.FamilyLogDAO.LoadByFamilyId(family.FamilyId).ToList();
                FamilyList[family.FamilyId] = family;
            });
        }

        public void LoadScriptedInstances()
        {
            Raids = new ConcurrentBag<ScriptedInstance>();
            TimeSpaces = new ConcurrentBag<ScriptedInstance>();
            Act6Raids = new ConcurrentBag<ScriptedInstance>();
            Parallel.ForEach(_mapinstances, map =>
            {
                if (map.Value.MapInstanceType == MapInstanceType.BaseMapInstance)
                {
                    map.Value.ScriptedInstances.Clear();
                    map.Value.Portals.Clear();
                    foreach (ScriptedInstanceDTO si in DAOFactory.ScriptedInstanceDAO.LoadByMap(map.Value.Map.MapId).ToList())
                    {
                        ScriptedInstance siObj = new ScriptedInstance(si);
                        switch (siObj.Type)
                        {
                            case ScriptedInstanceType.TimeSpace:
                            case ScriptedInstanceType.QuestTimeSpace:
                                siObj.LoadGlobals();
                                if (siObj.Script != null)
                                {
                                    TimeSpaces.Add(siObj);
                                }
                                map.Value.ScriptedInstances.Add(siObj);
                                break;
                            case ScriptedInstanceType.Raid:
                                siObj.LoadGlobals();
                                if (siObj.Script != null)
                                {
                                    Raids.Add(siObj);
                                }
                                Portal port = new Portal
                                {
                                    Type = (byte)PortalType.Raid,
                                    SourceMapId = siObj.MapId,
                                    SourceX = siObj.PositionX,
                                    SourceY = siObj.PositionY
                                };
                                map.Value.Portals.Add(port);
                                break;
                            case ScriptedInstanceType.RaidAct6:
                                siObj.LoadGlobals();
                                Raids.Add(siObj);
                                Act6Raids.Add(siObj);
                                break;
                        }
                    }
                    map.Value.LoadPortals();
                    map.Value.MapClear();
                }
            });
        }

        private void LoadBannedCharacters()
        {
            BannedCharacters.Clear();
            DAOFactory.CharacterDAO.LoadAll().ToList().ForEach(s =>
            {
                if (s.State != CharacterState.Active || DAOFactory.PenaltyLogDAO.LoadByAccount(s.AccountId).Any(c => c.DateEnd > DateTime.Now && c.Penalty == PenaltyType.Banned))
                {
                    BannedCharacters.Add(s.CharacterId);
                }
            });
        }

        private void LoadBossEntities()
        {
            BossVNums = new List<short>
            {
                580, //Perro infernal
                582, //Ginseng fuerte
                583, //Basilisco fuerte
                584, //Rey Tubérculo
                585, //Gran Rey Tubérculo
                586, //Rey Tubérculo monstruoso
                588, //Pollito gigante
                589, //Mandra milenaria
                590, //Perro guardián del infierno
                591, //Gallo de pelea gigante
                592, //Patata milenaria
                593, //Perro de Satanás
                594, //Barepardo gigante
                595, //Boing satélite
                596, //Raíz demoníaca
                597, //Slade musculoso
                598, //Rey de las tinieblas
                599, //Gólem slade
                600, //Mini Castra
                601, //Caballero de la muerte
                602, //Sanguijuela gorda
                603, //Ginseng milenario
                604, //Rey basilisco
                605, //Gigante guerrero
                606, //Árbol maldito
                607, //Rey cangrejo
                2529, //Castra Oscuro
                1904, //Sombra de Kertos
                796, //Rey Pollo
                388, //Rey Pollo
                774, //Reina Gallina
                2331, //Diablilla Hongbi
                2332, //Diablilla Cheongbi
                2309, //Vulpina
                2322, //Maru
                1381, //Jack O´Lantern
                2357, //Lola Cucharón
                533, //Cabeza grande de muñeco de nieve
                1500, //Capitán Pete O'Peng
                282, //Mamá cubi
                284, //Ginseng
                285, //Castra Oscuro
                289, //Araña negra gigante
                286, //Slade gigante
                587, //Lord Mukraju
                563, //Maestro Morcos
                629, //Lady Calvina
                624, //Lord Berios
                577, //Maestro Hatus
                557, //Ross Hisrant
                2305, //Caligor
                2326, //Bruja Laurena
                2327, //Bestia de Laurena
                2639, //Yertirán podrido
                1028, //Ibrahim
                2034, //Lord Draco
                2049, //Glacerus, el frío
                1044, //Valakus, Rey del Fuego
                1905, //Sombra de Valakus
                1046, //Kertos, el Perro Demonio
                1912, //Sombra de Kertos
                637, //Perro demonio Kertos fuerte
                1099, //Fantasma de Grenigas
                1058, //Grenigas, el Dios del Fuego
                2619, //Fafnir, el Codicioso
                2504, //Zenas
                2514, //Erenia
                2574, //Fernon incompleta
                679, //Guardián de los ángeles 
                680, //Guardián de los demonios
                967, //Altar de los ángeles
                968, //Altar de los diablo
                533, // Huge Snowman Head
            };
            MapBossVNums = new List<short>
            {
                580, //Perro infernal
                582, //Ginseng fuerte
                583, //Basilisco fuerte
                584, //Rey Tubérculo
                585, //Gran Rey Tubérculo
                586, //Rey Tubérculo monstruoso
                588, //Pollito gigante
                589, //Mandra milenaria
                590, //Perro guardián del infierno
                591, //Gallo de pelea gigante
                592, //Patata milenaria
                593, //Perro de Satanás
                594, //Barepardo gigante
                595, //Boing satélite
                596, //Raíz demoníaca
                597, //Slade musculoso
                598, //Rey de las tinieblas
                599, //Gólem slade
                600, //Mini Castra
                601, //Caballero de la muerte
                602, //Sanguijuela gorda
                603, //Ginseng milenario
                604, //Rey basilisco
                605, //Gigante guerrero
                606, //Árbol maldito
                607, //Rey cangrejo
                2529, //Castra Oscuro
                1904, //Sombra de Kertos
                1346, //Baúl de la banda de ladrones
                1347, //Baúl del tesoro del olvido
                1348, //Baúl extraño
                1384, //Halloween
                1906,
                1905,
                2350
            };
        }

        private void MaintenanceProcess()
        {
            List<ClientSession> sessions = Sessions.Where(c => c.IsConnected).ToList();
            MaintenanceLogDTO maintenanceLog = DAOFactory.MaintenanceLogDAO.LoadFirst();
            if (maintenanceLog != null)
            {
                if (maintenanceLog.DateStart <= DateTime.Now)
                {
                    Logger.LogUserEvent("MAINTENANCE_STATE", "Caller: ServerManager", $"[Maintenance]{Language.Instance.GetMessageFromKey("MAINTENANCE_PLANNED")}");
                    sessions.Where(s => s.Account.Authority < AuthorityType.SMOD).ToList().ForEach(session => session.Disconnect());
                }
                else if (LastMaintenanceAdvert.AddMinutes(1) <= DateTime.Now && maintenanceLog.DateStart <= DateTime.Now.AddMinutes(5))
                {
                    int min = (maintenanceLog.DateStart - DateTime.Now).Minutes;
                    if (min != 0)
                    {
                        Shout($"Maintenance will begin in {min} minutes");
                    }
                    LastMaintenanceAdvert = DateTime.Now;
                }
            }
        }

        private void OnBazaarRefresh(object sender, EventArgs e)
        {
            long bazaarId = (long)sender;
            BazaarItemDTO bzdto = DAOFactory.BazaarItemDAO.LoadById(bazaarId);
            BazaarItemLink bzlink = BazaarList.Find(s => s.BazaarItem.BazaarItemId == bazaarId);
            lock (BazaarList)
            {
                if (bzdto != null)
                {
                    CharacterDTO chara = DAOFactory.CharacterDAO.LoadById(bzdto.SellerId);
                    if (bzlink != null)
                    {
                        BazaarList.Remove(bzlink);
                        bzlink.BazaarItem = bzdto;
                        bzlink.Owner = chara.Name;
                        bzlink.Item = new ItemInstance(DAOFactory.ItemInstanceDAO.LoadById(bzdto.ItemInstanceId));
                        BazaarList.Add(bzlink);
                    }
                    else
                    {
                        BazaarItemLink item = new BazaarItemLink
                        {
                            BazaarItem = bzdto
                        };
                        if (chara != null)
                        {
                            item.Owner = chara.Name;
                            item.Item = new ItemInstance(DAOFactory.ItemInstanceDAO.LoadById(bzdto.ItemInstanceId));
                        }
                        BazaarList.Add(item);
                    }
                }
                else if (bzlink != null)
                {
                    BazaarList.Remove(bzlink);
                }
            }
            InBazaarRefreshMode = false;
        }

        private void OnFamilyRefresh(object sender, EventArgs e)
        {
            Tuple<long, bool> tuple = (Tuple<long, bool>)sender;
            long familyId = tuple.Item1;
            FamilyDTO famdto = DAOFactory.FamilyDAO.LoadById(familyId);
            Family fam = FamilyList[familyId];
            lock (FamilyList)
            {
                if (famdto != null)
                {
                    Family newFam = new Family(famdto);
                    if (fam != null)
                    {
                        newFam.LandOfDeath = fam.LandOfDeath;
                        newFam.Act4Raid = fam.Act4Raid;
                        newFam.Act4RaidBossMap = fam.Act4RaidBossMap;
                    }

                    newFam.FamilyCharacters = new List<FamilyCharacter>();
                    foreach (FamilyCharacterDTO famchar in DAOFactory.FamilyCharacterDAO.LoadByFamilyId(famdto.FamilyId).ToList())
                    {
                        newFam.FamilyCharacters.Add(new FamilyCharacter(famchar));
                    }
                    FamilyCharacter familyHead = newFam.FamilyCharacters.Find(s => s.Authority == FamilyAuthority.Head);
                    if (familyHead != null)
                    {
                        newFam.Warehouse = new Inventory(new Character(familyHead.Character));
                        foreach (ItemInstanceDTO inventory in DAOFactory.ItemInstanceDAO.LoadByCharacterId(familyHead.CharacterId).Where(s => s.Type == InventoryType.FamilyWareHouse).ToList())
                        {
                            inventory.CharacterId = familyHead.CharacterId;
                            newFam.Warehouse[inventory.Id] = new ItemInstance(inventory);
                        }
                    }
                    newFam.FamilyLogs = DAOFactory.FamilyLogDAO.LoadByFamilyId(famdto.FamilyId).ToList();
                    FamilyList[familyId] = newFam;

                    Parallel.ForEach(Sessions.Where(s => newFam.FamilyCharacters.Any(m => m.CharacterId == s.Character.CharacterId)), session =>
                    {
                        if (session.Character.LastFamilyLeave < DateTime.Now.AddDays(-1).Ticks)
                        {
                            session.Character.Family = newFam;

                            if (tuple.Item2)
                            {
                                session.Character.ChangeFaction((FactionType)newFam.FamilyFaction);
                            }
                        session?.CurrentMapInstance?.Broadcast(session?.Character?.GenerateGidx());
                        }
                    });
                }
                else if (fam != null)
                {
                    lock (FamilyList)
                    {
                        FamilyList.Remove(fam.FamilyId);
                    }
                    foreach (ClientSession sess in Sessions.Where(s => fam.FamilyCharacters.Any(f => f.CharacterId.Equals(s.Character.CharacterId))))
                    {
                        sess.Character.Family = null;
                        sess.SendPacket(sess.Character.GenerateGidx());
                    }
                }
            }
        }

        private static void OnGlobalEvent(object sender, EventArgs e) => EventHelper.GenerateEvent((EventType)sender);

        private void OnMessageSentToCharacter(object sender, EventArgs e)
        {
            if (sender != null)
            {
                SCSCharacterMessage message = (SCSCharacterMessage)sender;

                ClientSession targetSession = Sessions.SingleOrDefault(s => s.Character.CharacterId == message.DestinationCharacterId);
                switch (message.Type)
                {
                    case MessageType.WhisperGM:
                    case MessageType.Whisper:
                        if (targetSession == null/* || (message.Type == MessageType.WhisperGM && targetSession.Account.Authority != AuthorityType.GameMaster)*/)
                        {
                            return;
                        }

                        if (targetSession.Character.GmPvtBlock)
                        {
                            if (message.DestinationCharacterId != null)
                            {
                                CommunicationServiceClient.Instance.SendMessageToCharacter(new SCSCharacterMessage
                                {
                                    DestinationCharacterId = message.SourceCharacterId,
                                    SourceCharacterId = message.DestinationCharacterId.Value,
                                    SourceWorldId = WorldId,
                                    Message = targetSession.Character.GenerateSay(
                                        Language.Instance.GetMessageFromKey("GM_CHAT_BLOCKED"), 10),
                                    Type = MessageType.Other
                                });
                            }
                        }
                        else if (targetSession.Character.WhisperBlocked
                            && DAOFactory.AccountDAO.LoadById(DAOFactory.CharacterDAO.LoadById(message.SourceCharacterId).AccountId).Authority < AuthorityType.TMOD)
                        {
                            if (message.DestinationCharacterId != null)
                            {
                                CommunicationServiceClient.Instance.SendMessageToCharacter(new SCSCharacterMessage
                                {
                                    DestinationCharacterId = message.SourceCharacterId,
                                    SourceCharacterId = message.DestinationCharacterId.Value,
                                    SourceWorldId = WorldId,
                                    Message = UserInterfaceHelper.GenerateMsg(
                                        Language.Instance.GetMessageFromKey("USER_WHISPER_BLOCKED"), 0),
                                    Type = MessageType.Other
                                });
                            }
                        }
                        else
                        {
                            if (message.SourceWorldId != WorldId)
                            {
                                if (message.DestinationCharacterId != null)
                                {
                                    CommunicationServiceClient.Instance.SendMessageToCharacter(new SCSCharacterMessage
                                    {
                                        DestinationCharacterId = message.SourceCharacterId,
                                        SourceCharacterId = message.DestinationCharacterId.Value,
                                        SourceWorldId = WorldId,
                                        Message = targetSession.Character.GenerateSay(
                                            string.Format(
                                                Language.Instance.GetMessageFromKey("MESSAGE_SENT_TO_CHARACTER"),
                                                targetSession.Character.Name, ChannelId), 11),
                                        Type = MessageType.Other
                                    });
                                }
                                targetSession.SendPacket($"{message.Message} <{Language.Instance.GetMessageFromKey("CHANNEL")}: {CommunicationServiceClient.Instance.GetChannelIdByWorldId(message.SourceWorldId)}>");
                            }
                            else
                            {
                                targetSession.SendPacket(message.Message);
                            }
                        }
                        break;

                    case MessageType.Shout:
                        Shout(message.Message);
                        break;

                    case MessageType.PrivateChat:
                        targetSession?.SendPacket(message.Message);
                        break;

                    case MessageType.FamilyChat:
                        if (message.DestinationCharacterId.HasValue && message.SourceWorldId != WorldId)
                        {
                            Parallel.ForEach(Instance.Sessions, session =>
                            {
                                if (session.HasSelectedCharacter && session.Character.Family != null && session.Character.Family.FamilyId == message.DestinationCharacterId)
                                {
                                    session.SendPacket($"say 1 0 6 <{Language.Instance.GetMessageFromKey("CHANNEL")}: {CommunicationServiceClient.Instance.GetChannelIdByWorldId(message.SourceWorldId)}>{message.Message}");
                                }
                            });
                        }
                        break;

                    case MessageType.Family:
                        if (message.DestinationCharacterId.HasValue)
                        {
                            Parallel.ForEach(Instance.Sessions, session =>
                            {
                                if (session.HasSelectedCharacter && session.Character.Family != null && session.Character.Family.FamilyId == message.DestinationCharacterId)
                                {
                                    session.SendPacket(message.Message);
                                }
                            });
                        }
                        break;

                    case MessageType.Other:
                        targetSession?.SendPacket(message.Message);
                        break;

                    case MessageType.Broadcast:
                        Parallel.ForEach(Instance.Sessions, session => session.SendPacket(message.Message));
                        break;
                }
            }
        }

        private void OnPenaltyLogRefresh(object sender, EventArgs e)
        {
            int relId = (int)sender;
            PenaltyLogDTO reldto = DAOFactory.PenaltyLogDAO.LoadById(relId);
            PenaltyLogDTO rel = PenaltyLogs.Find(s => s.PenaltyLogId == relId);
            if (reldto != null)
            {
                if (rel != null)
                {
                }
                else
                {
                    PenaltyLogs.Add(reldto);
                }
            }
            else if (rel != null)
            {
                PenaltyLogs.Remove(rel);
            }
        }

        private void OnRelationRefresh(object sender, EventArgs e)
        {
            _inRelationRefreshMode = true;
            long relId = (long)sender;
            lock (CharacterRelations)
            {
                CharacterRelationDTO reldto = DAOFactory.CharacterRelationDAO.LoadById(relId);
                CharacterRelationDTO rel = CharacterRelations.Find(s => s.CharacterRelationId == relId);
                if (reldto != null)
                {
                    if (rel != null)
                    {
                        CharacterRelations.Find(s => s.CharacterRelationId == rel.CharacterRelationId).RelationType = reldto.RelationType;
                    }
                    else
                    {
                        CharacterRelations.Add(reldto);
                    }
                }
                else if (rel != null)
                {
                    CharacterRelations.Remove(rel);
                }
            }
            _inRelationRefreshMode = false;
        }

        private void OnSessionKicked(object sender, EventArgs e)
        {
            if (sender != null)
            {
                Tuple<long?, long?> kickedSession = (Tuple<long?, long?>)sender;
                if(!kickedSession.Item1.HasValue && !kickedSession.Item2.HasValue)
                {
                    return;
                }
                long? accId = kickedSession.Item1;
                long? sessId = kickedSession.Item2;

                ClientSession targetSession = CharacterScreenSessions.FirstOrDefault(s => s.SessionId == sessId || s.Account.AccountId == accId);
                targetSession?.Disconnect();
                targetSession = Sessions.FirstOrDefault(s => s.SessionId == sessId || s.Account.AccountId == accId);
                targetSession?.Disconnect();
            }
        }

        private static void OnShutdown(object sender, EventArgs e)
        {
            Instance.SaveAll();
            if (Instance.TaskShutdown != null)
            {
                Instance.ShutdownStop = true;
                Instance.TaskShutdown = null;
            }
            else
            {
                Instance.TaskShutdown = Instance.ShutdownTaskAsync();
                Instance.TaskShutdown.Start();
            }
        }

        private static void OnRestart(object sender, EventArgs e)
        {
            Instance.SaveAll();
            if (Instance.TaskShutdown != null)
            {
                Instance.IsReboot = false;
                Instance.ShutdownStop = true;
                Instance.TaskShutdown = null;
            }
            else
            {
                Instance.IsReboot = true;
                Instance.TaskShutdown = Instance.ShutdownTaskAsync((int)sender);
            }
        }

        private void RemoveItemProcess()
        {
            try
            {
                Parallel.ForEach(Sessions.Where(c => c.IsConnected), session => session.Character?.RefreshValidity());
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        private static void ReviveTask(ClientSession session)
        {
            Task.Factory.StartNew(async () =>
            {
                bool revive = true;
                for (int i = 1; i <= 30; i++)
                {
                    await Task.Delay(1000).ConfigureAwait(false);
                    if (session.Character.Hp > 0)
                    {
                        revive = false;
                        break;
                    }
                }
                if (revive)
                {
                    Instance.ReviveFirstPosition(session.Character.CharacterId);
                }
            });
        }

        // Server
        private void SaveAllProcess()
        {
            try
            {
               // Logger.Info(Language.Instance.GetMessageFromKey("SAVING_ALL"));
                SaveAll();
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

#endregion
    }
}