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
using OpenNos.Data;
using OpenNos.Domain;
using OpenNos.GameObject.Battle;
using OpenNos.GameObject.Event;
using OpenNos.GameObject.Helpers;
using OpenNos.PathFinder;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using OpenNos.GameObject.Networking;
using static OpenNos.Domain.BCardType;
using System.Threading.Tasks;
using System.Threading;

namespace OpenNos.GameObject
{
    public class MapMonster : MapMonsterDTO
    {
        #region Members

        public object _onHitLockObject = new object();

        private int _movetime;
        
        private int _waitCount;

        private const int _maxDistance = 20;

        private const int _timeAgroLoss = 8000;

        private byte _speed;

        private short _previousSkillVNum = 0;

        #endregion

        #region Instantiation

        public MapMonster()
        {
            HitQueue = new ConcurrentQueue<HitRequest>();
            OnNoticeEvents = new List<EventContainer>();
            OnSpawnEvents = new List<EventContainer>();
            PVELockObject = new object();
        }

        public MapMonster(MapMonsterDTO input) : this()
        {
            IsDisabled = input.IsDisabled;
            IsMoving = input.IsMoving;
            MapId = input.MapId;
            MapMonsterId = input.MapMonsterId;
            MapX = input.MapX;
            MapY = input.MapY;
            MonsterVNum = input.MonsterVNum;
            Name = input.Name;
            Position = input.Position;
        }

        #endregion

        #region Properties

        public bool IsSelfAttack => MonsterHelper.IsSelfAttack(MonsterVNum);

        public bool IsPendingDelete { get; set; }

        public List<UseSkillOnDamage> UseSkillOnDamage { get; set; }

        public Node[][] BrushFireJagged { get; set; }

        public ThreadSafeSortedList<short, Buff> Buff => BattleEntity.Buffs;

        public new ThreadSafeSortedList<short, IDisposable> BuffObservables => BattleEntity.BuffObservables;

        public double CurrentHp { get; set; }

        public double CurrentMp { get; set; }

        public IDictionary<BattleEntity, long> DamageList { get; private set; }

        public List<BattleEntity> AggroList { get; set; }

        public DateTime Death { get; set; }

        public ConcurrentQueue<HitRequest> HitQueue { get; }

        public bool IsAlive { get; set; }

        public bool IsBonus { get; set; }

        public bool IsBoss { get; set; }

        public bool IsHostile { get; set; }

        public bool IsTarget { get; set; }

        public bool IsJumping { get; set; }

        public DateTime LastDefence { get; set; }

        public DateTime LastEffect { get; set; }

        public DateTime LastHealth { get; set; }

        public DateTime LastMonsterAggro { get; set; }

        public DateTime LastMove { get; set; }
        
        public DateTime LastSkill { get; set; }
        
        public IDisposable LifeEvent { get; set; }

        public MapInstance MapInstance { get; set; }

        public int BaseMaxHp { get; set; }

        public int BaseMaxMp { get; set; }

        public double MaxHp { get; set; }

        public double MaxMp { get; set; }

        public NpcMonster Monster { get; private set; }

        public ZoneEvent MoveEvent { get; set; }

        public bool NoAggresiveIcon { get; internal set; }

        public byte NoticeRange { get; set; }

        public List<EventContainer> OnDeathEvents => BattleEntity.OnDeathEvents;

        public List<EventContainer> OnNoticeEvents { get; set; }

        public List<EventContainer> OnSpawnEvents { get; set; }

        public List<Node> Path { get; set; }

        public bool? ShouldRespawn { get; set; }

        public List<NpcMonsterSkill> Skills { get; set; }

        public bool Started { get; internal set; }

        public byte Speed
        {
            get
            {
                if (_speed > 59)
                {
                    return 59;
                }
                return _speed;
            }

            set
            {
                _speed = value > 59 ? (byte)59 : value;
            }
        }

        public BattleEntity Target { get; set; }

        public short RunToX { get; set; }

        public short RunToY { get; set; }

        public short FirstX { get; set; }

        public short FirstY { get; set; }

        public BattleEntity Owner { get; set; }

        public object PVELockObject { get; set; }

        public bool Invisible { get; set; }

        public int AliveTime { get; set; }

        public int AliveTimeMp { get; set; }

        public bool CanSeeHiddenThings => GetBuff(CardType.SpecialActions, (byte)AdditionalTypes.SpecialActions.SeeHiddenThings)[0] < 0;

        public FactionType Faction { get; set; }

        #endregion

        #region BattleEntityProperties

        public BattleEntity BattleEntity { get; set; }

        public void AddBuff(Buff indicator, BattleEntity battleEntity, bool forced = false) => BattleEntity.AddBuff(indicator, battleEntity, forced: forced);

        public void RemoveBuff(short cardId) => BattleEntity.RemoveBuff(cardId);

        public int[] GetBuff(CardType type, byte subtype) => BattleEntity.GetBuff(type, subtype);

        public bool HasBuff(CardType type, byte subtype) => BattleEntity.HasBuff(type, subtype);

        public void DisableBuffs(BuffType type, int level = 100) => BattleEntity.DisableBuffs(type, level);

        public void DisableBuffs(List<BuffType> types, int level = 100) => BattleEntity.DisableBuffs(types, level);

        public MapCell GetPos() => BattleEntity.GetPos();

        public void DecreaseMp(int amount) => BattleEntity.DecreaseMp(amount);

        #endregion

        #region Methods

        public int HpPercent() => BattleEntity.HpPercent();

        public int MpPercent() => BattleEntity.MpPercent();

        public string GenerateBoss() => $"rboss 3 {MapMonsterId} {CurrentHp} {MaxHp}";

        public string GenerateIn()
        {
            if (IsAlive && !IsDisabled && !IsJumping)
            {
                return StaticPacketHelper.In(UserType.Monster, Monster.OriginalNpcMonsterVNum > 0 ? Monster.OriginalNpcMonsterVNum : MonsterVNum, MapMonsterId, MapX, MapY, Position,
                    (int) (CurrentHp / MaxHp * 100), (int) (CurrentMp / MaxMp * 100), 0,
                    NoAggresiveIcon ? InRespawnType.NoEffect : InRespawnType.TeleportationEffect, false, string.IsNullOrEmpty(Name) ? "-" : Name, Invisible);
            }

            return "";
        }

        public void Initialize(MapInstance currentMapInstance)
        {
            MapInstance = currentMapInstance;
            Initialize();
        }

        public void Initialize()
        {
            if (MapInstance.MapInstanceType == MapInstanceType.BaseMapInstance && ServerManager.Instance.MapBossVNums.Contains(MonsterVNum))
            {
                MapCell randomCell = MapInstance.Map.GetRandomPosition();
                if (randomCell != null)
                {
                    if (MapInstance.Portals.Any(s => Map.GetDistance(new MapCell { X = s.SourceX, Y = s.SourceY }, new MapCell { X = randomCell.X, Y = randomCell.Y }) < 5))
                    {
                        randomCell = MapInstance.Map.GetRandomPosition();
                    }
                    MapX = randomCell.X;
                    MapY = randomCell.Y;
                }
            }
            FirstX = MapX;
            FirstY = MapY;
            LastSkill = LastMove = LastEffect = new DateTime();
            Path = new List<Node>();
            IsAlive = true;
            ShouldRespawn = ShouldRespawn ?? true;
            Monster = ServerManager.GetNpcMonster(MonsterVNum);
            Faction = MonsterHelper.GetFaction(MonsterVNum);

            if (BaseMaxHp <= 0)
            {
                BaseMaxHp = Monster.MaxHP > 0 ? Monster.MaxHP : -Monster.MaxHP;
            }
            if (BaseMaxMp <= 0)
            {
                BaseMaxMp = Monster.MaxMP;
            }
            MaxHp = BaseMaxHp;
            MaxMp = BaseMaxMp;
            
            if(MapInstance?.MapInstanceType != MapInstanceType.RaidInstance)
            {
                int DismisHp = (int)MaxHp * 5 / 100;
                if (MonsterVNum == 439)
                {
                    MaxHp = MaxHp - DismisHp;
                }
            }

            if (MapInstance?.MapInstanceType == MapInstanceType.RaidInstance)
            {
                if (IsBoss)
                {
                    MaxHp *= 7;
                    MaxMp *= 7;
                }
                else
                {
                    MaxHp *= 5;
                    MaxMp *= 5;

                    if (IsTarget)
                    {
                        MaxHp *= 6;
                        MaxMp *= 6;
                    }
                }
                // Huge Snowman Head
                if (MonsterVNum == 533)
                {
                    if (ServerManager.Instance.GetSessionByCharacterId(MapInstance.InstanceBag.CreatorId)?.Character.Group is Group raidGroup)
                    {
                        int entitiesCount = raidGroup.Sessions.Count;
                        BaseMaxHp = 63 * 300 * entitiesCount;
                        MaxHp = BaseMaxHp;
                    }
                }
            }

            // Irrelevant for now(Act4)
            //if (MapInstance?.MapInstanceType == MapInstanceType.Act4Morcos || MapInstance?.MapInstanceType == MapInstanceType.Act4Hatus || MapInstance?.MapInstanceType == MapInstanceType.Act4Calvina || MapInstance?.MapInstanceType == MapInstanceType.Act4Berios)
            //{
            //    if (MonsterVNum == 563 || MonsterVNum == 577 || MonsterVNum == 629 || MonsterVNum == 624)
            //    {
            //        MaxHp *= 5;
            //        MaxMp *= 5;
            //    }
            //}

            NoAggresiveIcon = Monster.NoAggresiveIcon;

            IsHostile = Monster.IsHostile;

            CurrentHp = MaxHp;
            CurrentMp = MaxMp;

            Skills = new List<NpcMonsterSkill>();

            foreach (NpcMonsterSkill ski in Monster.Skills)
            {
                Skills.Add(new NpcMonsterSkill { SkillVNum = ski.SkillVNum, Rate = ski.Rate });
            }

            DamageList = new Dictionary<BattleEntity, long>();
            AggroList = new List<BattleEntity>();
            _movetime = ServerManager.RandomNumber(400, 3200);
            
            BattleEntity = new BattleEntity(this);
            
            // Test damage on arena spawned by command mobs
            if (Owner == null && MapInstance.Map.MapId == 2006)
            {
                MaxHp = 500000;
                CurrentHp = MaxHp;
                BattleEntity.BCards.AddRange(new Buff(196, 99).Card.BCards);
            }

            Monster.BCards.Where(s => s.Type !=  25).ToList().ForEach(s => s.ApplyBCards(BattleEntity, BattleEntity));
            
            if (MonsterVNum == 1382)
            {
                AliveTime = 20;
            }
            if (MonsterVNum == 390) // Bird's Egg
            {
                AliveTime = 0;
                AliveTimeMp = 0;
                Thread TransformCountDownThread = new Thread(() => TransformCountDown(40));
                TransformCountDownThread.Start();
            }
            if (AliveTime > 0)
            {
                Thread AliveTimeThread = new Thread(() => AliveTimeCheck());
                AliveTimeThread.Start();
            }
            if (AliveTimeMp > 0)
            {
                Thread AliveTimeMpThread = new Thread(() => AliveTimeMpCheck());
                AliveTimeMpThread.Start();
            }
            if (BattleEntity.HasBuff(CardType.Count, (byte)AdditionalTypes.Count.Summon))
            {
                Thread DisminMpPerSecThread = new Thread(() => DisminPercentMpPerSec(1));
                if (MonsterVNum == 2013 || MonsterVNum == 2016)
                {
                    DisminMpPerSecThread = new Thread(() => DisminPercentMpPerSec(8));
                }
                DisminMpPerSecThread.Start();
            }

            if (MonsterVNum == 621)
            {
                OnDeathEvents.Add(new EventContainer(MapInstance, EventActionType.SPAWNNPC, new NpcToSummon(1408, new MapCell { X = MapX, Y = MapY }, -1, move: true)));
            }
            if (MonsterVNum == 622)
            {
                OnDeathEvents.Add(new EventContainer(MapInstance, EventActionType.SPAWNNPC, new NpcToSummon(1409, new MapCell { X = MapX, Y = MapY }, -1, move: true)));
            }
            if (MonsterVNum == 623)
            {
                OnDeathEvents.Add(new EventContainer(MapInstance, EventActionType.SPAWNNPC, new NpcToSummon(1410, new MapCell { X = MapX, Y = MapY }, -1, move: true)));
            }
            
            if (OnSpawnEvents.Any())
            {
                OnSpawnEvents.ToList().ForEach(e => { EventHelper.Instance.RunEvent(e, monster: this); });
                OnSpawnEvents.Clear();
            }
        }

        private void AliveTimeCheck()
        {
            double PercentPerSecond = 100 / (double)AliveTime;
            for (int i = 0; i < AliveTime; i++)
            {
                if (!IsAlive || CurrentHp <= 0)
                {
                    return;
                }
                CurrentHp -= MaxHp * PercentPerSecond / 100;
                if (CurrentHp <= 0)
                {
                    break;
                }
                Thread.Sleep(1000);
            }
            RunDeathEvent();
            MapInstance.Broadcast(StaticPacketHelper.Out(UserType.Monster, MapMonsterId));
            MapInstance.RemoveMonster(this);
        }

        private void AliveTimeMpCheck()
        {
            double PercentPerSecond = 100 / (double)AliveTimeMp;
            for (int i = 0; i < AliveTimeMp; i++)
            {
                if (!IsAlive || CurrentHp <= 0)
                {
                    return;
                }
                CurrentMp -= MaxMp * PercentPerSecond / 100;
                if (CurrentMp <= 0)
                {
                    break;
                }
                Thread.Sleep(1000);
            }
            RunDeathEvent();
            MapInstance.Broadcast(StaticPacketHelper.Out(UserType.Monster, MapMonsterId));
            MapInstance.RemoveMonster(this);
        }

        private void DisminPercentMpPerSec(double PercentPerSecond)
        {
            for (int i = 0; i < 100 / PercentPerSecond + 1; i++)
            {
                if (!IsAlive || CurrentHp <= 0)
                {
                    return;
                }
                CurrentMp -= MaxMp * PercentPerSecond / 100;
                if (CurrentMp <= 0)
                {
                    if (BattleEntity.GetBuff(CardType.Count, (byte)AdditionalTypes.Count.Summon) is int[] CountSummon)
                    {
                        MapInstance.SummonMonster(new MonsterToSummon((short)CountSummon[1], new MapCell() { X = MapX, Y = MapY }, null, true));
                    }
                    break;
                }
                Thread.Sleep(1000);
            }
            RunDeathEvent();
            MapInstance.Broadcast(StaticPacketHelper.Out(UserType.Monster, MapMonsterId));
            MapInstance.RemoveMonster(this);
        }

        private void TransformCountDown(double Seconds)
        {
            for (int i = 0; i < Seconds; i++)
            {
                if (!IsAlive || CurrentHp <= 0)
                {
                    return;
                }
                Thread.Sleep(1000);
            }
            if (MonsterVNum == 390) // Bird's Egg
            {
                MapInstance.SummonMonster(new MonsterToSummon(383, new MapCell() { X = MapX, Y = MapY }, null, true));
            }
            MapInstance.Broadcast(StaticPacketHelper.Out(UserType.Monster, MapMonsterId));
            MapInstance.RemoveMonster(this);
        }

