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
using OpenNos.GameObject.Helpers;
using OpenNos.PathFinder;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using OpenNos.GameObject.Networking;

namespace OpenNos.GameObject
{
    public class MapInstance : BroadcastableBase
    {
        #region Members

        public ConcurrentBag<MapDesignObject> MapDesignObjects = new ConcurrentBag<MapDesignObject>();

        private readonly ThreadSafeSortedList<int, int> _mapMonsterIds;

        private readonly ThreadSafeSortedList<int, int> _mapNpcIds;

        private readonly ThreadSafeSortedList<long, MapMonster> _monsters;

        private readonly ThreadSafeSortedList<long, MapMonster> _delayedMonsters;
        
        private readonly ThreadSafeSortedList<long, MapNpc> _npcs;

        private readonly Random _random;

        private bool _isSleeping;

        private bool _isSleepingRequest;

        #endregion

        #region Instantiation

        public MapInstance(Map map, Guid guid, bool shopAllowed, MapInstanceType type, InstanceBag instanceBag, bool dropAllowed = false)
        {
            OnSpawnEvents = new List<EventContainer>();
            Buttons = new List<MapButton>();
            XpRate = 0;

            if (type == MapInstanceType.BaseMapInstance)
            {
                XpRate = map.XpRate;
            }
            //check here
            MinLevel = 1;
            MaxLevel = 99;

            if (type != MapInstanceType.TimeSpaceInstance && type != MapInstanceType.RaidInstance)
            {
                switch (map.MapId)
                {
                    

                    case 154: // Caligor's Realm
                        MinLevel = 80;
                        break;
                    case 228: // Cylloan
                        MinLevel = 88;
                        break;
                }
            }

            DropRate = 0;
            DropAllowed = dropAllowed;
            InstanceMusic = map.Music;
            ShopAllowed = shopAllowed;
            MapInstanceType = type;
            _isSleeping = true;
            LastUserShopId = 0;
            InstanceBag = instanceBag;
            Clock = new Clock(3);
            _random = new Random();
            Map = map;
            MapInstanceId = guid;
            ScriptedInstances = new List<ScriptedInstance>();
            OnCharacterDiscoveringMapEvents = new List<Tuple<EventContainer, List<long>>>();
            OnMoveOnMapEvents = new ThreadSafeGenericList<EventContainer>();
            OnAreaEntryEvents = new ThreadSafeGenericList<ZoneEvent>();
            WaveEvents = new List<EventWave>();
            OnMapClean = new List<EventContainer>();
            _monsters = new ThreadSafeSortedList<long, MapMonster>();
            _delayedMonsters = new ThreadSafeSortedList<long, MapMonster>();
            _npcs = new ThreadSafeSortedList<long, MapNpc>();
            _mapMonsterIds = new ThreadSafeSortedList<int, int>();
            _mapNpcIds = new ThreadSafeSortedList<int, int>();
            DroppedList = new ThreadSafeSortedList<long, MapItem>();
            Portals = new List<Portal>();
            UnlockEvents = new List<EventContainer>();
            UserShops = new Dictionary<long, MapShop>();
            RemovedMobNpcList = new List<object>();
            StartLife();
        }

        #endregion

        #region Properties

        public List<EventContainer> OnSpawnEvents { get; set; }

        public List<MapButton> Buttons { get; set; }

        public Clock Clock { get; set; }

        public ThreadSafeSortedList<long, MapItem> DroppedList { get; }

        public bool DropAllowed { get; set; }

        public int DropRate { get; set; }

        public InstanceBag InstanceBag { get; set; }

        public int InstanceMusic { get; set; }

        public bool IsDancing { get; set; }

        public bool IsPVP { get; set; }

        public bool IsScriptedInstance => MapInstanceType == MapInstanceType.TimeSpaceInstance || MapInstanceType == MapInstanceType.RaidInstance;