        public void AddToDamageList(BattleEntity damagerEntity, long damage)
        {
            lock (DamageList)
            {
                if (DamageList.FirstOrDefault(s => s.Key.MapEntityId == damagerEntity.MapEntityId && s.Key.EntityType == damagerEntity.EntityType).Key is BattleEntity existingEntity)
                {
                    if (damagerEntity == existingEntity)
                    {
                        DamageList[existingEntity] += damage;
                    }
                    else
                    {
                        damage += DamageList[existingEntity];
                        DamageList.Remove(existingEntity);
                        DamageList.Add(damagerEntity, damage);
                    }
                }
                else
                {
                    DamageList.Add(damagerEntity, damage);
                }
            }

            AddToAggroList(damagerEntity);
        }

        public void AddToAggroList(BattleEntity aggroEntity)
        {
            lock (AggroList)
            {
                if (AggroList.ToList().FirstOrDefault(s => s.MapEntityId == aggroEntity.MapEntityId && s.EntityType == aggroEntity.EntityType) is BattleEntity existingEntity)
                {
                    if (existingEntity != aggroEntity)
                    {
                        AggroList.Remove(existingEntity);
                        AggroList.Add(aggroEntity);
                    }
                }
                else
                {
                    AggroList.Add(aggroEntity);
                }
            }
        }

        public void RemoveFromAggroList(BattleEntity aggroEntity)
        {
            lock (AggroList)
            {
                AggroList.RemoveAll(s => s.MapEntityId == aggroEntity.MapEntityId && s.EntityType == aggroEntity.EntityType);
            }
        }

        /// <summary>
        /// Check if the Monster is in the given Range.
        /// </summary>
        /// <param name="mapX">The X coordinate on the Map of the object to check.</param>
        /// <param name="mapY">The Y coordinate on the Map of the object to check.</param>
        /// <param name="distance">The maximum distance of the object to check.</param>
        /// <returns>True if the Monster is in range, False if not.</returns>
        public bool IsInRange(short mapX, short mapY, byte distance)
        {
            return Map.GetDistance(new MapCell
            {
                X = mapX,
                Y = mapY
            }, new MapCell
            {
                X = MapX,
                Y = MapY
            }) <= distance;
        }

        public void RunDeathEvent()
        {
            Buff.ClearAll();
            if (IsBonus)
            {
                MapInstance.InstanceBag.Combo++;
                MapInstance.InstanceBag.Point += EventHelper.CalculateComboPoint(MapInstance.InstanceBag.Combo + 1);
            }
            else
            {
                MapInstance.InstanceBag.Combo = 0;
                MapInstance.InstanceBag.Point += EventHelper.CalculateComboPoint(MapInstance.InstanceBag.Combo);
            }

            MapInstance.InstanceBag.MonstersKilled++;

            if (Owner != null && Monster.BCards.FirstOrDefault(s => s.CastType == 0 && s.Type == (byte)CardType.SummonAndRecoverHP && s.SubType == (byte)AdditionalTypes.SummonAndRecoverHP.RestoreHP / 10) is BCard RestoreHP)
            {
                double recoverHp = Owner.HpMax * RestoreHP.FirstData / 100;
                if (Owner.Hp + recoverHp > Owner.HpMax)
                {
                    recoverHp = Owner.HpMax - Owner.Hp;
                }
                Owner.Hp += (int)recoverHp;
                MapInstance.Broadcast(Owner.GenerateRc((int)recoverHp));
            }

            if (OnDeathEvents.Any(s => s.EventActionType == EventActionType.SPAWNMONSTERS) 
            && (List<MonsterToSummon>)OnDeathEvents.FirstOrDefault(e => e.EventActionType == EventActionType.SPAWNMONSTERS).Parameter is List<MonsterToSummon> summonParameters)
            {
                Parallel.ForEach(summonParameters, npcMonster =>
                {
                    npcMonster.SpawnCell.X = MapX;
                    npcMonster.SpawnCell.Y = MapY;
                });
            }
            if (OnDeathEvents.Any(s => s.EventActionType == EventActionType.SPAWNNPC)
            && (NpcToSummon)OnDeathEvents.FirstOrDefault(e => e.EventActionType == EventActionType.SPAWNNPC).Parameter is NpcToSummon npcMonsterToSummon)
            {
                npcMonsterToSummon.SpawnCell.X = MapX;
                npcMonsterToSummon.SpawnCell.Y = MapY;
            }

            OnDeathEvents.ForEach(e => EventHelper.Instance.RunEvent(e, monster: this));
            
            BattleEntity.ClearEnemyFalcon();
        }

        public bool SetDeathStatement()
        {
            if (Monster.BCards.Any(s => s.Type == (byte)CardType.NoDefeatAndNoDamage && s.SubType == (byte)AdditionalTypes.NoDefeatAndNoDamage.DecreaseHPNoDeath / 10 && s.FirstData == -1))
            {
                CurrentHp = MaxHp;
                return false;
            }
            IsAlive = false;
            CurrentHp = 0;
            CurrentMp = 0;
            Death = DateTime.Now;
            LastMove = DateTime.Now;
            DisableBuffs(BuffType.All);
            return true;
        }