        public bool IsReputationMap
        {
            get
            {
                if (!IsScriptedInstance)
                {
                    switch(Map.MapId)
                    {
                        case 134:
                        case 153:
                            return true;
                    }
                }

                return false;
            }
        }

        public bool IsSleeping
        {
            get
            {
                if (_isSleepingRequest && !_isSleeping && LastUnregister.AddSeconds(30) < DateTime.Now)
                {
                    _isSleeping = true;
                    _isSleepingRequest = false;
                    return true;
                }
                return _isSleeping;
            }
            set
            {
                if (value)
                {
                    _isSleepingRequest = true;
                }
                else
                {
                    _isSleeping = false;
                    _isSleepingRequest = false;
                }
            }
        }

        public long LastUserShopId { get; set; }

        public Map Map { get; set; }

        public byte MapIndexX { get; set; }

        public byte MapIndexY { get; set; }

        public Guid MapInstanceId { get; set; }

        public MapInstanceType MapInstanceType { get; set; }
        
        public byte MinLevel { get; set; }
        
        public byte MaxLevel { get; set; }
        public byte Class { get; set; }

        public List<MapMonster> Monsters => _monsters.GetAllItems();

        public List<MapMonster> DelayedMonsters => _delayedMonsters.GetAllItems();

        public List<MapNpc> Npcs => _npcs.GetAllItems();

        public ThreadSafeGenericList<ZoneEvent> OnAreaEntryEvents { get; set; }

        public List<Tuple<EventContainer, List<long>>> OnCharacterDiscoveringMapEvents { get; set; }

        public List<EventContainer> OnMapClean { get; set; }

        public ThreadSafeGenericList<EventContainer> OnMoveOnMapEvents { get; set; }

        public List<Portal> Portals { get; }

        public List<object> RemovedMobNpcList { get; set; }

        public List<ScriptedInstance> ScriptedInstances { get; set; }

        public bool ShopAllowed { get; set; }

        public List<EventContainer> UnlockEvents { get; set; }

        public Dictionary<long, MapShop> UserShops { get; }

        public List<EventWave> WaveEvents { get; set; }

        public int XpRate { get; set; }

        #endregion

        #region Methods

        public IEnumerable<BattleEntity> BattleEntities
        {
            get
            {
                return Sessions.Select(s => s.Character?.BattleEntity).Concat(Mates.Select(s => s.BattleEntity)).Concat(Monsters.Select(s => s.BattleEntity)).Concat(Npcs.Select(s => s.BattleEntity));
            }
        }
        
        public void AddMonster(MapMonster monster) => _monsters[monster.MapMonsterId] = monster;

        public void AddDelayedMonster(MapMonster monster) => _delayedMonsters[monster.MapMonsterId] = monster;

        public void AddNPC(MapNpc npc) => _npcs[npc.MapNpcId] = npc;

        public void DespawnMonster(int monsterVnum)
        {
            Parallel.ForEach(_monsters.Where(s => s.MonsterVNum == monsterVnum), monster =>
            {
                monster.SetDeathStatement();
                Broadcast(StaticPacketHelper.Out(UserType.Monster, monster.MapMonsterId));
            });
        }

        public void DespawnMonster(MapMonster monster)
        {
            monster.SetDeathStatement();
            Broadcast(StaticPacketHelper.Out(UserType.Monster, monster.MapMonsterId));
        }