        public void StartLife()
        {
            try
            {
                if (!MapInstance.IsSleeping)
                {
                    MonsterLife();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        internal void GetNearestOponent()
        {
            lock (AggroList)
            {
                List<BattleEntity> entitiesList = AggroList.ToList();

                if (entitiesList.Any())
                {
                    entitiesList.AddRange(entitiesList.Where(s => s.Mate != null).Select(s => s.Mate.Owner.BattleEntity).ToList());
                    entitiesList.AddRange(entitiesList.Where(s => s.Character != null).SelectMany(s => s.Character.Mates.Where(m => (m.IsTeamMember || m.IsTemporalMate) && !entitiesList.Contains(m.BattleEntity)).Select(m2 => m2.BattleEntity)).ToList());
                    entitiesList.AddRange(entitiesList.Where(s => s.Character != null).SelectMany(s => s.Character.BattleEntity.GetOwnedMonsters().Select(m => m.BattleEntity)).ToList());
                    entitiesList.AddRange(entitiesList.Where(s => s.MapNpc != null).ToList());

                    BattleEntity newTarget = entitiesList.Where(s => (s.Character == null || !s.Character.InvisibleGm && (!s.Character.Invisible || CanSeeHiddenThings)))
                        .OrderBy(e => Map.GetDistance(GetPos(), e.GetPos())).FirstOrDefault(e => BattleEntity.CanAttackEntity(e));

                    if (newTarget == null && Target != null)
                    {
                        RemoveTarget();
                    }
                    else
                    {
                        Target = newTarget;
                    }
                }
            }
        }

        internal void HostilityTarget()
        {
            if (!IsHostile || Target != null)
            {
                return;
            }

            BattleEntity target = MapInstance.BattleEntities.OrderBy(e => Map.GetDistance(GetPos(), e.GetPos()))
                .FirstOrDefault(e => BattleEntity.CanAttackEntity(e) && (e.CanBeTargetted) && (e.Mate == null || !BattleEntity.IsMateTrainer(MonsterVNum) || e.MapInstance != e.Mate?.Owner.Miniland || e.TargettedByMonstersList(false).Count() < 9) && (e.Character == null
                    || !e.Character.InvisibleGm && (!e.Character.Invisible || CanSeeHiddenThings)) && Map.GetDistance(GetPos(), e.GetPos()) < (NoticeRange == 0 ? Monster.NoticeRange : NoticeRange));

            if (target == null/* || MoveEvent != null*/)
            {
                return;
            }

            if (OnNoticeEvents.Any())
            {
                OnNoticeEvents.ToList().ForEach(e => { EventHelper.Instance.RunEvent(e, monster: this); });
                OnNoticeEvents.Clear();
                return;
            }

            LastMonsterAggro = DateTime.Now;

            Target = target;
            AddToAggroList(target);
            if (!Monster.NoAggresiveIcon && target.Character != null)
            {
                Target.Character.Session.SendPacket(StaticPacketHelper.GenerateEff(UserType.Monster, MapMonsterId, 5000));
            }
        }

        /// <summary>
        /// Remove the current Target from Monster.
        /// </summary>
        internal void RemoveTarget()
        {
            if (Target != null)
            {
                RemoveFromAggroList(Target);
                Target = null;

                //(Path ?? (Path = new List<Node>())).Clear();
                //return to origin
                //Path = BestFirstSearch.FindPathJagged(new Node {X = MapX, Y = MapY}, new Node {X = FirstX, Y = FirstY}, MapInstance.Map.JaggedGrid);
            }
        }

        private void RunAway()
        {
            if (Target == null || Monster == null || CurrentHp < 1 || (!IsMoving || Monster.Speed < 1))
            {
                return;
            }

            double time = (DateTime.Now - LastMove).TotalMilliseconds;

            int timeToWalk = 2000 / Monster.Speed;

            if (time > timeToWalk)
            {
                short tempX = MapX;
                short tempY = MapY;

                short cells = (short)ServerManager.RandomNumber(1, 3);

                if (SkillHelper.CalculateNewPosition(MapInstance, Target.PositionX, Target.PositionY, cells, ref tempX, ref tempY))
                {
                    MapX = tempX;
                    MapY = tempY;

                    Observable.Timer(TimeSpan.FromMilliseconds(timeToWalk))
                        .Subscribe(x =>
                        {
                            MapX = tempX;
                            MapY = tempY;

                            MoveEvent?.Events.ForEach(e => EventHelper.Instance.RunEvent(e, monster: this));
                        });

                    MapInstance.Broadcast(StaticPacketHelper.Move(UserType.Monster, MapMonsterId, tempX, tempY, Monster.Speed));
                    MapInstance.Broadcast(StaticPacketHelper.Say(3, MapMonsterId, 0, "!!!!"));
                }
            }
        }

        /// <summary>
        /// Follow the Monsters target to it's position.
        /// </summary>
        /// <param name="target">The TargetSession to follow</param>
        private void FollowTarget(BattleEntity target)
        {
            if (IsMoving && !HasBuff(CardType.Move, (byte)AdditionalTypes.Move.MovementImpossible))
            {
                if (Map.GetDistance(new MapCell
                {
                    X = MapX,
                    Y = MapY
                },
                new MapCell
                {
                    X = target.PositionX,
                    Y = target.PositionY
                }) <= Monster.BasicRange && Monster.BasicRange > 0)
                {
                    return;
                }

                /*if (target.LastMonsterAggro.AddSeconds(5) < DateTime.Now || target.BrushFireJagged == null)
                {
                    target.UpdateBushFire();
                }

                Path.Clear();

                target.LastMonsterAggro = DateTime.Now;
                if (Path.Count == 0)
                {
                    short xoffset = (short)ServerManager.RandomNumber(-1, 1);
                    short yoffset = (short)ServerManager.RandomNumber(-1, 1);
                    try
                    {
                        Path = BestFirstSearch.TracePathJagged(new Node { X = MapX, Y = MapY },
                            target.BrushFireJagged,
                            target.MapInstance.Map.JaggedGrid);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(
                            $"Pathfinding using Pathfinder failed. Map: {MapId} StartX: {MapX} StartY: {MapY} TargetX: {(short)(target.PositionX + xoffset)} TargetY: {(short)(target.PositionY + yoffset)}",
                            ex);
                        RemoveTarget();
                    }
                }*/

                Move();
            }
        }

        public void OnReceiveHit(HitRequest hitRequest)
        {
            if (IsAlive && hitRequest.Session.Character.Hp > 0 &&
                    (hitRequest.Mate == null || hitRequest.Mate.Hp > 0))
            {
                double cooldownReduction = hitRequest.Session.Character.GetBuff(CardType.Morale, (byte)AdditionalTypes.Morale.SkillCooldownDecreased)[0];

                int[] increaseEnemyCooldownChance = hitRequest.Session.Character.GetBuff(CardType.DarkCloneSummon, (byte)AdditionalTypes.DarkCloneSummon.IncreaseEnemyCooldownChance);

                if (ServerManager.RandomNumber() < increaseEnemyCooldownChance[0])
                {
                    cooldownReduction -= increaseEnemyCooldownChance[1];
                }
                
                int hitmode = 0;
                bool isCaptureSkill = hitRequest.SkillBCards.Any(s => s.Type.Equals((byte)CardType.Capture));

                // calculate damage
                bool onyxWings = false;
                BattleEntity attackerBattleEntity = hitRequest.Mate == null
                    ? new BattleEntity(hitRequest.Session.Character, hitRequest.Skill)
                    : new BattleEntity(hitRequest.Mate);
                int damage = DamageHelper.Instance.CalculateDamage(attackerBattleEntity, new BattleEntity(this),
                    hitRequest.Skill, ref hitmode, ref onyxWings);

                if (Monster.BCards.Find(s =>
                    s.Type == (byte)CardType.LightAndShadow &&
                    s.SubType == (byte)AdditionalTypes.LightAndShadow.InflictDamageToMP) is BCard card)
                {
                    int reduce = damage / 100 * card.FirstData;
                    if (CurrentMp < reduce)
                    {
                        reduce = (int)CurrentMp;
                        CurrentMp = 0;
                    }
                    else
                    {
                        DecreaseMp(reduce);
                    }
                    damage -= reduce;
                }

                if (damage >= CurrentHp &&
                    Monster.BCards.Any(s => s.Type == (byte)CardType.NoDefeatAndNoDamage && s.SubType == (byte)AdditionalTypes.NoDefeatAndNoDamage.DecreaseHPNoDeath / 10 && s.FirstData == -1))
                {
                    damage = (int)CurrentHp - 1;
                }
                else if (onyxWings)
                {
                    short onyxX = (short)(hitRequest.Session.Character.PositionX + 2);
                    short onyxY = (short)(hitRequest.Session.Character.PositionY + 2);
                    int onyxId = MapInstance.GetNextMonsterId();
                    MapMonster onyx = new MapMonster
                    {
                        MonsterVNum = 2371,
                        MapX = onyxX,
                        MapY = onyxY,
                        MapMonsterId = onyxId,
                        IsHostile = false,
                        IsMoving = false,
                        ShouldRespawn = false
                    };
                    MapInstance.Broadcast(UserInterfaceHelper.GenerateGuri(31, 1,
                        hitRequest.Session.Character.CharacterId, onyxX, onyxY));
                    onyx.Initialize(MapInstance);
                    MapInstance.AddMonster(onyx);
                    MapInstance.Broadcast(onyx.GenerateIn());
                    BattleEntity.GetDamage(damage / 2, attackerBattleEntity);
                    var request = hitRequest;
                    var damage1 = damage;
                    Observable.Timer(TimeSpan.FromMilliseconds(350)).Subscribe(o =>
                    {
                        MapInstance.Broadcast(StaticPacketHelper.SkillUsed(UserType.Monster, onyxId, 3,
                            MapMonsterId, -1, 0, -1, request.Skill?.Effect ?? 0, -1, -1, IsAlive, (int)(CurrentHp / MaxHp * 100), damage1 / 2, 0,
                            0));
                        MapInstance.RemoveMonster(onyx);
                        MapInstance.Broadcast(StaticPacketHelper.Out(UserType.Monster, onyx.MapMonsterId));
                    });
                }

                bool firstHit = false;

                lock (DamageList)
                {
                    if (!DamageList.Any(s => s.Value > 0))
                    {
                        firstHit = true;
                    }
                    if (attackerBattleEntity.MapMonster?.Owner != null)
                    {
                        AddToDamageList(attackerBattleEntity.MapMonster.Owner, damage);
                    }
                    else if (attackerBattleEntity.Mate?.Owner != null)
                    {
                        AddToDamageList(attackerBattleEntity.Mate.Owner.BattleEntity, damage);
                    }
                    else if (attackerBattleEntity.Character != null)
                    {
                        if(damage == 0 && attackerBattleEntity.Character.LastSkillComboUse < DateTime.Now)
                        {
                            attackerBattleEntity.Character.SkillComboCount = 0;
                            attackerBattleEntity.Character.LastSkillComboUse = DateTime.Now.AddSeconds(3);
                        }


                        AddToDamageList(attackerBattleEntity, damage);

                        #region C45 equipe buffs

                        ItemInstance itemInUse = null;

                        //Main weapons
                        itemInUse = attackerBattleEntity.Character.Inventory.LoadBySlotAndType(0, InventoryType.Wear);

                        //if it activates from another method
                        if (attackerBattleEntity.Character.Buff.ContainsKey(413) || attackerBattleEntity.Character.Buff.ContainsKey(414))
                        {
                            attackerBattleEntity.Character.Session.CurrentMapInstance?.Broadcast(attackerBattleEntity.Character.GenerateRc((int)(damage * 8 / 100D)));
                            attackerBattleEntity.Character.Hp += (int)(damage * 8 / 100D);
                            attackerBattleEntity.Character.Session.SendPacket(attackerBattleEntity.Character.GenerateStat());
                            if (attackerBattleEntity.Character.Buff.ContainsKey(413))
                                attackerBattleEntity.Character.RemoveBuff(413);
                            else
                                attackerBattleEntity.Character.RemoveBuff(414);
                        }
                        else if (attackerBattleEntity.Character.Buff.ContainsKey(416))
                        {
                            attackerBattleEntity.Character.Session.CurrentMapInstance?.Broadcast(attackerBattleEntity.Character.GenerateRc((int)(damage * 15 / 100D)));
                            attackerBattleEntity.Character.Hp += (int)(damage * 15 / 100D);
                            attackerBattleEntity.Character.Mp += (int)(damage * 15 / 100D);
                            attackerBattleEntity.Character.Session.SendPacket(attackerBattleEntity.Character.GenerateStat());
                            attackerBattleEntity.Character.RemoveBuff(416);
                        }


                        //Sword
                        if (itemInUse != null && itemInUse.Item.VNum == 4981 && ServerManager.RandomNumber() <= 3)
                        {
                            attackerBattleEntity.Character.ConvertedDamageToHP = (int)(damage * 8 / 100D);
                            attackerBattleEntity.Character.AddBuff(new Buff(413, attackerBattleEntity.Character.Level), attackerBattleEntity.Character.BattleEntity);
                            attackerBattleEntity.Character.RemoveBuff(413);
                        }

                        //Staff
                        if (itemInUse != null && itemInUse.Item.VNum == 4982 && ServerManager.RandomNumber() <= 4)
                        {
                            attackerBattleEntity.Character.ConvertedDamageToHP = (int)(damage * 15 / 100D);
                            attackerBattleEntity.Character.AddBuff(new Buff(416, attackerBattleEntity.Character.Level), attackerBattleEntity.Character.BattleEntity);
                            attackerBattleEntity.Character.RemoveBuff(416);
                        }

                        //Bow
                        
                        if (itemInUse != null && itemInUse.Item.VNum == 4983 && ServerManager.RandomNumber() <= 4)
                        {
                            AddBuff(new Buff(415, attackerBattleEntity.Character.Level), attackerBattleEntity.Character.BattleEntity);
                        }

                        if (Buff.ContainsKey(415) && ServerManager.RandomNumber() <= 50)
                            foreach (Buff b in Buff.Where(b => b.Card.BuffType == BuffType.Good && b.Card.Level < 4))
                                RemoveBuff(b.Card.CardId);

                        //Punch
                        if (itemInUse != null && itemInUse.Item.VNum == 4736 && ServerManager.RandomNumber() <= 7)
                        {
                            AddBuff(new Buff(672, attackerBattleEntity.Character.Level), attackerBattleEntity.Character.BattleEntity);
                        }

                        //Secondary weapons
                        itemInUse = attackerBattleEntity.Character.Inventory.LoadBySlotAndType(5, InventoryType.Wear);

                        //Crossbow
                        if (itemInUse != null && itemInUse.Item.VNum == 4978 && ServerManager.RandomNumber() <= 5)
                        {
                            this.AddBuff(new Buff(417, attackerBattleEntity.Character.Level), attackerBattleEntity.Character.BattleEntity);
                        }

                        //Gun
                        if(itemInUse != null && itemInUse.Item.VNum == 4979 && ServerManager.RandomNumber() <= 5)
                        {
                            this.AddBuff(new Buff(418, attackerBattleEntity.Character.Level), attackerBattleEntity.Character.BattleEntity);
                        }

                        //Dagger
                        if (itemInUse != null && itemInUse.Item.VNum == 4980 && ServerManager.RandomNumber() <= 3)
                        {
                            attackerBattleEntity.Character.ConvertedDamageToHP = (int)(damage * 8 / 100D);
                            attackerBattleEntity.Character.AddBuff(new Buff(414, attackerBattleEntity.Character.Level), attackerBattleEntity.Character.BattleEntity);
                            attackerBattleEntity.Character.RemoveBuff(414);
                        }

                        //NO c45 secondary weap for MA 

                        //Armors
                        itemInUse = attackerBattleEntity.Character.Inventory.LoadBySlotAndType(1, InventoryType.Wear);

                        //Swordsman
                        if (itemInUse != null && itemInUse.Item.VNum == 4984 && ServerManager.RandomNumber() <= 2)
                        {
                            attackerBattleEntity.Character.AddBuff(new Buff(419, attackerBattleEntity.Character.Level), attackerBattleEntity.Character.BattleEntity);
                        }

                        //Mage
                        if (itemInUse != null && itemInUse.Item.VNum == 4985 && ServerManager.RandomNumber() <= 2)
                        {
                            attackerBattleEntity.Character.AddBuff(new Buff(421, attackerBattleEntity.Character.Level), attackerBattleEntity.Character.BattleEntity);
                        }

                        //Archer
                        if (itemInUse != null && itemInUse.Item.VNum == 4986 && ServerManager.RandomNumber() <= 2)
                        {
                            attackerBattleEntity.Character.AddBuff(new Buff(420, attackerBattleEntity.Character.Level), attackerBattleEntity.Character.BattleEntity);
                        }

                        //Martial Artist
                        if (itemInUse != null && itemInUse.Item.VNum == 4754 && ServerManager.RandomNumber() <= 2)
                        {
                            attackerBattleEntity.Character.AddBuff(new Buff(673, attackerBattleEntity.Character.Level), attackerBattleEntity.Character.BattleEntity);
                        }

                        #endregion
                    }
                }

                attackerBattleEntity.BCards.Where(s => s.CastType == 1).ForEach(s =>
                {
                    if (s.Type != (byte)CardType.Buff)
                    {
                        s.ApplyBCards(BattleEntity, attackerBattleEntity);
                    }
                });

                hitRequest.SkillBCards.Where(s => !s.Type.Equals((byte)CardType.Buff) && !s.Type.Equals((byte)CardType.Capture) && s.CardId == null).ToList()
                    .ForEach(s => s.ApplyBCards(BattleEntity, attackerBattleEntity));

                if (hitmode != 4 && hitmode != 2)
                {
                    if (damage > 0)
                    {
                        RemoveBuff(36);
                        RemoveBuff(548);
                    }

                    attackerBattleEntity.BCards.Where(s => s.CastType == 1).ForEach(s =>
                    {
                        if (s.Type == (byte)CardType.Buff)
                        {
                            Buff b = new Buff((short)s.SecondData, Monster.Level);
                            if (b.Card != null)
                            {
                                switch (b.Card?.BuffType)
                                {
                                    case BuffType.Bad:
                                        s.ApplyBCards(BattleEntity, attackerBattleEntity);
                                        break;

                                    case BuffType.Good:
                                    case BuffType.Neutral:
                                        s.ApplyBCards(attackerBattleEntity, attackerBattleEntity);
                                        break;
                                }
                            }
                        }
                    });

                    BattleEntity.BCards.Where(s => s.CastType == 0).ForEach(s =>
                    {
                        if (s.Type == (byte)CardType.Buff)
                        {
                            Buff b = new Buff((short)s.SecondData, BattleEntity.Level);
                            if (b.Card != null)
                            {
                                switch (b.Card?.BuffType)
                                {
                                    case BuffType.Bad:
                                        s.ApplyBCards(attackerBattleEntity, BattleEntity);
                                        break;

                                    case BuffType.Good:
                                    case BuffType.Neutral:
                                        if (s.NpcMonsterVNum == null || firstHit)
                                        {
                                            s.ApplyBCards(BattleEntity, BattleEntity);
                                        }
                                        break;
                                }
                            }
                        }
                    });

                    hitRequest.SkillBCards.Where(s => s.Type.Equals((byte)CardType.Buff) && new Buff((short)s.SecondData, hitRequest.Session.Character.Level).Card?.BuffType == BuffType.Bad).ToList()
                        .ForEach(s => s.ApplyBCards(BattleEntity, attackerBattleEntity));

                    hitRequest.SkillBCards.Where(s => s.Type.Equals((byte)CardType.Capture)).ToList()
                        .ForEach(s => s.ApplyBCards(BattleEntity, attackerBattleEntity));

                    hitRequest.SkillBCards.Where(s => s.Type.Equals((byte)CardType.SniperAttack)).ToList()
                        .ForEach(s => s.ApplyBCards(BattleEntity, attackerBattleEntity));

                    if (attackerBattleEntity?.ShellWeaponEffects != null)
                    {
                        foreach (ShellEffectDTO shell in attackerBattleEntity.ShellWeaponEffects)
                        {
                            switch (shell.Effect)
                            {
                                case (byte)ShellWeaponEffectType.Blackout:
                                    {
                                        Buff buff = new Buff(7, attackerBattleEntity.Level);
                                        if (ServerManager.RandomNumber() < shell.Value)
                                        {
                                            AddBuff(buff, attackerBattleEntity);
                                        }

                                        break;
                                    }
                                case (byte)ShellWeaponEffectType.DeadlyBlackout:
                                    {
                                        Buff buff = new Buff(66, attackerBattleEntity.Level);
                                        if (ServerManager.RandomNumber() < shell.Value)
                                        {
                                            AddBuff(buff, attackerBattleEntity);
                                        }

                                        break;
                                    }
                                case (byte)ShellWeaponEffectType.MinorBleeding:
                                    {
                                        Buff buff = new Buff(1, attackerBattleEntity.Level);
                                        if (ServerManager.RandomNumber() < shell.Value)
                                        {
                                            AddBuff(buff, attackerBattleEntity);
                                        }

                                        break;
                                    }
                                case (byte)ShellWeaponEffectType.Bleeding:
                                    {
                                        Buff buff = new Buff(21, attackerBattleEntity.Level);
                                        if (ServerManager.RandomNumber() < shell.Value)
                                        {
                                            AddBuff(buff, attackerBattleEntity);
                                        }

                                        break;
                                    }
                                case (byte)ShellWeaponEffectType.HeavyBleeding:
                                    {
                                        Buff buff = new Buff(42, attackerBattleEntity.Level);
                                        if (ServerManager.RandomNumber() < shell.Value)
                                        {
                                            AddBuff(buff, attackerBattleEntity);
                                        }

                                        break;
                                    }
                                case (byte)ShellWeaponEffectType.Freeze:
                                    {
                                        Buff buff = new Buff(27, attackerBattleEntity.Level);
                                        if (ServerManager.RandomNumber() < shell.Value)
                                        {
                                            AddBuff(buff, attackerBattleEntity);
                                        }

                                        break;
                                    }
                            }
                        }
                    }

                    if (IsBoss && MapInstance == CaligorRaid.CaligorMapInstance)
                    {
                        switch (hitRequest.Session.Character.Faction)
                        {
                            case FactionType.Angel:
                                CaligorRaid.AngelDamage += damage;
                                if (onyxWings)
                                {
                                    CaligorRaid.AngelDamage += damage / 2;
                                }

                                break;

                            case FactionType.Demon:
                                CaligorRaid.DemonDamage += damage;
                                if (onyxWings)
                                {
                                    CaligorRaid.DemonDamage += damage / 2;
                                }

                                break;
                        }
                    }
                }

                if (isCaptureSkill)
                {
                    damage = 0;
                }

                if (CurrentHp <= damage)
                {
                    SetDeathStatement();
                }
                else
                {
                    BattleEntity.GetDamage(damage, attackerBattleEntity);
                }

                // only set the hit delay if we become the monsters target with this hit
                if (Target == null)
                {
                    LastSkill = DateTime.Now;
                }
                if (hitmode != 2)
                {
                    if (hitRequest.Skill != null)
                    {
                        switch (hitRequest.TargetHitType)
                        {
                            case TargetHitType.SingleTargetHit:
                                //if (!isCaptureSkill)
                                {
                                    BattleEntity.MapInstance?.Broadcast(StaticPacketHelper.SkillUsed(attackerBattleEntity.UserType,
                                        attackerBattleEntity.MapEntityId, (byte)BattleEntity.UserType, BattleEntity.MapEntityId,
                                        hitRequest.Skill.SkillVNum, (short)(hitRequest.Skill.Cooldown + hitRequest.Skill.Cooldown * cooldownReduction / 100D),
                                        hitRequest.Skill.AttackAnimation, hitRequest.SkillEffect,
                                        attackerBattleEntity.PositionX, attackerBattleEntity.PositionY,
                                        IsAlive, (int)(CurrentHp / MaxHp * 100), damage, hitmode,
                                        (byte)(hitRequest.Skill.SkillType - 1)));
                                }

                                break;

                            case TargetHitType.SingleTargetHitCombo:
                                BattleEntity.MapInstance?.Broadcast(StaticPacketHelper.SkillUsed(attackerBattleEntity.UserType,
                                    attackerBattleEntity.MapEntityId, (byte)BattleEntity.UserType, BattleEntity.MapEntityId,
                                    hitRequest.Skill.SkillVNum, (short)(hitRequest.Skill.Cooldown + hitRequest.Skill.Cooldown * cooldownReduction / 100D),
                                    hitRequest.SkillCombo.Animation, hitRequest.SkillCombo.Effect,
                                    attackerBattleEntity.PositionX, attackerBattleEntity.PositionY,
                                    IsAlive, (int)(CurrentHp / MaxHp * 100), damage, hitmode,
                                    (byte)(hitRequest.Skill.SkillType - 1)));
                                break;

                            case TargetHitType.SingleAOETargetHit:
                                if (hitRequest.ShowTargetHitAnimation)
                                {
                                    if (hitRequest.Session.Character != null)
                                    {
                                        if (hitRequest.Skill.SkillVNum == 1085 || hitRequest.Skill.SkillVNum == 1091 || hitRequest.Skill.SkillVNum == 1060)
                                        {
                                            attackerBattleEntity.PositionX = MapX;
                                            attackerBattleEntity.PositionY = MapY;
                                            attackerBattleEntity.MapInstance.Broadcast(hitRequest.Session.Character.GenerateTp());
                                        }
                                    }
                                    BattleEntity.MapInstance?.Broadcast(StaticPacketHelper.SkillUsed(attackerBattleEntity.UserType,
                                        attackerBattleEntity.MapEntityId, (byte)BattleEntity.UserType, BattleEntity.MapEntityId,
                                        hitRequest.Skill.SkillVNum, (short)(hitRequest.Skill.Cooldown + hitRequest.Skill.Cooldown * cooldownReduction / 100D),
                                        hitRequest.Skill.AttackAnimation, hitRequest.SkillEffect,
                                        attackerBattleEntity.PositionX, attackerBattleEntity.PositionY,
                                        IsAlive, (int)(CurrentHp / MaxHp * 100), damage, hitmode,
                                        (byte)(hitRequest.Skill.SkillType - 1)));
                                }
                                else
                                {
                                    switch (hitmode)
                                    {
                                        case 1:
                                        case 4:
                                            hitmode = 7;
                                            break;

                                        case 2:
                                            hitmode = 2;
                                            break;

                                        case 3:
                                            hitmode = 6;
                                            break;

                                        default:
                                            hitmode = 5;
                                            break;
                                    }

                                    BattleEntity.MapInstance?.Broadcast(StaticPacketHelper.SkillUsed(attackerBattleEntity.UserType,
                                        attackerBattleEntity.MapEntityId, (byte)BattleEntity.UserType, BattleEntity.MapEntityId,
                                        -1, (short)(hitRequest.Skill.Cooldown + hitRequest.Skill.Cooldown * cooldownReduction / 100D),
                                        hitRequest.Skill.AttackAnimation, hitRequest.SkillEffect,
                                        attackerBattleEntity.PositionX, attackerBattleEntity.PositionY,
                                        IsAlive, (int)(CurrentHp / MaxHp * 100), damage, hitmode,
                                        (byte)(hitRequest.Skill.SkillType - 1)));
                                }
                                break;

                            case TargetHitType.AOETargetHit:
                                switch (hitmode)
                                {
                                    case 1:
                                    case 4:
                                        hitmode = 7;
                                        break;

                                    case 2:
                                        hitmode = 2;
                                        break;

                                    case 3:
                                        hitmode = 6;
                                        break;

                                    default:
                                        hitmode = 5;
                                        break;
                                }

                                BattleEntity.MapInstance?.Broadcast(StaticPacketHelper.SkillUsed(attackerBattleEntity.UserType,
                                    attackerBattleEntity.MapEntityId, (byte)BattleEntity.UserType, BattleEntity.MapEntityId,
                                    hitRequest.Skill.SkillVNum, (short)(hitRequest.Skill.Cooldown + hitRequest.Skill.Cooldown * cooldownReduction / 100D),
                                    hitRequest.Skill.AttackAnimation, hitRequest.SkillEffect,
                                    attackerBattleEntity.PositionX, attackerBattleEntity.PositionY,
                                    IsAlive, (int)(CurrentHp / MaxHp * 100), damage, hitmode,
                                    (byte)(hitRequest.Skill.SkillType - 1)));
                                break;

                            case TargetHitType.ZoneHit:
                                BattleEntity.MapInstance?.Broadcast(StaticPacketHelper.SkillUsed(attackerBattleEntity.UserType,
                                    attackerBattleEntity.MapEntityId, (byte)BattleEntity.UserType, BattleEntity.MapEntityId,
                                    hitRequest.Skill.SkillVNum, (short)(hitRequest.Skill.Cooldown + hitRequest.Skill.Cooldown * cooldownReduction / 100D),
                                    hitRequest.Skill.AttackAnimation, hitRequest.SkillEffect, hitRequest.MapX,
                                    hitRequest.MapY, IsAlive, (int)(CurrentHp / MaxHp * 100), damage, hitmode,
                                    (byte)(hitRequest.Skill.SkillType - 1)));
                                break;

                            case TargetHitType.SpecialZoneHit:
                                BattleEntity.MapInstance?.Broadcast(StaticPacketHelper.SkillUsed(attackerBattleEntity.UserType,
                                    attackerBattleEntity.MapEntityId, (byte)BattleEntity.UserType, BattleEntity.MapEntityId,
                                    hitRequest.Skill.SkillVNum, (short)(hitRequest.Skill.Cooldown + hitRequest.Skill.Cooldown * cooldownReduction / 100D),
                                    hitRequest.Skill.AttackAnimation, hitRequest.SkillEffect,
                                    attackerBattleEntity.PositionX, attackerBattleEntity.PositionY,
                                    IsAlive, (int)(CurrentHp / MaxHp * 100), damage, hitmode,
                                    (byte)(hitRequest.Skill.SkillType - 1)));
                                break;
                        }
                    }
                    else
                    {
                        MapInstance?.Broadcast(StaticPacketHelper.SkillUsed(attackerBattleEntity.UserType,
                            attackerBattleEntity.MapEntityId, (byte)BattleEntity.UserType, BattleEntity.MapEntityId,
                            0, (short)(hitRequest.Mate != null ? hitRequest.Mate.Monster.BasicCooldown : 12), 11, (short)(hitRequest.Mate != null ? hitRequest.Mate.Monster.BasicSkill : 12), 0, 0, IsAlive,
                            (int)(CurrentHp / MaxHp * 100), damage, hitmode, 0));
                    }
                }
                else
                {
                    hitRequest.Session.SendPacket(StaticPacketHelper.Cancel(2, MapMonsterId));
                }
                if (attackerBattleEntity.Character != null)
                {
                    if (hitmode != 4 && hitmode != 2 && damage > 0)
                    {
                        attackerBattleEntity.Character.RemoveBuffByBCardTypeSubType(new List<KeyValuePair<byte, byte>>
                            {
                                new KeyValuePair<byte, byte>((byte)CardType.SpecialActions, (byte)AdditionalTypes.SpecialActions.Hide)
                            });
                    }

                    if (attackerBattleEntity.HasBuff(CardType.FalconSkill, (byte)AdditionalTypes.FalconSkill.Hide))
                    {
                        attackerBattleEntity.Character.RemoveBuffByBCardTypeSubType(new List<KeyValuePair<byte, byte>>
                            {
                                new KeyValuePair<byte, byte>((byte)CardType.FalconSkill, (byte)AdditionalTypes.FalconSkill.Hide)
                            });
                        attackerBattleEntity.AddBuff(new Buff(560, attackerBattleEntity.Level), attackerBattleEntity);
                    }
                }
                if (CurrentHp <= 0 && !isCaptureSkill)
                {
                    // generate the kill bonus
                    hitRequest.Session.Character.GenerateKillBonus(this, attackerBattleEntity);

                    if (attackerBattleEntity.Character != null && hitRequest.SkillBCards.FirstOrDefault(s => s.Type == (byte)CardType.TauntSkill && s.SubType == (byte)AdditionalTypes.TauntSkill.EffectOnKill / 10) is BCard EffectOnKill)
                    {
                        if (ServerManager.RandomNumber() < EffectOnKill.FirstData)
                        {
                            attackerBattleEntity.AddBuff(new Buff((short)EffectOnKill.SecondData, attackerBattleEntity.Level), attackerBattleEntity);
                        }
                    }
                }
            }
            else
            {
                // monster already has been killed, send cancel
                hitRequest.Session.SendPacket(StaticPacketHelper.Cancel(2, MapMonsterId));
            }

            if (IsBoss)
            {
                MapInstance.Broadcast(GenerateBoss());
            }
        }

        /// <summary>
        /// Handle any kind of Monster interaction
        /// </summary>
        private void MonsterLife()
        {
            lock (_onHitLockObject)
            {
                if (Monster == null)
                {
                    return;
                }

                if (MonsterVNum == 2305 && MapInstance.MapInstanceType == MapInstanceType.CaligorInstance
                    && RunToX == 0 && RunToY == 0 
                    && Map.GetDistance(new MapCell { X = FirstX, Y = FirstY }, new MapCell { X = MapX, Y = MapY }) > 30)
                {
                    RunToX = FirstX;
                    RunToY = FirstY;
                }

                if ((DateTime.Now - LastEffect).TotalSeconds >= 5)
                {
                    LastEffect = DateTime.Now;
                    if (IsTarget)
                    {
                        MapInstance.Broadcast(StaticPacketHelper.GenerateEff(UserType.Monster, MapMonsterId, 824));
                    }

                    if (IsBonus)
                    {
                        MapInstance.Broadcast(StaticPacketHelper.GenerateEff(UserType.Monster, MapMonsterId, 826));
                    }
                }

                if (IsBoss && IsAlive)
                {
                    MapInstance.Broadcast(GenerateBoss());
                }

                // handle hit queue
                while (HitQueue.TryDequeue(out HitRequest hitRequest))
                {
                    OnReceiveHit(hitRequest);
                }

                lock (DamageList)
                {
                    // Check DamageList members on map and entity (Character or Mate or Mob or Npc not null)
                    Dictionary<BattleEntity, long> newDamageList = new Dictionary<BattleEntity, long>();
                    DamageList.Where(s => s.Key.HasEntity && s.Key.MapInstance == MapInstance).ToList().ForEach(s => newDamageList.Add(s.Key, s.Value));
                    DamageList = newDamageList;
                }

                lock (AggroList)
                {
                    List<BattleEntity> newAggroList = new List<BattleEntity>();
                    AggroList.ToList().Where(s => s.HasEntity && s.MapInstance == MapInstance).ToList().ForEach(s => newAggroList.Add(s));
                    AggroList = newAggroList;
                }

                GetNearestOponent();

                // Respawn
                if (!IsAlive && ShouldRespawn != null && !ShouldRespawn.Value)
                {
                    MapInstance.RemoveMonster(this);
                }

                if (!IsAlive && ShouldRespawn != null && ShouldRespawn.Value)
                {
                    double timeDeath = (DateTime.Now - Death).TotalSeconds;
                    if (timeDeath >= Monster.RespawnTime / 10d)
                    {
                        Respawn();
                    }
                }

                else if (IsSelfAttack)
                {
                    if (!IsPendingDelete && MapInstance != null && Skills != null)
                    {
                        TargetHit(BattleEntity, Skills.FirstOrDefault());

                        IsPendingDelete = true;

                        Observable.Timer(TimeSpan.FromSeconds(1))
                            .Subscribe(observable =>
                            {
                                RunDeathEvent();
                                MapInstance?.RemoveMonster(this);
                                MapInstance?.Broadcast(StaticPacketHelper.Die(UserType.Monster, MapMonsterId, UserType.Monster, MapMonsterId));
                            });
                    }
                }

                // normal movement
                else if (Target == null)
                {
                    Move();
                }

                // target following
                else if (MapInstance != null)
                {
                    HostilityTarget();
                    NpcMonsterSkill npcMonsterSkill = null;
                    BattleEntity targetEntity = null;

                    switch (Target?.EntityType)
                    {
                        case EntityType.Player:
                            {
                                if (MapInstance.GetSessionByCharacterId(Target.MapEntityId) is ClientSession targetSession)
                                {
                                    targetEntity = targetSession.Character?.BattleEntity;
                                }
                            }
                            break;

                        case EntityType.Mate:
                            {
                                Mate mate = MapInstance.Sessions.SelectMany(x => x.Character.Mates)
                                    .FirstOrDefault(s => (s.IsTeamMember || s.IsTemporalMate) && s.BattleEntity.MapEntityId == Target.MapEntityId);

                                if (mate != null)
                                {
                                    targetEntity = mate.BattleEntity;
                                }
                            }
                            break;

                        case EntityType.Monster:
                            {
                                if (MapInstance.GetMonsterById(Target.MapEntityId) is MapMonster mapMonster)
                                {
                                    targetEntity = mapMonster.BattleEntity;
                                }
                            }
                            break;

                        case EntityType.Npc:
                            {
                                if (MapInstance.GetNpc(Target.MapEntityId) is MapNpc mapNpc)
                                {
                                    targetEntity = mapNpc.BattleEntity;
                                }
                            }
                            break;
                    }

                    // remove target in some situations
                    if (targetEntity == null
                        || targetEntity.Hp <= 0
                        || CurrentHp <= 0
                        || targetEntity.MapInstance != MapInstance
                        || (targetEntity.Character != null
                            && (targetEntity.Character.Invisible || targetEntity.Character.InvisibleGm)
                            && !CanSeeHiddenThings))
                    {
                        RemoveTarget();
                        return;
                    }

                    if (HasBuff(CardType.SpecialActions, (byte)AdditionalTypes.SpecialActions.RunAway))
                    {
                        RunAway();
                        return;
                    }

                    bool instantAttack = MonsterVNum == 1439 || MonsterVNum == 1436 || MonsterVNum == 946 || MonsterVNum == 1382;

                    if ((DateTime.Now - LastSkill).TotalMilliseconds >= 1100 + (instantAttack ? 0 : (Monster.BasicCooldown * 200)))
                    {
                        if (Skills != null)
                        {
                            int specialSkillsRate = ServerManager.RandomNumber(0, 12);

                            if (ServerManager.Instance.BossVNums.Contains(MonsterVNum))
                            {
                                specialSkillsRate = ServerManager.RandomNumber(0, 30);
                            }

                            #region UseSkillOnDamage

                            if (UseSkillOnDamage != null)
                            {
                                UseSkillOnDamage useSkillOnDamage = UseSkillOnDamage.FirstOrDefault(u => HpPercent() <= u.HpPercent);

                                if (useSkillOnDamage != null)
                                {
                                    UseSkillOnDamage.Remove(useSkillOnDamage);

                                    npcMonsterSkill = Skills.FirstOrDefault(s => s.SkillVNum == useSkillOnDamage.SkillVNum);

                                    if (npcMonsterSkill != null)
                                    {
                                        TargetHit(targetEntity, npcMonsterSkill);
                                    }

                                    return;
                                }
                            }

                            #endregion

                            List<NpcMonsterSkill> possibleSkills = Skills.Where(s => !SkillHelper.IsManagedSkill(s.SkillVNum) && ((DateTime.Now - s.LastSkillUse).TotalMilliseconds >= 100 * s.Skill.Cooldown || s.Rate == 0 || instantAttack)).ToList();

                            foreach (NpcMonsterSkill skill in possibleSkills.OrderBy(rnd => ServerManager.RandomNumber()))
                            {
                                if (skill.Rate == 0 || instantAttack)
                                {
                                    if ((skill.SkillVNum == 1226 && _previousSkillVNum != 1224) || (skill.SkillVNum == 1256 && _previousSkillVNum != 1255))
                                    {
                                        continue;
                                    }

                                    npcMonsterSkill = skill;
                                }
                                else if (ServerManager.RandomNumber() < skill.Rate && specialSkillsRate > 8)
                                {
                                    if ((skill.SkillVNum != 1226 && _previousSkillVNum == 1224) || (skill.SkillVNum != 1256 && _previousSkillVNum == 1255))
                                    {
                                        continue;
                                    }

                                    npcMonsterSkill = skill;

                                    break;
                                }
                            }
                        }
                        
                        if (_previousSkillVNum == 1255 && LastSkill.AddSeconds(10) > DateTime.Now) //Glacerus wait after storm skill)
                        {
                            return;
                        }

                        if ((DateTime.Now - LastMove).TotalMilliseconds >= Speed * 100)
                        {
                            if (npcMonsterSkill?.Skill.TargetType == 1 && npcMonsterSkill?.Skill.HitType == 0)
                            {
                                TargetHit(targetEntity, npcMonsterSkill);
                                return;
                            }
                            else if (npcMonsterSkill?.Skill.TargetType == 1 && npcMonsterSkill?.Skill.HitType == 2)
                            {
                                TargetHit(BattleEntity, npcMonsterSkill);
                                return;
                            }
                            else if ((targetEntity.Character == null || (!targetEntity.Character.InvisibleGm || (!targetEntity.Character.Invisible || CanSeeHiddenThings))) && targetEntity.Hp > 0)
                            {
                                if (npcMonsterSkill?.Skill.TargetType == 1 && npcMonsterSkill?.Skill.HitType == 1 && npcMonsterSkill?.Skill.TargetRange > 0
                                    && CurrentMp >= npcMonsterSkill.Skill.MpCost && ((Map.GetDistance(
                                      new MapCell
                                      {
                                          X = MapX,
                                          Y = MapY
                                      },
                                      new MapCell
                                      {
                                          X = targetEntity.PositionX,
                                          Y = targetEntity.PositionY
                                      }) <= npcMonsterSkill.Skill.TargetRange)))
                                {
                                    TargetHit(targetEntity, npcMonsterSkill);
                                    return;
                                }
                                else if (npcMonsterSkill != null && CurrentMp >= npcMonsterSkill.Skill.MpCost && (Map.GetDistance(
                                        new MapCell
                                        {
                                            X = MapX,
                                            Y = MapY
                                        },
                                        new MapCell
                                        {
                                            X = targetEntity.PositionX,
                                            Y = targetEntity.PositionY
                                        }) <= (npcMonsterSkill.Skill.Range > 0 ? npcMonsterSkill.Skill.Range : Monster.BasicRange)))
                                {
                                    TargetHit(targetEntity, npcMonsterSkill);
                                    return;
                                }
                                else
                                {
                                    if (Map.GetDistance(new MapCell
                                    {
                                        X = MapX,
                                        Y = MapY
                                    },
                                             new MapCell
                                             {
                                                 X = targetEntity.PositionX,
                                                 Y = targetEntity.PositionY
                                             }) <= Monster.BasicRange && Monster.BasicRange > 0)
                                    {
                                        TargetHit(targetEntity, null);
                                        return;
                                    }
                                }
                            }
                        }
                    }

                    FollowTarget(targetEntity);
                }
            }
        }

        public void LoadSpeed()
        {
            Speed = Monster.Speed;

            byte fixSpeed = (byte)GetBuff(CardType.Move, (byte)AdditionalTypes.Move.SetMovement)[0];
            if (fixSpeed != 0)
            {
                Speed = fixSpeed;
            }
            else
            {
                Speed += (byte)GetBuff(CardType.Move, (byte)AdditionalTypes.Move.MovementSpeedIncreased)[0];
                if (Speed == 59)
                {
                    Speed = 1;
                }
                Speed = (byte)(Speed * (1 + (GetBuff(CardType.Move, (byte)AdditionalTypes.Move.MoveSpeedIncreased)[0] / 100D)));
            }
        }

        private void Move()
        {
            MoveTest();

            /*
            if (Monster != null && IsAlive && !HasBuff(CardType.Move, (byte)AdditionalTypes.Move.MovementImpossible) && IsMoving && Monster.Speed > 0)
            {
                if (!Path.Any() && (DateTime.Now - LastMove).TotalMilliseconds > _movetime && Target == null) // Basic Move
                {
                    Path = BestFirstSearch.FindPathJagged(new Node { X = MapX, Y = MapY }, new Node { X = (short)(FirstX + ServerManager.RandomNumber(-2, 2)), Y = (short)(FirstY + ServerManager.RandomNumber(-2, 2)) }, MapInstance.Map.JaggedGrid);
                }
                else if (DateTime.Now > LastMove && Path.Any()) // Follow target || move back to original pos
                {
                    LoadSpeed();
                    byte speedIndex = (byte)(Speed / 2.5 < 1 ? 1 : Speed / 2.5);
                    int maxindex = Path.Count > speedIndex ? speedIndex : Path.Count;
                    short smapX = (short)ServerManager.RandomNumber(Path[maxindex - 1].X - 1, Path[maxindex - 1].X + 1);
                    short smapY = (short)_random.Next(Path[maxindex - 1].Y - 1, Path[maxindex - 1].Y + 1);
                    if (MapInstance.Map.IsBlockedZone(smapX, smapY))
                    {
                        smapX = Path[maxindex - 1].X;
                        smapY = Path[maxindex - 1].Y;
                    }

                    if (Target == null || Map.GetDistance(new MapCell { X = smapX, Y = smapY }, new MapCell { X = Target.PositionX, Y = Target.PositionY }) >= Monster.BasicRange - 1)
                    {
                        double waitingtime = Map.GetDistance(new MapCell { X = smapX, Y = smapY }, GetPos()) / (double)Speed;
                        MapInstance.Broadcast(new BroadcastPacket(null, $"mv 3 {MapMonsterId} {smapX} {smapY} {Speed}", ReceiverType.All, xCoordinate: smapX, yCoordinate: smapY));
                        LastMove = DateTime.Now.AddSeconds(waitingtime > 1 ? 1 : waitingtime);
                        Observable.Timer(TimeSpan.FromMilliseconds((int)((waitingtime > 1 ? 1 : waitingtime) * 1000))).Subscribe(x =>
                        {
                            MapX = smapX;
                            MapY = smapY;
                        });
                    }

                    if (Target != null && (int)Path[0].F > _maxDistance) // Remove Target if distance between target & monster is > max Distance
                    {
                        RemoveTarget();
                        return;
                    }
                    Path.RemoveRange(0, maxindex);
                }
            }
            HostilityTarget();
            */
        }
        
        public void MoveTest()
        {
            double walkWaitTime = (Target == null && RunToX == 0 && RunToY == 0 ? ServerManager.RandomNumber(400, 3200) : 0) + (Speed / 1.5f) * 100 - (DateTime.Now - LastMove).TotalMilliseconds;
            double skillWaitTime = 0 /*800 - (DateTime.Now - LastSkill).TotalMilliseconds*/;
            if (Monster != null && !IsDisabled && !IsJumping && IsAlive && !HasBuff(CardType.Move, (byte)AdditionalTypes.Move.MovementImpossible) && IsMoving && Monster.Speed > 0 && walkWaitTime <= 0 && skillWaitTime <= 0)
            {
                LoadSpeed();

                LastMove = DateTime.Now;

                MapCell moveToPosition = new MapCell { X = FirstX, Y = FirstY };

                if (RunToX != 0 || RunToY != 0)
                {
                    moveToPosition = new MapCell { X = RunToX, Y = RunToY };
                }
                if (Target != null && (MonsterVNum != 2305 || RunToX == 0 && RunToY == 0))
                {
                    moveToPosition = new MapCell { X = Target.PositionX, Y = Target.PositionY };
                }

                MapCell nextStep = Map.GetNextStep(new MapCell { X = MapX, Y = MapY }, new MapCell { X = (short)ServerManager.RandomNumber(moveToPosition.X - 3, moveToPosition.X + 3), Y = (short)ServerManager.RandomNumber(moveToPosition.Y - 3, moveToPosition.Y + 3) }, (Speed / 2.5f));

                short tries = 0;
                short maxTries = 15;

                while (MapInstance.Map.isBlockedZone(MapX, MapY, nextStep.X, nextStep.Y) && tries < maxTries)
                {
                    nextStep = Map.GetNextStep(new MapCell { X = MapX, Y = MapY }, new MapCell { X = (short)ServerManager.RandomNumber(moveToPosition.X - 3, moveToPosition.X + 3), Y = (short)ServerManager.RandomNumber(moveToPosition.Y - 3, moveToPosition.Y + 3) }, (Speed / 2.5f));
                    tries++;
                }

                if (tries < maxTries)
                {
                    MapInstance.Broadcast(new BroadcastPacket(null, $"mv 3 {MapMonsterId} {nextStep.X} {nextStep.Y} {Math.Round(Speed * 1.5f)}", ReceiverType.All, xCoordinate: MapX, yCoordinate: MapY));
                    Observable.Timer(TimeSpan.FromMilliseconds(Math.Round(Speed * 1.5f) * 10)).Subscribe(s =>
                    {
                        MapX = nextStep.X;
                        MapY = nextStep.Y;
                    });
                }
                else
                {
                    //Generate Coords 
                    bool notwalkable = false;
                    short gen_x, gen_y;
                    tries = 0;
                    do
                    {
                        gen_x = MapX;
                        gen_y = MapY;

                        int x_min = ServerManager.RandomNumber(0, 10);
                        int y_min = ServerManager.RandomNumber(0, 10);

                        if (x_min > 6) { gen_x -= 3; } else if (x_min < 4) { gen_x += 3; }
                        if (y_min > 6) { gen_y -= 3; } else if (y_min < 4) { gen_y += 3; }

                        notwalkable = MapInstance.Map.isBlockedZone(MapX, MapY, gen_x, gen_y);

                        if (Map.GetDistance(new MapCell { X = MapX, Y = MapY }, new MapCell { X = gen_x, Y = gen_y }) > 3)
                        {
                            notwalkable = true;
                        }
                        if (MapX == gen_x && MapY == gen_y)
                        {
                            notwalkable = true;
                        }
                        tries++;
                    } while (notwalkable && tries < maxTries);

                    if (!notwalkable)
                    {
                        MapInstance.Broadcast(new BroadcastPacket(null, $"mv 3 {MapMonsterId} {gen_x} {gen_y} {Math.Round(Speed * 1.3f)}", ReceiverType.All, xCoordinate: MapX, yCoordinate: MapY));
                        Observable.Timer(TimeSpan.FromMilliseconds(Math.Round(Speed * 1.3f) * 10)).Subscribe(s =>
                        {
                            MapX = gen_x;
                            MapY = gen_y;
                        });
                    }
                }

                if (Map.GetDistance(new MapCell { X = MapX, Y = MapY }, new MapCell { X = RunToX, Y = RunToY }) < 2)
                {
                    RunToX = 0;
                    RunToY = 0;
                }

                if ((Map.GetDistance(new MapCell { X = MapX, Y = MapY }, new MapCell { X = moveToPosition.X, Y = moveToPosition.Y }) > _maxDistance && LastSkill.AddMilliseconds(_timeAgroLoss) <= DateTime.Now)
                    || (MapInstance.MapInstanceType == MapInstanceType.BaseMapInstance && LastSkill.AddMilliseconds(20000) <= DateTime.Now && LastMonsterAggro.AddMilliseconds(20000) <= DateTime.Now))
                {
                    RemoveTarget();
                }
            }
            HostilityTarget();
        }
        
        private void Respawn()
        {
            if (Monster != null)
            {
                DisableBuffs(BuffType.All);

                lock (DamageList)
                {
                    DamageList = new Dictionary<BattleEntity, long>();
                }

                lock (AggroList)
                {
                    AggroList = new List<BattleEntity>();
                }

                IsAlive = true;
                Target = null;
                MaxHp = BaseMaxHp;
                MaxMp = BaseMaxMp;
                CurrentHp = MaxHp;
                CurrentMp = MaxMp;
                MapX = FirstX;
                MapY = FirstY;
                if (MapInstance.MapInstanceType == MapInstanceType.BaseMapInstance && ServerManager.Instance.MapBossVNums.Contains(MonsterVNum))
                {
                    MapCell randomCell = MapInstance.Map.GetRandomPosition();
                    if (randomCell != null)
                    {
                        MapX = randomCell.X;
                        MapY = randomCell.Y;
                    }
                }
                Path = new List<Node>();
                MapInstance.Broadcast(GenerateIn());
                Monster.BCards.Where(s => s.Type != 25).ToList().ForEach(s => s.ApplyBCards(BattleEntity, BattleEntity));
            }
        }

        /// <summary>
        /// Hit the Target.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="npcMonsterSkill"></param>
        private void TargetHit(BattleEntity target, NpcMonsterSkill npcMonsterSkill)
        {
            if (Monster != null && !HasBuff(CardType.SpecialAttack, (byte)AdditionalTypes.SpecialAttack.NoAttack))
            {
                int castTime = 0;
                if (npcMonsterSkill != null)
                {
                    if (CurrentMp < npcMonsterSkill.Skill.MpCost)
                    {
                        FollowTarget(target);
                        return;
                    }
                    
                    _previousSkillVNum = npcMonsterSkill.SkillVNum;

                    npcMonsterSkill.LastSkillUse = DateTime.Now;
                    DecreaseMp(npcMonsterSkill.Skill.MpCost);
                    MapInstance.Broadcast(StaticPacketHelper.CastOnTarget(UserType.Monster, MapMonsterId, target.UserType, target.MapEntityId,
                        npcMonsterSkill.Skill.CastAnimation, npcMonsterSkill.Skill.CastEffect,
                        npcMonsterSkill.Skill.SkillVNum));
                    if (npcMonsterSkill.Skill.CastEffect != 0)
                    {
                        MapInstance.Broadcast(
                            StaticPacketHelper.GenerateEff(UserType.Monster, MapMonsterId,
                                npcMonsterSkill.Skill.CastEffect), MapX, MapY);
                        castTime = npcMonsterSkill.Skill.CastTime * 100;
                    }
                    if (npcMonsterSkill.Skill.CastEffect == 4657 || npcMonsterSkill.Skill.CastEffect == 4940)
                    {
                        List<BattleEntity> possibleTargets = MapInstance.BattleEntities
                           .OrderBy(e => Map.GetDistance(GetPos(), e.GetPos()))
                           .Where(e => e.UserType == UserType.Player && e.Character != null && BattleEntity.CanAttackEntity(e) && !e.Character.InvisibleGm && (!e.Character.Invisible || CanSeeHiddenThings) && Map.GetDistance(GetPos(), e.GetPos()) < npcMonsterSkill.Skill.Range)
                           .ToList();
                        Observable.Timer(TimeSpan.FromMilliseconds(castTime > 200 ? 200 : 0)).Subscribe(obs =>
                        {
                            MapInstance.Broadcast(StaticPacketHelper.Out(UserType.Monster, MapMonsterId));
                        });
                        IsJumping = true;
                        if (possibleTargets?.Count > 0)
                        {
                            BattleEntity chosenTarget = possibleTargets.OrderBy(s => ServerManager.RandomNumber()).FirstOrDefault();
                            MapX = chosenTarget.PositionX;
                            MapY = chosenTarget.PositionY;
                            MapInstance.Broadcast(StaticPacketHelper.GenerateEff(chosenTarget.UserType, chosenTarget.MapEntityId, 4660));
                            possibleTargets.Except(new List<BattleEntity> { chosenTarget }).ToList().ForEach(s => MapInstance.Broadcast(StaticPacketHelper.GenerateEff(s.UserType, s.MapEntityId, 4660)));
                        }
                        Observable.Timer(TimeSpan.FromMilliseconds(castTime > 100 ? castTime - 100 : 0)).Subscribe(obs =>
                        {
                            IsJumping = false;
                            MapInstance.Broadcast(GenerateIn());
                        });
                    }
                    if (npcMonsterSkill.Skill.SkillVNum == 640)
                    {
                        List<BattleEntity> possibleTargets = MapInstance.BattleEntities
                           .OrderBy(e => Map.GetDistance(GetPos(), e.GetPos()))
                           .Where(e => e.UserType == UserType.Player && e.Character != null && BattleEntity.CanAttackEntity(e) && !e.Character.InvisibleGm && (!e.Character.Invisible || CanSeeHiddenThings) && Map.GetDistance(GetPos(), e.GetPos()) < npcMonsterSkill.Skill.Range)
                           .ToList();
                        if (possibleTargets?.Count == 0)
                        {
                            void spawnCircle(int round)
                            {
                                if (MapInstance != null)
                                {
                                    MapCell cell = MapInstance.Map.GetRandomPosition();

                                    int circleId = MapInstance.GetNextMonsterId();

                                    MapMonster circle = new MapMonster { MonsterVNum = 2345, MapX = cell.X, MapY = cell.Y, MapMonsterId = circleId, IsHostile = false, IsMoving = false, ShouldRespawn = false };
                                    circle.Initialize(MapInstance);
                                    circle.NoAggresiveIcon = true;
                                    MapInstance.AddMonster(circle);
                                    MapInstance.Broadcast(circle.GenerateIn());
                                    MapInstance.Broadcast(StaticPacketHelper.GenerateEff(UserType.Monster, circleId, 4660));
                                    Observable.Timer(TimeSpan.FromSeconds(3)).Subscribe(observer =>
                                    {
                                        if (MapInstance != null)
                                        {
                                            MapInstance.Broadcast(StaticPacketHelper.SkillUsed(UserType.Monster, circleId, 3, circleId, 2345, 220, 0, 7566, cell.X, cell.Y, true, 0, 4500, 0, 0));
                                            foreach (Character character in MapInstance.GetCharactersInRange(cell.X, cell.Y, 2))
                                            {
                                                character.GetDamage(4500, character.BattleEntity, true);
                                            }
                                            MapInstance.RemoveMonster(circle);
                                            MapInstance.Broadcast(StaticPacketHelper.Out(UserType.Monster, circle.MapMonsterId));
                                        }
                                    });
                                }
                            }

                            #region SpawnMeteoritos
                            while (true)
                            {
                                spawnCircle(1);
                                Thread.Sleep(500);
                                spawnCircle(1);
                                Thread.Sleep(500);
                                spawnCircle(1);
                                Thread.Sleep(500);
                                spawnCircle(1);
                                Thread.Sleep(500);
                                spawnCircle(1);
                                break;
                            }
                            #endregion
                            BattleEntity chosenTarget = possibleTargets.OrderBy(s => ServerManager.RandomNumber()).FirstOrDefault();

                            possibleTargets.Except(new List<BattleEntity> { chosenTarget }).ToList().ForEach(s =>
                            {
                                MapInstance.SummonMonster(new MonsterToSummon(2345, new MapCell() { X = s.PositionX, Y = s.PositionY }, null, false));
                            });
                        }                      
                    }
                    if (npcMonsterSkill.Skill.CastEffect == 4426)
                    {
                        List<BattleEntity> possibleTargets = MapInstance.BattleEntities
                           .OrderBy(e => Map.GetDistance(GetPos(), e.GetPos()))
                           .Where(e => e.UserType == UserType.Player && e.Character != null && BattleEntity.CanAttackEntity(e) && !e.Character.InvisibleGm && (!e.Character.Invisible || CanSeeHiddenThings) && Map.GetDistance(GetPos(), e.GetPos()) < npcMonsterSkill.Skill.Range)
                           .ToList();
                        Observable.Timer(TimeSpan.FromMilliseconds(castTime > 200 ? 200 : 0)).Subscribe(obs =>
                        {
                            MapInstance.Broadcast(StaticPacketHelper.Out(UserType.Monster, MapMonsterId));
                        });
                        IsJumping = true;
                        if (possibleTargets?.Count > 0)
                        {
                            void spawnCircle(int round)
                            {
                                if (MapInstance != null)
                                {
                                    MapCell cell = MapInstance.Map.GetRandomPosition();

                                    int circleId = MapInstance.GetNextMonsterId();

                                    MapMonster circle = new MapMonster { MonsterVNum = 2018, MapX = cell.X, MapY = cell.Y, MapMonsterId = circleId, IsHostile = false, IsMoving = false, ShouldRespawn = false };
                                    circle.Initialize(MapInstance);
                                    circle.NoAggresiveIcon = true;
                                    MapInstance.AddMonster(circle);
                                    MapInstance.Broadcast(circle.GenerateIn());
                                    MapInstance.Broadcast(StaticPacketHelper.GenerateEff(UserType.Monster, circleId, 4660));
                                    Observable.Timer(TimeSpan.FromSeconds(3)).Subscribe(observer =>
                                    {
                                        if (MapInstance != null)
                                        {
                                            MapInstance.Broadcast(StaticPacketHelper.SkillUsed(UserType.Monster, circleId, 3, circleId, 1220, 220, 0, 4983, cell.X, cell.Y, true, 0, 4500, 0, 0));
                                            foreach (Character character in MapInstance.GetCharactersInRange(cell.X, cell.Y, 2))
                                            {
                                                character.IsCustomSpeed = false;
                                                character.RemoveVehicle();
                                                character.GetDamage(4500, character.BattleEntity);
                                                Observable.Timer(TimeSpan.FromMilliseconds(1000)).Subscribe(o => ServerManager.Instance.AskRevive(character.CharacterId));
                                            }
                                            MapInstance.RemoveMonster(circle);
                                            MapInstance.Broadcast(StaticPacketHelper.Out(UserType.Monster, circle.MapMonsterId));
                                        }
                                    });
                                }
                            }

                            #region SpawnMeteoritos
                            spawnCircle(1);
                            spawnCircle(1);
                            spawnCircle(1);
                            spawnCircle(1);
                            spawnCircle(1);
                            spawnCircle(1);
                            spawnCircle(1);
                            spawnCircle(1);
                            spawnCircle(1);
                            spawnCircle(1);
                            spawnCircle(1);
                            spawnCircle(1);
                            spawnCircle(1);
                            spawnCircle(1);
                            spawnCircle(1);
                            spawnCircle(1);
                            spawnCircle(1);
                            spawnCircle(1);
                            spawnCircle(1);
                            spawnCircle(1);
                            #endregion
                            BattleEntity chosenTarget = possibleTargets.OrderBy(s => ServerManager.RandomNumber()).FirstOrDefault();

                            Observable.Timer(TimeSpan.FromMilliseconds(castTime > 2000 ? castTime - 2000 : 0)).Subscribe(obs =>
                            {
                                MapX = chosenTarget.PositionX;
                                MapY = chosenTarget.PositionY;
                                MapInstance.Broadcast($"eff_g  4431 0 {MapX} {MapY} 0");
                            });

                            possibleTargets.Except(new List<BattleEntity> { chosenTarget }).ToList().ForEach(s =>
                            {
                                MapInstance.SummonMonster(new MonsterToSummon(2048, new MapCell() { X = s.PositionX, Y = s.PositionY }, null, false));
                            });
                        }
                        Observable.Timer(TimeSpan.FromMilliseconds((castTime > 2000 ? castTime - 2000 : 0) + 1900)).Subscribe(obs2 =>
                        {
                            IsJumping = false;
                            MapInstance.Broadcast(GenerateIn());
                            MapInstance.Broadcast($"eff_g  4425 0 {MapX} {MapY} 0");
                        });
                    }
                    if (npcMonsterSkill.SkillVNum == 1226)
                    {
                        AddBuff(new Buff(529, BattleEntity.Level), BattleEntity, true);
                        AddBuff(new Buff(530, BattleEntity.Level), BattleEntity, true);
                    }
                }

                LastMove = DateTime.Now;

                LastSkill = DateTime.Now;

                Observable.Timer(TimeSpan.FromMilliseconds(castTime)).Subscribe(obs =>
                {
                    int hitmode = 0;
                    bool onyxWings = false;
                    BattleEntity targetEntity = null;
                    switch (target.EntityType)
                    {
                        case EntityType.Player:
                            targetEntity = new BattleEntity(target.Character, null);
                            break;
                        case EntityType.Mate:
                            targetEntity = new BattleEntity(target.Mate);
                            break;
                        case EntityType.Monster:
                            targetEntity = new BattleEntity(target.MapMonster);
                            break;
                        case EntityType.Npc:
                            targetEntity = new BattleEntity(target.MapNpc);
                            break;
                    }

                    int damage = DamageHelper.Instance.CalculateDamage(new BattleEntity(this),
                        targetEntity, npcMonsterSkill?.Skill, ref hitmode,
                        ref onyxWings);

                    // deal 0 damage to GM with GodMode
                    if (target.Character != null && target.Character.HasGodMode || target.Mate != null && target.Mate.Owner.HasGodMode)
                    {
                        damage = 0;
                    }

                    if (target.Character != null)
                    {
                        if (ServerManager.RandomNumber() < target.Character.GetBuff(CardType.DarkCloneSummon,
                            (byte)AdditionalTypes.DarkCloneSummon.ConvertDamageToHPChance)[0])
                        {
                            int amount = damage;

                            target.Character.ConvertedDamageToHP += amount;
                            target.Character.MapInstance?.Broadcast(target.Character.GenerateRc(amount));
                            target.Character.Hp += amount;

                            if (target.Character.Hp > target.Character.HPLoad())
                            {
                                target.Character.Hp = (int)target.Character.HPLoad();
                            }

                            target.Character.Session?.SendPacket(target.Character.GenerateStat());

                            damage = 0;
                        }
                    }

                    int[] manaShield = target.GetBuff(CardType.LightAndShadow,
                        (byte)AdditionalTypes.LightAndShadow.InflictDamageToMP);
                    if (manaShield[0] != 0 && hitmode != 4)
                    {
                        int reduce = damage / 100 * manaShield[0];
                        if (target.Mp < reduce)
                        {
                            reduce = target.Mp;
                            target.Mp = 0;
                        }
                        else
                        {
                            target.DecreaseMp(reduce);
                        }
                        damage -= reduce;
                    }

                    if (target.Character != null && target.Character.IsSitting)
                    {
                        target.Character.IsSitting = false;
                        MapInstance.Broadcast(target.Character.GenerateRest());
                    }

                    TargetHit2(target, npcMonsterSkill, damage, hitmode);

                    if (Owner?.Mate != null || MonsterHelper.IsKamikaze(MonsterVNum))
                    {
                        double ms = (MonsterVNum >= 2112 && MonsterVNum <= 2115) ? 1000 : 250;

                        Observable.Timer(TimeSpan.FromMilliseconds(ms)).Subscribe(t =>
                        {
                            RunDeathEvent();
                            MapInstance.RemoveMonster(this);
                            MapInstance.Broadcast(StaticPacketHelper.Die(UserType.Monster, MapMonsterId, UserType.Monster, MapMonsterId));
                        });
                    }
                });
            }
        }

        public void TargetHit2(BattleEntity target, NpcMonsterSkill npcMonsterSkill, int damage, int hitmode)
        {
            bool attackGreaterDistance = false;
            List<BCard> bCards = new List<BCard>();
            bCards.AddRange(Monster.BCards.ToList());
            byte skillRange = Monster.BasicRange;
            byte skillTargetRange = Monster.BasicRange;
            if (npcMonsterSkill != null)
            {
                if (npcMonsterSkill.SkillVNum == 1321)
                {
                    hitmode = 0;
                }

                skillRange = npcMonsterSkill.Skill.Range;
                skillTargetRange = npcMonsterSkill.Skill.TargetRange;
                bCards.AddRange(npcMonsterSkill.Skill.BCards.ToList());
                if (npcMonsterSkill.Skill.TargetType == 1 && npcMonsterSkill.Skill.HitType == 1 && npcMonsterSkill.Skill.TargetRange == 0 && npcMonsterSkill.Skill.Range > 0)
                {
                    if (npcMonsterSkill.SkillVNum != 1207)
                    {
                        attackGreaterDistance = true;
                    }
                }
                if (npcMonsterSkill.Skill.CastEffect == 4657 || npcMonsterSkill.Skill.CastEffect == 4940)
                {
                    skillRange = 3;
                }
                if (npcMonsterSkill.Skill.TargetType == 0 && npcMonsterSkill.Skill.HitType == 0)
                {
                    skillRange = npcMonsterSkill.Skill.TargetRange;
                    skillTargetRange = 0;
                }
                if (npcMonsterSkill.Skill.TargetType == 1 && npcMonsterSkill.Skill.HitType == 1)
                {
                    skillRange = npcMonsterSkill.Skill.TargetRange;
                    skillTargetRange = 0;
                }
                if (attackGreaterDistance || (npcMonsterSkill.Skill.TargetType == 0 && npcMonsterSkill.Skill.HitType == 1))
                {
                    skillRange = npcMonsterSkill.Skill.TargetRange;
                    skillTargetRange = npcMonsterSkill.Skill.Range;
                }
                if (npcMonsterSkill.Skill.TargetType == 1 && npcMonsterSkill.Skill.HitType == 2) // Area Buff
                {
                    MapInstance.Broadcast(StaticPacketHelper.SkillUsed(UserType.Monster, MapMonsterId, (byte)BattleEntity.UserType, MapMonsterId,
                            npcMonsterSkill.SkillVNum, npcMonsterSkill.Skill.Cooldown,
                            npcMonsterSkill.Skill.AttackAnimation, npcMonsterSkill.Skill.Effect, MapX, MapY,
                            CurrentHp > 0,
                            (int)(CurrentHp / MaxHp * 100), 0,
                            0, 0));

                    MapInstance.GetBattleEntitiesInRange(new MapCell { X = MapX, Y = MapY }, skillTargetRange)
                        .Where(s => !BattleEntity.CanAttackEntity(s))
                        .ToList().ForEach(s => npcMonsterSkill.Skill.BCards.ForEach(b => b.ApplyBCards(s, BattleEntity, MapX, MapY)));
                    return;
                }
                if (npcMonsterSkill.SkillVNum == 1215)
                {
                    int recoverHp = 0;
                    MapInstance.BattleEntities
                        .Where(s => !BattleEntity.CanAttackEntity(s) && s.MapMonster?.MonsterVNum == 2017)
                        .ToList().ForEach(s =>
                        {
                            recoverHp += s.Hp;
                            s.MapMonster.RunDeathEvent();
                            MapInstance.RemoveMonster(s.MapMonster);
                            MapInstance.Broadcast(StaticPacketHelper.Out(UserType.Monster, s.MapMonster.MapMonsterId));
                        });
                    if (CurrentHp + recoverHp > MaxHp)
                    {
                        recoverHp = (int)(MaxHp - CurrentHp);
                    }
                    if (recoverHp > 0)
                    {
                        MapInstance.Broadcast(UserInterfaceHelper.GenerateMsg(string.Format(
                            Language.Instance.GetMessageFromKey("EARNED_VITALLITY"), Monster.Name, ServerManager.GetNpcMonster(2017).Name), 0));
                        CurrentHp += recoverHp;
                        MapInstance.Broadcast(BattleEntity.GenerateRc(recoverHp));
                    }
                }
            }

            lock (target.PVELockObject)
            {
                if (target.Hp > 0 && target.MapInstance == MapInstance && !attackGreaterDistance
                    && (skillTargetRange == 0 || Map.GetDistance(new MapCell { X = MapX, Y = MapY }, new MapCell { X = target.PositionX, Y = target.PositionY }) <= skillTargetRange))
                {
                    if (damage >= target.Hp &&
                        Monster.BCards.Any(s => s.Type == (byte)CardType.NoDefeatAndNoDamage && s.SubType == (byte)AdditionalTypes.NoDefeatAndNoDamage.DecreaseHPNoKill / 10 && s.FirstData == 1))
                    {
                        damage = target.Hp - 1;
                    }
                    if (damage >= target.Hp &&
                        target.BCards.Any(s => s.Type == (byte)CardType.NoDefeatAndNoDamage && s.SubType == (byte)AdditionalTypes.NoDefeatAndNoDamage.DecreaseHPNoDeath / 10 && s.FirstData == -1))
                    {
                        damage = (int)target.Hp - 1;
                    }

                    if (Owner != null && MonsterVNum == 945)
                    {
                        hitmode = 0;
                    }

                    bool firstHit = false;

                    target.GetDamage(damage, BattleEntity);

                    if (target.MapNpc != null)
                    {
                        if (target.MapNpc.Target == -1)
                        {
                            target.MapNpc.Target = MapMonsterId;
                        }
                    }

                    if (target.Character != null)
                    {
                        MapInstance.Broadcast(null, target.Character.GenerateStat(), ReceiverType.OnlySomeone,
                            "", target.MapEntityId);

                        // Magical Fetters

                        if (damage > 0)
                        {
                            if (target.Character.HasMagicalFetters)
                            {
                                // Magic Spell

                                target.Character.AddBuff(new Buff(617, target.Character.Level), target.Character.BattleEntity);

                                int castId = 10 + Monster.Element;

                                if (castId == 10)
                                {
                                    castId += 5; // No element
                                }

                                target.Character.LastComboCastId = castId;
                                target.Character.Session?.SendPacket($"mslot {castId} -1");
                            }
                        }
                    }

                    if (target.Mate != null)
                    {
                        target.Mate.Owner.Session.SendPacket(target.Mate.Owner.GeneratePst().FirstOrDefault(s => s.Contains(target.Mate.MateTransportId.ToString())));
                        if (target.Mate.IsSitting)
                        {
                            target.Mate.Owner.MapInstance.Broadcast(target.Mate.GenerateRest(false));
                        }
                    }
                    if (target.MapMonster != null && Owner != null)
                    {
                        if (target.MapMonster.Target == null)
                        {
                            target.MapMonster.Target = BattleEntity;
                        }
                        lock (target.MapMonster.DamageList)
                        {
                            if (!target.MapMonster.DamageList.Any(s => s.Value > 0))
                            {
                                firstHit = true;
                            }
                            target.MapMonster.AddToDamageList(Owner, damage);
                        }
                    }
                    MapInstance.Broadcast(npcMonsterSkill != null
                        ? StaticPacketHelper.SkillUsed(UserType.Monster, MapMonsterId, (byte)target.UserType, target.MapEntityId,
                            npcMonsterSkill.SkillVNum, npcMonsterSkill.Skill.Cooldown,
                            npcMonsterSkill.Skill.AttackAnimation, npcMonsterSkill.Skill.Effect, MapX, MapY,
                            target.Hp > 0,
                            (int)(target.Hp / target.HPLoad() * 100), damage,
                            hitmode, 0)
                        : StaticPacketHelper.SkillUsed(UserType.Monster, MapMonsterId, (byte)target.UserType, target.MapEntityId, 0,
                            Monster.BasicCooldown, 11, Monster.BasicSkill, 0, 0, target.Hp > 0,
                            (int)(target.Hp / target.HPLoad() * 100), damage,
                            hitmode, 0));

                    if (hitmode != 4 && hitmode != 2)
                    {
                        // Maybe must be out (hitmode != 4 && hitmode != 2) condition
                        bCards.Where(s => s.CastType == 1 || s.SkillVNum != null).ToList().ForEach(s =>
                        {
                            if (s.Type != (byte)CardType.Buff)
                            {
                                s.ApplyBCards(target, BattleEntity);
                            }
                        });

                        bCards.Where(s => s.CastType == 1 || s.SkillVNum != null).ToList().ForEach(s =>
                        {
                            if (s.Type == (byte)CardType.Buff)
                            {
                                Buff b = new Buff((short)s.SecondData, Monster.Level);
                                if (b.Card != null)
                                {
                                    switch (b.Card?.BuffType)
                                    {
                                        case BuffType.Bad:
                                            s.ApplyBCards(target, BattleEntity);
                                            break;

                                        case BuffType.Good:
                                        case BuffType.Neutral:
                                            s.ApplyBCards(BattleEntity, BattleEntity);
                                            break;
                                    }
                                }
                            }
                        });

                        target.BCards.Where(s => s.CastType == 0).ForEach(s =>
                        {
                            if (s.Type == (byte)CardType.Buff)
                            {
                                Buff b = new Buff((short)s.SecondData, BattleEntity.Level);
                                if (b.Card != null)
                                {
                                    switch (b.Card?.BuffType)
                                    {
                                        case BuffType.Bad:
                                            s.ApplyBCards(BattleEntity, target);
                                            break;

                                        case BuffType.Good:
                                        case BuffType.Neutral:
                                            if (s.NpcMonsterVNum == null || firstHit)
                                            {
                                                s.ApplyBCards(target, target);
                                            }
                                            break;
                                    }
                                }
                            }
                        });

                        if (damage > 0)
                        {
                            target.Character?.RemoveBuffByBCardTypeSubType(new List<KeyValuePair<byte, byte>>
                            {
                                new KeyValuePair<byte, byte>((byte)CardType.SpecialActions, (byte)AdditionalTypes.SpecialActions.Hide)
                            });
                            target.RemoveBuff(36);
                            target.RemoveBuff(548);
                        }
                    }
                    if (target.Hp <= 0)
                    {
                        if (target.Character != null)
                        {
                            if (target.Character.IsVehicled)
                            {
                                target.Character.RemoveVehicle();
                            }
                            if (Owner?.Character != null)
                            {
                                Owner.Character.BattleEntity.ApplyScoreArena(target);
                                Owner.MapInstance?.Broadcast(Owner.Character.GenerateSay(
                                    string.Format(Language.Instance.GetMessageFromKey("PVP_KILL"),
                                        Owner.Character.Name, target.Character.Name), 10));
                                Observable.Timer(TimeSpan.FromMilliseconds(3000)).Subscribe(o =>
                                    ServerManager.Instance.AskPvpRevive((long)target.Character?.CharacterId));
                            }
                            else
                            {
                                Observable.Timer(TimeSpan.FromMilliseconds(3000)).Subscribe(o =>
                                    ServerManager.Instance.AskRevive((long)target.Character?.CharacterId));
                            }
                        }
                        else if (target.Mate != null)
                        {
                            if (target.Mate.IsTsProtected)
                            {
                                target.Mate.Owner.Session.SendPacket(target.Mate.Owner.GenerateSay(
                                    string.Format(Language.Instance.GetMessageFromKey("PET_DIED"), target.Mate.Name), 11));
                                target.Mate.Owner.Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                    string.Format(Language.Instance.GetMessageFromKey("PET_DIED"), target.Mate.Name), 0));
                            }
                        }
                        else if (target.MapMonster != null)
                        {
                            if (target.MapMonster.SetDeathStatement())
                            {
                                if (Owner?.Character != null)
                                {
                                    Owner.Character.GenerateKillBonus(target.MapMonster, BattleEntity);
                                }
                                else
                                {
                                    target.MapMonster.RunDeathEvent();
                                }
                            }
                        }
                        else if (target.MapNpc != null)
                        {
                            target.MapNpc.SetDeathStatement();
                            target.MapNpc.RunDeathEvent();
                        }
                        RemoveTarget();
                    }
                }
                else
                {
                    if (npcMonsterSkill != null && (npcMonsterSkill.Skill.TargetType == 1 && npcMonsterSkill.Skill.HitType == 1))
                    {
                        MapInstance.Broadcast(StaticPacketHelper.SkillUsed(UserType.Monster, MapMonsterId, (byte)BattleEntity.UserType, BattleEntity.MapEntityId,
                            npcMonsterSkill.SkillVNum, npcMonsterSkill.Skill.Cooldown,
                            npcMonsterSkill.Skill.AttackAnimation, npcMonsterSkill.Skill.Effect, MapX, MapY,
                            BattleEntity.Hp > 0,
                            (int)(BattleEntity.Hp / BattleEntity.HPLoad() * 100), 0,
                            0, 0));

                        if (npcMonsterSkill.Skill.Range > 0)
                        {
                            MapInstance.GetBattleEntitiesInRange(new MapCell { X = MapX, Y = MapY }, npcMonsterSkill.Skill.Range).ToList()
                                .ForEach(e =>
                                {
                                    npcMonsterSkill.Skill.BCards.ForEach(bc => bc.ApplyBCards(e, BattleEntity));
                                });
                        }
                    }
                }
            }

            if (npcMonsterSkill != null
                && SkillHelper.IsSelfAttack(npcMonsterSkill.SkillVNum))
            {
                return;
            }

            // In range entities

            short RangeBaseX = target.PositionX;
            short RangeBaseY = target.PositionY;

            if (npcMonsterSkill != null && npcMonsterSkill.Skill.HitType == 1 && npcMonsterSkill.Skill.TargetType == 1)
            {
                RangeBaseX = MapX;
                RangeBaseY = MapY;
            }

            if (npcMonsterSkill != null && (npcMonsterSkill.Skill.Range > 0 || npcMonsterSkill.Skill.TargetRange > 0))
            {
                bool onyxWings = false;
                foreach (Character characterInRange in MapInstance
                    .GetCharactersInRange(
                        npcMonsterSkill.Skill.TargetRange == 0 ? MapX : RangeBaseX,
                        npcMonsterSkill.Skill.TargetRange == 0 ? MapY : RangeBaseY,
                        skillRange,
                        attackGreaterDistance).Where(s => s.CharacterId != target.MapEntityId))
                {
                    if (!BattleEntity.CanAttackEntity(characterInRange.BattleEntity))
                    {
                        npcMonsterSkill.Skill.BCards.Where(s => s.Type == (byte)CardType.Buff).ToList().ForEach(s =>
                        {
                            if (new Buff((short)s.SecondData, Monster.Level) is Buff b)
                            {
                                switch (b.Card?.BuffType)
                                {
                                    case BuffType.Good:
                                    case BuffType.Neutral:
                                        s.ApplyBCards(characterInRange.BattleEntity, BattleEntity);
                                        break;
                                }
                            }
                        });
                    }
                    else
                    {
                        if (characterInRange.IsSitting)
                        {
                            characterInRange.IsSitting = false;
                            MapInstance.Broadcast(characterInRange.GenerateRest());
                        }

                        if (characterInRange.HasGodMode)
                        {
                            damage = 0;
                            hitmode = 4;
                        }

                        if (characterInRange.Hp > 0)
                        {
                            int dmg = DamageHelper.Instance.CalculateDamage(new BattleEntity(this), new BattleEntity(characterInRange, null), npcMonsterSkill.Skill, ref hitmode, ref onyxWings, attackGreaterDistance);
                            if (dmg >= characterInRange.Hp &&
                                Monster.BCards.Any(s => s.Type == (byte)CardType.NoDefeatAndNoDamage && s.SubType == (byte)AdditionalTypes.NoDefeatAndNoDamage.DecreaseHPNoKill / 10 && s.FirstData == 1))
                            {
                                dmg = characterInRange.Hp - 1;
                            }

                            if (hitmode != 4 && hitmode != 2)
                            {
                                bCards.Where(s => s.CastType == 1 || s.SkillVNum != null).ToList().ForEach(s =>
                                {
                                    if (s.Type != (byte)CardType.Buff && s.Type != (byte)CardType.Summons && s.Type != (byte)CardType.SummonSkill)
                                    {
                                        s.ApplyBCards(characterInRange.BattleEntity, BattleEntity);
                                    }
                                });

                                bCards.Where(s => s.CastType == 1 || s.SkillVNum != null).ToList().ForEach(s =>
                                {
                                    if (s.Type == (byte)CardType.Buff)
                                    {
                                        Buff b = new Buff((short)s.SecondData, Monster.Level);
                                        if (b.Card != null)
                                        {
                                            switch (b.Card?.BuffType)
                                            {
                                                case BuffType.Bad:
                                                    s.ApplyBCards(characterInRange.BattleEntity, BattleEntity);
                                                    break;

                                                case BuffType.Good:
                                                case BuffType.Neutral:
                                                    s.ApplyBCards(BattleEntity, BattleEntity);
                                                    break;
                                            }
                                        }
                                    }
                                });

                                characterInRange.BattleEntity.BCards.Where(s => s.CastType == 0).ForEach(s =>
                                {
                                    if (s.Type == (byte)CardType.Buff)
                                    {
                                        Buff b = new Buff((short)s.SecondData, BattleEntity.Level);
                                        if (b.Card != null)
                                        {
                                            switch (b.Card?.BuffType)
                                            {
                                                case BuffType.Bad:
                                                    s.ApplyBCards(BattleEntity, characterInRange.BattleEntity);
                                                    break;

                                                case BuffType.Good:
                                                case BuffType.Neutral:
                                                    s.ApplyBCards(characterInRange.BattleEntity, characterInRange.BattleEntity);
                                                    break;
                                            }
                                        }
                                    }
                                });
                            }

                            characterInRange.GetDamage(dmg, BattleEntity);
                            lock (DamageList)
                            {
                                if (!DamageList.Any(s => s.Key.MapEntityId == characterInRange.CharacterId))
                                {
                                    AddToAggroList(characterInRange.BattleEntity);
                                }
                            }
                            MapInstance.Broadcast(null, characterInRange.GenerateStat(), ReceiverType.OnlySomeone,
                                "", characterInRange.CharacterId);

                            switch (hitmode)
                            {
                                case 1:
                                case 4:
                                    hitmode = 7;
                                    break;

                                case 2:
                                    hitmode = 2;
                                    break;

                                case 3:
                                    hitmode = 6;
                                    break;

                                default:
                                    hitmode = 5;
                                    break;
                            }

                            MapInstance.Broadcast(npcMonsterSkill != null
                                ? StaticPacketHelper.SkillUsed(UserType.Monster, MapMonsterId, (byte)UserType.Player, characterInRange.CharacterId,
                                    npcMonsterSkill.SkillVNum, npcMonsterSkill.Skill.Cooldown,
                                    npcMonsterSkill.Skill.AttackAnimation, npcMonsterSkill.Skill.Effect, MapX, MapY,
                                    characterInRange.Hp > 0,
                                    (int)(characterInRange.Hp / characterInRange.HPLoad() * 100), dmg,
                                    hitmode, 0)
                                : StaticPacketHelper.SkillUsed(UserType.Monster, MapMonsterId, (byte)UserType.Player, characterInRange.CharacterId, 0,
                                    Monster.BasicCooldown, 11, Monster.BasicSkill, 0, 0, characterInRange.Hp > 0,
                                    (int)(characterInRange.Hp / characterInRange.HPLoad() * 100), dmg,
                                    hitmode, 0));

                            if (hitmode != 4 && hitmode != 2 && dmg > 0)
                            {
                                characterInRange.RemoveBuffByBCardTypeSubType(new List<KeyValuePair<byte, byte>>
                                {
                                    new KeyValuePair<byte, byte>((byte)CardType.SpecialActions, (byte)AdditionalTypes.SpecialActions.Hide)
                                });
                                characterInRange.RemoveBuff(36);
                                characterInRange.RemoveBuff(548);
                            }
                            if (characterInRange.Hp <= 0)
                            {
                                if (characterInRange.IsVehicled)
                                {
                                    characterInRange.RemoveVehicle();
                                }
                                if (Owner?.Character != null)
                                {
                                    Owner.Character.BattleEntity.ApplyScoreArena(characterInRange.BattleEntity);
                                    Owner.MapInstance?.Broadcast(Owner.Character.GenerateSay(
                                        string.Format(Language.Instance.GetMessageFromKey("PVP_KILL"),
                                            Owner.Character.Name, characterInRange?.Name), 10));
                                    Observable.Timer(TimeSpan.FromMilliseconds(3000)).Subscribe(o =>
                                    {
                                        ServerManager.Instance.AskPvpRevive((long)characterInRange?.CharacterId);
                                    });
                                }
                                else
                                {
                                    Observable.Timer(TimeSpan.FromMilliseconds(3000)).Subscribe(o =>
                                    {
                                        ServerManager.Instance.AskRevive((long)characterInRange?.CharacterId);
                                    });
                                }
                            }
                        }
                    }
                }

                foreach (Mate mateInRange in BattleEntity.MapInstance
                    .GetListMateInRange(
                        npcMonsterSkill.Skill.TargetRange == 0 ? BattleEntity.PositionX : RangeBaseX,
                        npcMonsterSkill.Skill.TargetRange == 0 ? BattleEntity.PositionY : RangeBaseY,
                        skillRange,
                        attackGreaterDistance).Where(s => s.MateTransportId != target.MapEntityId))
                {
                    if (!BattleEntity.CanAttackEntity(mateInRange.BattleEntity))
                    {
                        npcMonsterSkill.Skill.BCards.Where(s => s.Type == (byte)CardType.Buff).ToList().ForEach(s =>
                        {
                            if (new Buff((short)s.SecondData, Monster.Level) is Buff b)
                            {
                                switch (b.Card?.BuffType)
                                {
                                    case BuffType.Good:
                                    case BuffType.Neutral:
                                        s.ApplyBCards(mateInRange.BattleEntity, BattleEntity);
                                        break;
                                }
                            }
                        });
                    }
                    else
                    {
                        mateInRange.HitRequest(new HitRequest(TargetHitType.AOETargetHit, this, npcMonsterSkill));
                    }
                }

                foreach (MapMonster monsterInRange in MapInstance
                    .GetMonsterInRangeList(
                        npcMonsterSkill.Skill.TargetRange == 0 ? BattleEntity.PositionX : RangeBaseX,
                        npcMonsterSkill.Skill.TargetRange == 0 ? BattleEntity.PositionY : RangeBaseY,
                        skillRange,
                        attackGreaterDistance).Where(s => s.MapMonsterId != target.MapEntityId))
                {
                    if (!BattleEntity.CanAttackEntity(monsterInRange.BattleEntity))
                    {
                        npcMonsterSkill.Skill.BCards.Where(s => s.Type == (byte)CardType.Buff).ToList().ForEach(s =>
                        {
                            if (new Buff((short)s.SecondData, Monster.Level) is Buff b)
                            {
                                switch (b.Card?.BuffType)
                                {
                                    case BuffType.Good:
                                    case BuffType.Neutral:
                                        s.ApplyBCards(monsterInRange.BattleEntity, BattleEntity);
                                        break;
                                }
                            }
                        });
                    }
                    else
                    {
                        if (monsterInRange.CurrentHp > 0 && monsterInRange.Owner?.Character == null && monsterInRange.Owner?.Mate == null)
                        {
                            int dmg = DamageHelper.Instance.CalculateDamage(new BattleEntity(this), new BattleEntity(monsterInRange), npcMonsterSkill.Skill, ref hitmode, ref onyxWings, attackGreaterDistance);
                            if (dmg >= monsterInRange.CurrentHp &&
                                Monster.BCards.Any(s => s.Type == (byte)CardType.NoDefeatAndNoDamage && s.SubType == (byte)AdditionalTypes.NoDefeatAndNoDamage.DecreaseHPNoKill / 10 && s.FirstData == 1))
                            {
                                dmg = (int)monsterInRange.CurrentHp - 1;
                            }
                            if (dmg >= monsterInRange.CurrentHp &&
                                monsterInRange.Monster.BCards.Any(s => s.Type == (byte)CardType.NoDefeatAndNoDamage && s.SubType == (byte)AdditionalTypes.NoDefeatAndNoDamage.DecreaseHPNoDeath / 10 && s.FirstData == -1))
                            {
                                dmg = (int)monsterInRange.CurrentHp - 1;
                            }

                            bool firstHit = false;
                            lock (monsterInRange.DamageList)
                            {
                                if (!monsterInRange.DamageList.Any(s => s.Value > 0))
                                {
                                    firstHit = true;
                                }
                            }

                            if (monsterInRange.Target == null)
                            {
                                monsterInRange.Target = BattleEntity;
                            }

                            if (hitmode != 4 && hitmode != 2)
                            {
                                bCards.Where(s => s.CastType == 1 || s.SkillVNum != null).ToList().ForEach(s =>
                                {
                                    if (s.Type != (byte)CardType.Buff && s.Type != (byte)CardType.Summons && s.Type != (byte)CardType.SummonSkill)
                                    {
                                        s.ApplyBCards(monsterInRange.BattleEntity, BattleEntity);
                                    }
                                });

                                if (dmg > 0)
                                {
                                    monsterInRange.RemoveBuff(36);
                                    monsterInRange.RemoveBuff(548);
                                }

                                bCards.Where(s => s.CastType == 1 || s.SkillVNum != null).ToList().ForEach(s =>
                                {
                                    if (s.Type == (byte)CardType.Buff)
                                    {
                                        Buff b = new Buff((short)s.SecondData, Monster.Level);
                                        if (b.Card != null)
                                        {
                                            switch (b.Card?.BuffType)
                                            {
                                                case BuffType.Bad:
                                                    s.ApplyBCards(monsterInRange.BattleEntity, BattleEntity);
                                                    break;

                                                case BuffType.Good:
                                                case BuffType.Neutral:
                                                    s.ApplyBCards(BattleEntity, BattleEntity);
                                                    break;
                                            }
                                        }
                                    }
                                });

                                monsterInRange.BattleEntity.BCards.Where(s => s.CastType == 0).ForEach(s =>
                                {
                                    if (s.Type == (byte)CardType.Buff)
                                    {
                                        Buff b = new Buff((short)s.SecondData, BattleEntity.Level);
                                        if (b.Card != null)
                                        {
                                            switch (b.Card?.BuffType)
                                            {
                                                case BuffType.Bad:
                                                    s.ApplyBCards(BattleEntity, monsterInRange.BattleEntity);
                                                    break;

                                                case BuffType.Good:
                                                case BuffType.Neutral:
                                                    if (s.NpcMonsterVNum == null || firstHit)
                                                    {
                                                        s.ApplyBCards(monsterInRange.BattleEntity, monsterInRange.BattleEntity);
                                                    }
                                                    break;
                                            }
                                        }
                                    }
                                });
                            }

                            monsterInRange.BattleEntity.GetDamage(dmg, BattleEntity);
                            lock (DamageList)
                            {
                                if (!DamageList.Any(s => s.Key.MapEntityId == monsterInRange.MapMonsterId))
                                {
                                    AddToAggroList(monsterInRange.BattleEntity);
                                }
                            }
                            if (Owner != null)
                            {
                                lock (monsterInRange.DamageList)
                                {
                                    monsterInRange.AddToDamageList(Owner, dmg);
                                }
                            }

                            switch (hitmode)
                            {
                                case 1:
                                case 4:
                                    hitmode = 7;
                                    break;

                                case 2:
                                    hitmode = 2;
                                    break;

                                case 3:
                                    hitmode = 6;
                                    break;

                                default:
                                    hitmode = 5;
                                    break;
                            }

                            MapInstance.Broadcast(npcMonsterSkill != null
                                ? StaticPacketHelper.SkillUsed(UserType.Monster, MapMonsterId, (byte)UserType.Monster, monsterInRange.MapMonsterId,
                                    npcMonsterSkill.SkillVNum, npcMonsterSkill.Skill.Cooldown,
                                    npcMonsterSkill.Skill.AttackAnimation, npcMonsterSkill.Skill.Effect, MapX, MapY,
                                    monsterInRange.CurrentHp > 0,
                                    (int)(monsterInRange.CurrentHp / monsterInRange.MaxHp * 100), dmg,
                                    hitmode, 0)
                                : StaticPacketHelper.SkillUsed(UserType.Monster, MapMonsterId, (byte)UserType.Monster, monsterInRange.MapMonsterId, 0,
                                    Monster.BasicCooldown, 11, Monster.BasicSkill, 0, 0, monsterInRange.CurrentHp > 0,
                                    (int)(monsterInRange.CurrentHp / monsterInRange.MaxHp * 100), dmg,
                                    hitmode, 0));

                            if (monsterInRange.CurrentHp <= 0)
                            {
                                if (monsterInRange.SetDeathStatement())
                                {
                                    if (Owner?.Character != null)
                                    {
                                        Owner.Character.GenerateKillBonus(monsterInRange, BattleEntity);
                                    }
                                    else
                                    {
                                        monsterInRange.RunDeathEvent();
                                    }
                                }
                            }
                        }
                    }
                }

                foreach (MapNpc npcInRange in MapInstance
                    .GetListNpcInRange(
                        npcMonsterSkill.Skill.TargetRange == 0 ? BattleEntity.PositionX : RangeBaseX,
                        npcMonsterSkill.Skill.TargetRange == 0 ? BattleEntity.PositionY : RangeBaseY,
                        skillRange,
                        attackGreaterDistance).Where(s => s.MapNpcId != target.MapEntityId))
                {
                    if (!BattleEntity.CanAttackEntity(npcInRange.BattleEntity))
                    {
                        npcMonsterSkill.Skill.BCards.Where(s => s.Type == (byte)CardType.Buff).ToList().ForEach(s =>
                        {
                            if (new Buff((short)s.SecondData, Monster.Level) is Buff b)
                            {
                                switch (b.Card?.BuffType)
                                {
                                    case BuffType.Good:
                                    case BuffType.Neutral:
                                        s.ApplyBCards(npcInRange.BattleEntity, BattleEntity);
                                        break;
                                }
                            }
                        });
                    }
                    else
                    {
                        if (npcInRange.CurrentHp > 0)
                        {
                            int dmg = DamageHelper.Instance.CalculateDamage(new BattleEntity(this), new BattleEntity(npcInRange), npcMonsterSkill.Skill, ref hitmode, ref onyxWings, attackGreaterDistance);
                            if (dmg >= npcInRange.CurrentHp &&
                                Monster.BCards.Any(s => s.Type == (byte)CardType.NoDefeatAndNoDamage && s.SubType == (byte)AdditionalTypes.NoDefeatAndNoDamage.DecreaseHPNoKill / 10 && s.FirstData == 1))
                            {
                                dmg = (int)npcInRange.CurrentHp - 1;
                            }

                            if (hitmode != 4 && hitmode != 2)
                            {
                                bCards.Where(s => s.CastType == 1 || s.SkillVNum != null).ToList().ForEach(s =>
                                {
                                    if (s.Type != (byte)CardType.Buff && s.Type != (byte)CardType.Summons && s.Type != (byte)CardType.SummonSkill)
                                    {
                                        s.ApplyBCards(npcInRange.BattleEntity, BattleEntity);
                                    }
                                });

                                if (dmg > 0)
                                {
                                    npcInRange.RemoveBuff(36);
                                    npcInRange.RemoveBuff(548);
                                }

                                bCards.Where(s => s.CastType == 1 || s.SkillVNum != null).ToList().ForEach(s =>
                                {
                                    if (s.Type == (byte)CardType.Buff)
                                    {
                                        Buff b = new Buff((short)s.SecondData, Monster.Level);
                                        if (b.Card != null)
                                        {
                                            switch (b.Card?.BuffType)
                                            {
                                                case BuffType.Bad:
                                                    s.ApplyBCards(npcInRange.BattleEntity, BattleEntity);
                                                    break;

                                                case BuffType.Good:
                                                case BuffType.Neutral:
                                                    s.ApplyBCards(BattleEntity, BattleEntity);
                                                    break;
                                            }
                                        }
                                    }
                                });

                                npcInRange.BattleEntity.BCards.Where(s => s.CastType == 0).ForEach(s =>
                                {
                                    if (s.Type == (byte)CardType.Buff)
                                    {
                                        Buff b = new Buff((short)s.SecondData, BattleEntity.Level);
                                        if (b.Card != null)
                                        {
                                            switch (b.Card?.BuffType)
                                            {
                                                case BuffType.Bad:
                                                    s.ApplyBCards(BattleEntity, npcInRange.BattleEntity);
                                                    break;

                                                case BuffType.Good:
                                                case BuffType.Neutral:
                                                    s.ApplyBCards(npcInRange.BattleEntity, npcInRange.BattleEntity);
                                                    break;
                                            }
                                        }
                                    }
                                });
                            }

                            npcInRange.BattleEntity.GetDamage(dmg, BattleEntity);

                            if (npcInRange.Target == -1)
                            {
                                npcInRange.Target = MapMonsterId;
                            }
                            lock (DamageList)
                            {
                                if (!DamageList.Any(s => s.Key.MapEntityId == npcInRange.MapNpcId))
                                {
                                    AddToAggroList(npcInRange.BattleEntity);
                                }
                            }

                            switch (hitmode)
                            {
                                case 1:
                                case 4:
                                    hitmode = 7;
                                    break;

                                case 2:
                                    hitmode = 2;
                                    break;

                                case 3:
                                    hitmode = 6;
                                    break;

                                default:
                                    hitmode = 5;
                                    break;
                            }

                            MapInstance.Broadcast(npcMonsterSkill != null
                                ? StaticPacketHelper.SkillUsed(UserType.Monster, MapMonsterId, (byte)UserType.Npc, npcInRange.MapNpcId,
                                    npcMonsterSkill.SkillVNum, npcMonsterSkill.Skill.Cooldown,
                                    npcMonsterSkill.Skill.AttackAnimation, npcMonsterSkill.Skill.Effect, MapX, MapY,
                                    npcInRange.CurrentHp > 0,
                                    (int)(npcInRange.CurrentHp / npcInRange.MaxHp * 100), dmg,
                                    hitmode, 0)
                                : StaticPacketHelper.SkillUsed(UserType.Monster, MapMonsterId, (byte)UserType.Npc, npcInRange.MapNpcId, 0,
                                    Monster.BasicCooldown, 11, Monster.BasicSkill, 0, 0, npcInRange.CurrentHp > 0,
                                    (int)(npcInRange.CurrentHp / npcInRange.MaxHp * 100), dmg,
                                    hitmode, 0));

                            if (npcInRange.CurrentHp <= 0)
                            {
                                npcInRange.SetDeathStatement();
                                npcInRange.RunDeathEvent();
                            }
                        }
                    }
                }
            }
        }

        #endregion
    }
}