        public void DropItemByMonster(long? owner, DropDTO drop, short mapX, short mapY, bool isQuest = false)
        {
            try
            {
                short localMapX = mapX;
                short localMapY = mapY;
                List<MapCell> possibilities = new List<MapCell>();

                for (short x = -1; x < 2; x++)
                {
                    for (short y = -1; y < 2; y++)
                    {
                        possibilities.Add(new MapCell { X = x, Y = y });
                    }
                }

                foreach (MapCell possibility in possibilities.OrderBy(s => ServerManager.RandomNumber()))
                {
                    localMapX = (short)(mapX + possibility.X);
                    localMapY = (short)(mapY + possibility.Y);
                    if (!Map.IsBlockedZone(localMapX, localMapY))
                    {
                        break;
                    }
                }

                MonsterMapItem droppedItem = new MonsterMapItem(localMapX, localMapY, drop.ItemVNum, drop.Amount, owner ?? -1, isQuest);
                DroppedList[droppedItem.TransportId] = droppedItem;
                Broadcast($"drop {droppedItem.ItemVNum} {droppedItem.TransportId} {droppedItem.PositionX} {droppedItem.PositionY} {(droppedItem.GoldAmount > 1 ? droppedItem.GoldAmount : droppedItem.Amount)} {(isQuest ? 1 : 0)} {owner}");
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        public void DropItems(List<Tuple<short, int, short, short>> list)
        {
            foreach (Tuple<short, int, short, short> drop in list)
            {
                MonsterMapItem droppedItem = new MonsterMapItem(drop.Item3, drop.Item4, drop.Item1, drop.Item2);
                DroppedList[droppedItem.TransportId] = droppedItem;
                Broadcast($"drop {droppedItem.ItemVNum} {droppedItem.TransportId} {droppedItem.PositionX} {droppedItem.PositionY} {(droppedItem.GoldAmount > 1 ? droppedItem.GoldAmount : droppedItem.Amount)} 0 {droppedItem.OwnerId ?? -1}");
            }
        }
        public string GenerateMapDesignObjects()
        {
            var mlobjstring = "mltobj";
            var i = 0;
            foreach (MapDesignObject mp in MapDesignObjects)
            {
                mlobjstring += $" {mp.ItemInstance.ItemVNum}.{i}.{mp.MapX}.{mp.MapY}";
                i++;
            }

            return mlobjstring;
        }

        public IEnumerable<string> GenerateNPCShopOnMap() => (from npc in Npcs where npc.Shop != null select $"shop 2 {npc.MapNpcId} {npc.Shop.ShopId} {npc.Shop.MenuType} {npc.Shop.ShopType} {npc.Shop.Name}").ToList();

        public IEnumerable<string> GeneratePlayerShopOnMap() => UserShops.Select(shop => $"pflag 1 {shop.Value.OwnerId} {shop.Key + 1}").ToList();

        public string GenerateRsfn(bool isInit = false)
        {
            if (MapInstanceType == MapInstanceType.TimeSpaceInstance)
            {
                return $"rsfn {MapIndexX} {MapIndexY} {(isInit ? 1 : (Monsters.Where(s => s.IsAlive).ToList().Count == 0 ? 0 : 1))}";
            }
            return "";
        }

        public IEnumerable<string> GenerateUserShops() => UserShops.Select(shop => $"shop 1 {shop.Value.OwnerId} 1 3 0 {shop.Value.Name}").ToList();

        public List<MapMonster> GetMonsterInRangeList(short mapX, short mapY, byte distance, bool attackGreaterDistance = false) => _monsters.Where(s => s.IsAlive && (!attackGreaterDistance ? s.IsInRange(mapX, mapY, distance) : !s.IsInRange(mapX, mapY, distance))).ToList();

        public List<MapNpc> GetListNpcInRange(short mapX, short mapY, byte distance, bool attackGreaterDistance = false) => _npcs.Where(s => s.CurrentHp > 0 && (!attackGreaterDistance ? s.IsInRange(mapX, mapY, distance) : !s.IsInRange(mapX, mapY, distance))).ToList();

        public List<Mate> GetListMateInRange(short mapX, short mapY, byte distance, bool attackGreaterDistance = false) => Sessions.SelectMany(s => s.Character?.Mates.Where(m => (m.IsTeamMember || m.IsTemporalMate) && (!attackGreaterDistance ? m.IsInRange(mapX, mapY, distance) : !m.IsInRange(mapX, mapY, distance)))).ToList();

        public IEnumerable<string> GetMapDesignObjectEffects()
        {
            return MapDesignObjects.Select(mp => mp.GenerateEffect(false)).ToList();
        }
        public List<string> GetMapItems()
        {
            List<string> packets = new List<string>();
            Sessions.Where(s => s.Character?.InvisibleGm == false).ToList().ForEach(s => s.Character.Mates.Where(m => m.IsTeamMember).ToList().ForEach(m => packets.Add(m.GenerateIn())));
            Portals.ForEach(s => packets.Add(s.GenerateGp()));
            ScriptedInstances.Where(s => s.Type == ScriptedInstanceType.TimeSpace).ToList().ForEach(s => packets.Add(s.GenerateWp()));
            Monsters.ForEach(s =>
            {
                packets.Add(s.GenerateIn());
                if (s.IsBoss)
                {
                    packets.Add(s.GenerateBoss());
                }
            });

            Npcs.ForEach(npc =>
            {
                packets.Add(npc.GenerateIn());

                if (npc.EffectDelay == 0)
                {
                    if (npc.Effect > 0 && npc.EffectActivated)
                    {
                        packets.Add($"eff {(byte)UserType.Npc} {npc.MapNpcId} {npc.Effect}");
                    }
                }
            });

            packets.AddRange(GenerateNPCShopOnMap());
            DroppedList.ForEach(s => packets.Add(s.GenerateIn()));
            Buttons.ForEach(s => packets.Add(s.GenerateIn()));
            packets.AddRange(GenerateUserShops());
            packets.AddRange(GeneratePlayerShopOnMap());
            return packets;
        }

        public MapMonster GetMonsterById(long mapMonsterId) => _monsters[mapMonsterId];

        public Mate GetMate(long mateTransportId) => Sessions.SelectMany(s => s.Character?.Mates).FirstOrDefault(m => m.MateTransportId == mateTransportId);

        public int GetNextMonsterId()
        {
            int nextId = _mapMonsterIds.Count > 0 ? _mapMonsterIds.Last() + 1 : 1;
            _mapMonsterIds[nextId] = nextId;
            return nextId;
        }

        public int GetNextNpcId()
        {
            int nextId = _mapNpcIds.Count > 0 ? _mapNpcIds.Last() + 1 : 1;
            while (ServerManager.Instance.GetShopByMapNpcId(nextId) != null) nextId++;
            _mapNpcIds[nextId] = nextId;
            return nextId;
        }
        public void KickAll(Character exeptCharacter = null, bool exeptFriends = false)
        {
            Sessions.Where(s => exeptCharacter == null || s.Character != exeptCharacter && (!exeptFriends || !exeptCharacter.IsFriendOfCharacter(s.Character.CharacterId))).ToList()
            .ForEach(s => ServerManager.Instance.ChangeMap(s.Character.CharacterId, s.Character.MapId, s.Character.MapX, s.Character.MapY));
        }
        public MapNpc GetNpc(long mapNpcId) => _npcs[mapNpcId];

        public void LoadMonsters()
        {
            Parallel.ForEach(DAOFactory.MapMonsterDAO.LoadFromMap(Map.MapId).ToList(), monster =>
            {
                MapMonster mapMonster = new MapMonster(monster);
                mapMonster.Initialize(this);
                int mapMonsterId = mapMonster.MapMonsterId;
                _monsters[mapMonsterId] = mapMonster;
                _mapMonsterIds[mapMonsterId] = mapMonsterId;
            });
        }

        public void LoadNpcs()
        {
            Parallel.ForEach(DAOFactory.MapNpcDAO.LoadFromMap(Map.MapId).ToList(), npc =>
            {
                MapNpc mapNpc = new MapNpc(npc);
                mapNpc.Initialize(this);
                int mapNpcId = mapNpc.MapNpcId;
                _npcs[mapNpcId] = mapNpc;
                _mapNpcIds[mapNpcId] = mapNpcId;
            });
        }

        public void LoadPortals()
        {
            foreach (PortalDTO portal in DAOFactory.PortalDAO.LoadByMap(Map.MapId))
            {
                Portal p = new Portal(portal)
                {
                    SourceMapInstanceId = MapInstanceId
                };
                Portals.Add(p);
            }
        }

        public void MapClear()
        {
            Broadcast("mapclear");
            Parallel.ForEach(GetMapItems(), s => Broadcast(s));
        }

        public MapItem PutItem(InventoryType type, short slot, short amount, ref ItemInstance inv, ClientSession session)
        {
            Logger.LogUserEventDebug("PUTITEM", session.GenerateIdentity(), $"type: {type} slot: {slot} amount: {amount}");
            Guid random2 = Guid.NewGuid();
            MapItem droppedItem = null;
            List<GridPos> possibilities = new List<GridPos>();

            for (short x = -2; x < 3; x++)
            {
                for (short y = -2; y < 3; y++)
                {
                    possibilities.Add(new GridPos { X = x, Y = y });
                }
            }

            short mapX = 0;
            short mapY = 0;
            bool niceSpot = false;
            foreach (GridPos possibility in possibilities.OrderBy(s => _random.Next()))
            {
                mapX = (short)(session.Character.PositionX + possibility.X);
                mapY = (short)(session.Character.PositionY + possibility.Y);
                if (!Map.IsBlockedZone(mapX, mapY))
                {
                    niceSpot = true;
                    break;
                }
            }

            if (niceSpot && amount > 0 && amount <= inv.Amount)
            {
                ItemInstance newItemInstance = inv.DeepCopy();
                newItemInstance.Id = random2;
                newItemInstance.Amount = amount;
                droppedItem = new CharacterMapItem(mapX, mapY, newItemInstance);

                DroppedList[droppedItem.TransportId] = droppedItem;
                inv.Amount -= amount;
            }

            return droppedItem;
        }

        public void RemoveMapItem()
        {
            // take the data from list to remove it without having enumeration problems (ToList)
            try
            {
                List<MapItem> dropsToRemove = DroppedList.Where(dl => dl.CreatedDate.AddMinutes(3) < DateTime.Now);
                Parallel.ForEach(dropsToRemove, drop =>
                {
                    Broadcast(StaticPacketHelper.Out(UserType.Object, drop.TransportId));
                    DroppedList.Remove(drop.TransportId);
                });
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        public void RemoveMonster(MapMonster monsterToRemove) => _monsters.Remove(monsterToRemove.MapMonsterId);

        public void RemoveDelayedMonster(MapMonster monsterToRemove) => _delayedMonsters.Remove(monsterToRemove.MapMonsterId);

        public void RemoveNpc(MapNpc npcToRemove) => _npcs.Remove(npcToRemove.MapNpcId);

        public void SpawnButton(MapButton parameter)
        {
            Buttons.Add(parameter);
            Broadcast(parameter.GenerateIn());
        }

        public void ThrowItems(Tuple<int, short, byte, int, int, short> parameter)
        {
            MapMonster mon = Monsters.Find(s => s.MapMonsterId == parameter.Item1) ?? Monsters.Find(s => s.MonsterVNum == parameter.Item1);
            if (mon == null)
            {
                return;
            }
            short originX = mon.MapX;
            short originY = mon.MapY;
            short destX;
            short destY;
            int amount = 1;
            Observable.Timer(TimeSpan.FromSeconds(parameter.Item6)).Subscribe(s =>
            {
                for (int i = 0; i < parameter.Item3; i++)
                {
                    amount = ServerManager.RandomNumber(parameter.Item4, parameter.Item5);
                    destX = (short)(originX + ServerManager.RandomNumber(-10, 10));
                    destY = (short)(originY + ServerManager.RandomNumber(-10, 10));
                    if (Map.IsBlockedZone(destX, destY))
                    {
                        destX = originX;
                        destY = originY;
                    }
                    MonsterMapItem droppedItem = new MonsterMapItem(destX, destY, parameter.Item2, amount);
                    DroppedList[droppedItem.TransportId] = droppedItem;
                    Broadcast($"throw {droppedItem.ItemVNum} {droppedItem.TransportId} {originX} {originY} {droppedItem.PositionX} {droppedItem.PositionY} {(droppedItem.GoldAmount > 1 ? droppedItem.GoldAmount : droppedItem.Amount)}");
                }
            });
        }

        internal void CreatePortal(Portal portal)
        {
            portal.SourceMapInstanceId = MapInstanceId;
            Portals.Add(portal);
            Broadcast(portal.GenerateGp());
        }

        internal void CreatePortalTemp(Portal portal, int timeInSeconds = 0, bool isTemporary = false)
        {
            portal.SourceMapInstanceId = MapInstanceId;
            Portals.Add(portal);
            Broadcast(portal.GenerateGp());
            if (isTemporary)
            {
                Observable.Timer(TimeSpan.FromSeconds(timeInSeconds)).Subscribe(o =>
                {
                    Portals.Remove(portal);
                    MapClear();
                });
            }
        }

        internal IEnumerable<Character> GetCharactersInRange(short mapX, short mapY, byte distance, bool attackGreaterDistance = false)
        {
            List<Character> characters = new List<Character>();
            IEnumerable<ClientSession> cl = Sessions.Where(s => s.HasSelectedCharacter && s.Character.Hp > 0);
            IEnumerable<ClientSession> clientSessions = cl as IList<ClientSession> ?? cl.ToList();
            for (int i = clientSessions.Count() - 1; i >= 0; i--)
            {
                if (!attackGreaterDistance)
                {
                    if (Map.GetDistance(new MapCell { X = mapX, Y = mapY }, new MapCell { X = clientSessions.ElementAt(i).Character.PositionX, Y = clientSessions.ElementAt(i).Character.PositionY }) <= distance + 1)
                    {
                        characters.Add(clientSessions.ElementAt(i).Character);
                    }
                }
                else
                {
                    if (Map.GetDistance(new MapCell { X = mapX, Y = mapY }, new MapCell { X = clientSessions.ElementAt(i).Character.PositionX, Y = clientSessions.ElementAt(i).Character.PositionY }) > distance)
                    {
                        characters.Add(clientSessions.ElementAt(i).Character);
                    }
                }
            }
            return characters;
        }

        public IEnumerable<BattleEntity> GetBattleEntitiesInRange(MapCell pos, byte distance)
        {
            return BattleEntities.Where(b => Map.GetDistance(b.GetPos(), pos) <= distance);
        }

        internal void RemoveMonstersTarget(long characterId) => Parallel.ForEach(Monsters.Where(m => m.Target?.MapEntityId == characterId), monster => monster.RemoveTarget());

        internal void StartLife()
        {
            Observable.Interval(TimeSpan.FromSeconds(1)).Subscribe(x =>
            {
                if (InstanceBag?.EndState == 0)
                {
                    Parallel.ForEach(WaveEvents, waveEvent =>
                    {
                        if (waveEvent?.LastStart.AddSeconds(waveEvent.Delay) <= DateTime.Now)
                        {
                            if (waveEvent.Offset == 0 && waveEvent.RunTimes > 0)
                            {
                                waveEvent.Events.ForEach(e => EventHelper.Instance.RunEvent(e));
                                waveEvent.RunTimes--;
                            }
                            waveEvent.Offset = waveEvent.Offset > 0 ? (byte)(waveEvent.Offset - 1) : (byte)0;
                            waveEvent.LastStart = DateTime.Now;
                        }
                    });
                    try
                    {
                        if (!Monsters.Any(s => s.IsAlive && s.Owner?.Character == null && s.Owner?.Mate == null) && DelayedMonsters.Count == 0)
                        {
                            List<EventContainer> OnMapCleanCopy = OnMapClean.ToList();
                            OnMapCleanCopy.ForEach(e => EventHelper.Instance.RunEvent(e));
                            OnMapClean.RemoveAll(s => s != null && OnMapCleanCopy.Contains(s));
                        }
                        if (!IsSleeping)
                        {
                            RemoveMapItem();
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e);
                    }
                }
            });
        }

        internal int SummonMonster(MonsterToSummon summon)
        {
            NpcMonster npcMonster = ServerManager.GetNpcMonster(summon.VNum);

            if (npcMonster != null)
            {
                short mapX = summon.SpawnCell.X;
                short mapY = summon.SpawnCell.Y;

                if (mapX == 0 && mapY == 0)
                {
                    MapCell cell = Map.GetRandomPosition();

                    if (cell != null)
                    {
                        mapX = cell.X;
                        mapY = cell.Y;
                    }
                }

                MapMonster mapMonster = new MapMonster
                {
                    MonsterVNum = npcMonster.NpcMonsterVNum,
                    MapX = mapX,
                    MapY = mapY,
                    Position = 2,
                    MapId = Map.MapId,
                    IsMoving = summon.IsMoving,
                    MapMonsterId = GetNextMonsterId(),
                    ShouldRespawn = false,
                    OnNoticeEvents = summon.NoticingEvents,
                    UseSkillOnDamage = summon.UseSkillOnDamage,
                    OnSpawnEvents = summon.SpawnEvents,
                    IsTarget = summon.IsTarget,
                    Target = summon.Target,
                    IsBonus = summon.IsBonus,
                    IsBoss = summon.IsBoss,
                    NoticeRange = summon.NoticeRange,
                    Owner = summon.Owner,
                    AliveTime = summon.AliveTime,
                    AliveTimeMp = summon.AliveTimeMp,
                    BaseMaxHp = summon.MaxHp,
                    BaseMaxMp = summon.MaxMp
                };

                if (summon.HasDelay > 0)
                {
                    AddDelayedMonster(mapMonster);
                }

                Observable.Timer(TimeSpan.FromSeconds(summon.HasDelay))
                    .Subscribe(o =>
                    {
                        mapMonster.Initialize(this);
                        mapMonster.BattleEntity.OnDeathEvents.AddRange(summon.DeathEvents);
                        mapMonster.IsHostile = summon.IsHostile;

                        AddMonster(mapMonster);
                        Broadcast(mapMonster.GenerateIn());
                        RemoveDelayedMonster(mapMonster);

                        if (summon.AfterSpawnEvents.Any())
                        {
                            summon.AfterSpawnEvents.ForEach(e => EventHelper.Instance.RunEvent(e, monster: mapMonster));
                        }

                        if (summon.IsMeteorite)
                        {
                            OnMeteoriteEvents(summon, mapMonster);
                        }
                    });

                return mapMonster.MapMonsterId;
            }

            return default;
        }

        internal void SummonMonsters(List<MonsterToSummon> monstersToSummon)
        {
            foreach (MonsterToSummon monsterToSummon in monstersToSummon)
            {
                SummonMonster(monsterToSummon);
            }
        }

        private void OnMeteoriteEvents(MonsterToSummon monsterToSummon, MapMonster mapMonster)
        {
            NpcMonsterSkill ski = mapMonster.Skills?.FirstOrDefault();

            if (ski?.Skill == null)
            {
                return;
            }

            Broadcast(StaticPacketHelper.GenerateEff(UserType.Monster, mapMonster.MapMonsterId, ski.Skill.CastEffect));

            Observable.Timer(TimeSpan.FromSeconds(2))
                .Subscribe(a =>
                {
                    Broadcast(StaticPacketHelper.SkillUsed(UserType.Monster, mapMonster.MapMonsterId, 3, mapMonster.MapMonsterId,
                        ski.SkillVNum, ski.Skill.Cooldown, ski.Skill.AttackAnimation, ski.Skill.Effect, mapMonster.MapX, mapMonster.MapY,
                        true, 0, 0, 0, ski.Skill.SkillType));

                    Observable.Timer(TimeSpan.FromMilliseconds(500))
                        .Subscribe(b =>
                        {
                            Parallel.ForEach(GetBattleEntitiesInRange(new MapCell { X = mapMonster.MapX, Y = mapMonster.MapY }, ski.Skill.Range),
                                x =>
                                {
                                    if (x.Mate != null || x.MapNpc != null || x.MapMonster?.IsBoss == true
                                        || (x.Character != null && x.Character.CharacterId == mapMonster.Owner?.MapEntityId) 
                                        || (x.MapMonster != null && monsterToSummon.Owner == null))
                                    {
                                        return;
                                    }

                                    int damage = 0;

                                    if (x.MapMonster?.Owner is BattleEntity owner)
                                    {
                                        int hitMode = 0;
                                        bool onyxWings = false;

                                        damage = DamageHelper.Instance.CalculateDamage(owner, x, ski.Skill, ref hitMode, ref onyxWings);
                                    }
                                    else
                                    {
                                        damage = monsterToSummon.Damage;
                                    }

                                    x.GetDamage(damage, mapMonster.BattleEntity);

                                    if (x.Character != null)
                                    {
                                        x.Character.Session?.SendPacket(x.Character.GenerateStat());
                                    }

                                    x.MapInstance.Broadcast(x.GenerateDm(damage));

                                    if (x.Hp < 1)
                                    {
                                        x.MapInstance.Broadcast(StaticPacketHelper.Die(x.UserType, x.MapEntityId, x.UserType, x.MapEntityId));

                                        if (x.Character != null)
                                        {
                                            Observable.Timer(TimeSpan.FromSeconds(1)).Subscribe(c => ServerManager.Instance.AskRevive(x.Character.CharacterId));
                                        }
                                    }
                                });
                        });

                    RemoveMonster(mapMonster);
                    Broadcast(StaticPacketHelper.Out(UserType.Monster, mapMonster.MapMonsterId));
                });
        }

        internal int SummonNpc(NpcToSummon npcToSummon)
        {
            NpcMonster npcMonster = ServerManager.GetNpcMonster(npcToSummon.VNum);
            if (npcMonster != null)
            {
                MapNpc mapNpc = new MapNpc
                {
                    NpcVNum = npcMonster.NpcMonsterVNum,
                    MapX = npcToSummon.SpawnCell.X,
                    MapY = npcToSummon.SpawnCell.Y,
                    Position = npcToSummon.Dir,
                    MapId = Map.MapId,
                    ShouldRespawn = false,
                    IsMoving = npcToSummon.Move,
                    MapNpcId = GetNextNpcId(),
                    Target = npcToSummon.Target,
                    IsMate = npcToSummon.IsMate,
                    IsTsReward = npcToSummon.IsTsReward,
                    IsProtected = npcToSummon.IsProtected,
                };
                
                mapNpc.OnSpawnEvents = npcToSummon.SpawnEvents.ToList();
                mapNpc.Initialize(this);
                mapNpc.IsHostile = npcToSummon.IsHostile;
                mapNpc.BattleEntity.OnDeathEvents.AddRange(npcToSummon.DeathEvents);
                AddNPC(mapNpc);
                Broadcast(mapNpc.GenerateIn());
                return mapNpc.MapNpcId;
            }
            return default;
        }

        internal void SummonNpcs(List<NpcToSummon> npcsToSummon)
        {
            foreach (NpcToSummon npcToSummon in npcsToSummon)
            {
                SummonNpc(npcToSummon);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _npcs.Dispose();
                _monsters.Dispose();
                _delayedMonsters.Dispose();
                _mapNpcIds.Dispose();
                _mapMonsterIds.Dispose();
                DroppedList.Dispose();
                foreach (ClientSession session in ServerManager.Instance.Sessions.Where(s => s.Character != null && s.Character.MapInstanceId == MapInstanceId))
                {
                    ServerManager.Instance.ChangeMap(session.Character.CharacterId, session.Character.MapId, session.Character.MapX, session.Character.MapY);
                }
            }
        }

        #endregion
    }
}