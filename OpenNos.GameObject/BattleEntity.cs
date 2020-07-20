using OpenNos.Core;
using OpenNos.Data;
using OpenNos.Domain;
using OpenNos.GameObject.Event;
using OpenNos.GameObject.Helpers;
using OpenNos.GameObject.Networking;
using OpenNos.PathFinder;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using OpenNos.Core.ConcurrencyExtensions;
using static OpenNos.Domain.BCardType;
using System.Threading.Tasks;
using System.Threading;

namespace OpenNos.GameObject
{
    public class BattleEntity
    {
        #region Instantiation

        public BattleEntity(Character character, Skill skill)
        {
            Character = character;

            if (character.BattleEntity != null)
            {
                Buffs = character.Buff;
                BuffObservables = character.BuffObservables;
                BCardDisposables = character.BattleEntity.BCardDisposables;
                BCards = character.EquipmentBCards;
                CellonOptions = character.CellonOptions;
                OnDeathEvents = character.OnDeathEvents;
            }
            else
            {
                Buffs = new ThreadSafeSortedList<short, Buff>();
                BuffObservables = new ThreadSafeSortedList<short, IDisposable>();
                BCardDisposables = new ThreadSafeSortedList<int, IDisposable>();
                BCards = new ThreadSafeGenericLockedList<BCard>();
                CellonOptions = new ThreadSafeGenericList<CellonOptionDTO>();
                OnDeathEvents = new List<EventContainer>();
            }

            EntityType = EntityType.Player;
            UserType = UserType.Player;

            DamageMinimum = character.MinHit;
            DamageMaximum = character.MaxHit;
            Hitrate = character.HitRate;
            CritChance = character.HitCriticalChance;
            CritRate = character.HitCriticalRate;
            Morale = character.Level;
            FireResistance = character.FireResistance;
            WaterResistance = character.WaterResistance;
            LightResistance = character.LightResistance;
            ShadowResistance = character.DarkResistance;

            ItemInstance weapon = null;

            if (skill != null)
            {
                switch (skill.Type)
                {
                    case 0:
                        AttackType = AttackType.Melee;
                        if (character.Class == ClassType.Archer)
                        {
                            DamageMinimum = character.SecondWeaponMinHit;
                            DamageMaximum = character.SecondWeaponMaxHit;
                            Hitrate = character.SecondWeaponHitRate;
                            CritChance = character.SecondWeaponCriticalChance;
                            CritRate = character.SecondWeaponCriticalRate;
                            weapon = character.Inventory.LoadBySlotAndType((byte)EquipmentType.SecondaryWeapon, InventoryType.Wear);
                        }
                        else
                        {
                            weapon = character.Inventory.LoadBySlotAndType((byte)EquipmentType.MainWeapon, InventoryType.Wear);
                        }
                        break;

                    case 1:
                        AttackType = AttackType.Range;
                        if (character.Class == ClassType.Adventurer || character.Class == ClassType.Swordsman || character.Class == ClassType.Magician)
                        {
                            DamageMinimum = character.SecondWeaponMinHit;
                            DamageMaximum = character.SecondWeaponMaxHit;
                            Hitrate = character.SecondWeaponHitRate;
                            CritChance = character.SecondWeaponCriticalChance;
                            CritRate = character.SecondWeaponCriticalRate;
                            weapon = character.Inventory.LoadBySlotAndType((byte)EquipmentType.SecondaryWeapon, InventoryType.Wear);
                        }
                        else
                        {
                            weapon = character.Inventory.LoadBySlotAndType((byte)EquipmentType.MainWeapon, InventoryType.Wear);
                        }
                        break;

                    case 2:
                        AttackType = AttackType.Magical;
                        weapon = character.Inventory.LoadBySlotAndType((byte)EquipmentType.MainWeapon, InventoryType.Wear);
                        break;

                    case 3:
                        weapon = character.Inventory.LoadBySlotAndType((byte)EquipmentType.MainWeapon, InventoryType.Wear);
                        switch (character.Class)
                        {
                            case ClassType.Adventurer:
                            case ClassType.Swordsman:
                            case ClassType.MartialArtist:
                                AttackType = AttackType.Melee;
                                break;

                            case ClassType.Archer:
                                AttackType = AttackType.Range;
                                break;

                            case ClassType.Magician:
                                AttackType = AttackType.Magical;
                                break;
                        }
                        break;

                    case 5:
                        AttackType = AttackType.Melee;
                        switch (character.Class)
                        {
                            case ClassType.Adventurer:
                            case ClassType.Swordsman:
                            case ClassType.Magician:
                            case ClassType.MartialArtist:
                                weapon = character.Inventory.LoadBySlotAndType((byte)EquipmentType.MainWeapon, InventoryType.Wear);
                                break;

                            case ClassType.Archer:
                                DamageMinimum = character.SecondWeaponMinHit;
                                DamageMaximum = character.SecondWeaponMaxHit;
                                Hitrate = character.SecondWeaponHitRate;
                                CritChance = character.SecondWeaponCriticalChance;
                                CritRate = character.SecondWeaponCriticalRate;
                                weapon = character.Inventory.LoadBySlotAndType((byte)EquipmentType.SecondaryWeapon, InventoryType.Wear);
                                break;
                        }
                        break;
                }
            }
            else
            {
                weapon = character.Inventory?.LoadBySlotAndType((byte)EquipmentType.SecondaryWeapon, InventoryType.Wear);
                switch (character.Class)
                {
                    case ClassType.Adventurer:
                    case ClassType.Swordsman:
                    case ClassType.MartialArtist:
                        AttackType = AttackType.Melee;
                        break;

                    case ClassType.Archer:
                        AttackType = AttackType.Range;
                        break;

                    case ClassType.Magician:
                        AttackType = AttackType.Magical;
                        break;
                }
            }

            if (weapon != null)
            {
                AttackUpgrade = weapon.Upgrade;
                WeaponDamageMinimum = weapon.DamageMinimum + weapon.Item.DamageMinimum;
                WeaponDamageMaximum = weapon.DamageMaximum + weapon.Item.DamageMaximum;

                if (weapon == character.Inventory.LoadBySlotAndType((byte)EquipmentType.MainWeapon, InventoryType.Wear))
                {
                    ShellWeaponEffects = character.ShellEffectMain.ToList();
                }
                else
                {
                    ShellWeaponEffects = character.ShellEffectSecondary.ToList();
                }
            }

            DamageMinimum -= WeaponDamageMinimum;
            DamageMaximum -= WeaponDamageMaximum;

            if (DamageMaximum <= DamageMinimum)
            {
                DamageMaximum = DamageMinimum + 1;
            }

            ItemInstance armor = character.Inventory?.LoadBySlotAndType((byte)EquipmentType.Armor, InventoryType.Wear);
            if (armor != null)
            {
                DefenseUpgrade = armor.Upgrade;
                ArmorMeleeDefense = armor.CloseDefence + armor.Item.CloseDefence;
                ArmorRangeDefense = armor.DistanceDefence + armor.Item.DistanceDefence;
                ArmorMagicalDefense = armor.MagicDefence + armor.Item.MagicDefence;

                ShellArmorEffects = character.ShellEffectArmor.ToList();
            }

            MeleeDefense = character.Defence - ArmorMeleeDefense;
            MeleeDefenseDodge = character.DefenceRate;
            RangeDefense = character.DistanceDefence - ArmorRangeDefense;
            RangeDefenseDodge = character.DistanceDefenceRate;
            MagicalDefense = character.MagicalDefence - ArmorMagicalDefense;
            Element = character.Element;
            ElementRate = character.ElementRate + character.ElementRateSP;
        }

        public BattleEntity(Mate mate)
        {
            Mate = mate;

            if (mate.BattleEntity != null)
            {
                Buffs = mate.Buff;
                BuffObservables = mate.BuffObservables;
                BCardDisposables = mate.BattleEntity.BCardDisposables;
                BCards = mate.BattleEntity.BCards;
                OnDeathEvents = mate.OnDeathEvents;
            }
            else
            {
                Buffs = new ThreadSafeSortedList<short, Buff>();
                BuffObservables = new ThreadSafeSortedList<short, IDisposable>();
                BCardDisposables = new ThreadSafeSortedList<int, IDisposable>();
                BCards = new ThreadSafeGenericLockedList<BCard>(mate.Monster.BCards);
                OnDeathEvents = new List<EventContainer>();
            }

            //Add Partner EquipmentBCards

            EntityType = EntityType.Mate;
            UserType = UserType.Npc;
            DamageMinimum = mate.DamageMinimum;
            DamageMaximum = mate.DamageMaximum;
            WeaponDamageMinimum = (mate.WeaponInstance?.Item.DamageMinimum ?? 0);
            WeaponDamageMaximum = (mate.WeaponInstance?.Item.DamageMaximum ?? 0);
            Hitrate = mate.Concentrate + (mate.WeaponInstance?.Item.HitRate ?? 0);
            CritChance = mate.Monster.CriticalChance + (mate.WeaponInstance?.Item.CriticalLuckRate ?? 0);
            CritRate = mate.Monster.CriticalRate + (mate.WeaponInstance?.Item.CriticalRate ?? 0);
            Morale = mate.Level;
            AttackUpgrade = mate.WeaponInstance?.Upgrade ?? mate.Attack;
            FireResistance = mate.Monster.FireResistance + (mate.GlovesInstance?.FireResistance ?? 0) + (mate.GlovesInstance?.Item.FireResistance ?? 0) + (mate.BootsInstance?.FireResistance ?? 0) + (mate.BootsInstance?.Item.FireResistance ?? 0);
            WaterResistance = mate.Monster.WaterResistance + (mate.GlovesInstance?.WaterResistance ?? 0) + (mate.GlovesInstance?.Item.WaterResistance ?? 0) + (mate.BootsInstance?.WaterResistance ?? 0) + (mate.BootsInstance?.Item.WaterResistance ?? 0);
            LightResistance = mate.Monster.LightResistance + (mate.GlovesInstance?.LightResistance ?? 0) + (mate.GlovesInstance?.Item.LightResistance ?? 0) + (mate.BootsInstance?.LightResistance ?? 0) + (mate.BootsInstance?.Item.LightResistance ?? 0);
            ShadowResistance = mate.Monster.DarkResistance + (mate.GlovesInstance?.DarkResistance ?? 0) + (mate.GlovesInstance?.Item.DarkResistance ?? 0) + (mate.BootsInstance?.DarkResistance ?? 0) + (mate.BootsInstance?.Item.DarkResistance ?? 0);
            AttackType = (AttackType)mate.Monster.AttackClass;

            DefenseUpgrade = mate.ArmorInstance?.Upgrade ?? mate.Defence;
            MeleeDefense = mate.MeleeDefense;
            RangeDefense = mate.RangeDefense;
            MagicalDefense = mate.MagicalDefense;
            MeleeDefenseDodge = (mate.ArmorInstance?.Item.DefenceDodge ?? 0) + mate.MeleeDefenseDodge + (mate.GlovesInstance?.Item.DefenceDodge ?? 0) + (mate.BootsInstance?.Item.DefenceDodge ?? 0);
            RangeDefenseDodge = (mate.ArmorInstance?.Item.DistanceDefenceDodge ?? 0) + mate.RangeDefenseDodge + (mate.GlovesInstance?.Item.DistanceDefenceDodge ?? 0) + (mate.BootsInstance?.Item.DistanceDefenceDodge ?? 0);

            ArmorMeleeDefense = (mate.ArmorInstance?.Item.CloseDefence ?? 0) + (mate.GlovesInstance?.Item.CloseDefence ?? 0) + (mate.BootsInstance?.Item.CloseDefence ?? 0);
            ArmorRangeDefense = (mate.ArmorInstance?.Item.DistanceDefence ?? 0) + (mate.GlovesInstance?.Item.DistanceDefence ?? 0) + (mate.BootsInstance?.Item.DistanceDefence ?? 0);
            ArmorMagicalDefense = (mate.ArmorInstance?.Item.MagicDefence ?? 0) + (mate.GlovesInstance?.Item.MagicDefence ?? 0) + (mate.BootsInstance?.Item.MagicDefence ?? 0);

            Element = (byte)(mate.MateType == MateType.Pet ? mate.Monster.Element : (mate.IsUsingSp ? mate.Sp.Instance.Item.Element : 0));
            ElementRate = mate.Monster.ElementRate;
        }

        public BattleEntity(MapMonster monster)
        {
            MapMonster = monster;

            if (monster.BattleEntity != null)
            {
                Buffs = monster.Buff;
                BuffObservables = monster.BuffObservables;
                BCardDisposables = monster.BattleEntity.BCardDisposables;
                OnDeathEvents = monster.OnDeathEvents;
            }
            else
            {
                Buffs = new ThreadSafeSortedList<short, Buff>();
                BuffObservables = new ThreadSafeSortedList<short, IDisposable>();
                BCardDisposables = new ThreadSafeSortedList<int, IDisposable>();
                OnDeathEvents = new List<EventContainer>();
            }

            BCards = new ThreadSafeGenericLockedList<BCard>(monster.Monster.BCards);

            EntityType = EntityType.Monster;
            UserType = UserType.Monster;
            if (monster.Owner?.Mate != null)
            {
                DamageMinimum = monster.Owner.Mate.DamageMinimum;
                DamageMaximum = monster.Owner.Mate.DamageMaximum;
            }
            else
            {
                DamageMinimum = monster.Monster.DamageMinimum;
                DamageMaximum = monster.Monster.DamageMaximum;
            }
            WeaponDamageMinimum = 0;
            WeaponDamageMaximum = 0;
            Hitrate = monster.Monster.Concentrate;
            CritChance = monster.Monster.CriticalChance;
            CritRate = monster.Monster.CriticalRate;
            Morale = monster.Monster.Level;
            AttackUpgrade = monster.Monster.AttackUpgrade;
            FireResistance = monster.Monster.FireResistance;
            WaterResistance = monster.Monster.WaterResistance;
            LightResistance = monster.Monster.LightResistance;
            ShadowResistance = monster.Monster.DarkResistance;
            AttackType = (AttackType)monster.Monster.AttackClass;
            DefenseUpgrade = monster.Monster.DefenceUpgrade;
            MeleeDefense = monster.Monster.CloseDefence;
            MeleeDefenseDodge = monster.Monster.DefenceDodge;
            RangeDefense = monster.Monster.DistanceDefence;
            RangeDefenseDodge = monster.Monster.DistanceDefenceDodge;
            MagicalDefense = monster.Monster.MagicDefence;
            ArmorMeleeDefense = 0;
            ArmorRangeDefense = 0;
            ArmorMagicalDefense = 0;
            Element = monster.Monster.Element;
            ElementRate = monster.Monster.ElementRate;
            Death = monster.Death;
        }


        public BattleEntity(MapNpc npc)
        {
            MapNpc = npc;

            if (npc.BattleEntity != null)
            {
                Buffs = npc.Buff;
                BuffObservables = npc.BuffObservables;
                BCardDisposables = npc.BattleEntity.BCardDisposables;
                BCards = new ThreadSafeGenericLockedList<BCard>(npc.Npc.BCards);
                OnDeathEvents = npc.OnDeathEvents;
            }
            else
            {
                Buffs = new ThreadSafeSortedList<short, Buff>();
                BuffObservables = new ThreadSafeSortedList<short, IDisposable>();
                BCardDisposables = new ThreadSafeSortedList<int, IDisposable>();
                BCards = new ThreadSafeGenericLockedList<BCard>();
                OnDeathEvents = new List<EventContainer>();
            }

            //npc.Buff.CopyTo(Buffs);
            EntityType = EntityType.Npc;
            UserType = UserType.Npc;
            DamageMinimum = 0;
            DamageMaximum = 0;
            WeaponDamageMinimum = npc.Npc.DamageMinimum;
            WeaponDamageMaximum = npc.Npc.DamageMaximum;
            Hitrate = npc.Npc.Concentrate;
            CritChance = npc.Npc.CriticalChance;
            CritRate = npc.Npc.CriticalRate;
            Morale = npc.Npc.Level;
            AttackUpgrade = npc.Npc.AttackUpgrade;
            FireResistance = npc.Npc.FireResistance;
            WaterResistance = npc.Npc.WaterResistance;
            LightResistance = npc.Npc.LightResistance;
            ShadowResistance = npc.Npc.DarkResistance;
            AttackType = (AttackType)npc.Npc.AttackClass;
            DefenseUpgrade = npc.Npc.DefenceUpgrade;
            MeleeDefense = npc.Npc.CloseDefence;
            MeleeDefenseDodge = npc.Npc.DefenceDodge;
            RangeDefense = npc.Npc.DistanceDefence;
            RangeDefenseDodge = npc.Npc.DistanceDefenceDodge;
            MagicalDefense = npc.Npc.MagicDefence;
            ArmorMeleeDefense = npc.Npc.CloseDefence;
            ArmorRangeDefense = npc.Npc.DistanceDefence;
            ArmorMagicalDefense = npc.Npc.MagicDefence;
            Element = npc.Npc.Element;
            ElementRate = npc.Npc.ElementRate;
        }

        #endregion

        #region Properties

        public Character Character { get; set; }

        public Mate Mate { get; set; }

        public MapMonster MapMonster { get; set; }

        public MapNpc MapNpc { get; set; }

        public double AdditionalHp { get; set; }

        public double AdditionalMp { get; set; }

        public int ArmorDefense { get; set; }

        public int ArmorMagicalDefense { get; }

        public int ArmorMeleeDefense { get; }

        public int ArmorRangeDefense { get; }

        public AttackType AttackType { get; }

        public short AttackUpgrade { get; set; }

        public ThreadSafeGenericLockedList<BCard> BCards { get; }

        public Node[][] BrushFireJagged
        {
            get
            {
                if (Character != null) return Character.BrushFireJagged;
                else if (Mate != null) return Mate.BrushFireJagged;
                else if (MapMonster != null) return MapMonster.BrushFireJagged;
                else if (MapNpc != null) return MapNpc.BrushFireJagged;
                else return null;
            }
            set
            {
                if (Character != null) Character.BrushFireJagged = value;
                else if (Mate != null) Mate.BrushFireJagged = value;
                else if (MapMonster != null) MapMonster.BrushFireJagged = value;
                else if (MapNpc != null) MapNpc.BrushFireJagged = value;
            }
        }

        public ThreadSafeSortedList<short, Buff> Buffs { get; set; }

        public ThreadSafeSortedList<short, IDisposable> BuffObservables { get; set; }

        public ThreadSafeSortedList<int, IDisposable> BCardDisposables { get; set; }

        public ThreadSafeGenericList<CellonOptionDTO> CellonOptions { get; set; }

        public int CritChance { get; set; }

        public int CritRate { get; set; }

        public int DamageMaximum { get; }

        public int DamageMinimum { get; }

        public DateTime Death { get; set; }

        public int Defense { get; set; }

        public short DefenseUpgrade { get; set; }

        public int Dodge { get; set; }

        public byte Element { get; }

        public int ElementRate { get; }

        public EntityType EntityType { get; }

        public int FireResistance { get; set; }

        public int Hitrate { get; }

        public int Hp
        {
            get
            {
                if (Character != null) return Character.Hp;
                else if (Mate != null) return (int)Mate.Hp;
                else if (MapMonster != null) return (int)MapMonster.CurrentHp;
                else if (MapNpc != null) return (int)MapNpc.CurrentHp;
                else return 0;
            }
            set
            {
                if (Character != null) Character.Hp = value;
                else if (Mate != null) Mate.Hp = value;
                else if (MapMonster != null) MapMonster.CurrentHp = value;
                else if (MapNpc != null) MapNpc.CurrentHp = value;
            }
        }

        public int HpMax
        {
            get
            {
                if (Character != null) return (int)Character.HPLoad();
                else if (Mate != null) return (int)Mate.MaxHp;
                else if (MapMonster != null) return (int)MapMonster.MaxHp;
                else if (MapNpc != null) return (int)MapNpc.MaxHp;
                else return 0;
            }
        }

        public bool Invincible { get; set; }

        public DateTime LastDefence
        {
            get
            {
                if (Character != null) return Character.LastDefence;
                else if (Mate != null) return Mate.LastDefence;
                else if (MapMonster != null) return MapMonster.LastDefence;
                else if (MapNpc != null) return MapNpc.LastDefence;
                else return new DateTime();
            }
            set
            {
                if (Character != null) Character.LastDefence = value;
                else if (Mate != null) Mate.LastDefence = value;
                else if (MapMonster != null) MapMonster.LastDefence = value;
                else if (MapNpc != null) MapNpc.LastDefence = value;
            }
        }

        public int Level => Character?.Level ?? Mate?.Level ?? MapMonster?.Monster?.Level ?? MapNpc?.Npc?.Level ?? 0;

        public int LightResistance { get; set; }

        public int MagicalDefense { get; }

        public long MapEntityId
        {
            get
            {
                if (Character != null) return Character.CharacterId;
                else if (Mate != null) return Mate.MateTransportId;
                else if (MapMonster != null) return MapMonster.MapMonsterId;
                else if (MapNpc != null) return MapNpc.MapNpcId;
                else return 0;
            }
        }

        public MapInstance MapInstance => Character?.MapInstance ?? Mate?.Owner?.MapInstance ?? MapMonster?.MapInstance ?? MapNpc?.MapInstance;

        public int MeleeDefense { get; }

        public int MeleeDefenseDodge { get; }

        public int Morale { get; set; }

        public int Mp
        {
            get
            {
                if (Character != null) return Character.Mp;
                else if (Mate != null) return (int)Mate.Mp;
                else if (MapMonster != null) return (int)MapMonster.CurrentMp;
                else if (MapNpc != null) return (int)MapNpc.CurrentMp;
                else return 0;
            }
            set
            {
                if (Character != null) Character.Mp = value;
                else if (Mate != null) Mate.Mp = value;
                else if (MapMonster != null) MapMonster.CurrentMp = value;
                else if (MapNpc != null) MapNpc.CurrentMp = value;
            }
        }

        public int MpMax
        {
            get
            {
                if (Character != null) return (int)Character.MPLoad();
                else if (Mate != null) return (int)Mate.MaxMp;
                else if (MapMonster != null) return (int)MapMonster.MaxMp;
                else if (MapNpc != null) return (int)MapNpc.MaxMp;
                else return 0;
            }
        }
        public DateTime LastMonsterAggro
        {
            get
            {
                if (Character != null) return Character.LastMonsterAggro;
                else if (Mate != null) return Mate.LastMonsterAggro;
                else if (MapMonster != null) return MapMonster.LastMonsterAggro;
                else if (MapNpc != null) return MapNpc.LastMonsterAggro;
                else return new DateTime();
            }
            set
            {
                if (Character != null) Character.LastMonsterAggro = value;
                else if (Mate != null) Mate.LastMonsterAggro = value;
                else if (MapMonster != null) MapMonster.LastMonsterAggro = value;
                else if (MapNpc != null) MapNpc.LastMonsterAggro = value;
            }
        }
        public short PositionX
        {
            get
            {
                if (Character != null) return Character.PositionX;
                else if (Mate != null) return Mate.PositionX;
                else if (MapMonster != null) return MapMonster.MapX;
                else if (MapNpc != null) return MapNpc.MapX;
                else return 0;
            }
            set
            {
                if (Character != null) Character.PositionX = value;
                else if (Mate != null) Mate.PositionX = value;
                else if (MapMonster != null) MapMonster.MapX = value;
                else if (MapNpc != null) MapNpc.MapX = value;
            }
        }

        public short PositionY
        {
            get
            {
                if (Character != null) return Character.PositionY;
                else if (Mate != null) return Mate.PositionY;
                else if (MapMonster != null) return MapMonster.MapY;
                else if (MapNpc != null) return MapNpc.MapY;
                else return 0;
            }
            set
            {
                if (Character != null) Character.PositionY = value;
                else if (Mate != null) Mate.PositionY = value;
                else if (MapMonster != null) MapMonster.MapY = value;
                else if (MapNpc != null) MapNpc.MapY = value;
            }
        }

        public object PVELockObject
        {
            get
            {
                if (Character != null) return Character.PVELockObject;
                else if (Mate != null) return Mate.PVELockObject;
                else if (MapMonster != null) return MapMonster.PVELockObject;
                else if (MapNpc != null) return MapNpc.PVELockObject;
                else return 0;
            }
            set
            {
                if (Character != null) Character.PVELockObject = value;
                else if (Mate != null) Mate.PVELockObject = value;
                else if (MapMonster != null) MapMonster.PVELockObject = value;
                else if (MapNpc != null) MapNpc.PVELockObject = value;
            }
        }

        public int RangeDefense { get; }

        public int RangeDefenseDodge { get; }

        public int Resistance { get; set; }

        public int ShadowResistance { get; set; }

        public List<ShellEffectDTO> ShellArmorEffects { get; }

        public List<ShellEffectDTO> ShellWeaponEffects { get; }

        public UserType UserType { get; }

        public int WaterResistance { get; set; }

        public int WeaponDamageMaximum { get; }

        public int WeaponDamageMinimum { get; }

        public long FalconFocusedEntityId { get; set; }

        public int ResistForcedMovement => GetBuff(CardType.AbsorbedSpirit, (byte)AdditionalTypes.AbsorbedSpirit.ResistForcedMovement)[0];

        public bool CanBeTargetted
        {
            get
            {
                return
                    MapInstance?.MapInstanceType != MapInstanceType.BaseMapInstance ||
                    TargettedByMonstersList(false)?.Count < MaxTargetedByMonstersCount(false) &&
                    TargettedByMonstersList(true)?.Count < MaxTargetedByMonstersCount(true);
            }
        }

        public List<BattleEntity> TargettedByMonstersList(bool teamCheck)
        {
            if (!teamCheck)
            {
                return MapInstance?.Monsters.Where(s => s?.Target?.MapEntityId == MapEntityId && s.Target?.EntityType == EntityType).Select(s => s.BattleEntity).ToList();
            }
            else
            {
                List<BattleEntity> targettedByMonsters = new List<BattleEntity>();
                if (Mate?.Owner != null)
                {
                    targettedByMonsters = Mate.Owner.BattleEntity.TargettedByMonstersList(true);
                }
                else
                {
                    targettedByMonsters = MapInstance?.Monsters.Where(s => s?.Target?.MapEntityId == MapEntityId && s.Target?.EntityType == EntityType).Select(s => s.BattleEntity).ToList();
                    if (Character != null)
                    {
                        Character?.Mates.Where(s => s.IsTeamMember).ToList().ForEach(m => targettedByMonsters?.AddRange(MapInstance?.Monsters?.Where(s => s?.Target?.MapEntityId == m.BattleEntity.MapEntityId && s.Target?.EntityType == m.BattleEntity.EntityType).Select(s => s.BattleEntity).ToList()));
                    }
                }
                return targettedByMonsters;
            }
        }
        public int MaxTargetedByMonstersCount(bool teamCheck)
        {
            if (MapInstance?.MapInstanceType == MapInstanceType.BaseMapInstance)
            {
                if (teamCheck)
                {
                    switch (EntityType)
                    {
                        case EntityType.Player:
                            return (1 + Character.Mates.Count(m => m.IsTeamMember || m.IsTemporalMate)) * 50000;

                        case EntityType.Mate:
                            return (1 + Mate.Owner.Mates.Count(m => m.IsTeamMember || m.IsTemporalMate)) * 50000;
                    }
                }
                else
                {
                    return 10;
                }
            }
            return int.MaxValue;
        }

        #endregion

        #region Methods

        public int GetDistance(BattleEntity other)
        {
            return (int)Math.Sqrt(Math.Pow(other.PositionX - PositionX, 2) + Math.Pow(other.PositionY - PositionY, 2));
        }

        public int HpPercent() => (int)((double)Hp / (double)HpMax * 100D);

        public int MpPercent() => (int)((double)Mp / (double)MpMax * 100D);

        public MapCell GetRandomMapCellInRange(short numberOfCells)
        {
            if (MapInstance?.Map == null)
            {
                return null;
            }

            List<MapCell> walkableCellsInRange = new List<MapCell>();

            while (numberOfCells > 0)
            {
                for (int dX = -1; dX <= 1; dX++)
                {
                    for (int dY = -1; dY <= 1; dY++)
                    {
                        if (dX == 0 && dY == 0)
                        {
                            continue;
                        }

                        short x = (short)(PositionX + (dX * numberOfCells));
                        short y = (short)(PositionY + (dY * numberOfCells));

                        if (!MapInstance.Map.IsBlockedZone(x, y) && MapInstance.Map.CanWalkAround(x, y))
                        {
                            walkableCellsInRange.Add(new MapCell { X = x, Y = y });
                        }
                    }
                }

                numberOfCells--;
            }

            return walkableCellsInRange
                .OrderBy(s => ServerManager.RandomNumber(0, 1000))
                .FirstOrDefault();
        }

        public void AddBuff(Buff indicator, BattleEntity sender, bool noMessage = false, short x = 0, short y = 0, bool forced = false)
        {
            if (indicator.Card != null)
            {
                indicator.Level = sender.MapMonster?.Owner?.Level ?? sender.Level;

                indicator.Sender = sender;

                if ((Character != null && ((indicator.Card.BuffType == BuffType.Bad && Character.HasGodMode) || Character.InvisibleGm))
                 || (Mate != null && ((indicator.Card.BuffType == BuffType.Bad && Mate.Owner.HasGodMode) || Mate.Owner.InvisibleGm)))
                {
                    return;
                }
                if (Character != null && indicator.Card.BuffType == BuffType.Bad && Character.HasBuff(CardType.NoDefeatAndNoDamage, (byte)AdditionalTypes.NoDefeatAndNoDamage.TransferAttackPower))
                {
                    return;
                }

                if (MapMonster != null && (MapMonster.IsBoss || ServerManager.Instance.BossVNums.Contains(MapMonster.MonsterVNum)))
                {
                    if (!forced && (indicator.Card.BuffType == BuffType.Bad &&
                       (indicator.Card.BCards.Any(b => b.Type == (byte)CardType.SpecialAttack && b.SubType == (byte)AdditionalTypes.SpecialAttack.NoAttack / 10)
                     || indicator.Card.BCards.Any(b => b.Type == (byte)CardType.Move && b.SubType == (byte)AdditionalTypes.Move.MovementImpossible / 10)
                     || indicator.Card.BCards.Any(b => b.Type == (byte)CardType.Move && b.SubType == (byte)AdditionalTypes.Move.SetMovement / 10)
                     || indicator.Card.BCards.Any(b => b.Type == (byte)CardType.Move && b.SubType == (byte)AdditionalTypes.Move.MovementSpeedDecreased / 10)
                     || indicator.Card.BCards.Any(b => b.Type == (byte)CardType.Move && b.SubType == (byte)AdditionalTypes.Move.MoveSpeedDecreased / 10))))
                    {
                        return;
                    }
                }
                if (indicator.Card.BCards.Any(newbuff => Buffs.GetAllItems().Any(b => b.Card.BCards.Any(buff =>
                    buff.CardId != newbuff.CardId
                 && ((buff.Type == 33 && buff.SubType == 5 && (newbuff.Type == 33 || newbuff.Type == 58)) || (newbuff.Type == 33 && newbuff.SubType == 5 && (buff.Type == 33 || buff.Type == 58))
                 || (buff.Type == 33 && (buff.SubType == 1 || buff.SubType == 3) && (newbuff.Type == 58 && (newbuff.SubType == 1))) || (buff.Type == 33 && (buff.SubType == 2 || buff.SubType == 4) && (newbuff.Type == 58 && (newbuff.SubType == 3)))
                 || (newbuff.Type == 33 && (newbuff.SubType == 1 || newbuff.SubType == 3) && (buff.Type == 58 && (buff.SubType == 1))) || (newbuff.Type == 33 && (newbuff.SubType == 2 || newbuff.SubType == 4) && (buff.Type == 58 && (buff.SubType == 3)))
                 || (buff.Type == 33 && newbuff.Type == 33 && buff.SubType == newbuff.SubType) || (buff.Type == 58 && newbuff.Type == 58 && buff.SubType == newbuff.SubType))))))
                {
                    return;
                }

                if (indicator.Card.BCards.Any(s => s.Type == (byte)CardType.LotusSkills && s.SubType.Equals((byte)AdditionalTypes.LotusSkills.ChangeMoonSkills / 10)))
                {
                    RemoveBuff(697);
                }

                if (indicator.Card.BCards.Any(s => s.Type == (byte)CardType.LotusSkills && s.SubType.Equals((byte)AdditionalTypes.LotusSkills.ChangeLotusSkills / 10)))
                {
                    RemoveBuff(690);
                }


                switch (indicator.Card.CardId)
                {
                    case 272:
                    case 273:
                    case 274:
                        if (Buffs.Any(s => new short[] { 272, 273, 274 }.Contains(s.Card.CardId)))
                        {
                            return;
                        }
                        break;
                    case 75:
                    case 28:
                    case 29:
                        if (Buffs.Any(s => s.Card.CardId == 153))
                        {
                            return;
                        }
                        break;
                    case 153:
                        RemoveBuff(28);
                        RemoveBuff(29);
                        RemoveBuff(75);
                        break;
                    case 475: // Devil's Blessing
                        {
                            if (MapInstance?.MapInstanceType == MapInstanceType.RaidInstance
                                && MapMonster?.MonsterVNum == 2326 /* Witch Laurena */)
                            {
                                MapInstance.InstanceBag.LaurenaRound++;

                                MapInstance.Broadcast(StaticPacketHelper.Say(3, MapEntityId, 1, Language.Instance.GetMessageFromKey("GET_OVER_HERE")));
                                MapInstance.Broadcast($"npc_req 3 {MapEntityId} 9685");

                                Observable.Timer(TimeSpan.FromSeconds(1))
                                    .Subscribe(observer =>
                                    {
                                        if (MapInstance != null)
                                        {
                                            MapInstance.Broadcast($"ca_t {MapEntityId} 2000");
                                            TeleportTo(new MapCell { X = 53, Y = 59 });
                                            MapInstance.Broadcast($"guri 11 3 {MapEntityId}");

                                            List<MonsterToSummon> monstersToSummon = new List<MonsterToSummon>();

                                            // Increase n by 1 every round -- max. 2
                                            int n = Math.Min(MapInstance.InstanceBag.LaurenaRound, 2);

                                            for (int i = 0; i < n; i++)
                                            {
                                                MapCell spawnCell = GetRandomMapCellInRange(20) ?? new MapCell { X = PositionX, Y = PositionY };

                                                monstersToSummon.Add(new MonsterToSummon(2327, spawnCell, null, true) { DeathEvents = { new EventContainer(MapInstance, EventActionType.REMOVELAURENABUFF, this) } });
                                            }

                                            EventHelper.Instance.RunEvent(new EventContainer(MapInstance, EventActionType.SPAWNMONSTERS, monstersToSummon));
                                        }
                                    });

                                Observable.Timer(TimeSpan.FromSeconds(3))
                                    .Subscribe(observer =>
                                    {
                                        MapInstance?.Broadcast("npc_req -1 -1");
                                    });
                            }
                        }
                        break;
                    case 532:
                        RemoveBuff(533);
                        RemoveBuff(534);
                        break;
                    case 533:
                        RemoveBuff(532);
                        RemoveBuff(534);
                        break;
                    case 534:
                        RemoveBuff(532);
                        RemoveBuff(533);
                        break;
                    case 562:
                        RemoveBuff(567);
                        break;
                    case 563:
                        RemoveBuff(568);
                        break;
                    case 564:
                        RemoveBuff(562);
                        break;
                    case 565:
                        RemoveBuff(563);
                        break;
                    case 567:
                        RemoveBuff(562);
                        RemoveBuff(563);
                        RemoveBuff(564);
                        RemoveBuff(565);
                        RemoveBuff(568);
                        break;
                    case 568:
                        RemoveBuff(562);
                        RemoveBuff(563);
                        RemoveBuff(564);
                        RemoveBuff(565);
                        RemoveBuff(567);
                        break;
                    case 589:
                        sender?.AddBuff(new Buff(592, sender.Level), sender);
                        break;
                    case 601:
                        RemoveBuff(602);
                        RemoveBuff(603);
                        break;
                    case 602:
                        RemoveBuff(601);
                        RemoveBuff(603);
                        break;
                    case 603:
                        RemoveBuff(601);
                        RemoveBuff(602);
                        break;
                    case 608:
                        RemoveBuff(617); // Magic Spell
                        RemoveBuff(609); // Fire
                        RemoveBuff(610); // Ice
                        RemoveBuff(611); // Light
                        RemoveBuff(612); // No Element
                        RemoveBuff(613); // Dark
                        break;
                    case 617:
                        RemoveBuff(608); // Magical Fetters
                        break;
                    case 727:
                        if (Character == null)
                        {
                            break;
                        }
                        if (Buffs.Any(s => s.Card.CardId == 728))
                        {
                            MapInstance.Broadcast(Character.GenerateBfePacket(728, 0));
                        }

                        if (Buffs.Any(s => s.Card.CardId == 729))
                        {
                            MapInstance.Broadcast(Character.GenerateBfePacket(729, 0));
                        }
                        MapInstance.Broadcast(Character.GenerateBfePacket(727, 1000));
                        break;

                    case 728:
                        if (Character == null)
                        {
                            break;
                        }
                        if (Buffs.Any(s => s.Card.CardId == 727))
                        {
                            MapInstance.Broadcast(Character.GenerateBfePacket(727, 0));
                        }

                        if (Buffs.Any(s => s.Card.CardId == 729))
                        {
                            MapInstance.Broadcast(Character.GenerateBfePacket(729, 0));
                        }
                        MapInstance.Broadcast(Character.GenerateBfePacket(728, 1000));
                        break;

                    case 729:
                        if (Character == null)
                        {
                            break;
                        }
                        if (Buffs.Any(s => s.Card.CardId == 727))
                        {
                            MapInstance.Broadcast(Character.GenerateBfePacket(727, 0));
                        }

                        if (Buffs.Any(s => s.Card.CardId == 728))
                        {
                            MapInstance.Broadcast(Character.GenerateBfePacket(728, 0));
                        }
                        MapInstance.Broadcast(Character.GenerateBfePacket(729, 1000));
                        break;

                }



                indicator.Card.BCards.ForEach(b => BCardDisposables[b.BCardId]?.Dispose());
                if (Buffs[indicator.Card.CardId] is Buff oldBuff)
                {
                    Buffs.Remove(indicator.Card.CardId);
                }

                Buffs[indicator.Card.CardId] = indicator;

                int buffTime = 0;
                int amuletMaxDurability = 0;
                if (Character != null)
                {
                    if (indicator.Card.CardId == 85 && indicator.Card.Duration == 0)
                    {
                        buffTime = ServerManager.RandomNumber(50, 350);
                    }
                    else if (indicator.Card.CardId == 559 && indicator.Card.Duration == 0)
                    {
                        buffTime = ServerManager.RandomNumber(250, 450);
                    }
                    else if (indicator.Card.CardId == 336 && indicator.Card.Duration == 0)
                    {
                        if (Character.VehicleItem != null)
                        {
                            buffTime = Character.VehicleItem.SpeedBoostDuration * 10;
                        }
                        else
                        {
                            buffTime = ServerManager.RandomNumber(30, 70);
                        }
                    }
                    else if (indicator.Card.CardId == 0)
                    {
                        buffTime = Character.ChargeValue > 7000 ? 7000 : Character.ChargeValue;
                    }
                    else if (indicator.Card.CardId == 562 || indicator.Card.CardId == 563)
                    {
                        buffTime = 400;
                    }
                    indicator.RemainingTime = indicator.Card.Duration == 0 ? buffTime : indicator.Card.Duration;

                    // Amulet remaining time
                    if (indicator.Card.CardId == 62)
                    {
                        ItemInstance amulet = Character.Inventory.LoadBySlotAndType((byte)EquipmentType.Amulet, InventoryType.Wear);
                        if (amulet?.ItemDeleteTime != null)
                        {
                            buffTime = (int)amulet.ItemDeleteTime.Value.Subtract(DateTime.Now).TotalSeconds * 10;
                            indicator.RemainingTime = buffTime;
                        }
                        else if (amulet?.DurabilityPoint > 0)
                        {
                            amuletMaxDurability = amulet.Item.EffectValue;
                            buffTime = amulet.DurabilityPoint;
                            indicator.RemainingTime = buffTime;
                        }
                    }
                    indicator.Start = DateTime.Now;

                    Character.Session.SendPacket($"bf 1 {MapEntityId} {(indicator.Card.CardId == 0 ? Character.ChargeValue > 7000 ? 7000 : Character.ChargeValue : amuletMaxDurability > 0 ? buffTime : 0)}.{indicator.Card.CardId}.{(indicator.Card.Duration == 0 || indicator.Card.CardId == 62 ? amuletMaxDurability > 0 ? amuletMaxDurability : buffTime : indicator.Card.Duration)} {sender.Level}");

                    if (!noMessage || !Buffs.Any(s => s.Card.CardId == indicator.Card.CardId))
                    {
                        Character.Session.SendPacket(Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("UNDER_EFFECT"), indicator.Card.Name), 20));
                    }

                    Character.Session.SendPacket(Character.GenerateStat());

                    if (Mate != null)
                    {
                        Mate.Owner.Session.SendPackets(Mate.Owner.GeneratePst());
                    }
                }

                if (BuffObservables.ContainsKey(indicator.Card.CardId))
                {
                    BuffObservables[indicator.Card.CardId]?.Dispose();
                    BuffObservables.Remove(indicator.Card.CardId);
                }

                indicator.Card.BCards.ForEach(c => c.ApplyBCards(this, sender, x: x, y: y));

                if (indicator.Card.BCards.Any(s => s.Type == (byte)CardType.Move && !s.SubType.Equals((byte)AdditionalTypes.Move.MovementImpossible / 10)))
                {
                    if (Character != null)
                    {
                        Character.LoadSpeed();
                        Character.Session.SendPacket(Character.GenerateCond());
                        Character.LastSpeedChange = DateTime.Now;
                    }
                    else if (Mate != null)
                    {
                        Mate.Owner.Session.SendPacket(Mate.GenerateCond());
                    }
                }

                if (indicator.Card.BCards.Any(s
                    => (s.Type == (byte)CardType.SpecialAttack && s.SubType == (byte)AdditionalTypes.SpecialAttack.NoAttack / 10)
                    || (s.Type == (byte)CardType.Move && s.SubType == (byte)AdditionalTypes.Move.MovementImpossible / 10)
                    || (s.Type == (byte)CardType.FrozenDebuff && s.SubType == (byte)AdditionalTypes.FrozenDebuff.EternalIce / 10)
                    ))
                {
                    if (Character != null)
                    {
                        Character.Session.SendPacket(Character.GenerateCond());
                    }
                    if (Mate != null)
                    {
                        Mate.Owner.Session.SendPacket(Mate.GenerateCond());
                    }
                }

                Character?.Session?.SendPackets(Character?.GenerateQuicklist());
                if (indicator.Card.CardId == 518)
                {
                    MapInstance?.Broadcast($"eff {(byte)UserType} {MapEntityId} 4537");
                }

                MapInstance?.Broadcast($"bf_e {(short)UserType} {MapEntityId} {indicator.Card.CardId} 100");

                BuffObservables[indicator.Card.CardId] = Observable.Timer(TimeSpan.FromMilliseconds((indicator.Card.Duration == 0 || indicator.Card.CardId == 62 ? buffTime : indicator.Card.Duration) * 100)).Subscribe(o =>
                {
                    if (indicator.Card.CardId != 0 && amuletMaxDurability == 0)
                    {
                        RemoveBuff(indicator.Card.CardId);
                        if (indicator.Card.TimeoutBuff != 0 && ServerManager.RandomNumber() < indicator.Card.TimeoutBuffChance)
                        {
                            AddBuff(new Buff(indicator.Card.TimeoutBuff, indicator.Level), sender);
                        }
                    }
                });
            }
        }

        public void SendBuffsPacket()
        {
            if (Character != null)
            {
                foreach (Buff indicator in Buffs.GetAllItems())
                {
                    if (indicator.StaticBuff)
                    {
                        Character.Session.SendPacket($"vb {indicator.Card.CardId} 1 {indicator.RemainingTime * 10}");
                    }
                    else
                    {
                        Character.Session.SendPacket($"bf 1 {MapEntityId} {(indicator.Card.CardId == 0 ? Character.ChargeValue > 7000 ? 7000 : Character.ChargeValue : 0)}.{indicator.Card.CardId}.{indicator.RemainingTime} {indicator.Level}");
                    }
                }
            }
        }
        public bool HasBuff(short cardId) => Buffs.GetAllItems().Any(b => b?.Card?.CardId == cardId);

        public bool HasBuff(CardType type, byte subtype, bool castTypeNotZero = false)
        {
            try
            {
                List<BCard> bcards = Buffs.GetAllItems().SelectMany(s => s.Card.BCards).ToList();

                bcards.AddRange(BCards.ToList());

                return subtype % 10 == 1
                    ? bcards.Any(s =>
                        (!castTypeNotZero || s.CastType != 0) && s.Type.Equals((byte)type) && s.SubType.Equals((byte)(subtype / 10)) && s.FirstData >= 0)
                    : bcards.Any(s =>
                        (!castTypeNotZero || s.CastType != 0) && s.Type.Equals((byte)type) && s.SubType.Equals((byte)(subtype / 10))
                        && (s.FirstData <= 0 || s.ThirdData < 0));
            }
            catch (Exception ex)
            {
                Logger.LogEventError("HASBUFF", "Error on HasBuff(CardType type, byte subtype, bool castTypeNotZero = false) method", ex);
                return false;
            }
        }

        public void RemoveBuff(short id, bool removePermaBuff = false)
        {
            if (!Buffs.ContainsKey(id))
            {
                return;
            }

            Buff indicator = Buffs[id];

            if (indicator?.Card != null)
            {
                lock (indicator)
                {
                    if (indicator.IsPermaBuff && !removePermaBuff)
                    {
                        AddBuff(indicator, this, true);
                        return;
                    }

                    Buffs.Remove(id);

                    if (indicator.Card.BCards.Any(s
                        => s.Type == (byte)CardType.SpecialAttack && s.SubType == (byte)AdditionalTypes.SpecialAttack.NoAttack / 10
                        || s.Type == (byte)CardType.Move && s.SubType == (byte)AdditionalTypes.Move.MovementImpossible / 10
                        || s.Type == (byte)CardType.FrozenDebuff && s.SubType == (byte)AdditionalTypes.FrozenDebuff.EternalIce / 10
                        ))
                    {
                        if (Character != null)
                        {
                            Character.LastSpeedChange = DateTime.Now;
                            Character.LoadSpeed();
                            Character.Session?.SendPacket(Character.GenerateCond());
                        }
                        else if (Mate != null)
                        {
                            Mate.Owner?.Session?.SendPacket(Mate.GenerateCond());
                        }
                    }

                    if (indicator.Card.BCards.Any(s => s.Type == (byte)CardType.SpecialEffects && s.SubType == (byte)AdditionalTypes.SpecialEffects.ShadowAppears / 10)
                        && GetBuff(CardType.SpecialEffects, (byte)AdditionalTypes.SpecialEffects.ShadowAppears) is int[] BuffData)
                    {
                        MapInstance?.Broadcast($"guri 0 {(short)UserType} {MapEntityId} {BuffData[0]} {BuffData[1]}");
                    }

                    MapInstance?.Broadcast($"bf_e {(short)UserType} {MapEntityId} {indicator.Card.CardId} 0");

                    if (Character != null)
                    {
                        if (indicator.StaticBuff)
                        {
                            Character.Session?.SendPacket($"vb {indicator.Card.CardId} 0 {indicator.Card.Duration}");
                            Character.Session?.SendPacket(Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("EFFECT_TERMINATED"), indicator.Card.Name), 11));
                        }
                        else
                        {
                            Character.Session?.SendPacket($"bf 1 {Character.CharacterId} 0.{indicator.Card.CardId}.0 {Level}");
                            Character.Session?.SendPacket(Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("EFFECT_TERMINATED"), indicator.Card.Name), 20));
                        }

                        if (Buffs[indicator.Card.CardId] != null)
                        {
                            Buffs.Remove(id);
                        }
                        if (indicator.Card.BCards.Any(s => s.Type == (byte)CardType.Move && !s.SubType.Equals((byte)AdditionalTypes.Move.MovementImpossible / 10)))
                        {
                            Character.LastSpeedChange = DateTime.Now;
                            Character.LoadSpeed();
                            Character.Session?.SendPacket(Character.GenerateCond());
                        }
                        if (indicator.Card.BCards.Any(s => s.Type == (byte)CardType.SpecialActions && s.SubType.Equals((byte)AdditionalTypes.SpecialActions.Hide / 10))
                         || indicator.Card.BCards.Any(s => s.Type == (byte)CardType.FalconSkill && s.SubType.Equals((byte)AdditionalTypes.FalconSkill.Hide / 10))
                         || indicator.Card.BCards.Any(s => s.Type == (byte)CardType.FalconSkill && s.SubType.Equals((byte)AdditionalTypes.FalconSkill.Ambush / 10)))
                        {
                            Character.Invisible = false;
                            foreach (Mate teamMate in Character.Mates?.Where(m => m != null && m.IsTeamMember))
                            {
                                teamMate.PositionX = Character.PositionX;
                                teamMate.PositionY = Character.PositionY;
                                teamMate.UpdateBushFire();

                                if (Character.MapInstance?.Sessions != null)
                                {
                                    Parallel.ForEach(Character.MapInstance.Sessions.Where(s => s?.Character != null), s =>
                                    {
                                        if (ServerManager.Instance.ChannelId != 51 || Character.Faction == s.Character.Faction)
                                        {
                                            s.SendPacket(teamMate.GenerateIn(false, ServerManager.Instance.ChannelId == 51));
                                        }
                                        else
                                        {
                                            s.SendPacket(teamMate.GenerateIn(true, ServerManager.Instance.ChannelId == 51, s.Account.Authority));
                                        }
                                    });
                                }

                                Character.Session?.SendPacket(Character.GeneratePinit());
                                Character.Mates?.ForEach(s => Character.Session?.SendPacket(s.GenerateScPacket()));
                                Character.Session?.SendPackets(Character.GeneratePst());
                            }
                            MapInstance?.Broadcast(Character.GenerateInvisible());
                        }
                        if (indicator.Card.BCards.Any(s => s.Type == (byte)CardType.FearSkill && s.SubType.Equals((byte)AdditionalTypes.FearSkill.MoveAgainstWill / 10)))
                        {
                            Character.Session?.SendPacket($"rv_m {MapEntityId} 1 0");
                        }
                        if (indicator.Card.BCards.Any(s => s.Type == (byte)CardType.FearSkill && s.SubType.Equals((byte)AdditionalTypes.FearSkill.AttackRangedIncreased / 10)))
                        {
                            if (!Buffs.Any(s => s.Card.BCards.Any(b => b.Type == (byte)CardType.FearSkill && b.SubType.Equals((byte)AdditionalTypes.FearSkill.AttackRangedIncreased / 10))))
                            {
                                Character.Session?.SendPacket($"bf_d 0 1");
                            }
                        }

                        if (indicator.Card.BCards.Any(s => s.Type == (byte)CardType.DarkCloneSummon
                            && s.SubType == (byte)AdditionalTypes.DarkCloneSummon.ConvertDamageToHPChance / 10))
                        {
                            GetDamage(Character.ConvertedDamageToHP, this, true);
                            Character.ConvertedDamageToHP = 0;
                            Character.Session?.SendPacket(Character.GenerateStat());
                        }

                        // TODO : Find another way because it is hardcode

                        switch (indicator.Card.CardId)
                        {
                            case 131:
                                Character.Session?.SendPacket(Character.GeneratePairy());
                                break;

                            case 340:
                                if (MapInstance?.Map?.MapTypes != null
                                    && MapInstance.Map.MapTypes.Any(s => s.MapTypeId == (short)MapTypeEnum.Act52) && MapInstance.Map?.MapId != 2640)
                                {
                                    Character.AddStaticBuff(new StaticBuffDTO
                                    {
                                        CardId = 339,
                                        CharacterId = Character.CharacterId,
                                        RemainingTime = -1
                                    });
                                    Character.Session?.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("ENCASED_BURNING_SWORD")));
                                }
                                else if(MapInstance?.Map?.MapTypes != null
                                     && MapInstance.Map?.MapId == 2640)
                                {
                                    Character.AddStaticBuff(new StaticBuffDTO
                                    {
                                        CardId = 339,
                                        CharacterId = Character.CharacterId,
                                        RemainingTime = -1
                                    });
                                    Character.Session?.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("COURSE")));
                                }
                                break;

                            case 617:
                                {
                                    Character.LastComboCastId = 0;
                                    Character.Session?.SendPacket("ms_c 1");
                                }
                                break;

                            case 620:
                                {
                                    if (Character.Session != null && Character.SavedLocation != null && indicator.Sender?.Character?.CharacterId == Character.CharacterId)
                                    {
                                        Character.SavedLocation = null;

                                        CharacterSkill characterSkill = indicator.SkillVNum.HasValue ? Character.GetSkill(indicator.SkillVNum.Value) : null;

                                        Skill skill = characterSkill?.Skill;

                                        if (skill != null)
                                        {
                                            short cooldown = 600; // 60 seconds * 10

                                            Character.Session.SendPacket(StaticPacketHelper.SkillResetWithCoolDown(skill.CastId, cooldown));

                                            characterSkill.LastUse = DateTime.Now.AddMilliseconds(cooldown * 100);

                                            Observable.Timer(characterSkill.LastUse).Subscribe(s => Character.Session.SendPacket(StaticPacketHelper.SkillReset(skill.CastId)));
                                        }
                                    }
                                }
                                break;
                            case 697:
                            case 690:
                                Character.Session.SendPackets(Character.GenerateQuicklist());
                                break;
                            case 676:
                                Character.Session.Character.DragonModeObservable?.Dispose();
                                break;
                            case 727:
                            case 728:
                            case 729:
                                if (Buffs.Any(s => s.Card.CardId == 727 || s.Card.CardId == 728 || s.Card.CardId == 729))
                                {
                                    break;
                                }

                                MapInstance.Broadcast(Character.GenerateBfePacket(indicator.Card.CardId, 0));

                                Character.UltimatePoints = 0;
                                Character.Session.SendPacket(Character.GenerateFtPtPacket());
                                Character.Session.SendPackets(Character.GenerateQuicklist());
                                break;

                            case 730:
                                Character.Session.SendPackets(Character.GenerateQuicklist());
                                break;

                            case 724:
                                Character.RemoveUltimatePoints(1000);
                                break;

                        }
                    }

                    if (BuffObservables != null && BuffObservables.ContainsKey(indicator.Card.CardId))
                    {
                        BuffObservables[indicator.Card.CardId]?.Dispose();
                        BuffObservables.Remove(indicator.Card.CardId);
                    }

                    indicator.Card.BCards.ForEach(b => BCardDisposables[b.BCardId]?.Dispose());
                    indicator.StaticVisualEffect?.Dispose();
                }
            }
        }

        public void DisableBuffs(BuffType type, int level = 100)
        {
            if (type == BuffType.All || type == BuffType.Good)
            {
                ClearSacrificeBuff();
            }

            List<Buff> BuffsCopy = new List<Buff>();

            lock (Buffs)
            {
                BuffsCopy = Buffs.GetAllItems();
            }

            List<Buff> buff = BuffsCopy.Where(s => (type == BuffType.All || s.Card.BuffType == type) && !s.StaticBuff && s.Card.Level < level && s.Card.CardId != 62).ToList();

            buff.ForEach(s =>
            {
                s.Card.BCards.ForEach(b =>
                {
                    if (BCardDisposables?.ContainsKey(b.BCardId) == true && BCardDisposables[b.BCardId] != null)
                    {
                        BCardDisposables[b.BCardId]?.Dispose();
                        BCardDisposables[b.BCardId] = null;
                    }
                });

                if (BuffObservables != null && BuffObservables.ContainsKey(s.Card.CardId))
                {
                    BuffObservables[s.Card.CardId]?.Dispose();
                    BuffObservables.Remove(s.Card.CardId);
                }

                RemoveBuff(s.Card.CardId);
            });

            if (type == BuffType.All)
            {
                ThreadSafeSortedList<int, IDisposable> StaticBuffsBCardDisposables = new ThreadSafeSortedList<int, IDisposable>();

                Buffs.Where(s => s.StaticBuff)
                    .SelectMany(s => s.Card.BCards)
                    .Where(s => BCardDisposables.ContainsKey(s.BCardId)).ToList()
                    .ForEach(s => StaticBuffsBCardDisposables[s.BCardId] = BCardDisposables[s.BCardId]);

                BCardDisposables.GetAllItems().Except(StaticBuffsBCardDisposables.GetAllItems()).ToList().ForEach(s =>
                {
                    s?.Dispose();
                });

                BCardDisposables = StaticBuffsBCardDisposables;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="types"></param>
        /// <param name="level"></param>
        public void DisableBuffs(List<BuffType> types, int level = 100)
        {
            types.ForEach(bt => DisableBuffs(bt, level));
        }

        public int[] GetBuff(CardType type, byte subtype, int secondData = -1)
        {
            int value1 = 0;
            int value2 = 0;
            int value3 = 0;

            foreach (BCard entry in BCards.Where(s => s?.Type.Equals((byte)type) == true && s.SubType.Equals((byte)(subtype / 10)) && (secondData == -1 || s.SecondData == secondData)))
            {
                if (entry.IsLevelScaled)
                {
                    if (entry.IsLevelDivided)
                    {
                        value1 += Level / entry.FirstData;
                    }
                    else
                    {
                        value1 += entry.FirstData * Level;
                    }
                }
                else
                {
                    value1 += entry.FirstData;
                }
                value2 += entry.SecondData;
                value3 += entry.ThirdData;
            }

            lock (Buffs)
            {
                foreach (Buff buff in Buffs.GetAllItems())
                {
                    // THIS ONE DOES NOT FOR STUFFS

                    foreach (BCard entry in buff.Card.BCards
                        .Where(s => s.Type.Equals((byte)type) && s.SubType.Equals((byte)(subtype / 10)) && (secondData == -1 || s.SecondData == secondData) && (s.CastType != 1 || (s.CastType == 1 && buff.Start.AddMilliseconds(buff.Card.Delay * 100) < DateTime.Now))))
                    {
                        if (entry.IsLevelScaled)
                        {
                            if (entry.IsLevelDivided)
                            {
                                value1 += buff.Level / entry.FirstData;
                            }
                            else
                            {
                                value1 += entry.FirstData * buff.Level;
                            }
                        }
                        else
                        {
                            value1 += entry.FirstData;
                        }
                        value2 += entry.SecondData;
                        value3 += entry.ThirdData;
                    }
                }
            }

            if (Character != null && Character.Skills != null)
            {
                List<BCard> PassiveSkillsBCards = PassiveSkillHelper.Instance.PassiveSkillToBCards(Character.Skills.Where(s => s.Skill.SkillType == 0));
                foreach (BCard entry in PassiveSkillsBCards.Where(s => s?.Type.Equals((byte)type) == true && s.SubType.Equals((byte)(subtype / 10)) && (secondData == -1 || s.SecondData == secondData)))
                {
                    if (entry.IsLevelScaled)
                    {
                        if (entry.IsLevelDivided)
                        {
                            value1 += Level / entry.FirstData;
                        }
                        else
                        {
                            value1 += entry.FirstData * Level;
                        }
                    }
                    else
                    {
                        value1 += entry.FirstData;
                    }
                    value2 += entry.SecondData;
                    value3 += entry.ThirdData;
                }
            }

            return new[] { value1, value2, value3 };
        }

        public List<MapMonster> GetOwnedMonsters()
        {
            List<MapMonster> ownedMonsters = MapInstance?.Monsters.Where(m => m.Owner?.MapEntityId == MapEntityId || Character != null && Character.Mates.Any(mate => m.Owner?.MapEntityId == mate.MateTransportId)).ToList();
            return ownedMonsters;
        }

        public void RemoveOwnedMonsters(bool OnlyFirst = false, int MonsterVNum = -1)
        {
            IEnumerable<MapMonster> ownedMonsters = GetOwnedMonsters()?.Where(m => (MonsterVNum == -1 && !IsMateTrainer(m.MonsterVNum)) || m.MonsterVNum == MonsterVNum);
            if (ownedMonsters != null)
            {
                if (OnlyFirst)
                {
                    if (ownedMonsters.LastOrDefault() is MapMonster first)
                    {
                        first.MapInstance.Broadcast(StaticPacketHelper.Out(UserType.Monster, first.MapMonsterId));
                        first.MapInstance.RemoveMonster(first);
                    }
                }
                else
                {
                    ownedMonsters.ToList().ForEach(m => {
                        m.MapInstance.Broadcast(StaticPacketHelper.Out(UserType.Monster, m.MapMonsterId));
                        m.MapInstance.RemoveMonster(m);
                    });
                }
            }
        }

        public List<MapNpc> GetOwnedNpcs()
        {
            List<MapNpc> ownedNpcs = MapInstance?.Npcs.Where(m => m.Owner?.MapEntityId == MapEntityId || Character != null && Character.Mates.Any(mate => m.Owner?.MapEntityId == mate.MateTransportId)).ToList();
            return ownedNpcs;
        }

        public void RemoveOwnedNpcs(bool OnlyFirst = false, int NpcVNum = -1)
        {
            IEnumerable<MapNpc> ownedNpcs = GetOwnedNpcs()?.Where(m => NpcVNum == -1 || m.NpcVNum == NpcVNum);
            if (ownedNpcs != null)
            {
                if (OnlyFirst)
                {
                    if (ownedNpcs.LastOrDefault() is MapNpc first)
                    {
                        first.MapInstance.Broadcast(StaticPacketHelper.Out(UserType.Npc, first.MapNpcId));
                        first.MapInstance.RemoveNpc(first);
                    }
                }
                else
                {
                    ownedNpcs.ToList().ForEach(m => {
                        m.MapInstance.Broadcast(StaticPacketHelper.Out(UserType.Npc, m.MapNpcId));
                        m.MapInstance.RemoveNpc(m);
                    });
                }
            }
        }

        public void ClearEnemyFalcon()
        {
            MapInstance.BattleEntities.Where(s => s.FalconFocusedEntityId == MapEntityId).ToList().ForEach(s =>
            {
                s.ClearOwnFalcon();
            });
        }

        public void ClearOwnFalcon()
        {
            if (FalconFocusedEntityId != 0)
            {
                if (MapInstance.BattleEntities.FirstOrDefault(s => s.MapEntityId == FalconFocusedEntityId) is BattleEntity FalconFocusedEntity)
                {
                    MapInstance.Broadcast($"eff_ob  {(byte)FalconFocusedEntity.UserType} {FalconFocusedEntityId} 0 4269");
                }
                FalconFocusedEntityId = 0;
            }
        }

        public void ClearSacrificeBuff()
        {
            if (Buffs.FirstOrDefault(s => s.Card.BCards.Any(b => b.Type.Equals((byte)CardType.DamageConvertingSkill) && b.SubType.Equals((byte)AdditionalTypes.DamageConvertingSkill.TransferInflictedDamage / 10)))?.Sender is BattleEntity SacrificeSender)
            {
                SacrificeSender.RemoveBuff(546);
            }
            RemoveBuff(531);
        }

        public MapCell GetPos() => new MapCell { X = PositionX, Y = PositionY };

        public double HPLoad()
        {
            double MaxHp = 0;
            if (Character != null)
            {
                double multiplicator = 1.0;
                int hp = 0;
                if (Character.UseSp)
                {
                    ItemInstance specialist = Character.Inventory?.LoadBySlotAndType((byte)EquipmentType.Sp, InventoryType.Wear);
                    if (specialist != null)
                    {
                        int point = CharacterHelper.SlPoint(specialist.SlHP, 3) + Character.slhpbonus;
                        if (point > 100) { point = 100; };

                        if (point <= 50)
                        {
                            multiplicator += point / 100.0;
                        }
                        else
                        {
                            multiplicator += 0.5 + ((point - 50.00) / 50.00);
                        }
                        hp = specialist.HP + (specialist.SpHP * 100);
                    }
                }
                hp += CellonOptions.Where(s => s.Type == CellonOptionType.HPMax).Sum(s => s.Value);
                multiplicator += GetBuff(CardType.BearSpirit, (byte)AdditionalTypes.BearSpirit.IncreaseMaximumHP)[0] / 100D;
                multiplicator += GetBuff(CardType.MaxHPMP, (byte)AdditionalTypes.MaxHPMP.IncreasesMaximumHP)[0] / 100D;

                MaxHp = (int)((CharacterHelper.HPData[(byte)Character.Class, Level] + hp + GetBuff(CardType.MaxHPMP, (byte)AdditionalTypes.MaxHPMP.MaximumHPIncreased)[0] + GetBuff(CardType.MaxHPMP, (byte)AdditionalTypes.MaxHPMP.MaximumHPMPIncreased)[0]) * multiplicator);
            }
            else
            {
                MaxHp = HpMax;
            }

            if (Hp > MaxHp)
            {
                Hp = (int)MaxHp;
            }
            return MaxHp;
        }

        public double MPLoad()
        {
            double MaxMp = 0;
            if (Character != null)
            {
                int mp = 0;
                double multiplicator = 1.0;
                if (Character.UseSp)
                {
                    ItemInstance specialist = Character.Inventory?.LoadBySlotAndType((byte)EquipmentType.Sp, InventoryType.Wear);
                    if (specialist != null)
                    {
                        int point = CharacterHelper.SlPoint(specialist.SlHP, 3) + Character.slhpbonus;
                        if (point > 100) { point = 100; };

                        if (point <= 50)
                        {
                            multiplicator += point / 100.0;
                        }
                        else
                        {
                            multiplicator += 0.5 + ((point - 50.00) / 50.00);
                        }
                        mp = specialist.MP + (specialist.SpHP * 100);
                    }
                }
                mp += CellonOptions.Where(s => s.Type == CellonOptionType.MPMax).Sum(s => s.Value);

                multiplicator += GetBuff(CardType.BearSpirit, (byte)AdditionalTypes.BearSpirit.IncreaseMaximumMP)[0] / 100D;
                multiplicator += GetBuff(CardType.MaxHPMP, (byte)AdditionalTypes.MaxHPMP.IncreasesMaximumMP)[0] / 100D;

                MaxMp = (int)((CharacterHelper.MPData[(byte)Character.Class, Level] + mp + GetBuff(CardType.MaxHPMP, (byte)AdditionalTypes.MaxHPMP.MaximumMPIncreased)[0] + GetBuff(CardType.MaxHPMP, (byte)AdditionalTypes.MaxHPMP.MaximumHPMPIncreased)[0]) * multiplicator);
            }
            else
            {
                MaxMp = MpMax;
            }

            if (Mp > MaxMp)
            {
                Mp = (int)MaxMp;
            }
            return MaxMp;
        }

        public void UpdateBushFire()
        {
            BrushFireJagged = BestFirstSearch.LoadBrushFireJagged(new GridPos
            {
                X = PositionX,
                Y = PositionY
            }, MapInstance.Map.JaggedGrid);
        }

        public int GetDamage(int damage, BattleEntity damager, bool dontKill = false, bool fromDebuff = false)
        {
            if (Character?.HasGodMode == true || Mate?.Owner.HasGodMode == true || HasBuff(CardType.HideBarrelSkill, (byte)AdditionalTypes.HideBarrelSkill.NoHPConsumption))
            {
                return 0;
            }

            if (fromDebuff) // If it comes from attack percent defense, dismin damage percent with chance, and static damages are already applied
            {
                int[] percentDefense = GetBuff(BCardType.CardType.RecoveryAndDamagePercent, (byte)AdditionalTypes.RecoveryAndDamagePercent.DecreaseSelfHP);
                if (percentDefense[0] != 0)
                {
                    int percentDefenseDamage = HpMax / 100 * Math.Abs(percentDefense[0]);
                    if (percentDefenseDamage < damage) damage = percentDefenseDamage;
                }

                if (MapMonster?.MonsterVNum == 533)
                {
                    if (63 < damage) damage = 63;
                }
            }

            if (damager.MapEntityId != MapEntityId)
            {
                LastDefence = DateTime.Now;
            }

            Character?.DisposeShopAndExchange();

            if (Character != null)
            {
                if (Character.BattleEntity.AdditionalHp > damage)
                {
                    Character.BattleEntity.AdditionalHp -= damage;
                    damage = 0;
                    Character.Session.SendPacket(Character.GenerateAdditionalHpMp());
                }
                else if (Character.BattleEntity.AdditionalHp > 0)
                {
                    damage -= (int)Character.BattleEntity.AdditionalHp;
                    Character.BattleEntity.AdditionalHp = 0;
                    Character.Session.SendPacket(Character.GenerateAdditionalHpMp());
                }
            }

            if (Mate != null && MapInstance == Mate.Owner.Miniland && Mate.MateType == MateType.Pet)
            {
                if (damager.MapMonster != null)
                {
                    if (IsMateTrainer(damager.MapMonster.MonsterVNum))
                    {
                        Mate.DefendTrainer(damager.MapMonster.MonsterVNum);
                    }
                }
            }

            if (MapMonster != null)
            {
                if (damager.Mate != null && damager.MapInstance == damager.Mate.Owner.Miniland && damager.Mate.MateType == MateType.Pet)
                {
                    if (IsMateTrainer(MapMonster.MonsterVNum))
                    {
                        damager.Mate.HitTrainer(MapMonster.MonsterVNum);
                    }
                }
            }

            if (MapMonster?.AliveTimeMp > 0)
            {
                Mp -= damage;
            }
            else
            {
                if (Hp <= damage && dontKill)
                {
                    damage = Hp - 1;
                }
                else if (Hp < damage)
                {
                    damage = Hp;
                }

                Hp -= damage;

                if (Hp > 0)
                {
                    return damage;
                }

                Hp = 0;

                if (Character != null)
                {
                    Character.WalkDisposable?.Dispose();
                    RemoveBuff(569);

                    if (MapInstance != null)
                    {
                        if (MapInstance.MapInstanceType != MapInstanceType.TalentArenaMapInstance)
                        {
                            Character.MapInstance.InstanceBag.DeadList.Add(Character.CharacterId);
                        }
                    }
                }
                else if (Mate != null)
                {
                    Mate.GenerateDeath(damager);
                }

                ClearEnemyFalcon();
            }
            return damage;
        }

        public void DecreaseMp(int amount)
        {
            if (CellonOptions != null)
            {
                amount = (short)(amount * ((100 - CellonOptions.Where(s => s.Type == CellonOptionType.MPUsage).Sum(s => s.Value)) / 100D));
            }

            if (GetBuff(CardType.HealingBurningAndCasting, (byte)AdditionalTypes.HealingBurningAndCasting.HPDecreasedByConsumingMP)[0] is int HPDecreasedByConsumingMP)
            {
                if (HPDecreasedByConsumingMP < 0)
                {
                    int amountDecreased = -(amount * HPDecreasedByConsumingMP / 100);
                    GetDamage(amountDecreased, this, true);
                    amount -= amountDecreased;
                }
            }
            if (Character != null)
            {
                if (Character.BattleEntity.AdditionalMp > amount)
                {
                    Character.BattleEntity.AdditionalMp -= amount;
                    amount = 0;
                    Character.Session.SendPacket(Character.GenerateAdditionalHpMp());
                }
                else if (Character.BattleEntity.AdditionalMp > 0)
                {
                    amount -= (int)Character?.BattleEntity.AdditionalMp;
                    Character.BattleEntity.AdditionalMp = 0;
                    Character.Session.SendPacket(Character.GenerateAdditionalHpMp());
                }
            }

            Mp -= amount;
            if (Mp < 0)
            {
                Mp = 0;
            }
            Character?.Session.SendPacket(Character.GenerateStat());
        }

        public short[] CantAttackEntitiesList = new short[] {
            327 // Carven
            , 2004  // Eisblume
			,2020  // Eisblume
            ,3051 //Busch
			,965  // Angel Base Camp
			,966  // Demon Base Camp
			,967  // Altar of Angel
			,968  // Altar of Demon
			,433  // Jajamaru's Pot
			,238  // Weak Skull
			,239  // Weak Skull
			,240  // Weak Skull
			,258  // Weak Skull
			,259  // Weak Skull
			,260  // Weak Skull
			,433  // Jajamaru's Pot
			,797  // Giant Bird's Egg
			,798  // Giant Bird's Egg
			,390  // Bird's Egg
            ,424  // Meteorite Chunk	
            ,425  // Big Tombstone	
            ,426  // Small Tombstone	 
            ,453  // Gate to the Land of Death	
            ,465  // Giant Clam	
            ,861  // Tree Stump
            ,862  // Blossom Tree	
            ,863  // Kovolt Statue	
            ,864  // Pentagram	
            ,865  // Evil Circle	
            ,866  // Sacred Shrub	
            ,867  // Holy Altar 	
            ,869  // Magic Branch	
            ,880  // Wheat
            ,881  // Iron Ore	
            ,892  // Strange Trace	
            ,893  // Mysterious Well	
            ,894  // Mysterious Well	
            ,895  // Bird Nest	
            ,896  // Golden Nest	
            ,897  // Magic Well 	
            ,898  // Weird Well 	
            ,902  // Magic Branch	
            ,905  // Wanted List	
            ,906  // Shinebone Statue 	
            ,907  // Damaged Guard Post	
            ,908  // Soft Ground	
            ,909  // Rough Ground	
            ,910  // Sparkling Rock 	
            ,911  // Broom	
            ,912  // Rice Plant	
            ,913  // Statue of Terror 	
            ,914  // Fruit Tree	
            ,915  // Score Board 	
            ,916  // Red Stone	
            ,917  // Broken Shinebone Statue 	  
            ,920  // Small Signpost	
            ,921  // Large Signpost	
            ,922  // Large Rainbow Crystal 	
            ,923  // Medium Rainbow Crystal 	
            ,924  // Small Rainbow Crystal 	
            ,928  // Daisy	
            ,929  // Iris 	
            ,930  // Time-Space Portal 	
            ,931  // Fairy Clue 	
            ,932  // Yellow Soulstone	
            ,933  // Red Soulstone 	
            ,934  // Blue Soulstone	
            ,938  // Chrysos Statue 	
            ,941  // Big Red Flower	
            ,944  // Well	
            ,953  // Mysterious Soulstone	
            ,954  // Grey Soulstone	
            ,955  // Teleportation Tower
            ,956  // Small Campfire	
            ,957  // Large Campfire	
            ,959  // Ice Machine	
            ,985  // Easter Sack
            ,988  // Mysterious Egg	
            ,1264  // Back of Prickly Pears	
            ,1265  // Neil's Mailbox	
            ,1266  // Prisoner Humad	
            ,1271  // AbandonedAmakur Merchant Chest	
            ,1272  // Ruined Trade Goods 	
            ,1273  // Pillaged Trade Goods	
            ,1274  // Destroyed Akamur Flag	
            ,1275  // First Trade Goods	
            ,1276  // Second Trade Goods	
            ,1277  // Third Trade Goods	
            ,1278  // Fourth Trade Goods	
            ,1279  // Fifth Trade Goods	
            ,1280  // Sixth Trade Goods	
            ,1281  // Chest of Command Lists 	
            ,1282  // Suspicious Potion	 
            ,1283  // Keru's Strange Chest	
            ,1284  // Garton's Strange Chest	
            ,1285  // Keru's Stolen Relics	
            ,1286  // Garton's Stolen Relics	
            ,1287  // Stolen Relics	
            ,1288  // Chest of Scrolls	
            ,1289  // Strange Relic Piece	
            ,1308  // Broken Relics	
            ,1309  // Destroyed Relic	
            ,1310  // Sealed Chest	
            ,1311  // Broken Relic Pieces	
            ,1312  // Broken Relic Pieces	
            ,1313  // Broken Relic Pieces	
            ,1314  // Broken Relic Pieces	
            ,1315  // First Script Tablet	
            ,1316  // Second Script Tablet	
            ,1317  // Third Script Tablet	
            ,1318  // Broken Script Tablet	
            ,1319  // Broken Script Tablet	
            ,1320  // Broken Script Tablet	
            ,1321  // Broken Script Tablet	
            ,1322  // Dead Monk	     	
            ,1323  // Dead Clergyman	
            ,1327  // Chest of Fairy Dust	
            ,1328  // Letter the the Desert Rober Leader	
            ,1329  // Traveller's Diary	
            ,1330  // Posion Bottle	
            ,1331  // Elf's Leather Bag	
            ,1332  // Earth Fairy Dust 	
            ,1333  // Wind Fairy Dust   	
            ,1334  // Sand Fairy Dust	 
            ,1335  // Medicine Bottle	
            ,1337  // Akamur Merchant Chest	
            ,1338  // Lost Chest (Keru's Camp)	
            ,1339  // Lost Chest (Garton's Camp) 	
            ,1340  // Lost Goods (Desert Robber Camp)	
            ,1341  // Lost Chest (Robber Centre)	
            ,1342  // Undiscovered Strange Chest	
            ,1346  // 	
            ,1347  //	
            ,1385  // Halloween Signpost	
            ,2004  // Ice Flower	
            ,2320  // Rope 	
            ,2321  // Terrified Siblings	
            ,2350  //    
            ,4280  //
            ,4281  //
            ,4282  //
            ,3036 // Feuerobelisk
           // ,510 // Small Hellduke
            ,3008 // Warrior
            ,3024 // Magier
        };
public short[] CantAttackToEntitiesList = new short[] {
             848   // Jajamarus Falle (Raid AOE)
            ,849   // Bambusstäbe (Raid AOE)
             ,3051 //busch
            ,850   // Morgenstern (Raid AOE)
            ,851   // Fallender Stein (Raid AOE)
            ,852   // Kleine Spinne (Raid AOE)
            ,854   // Schwarzer Meteorit (Raid AOE)
            ,958   // Beschwörungsportal für Monster
            ,2591  // Fernons kleiner Meteor (Raid AOE)
            ,2592  // Fernons großer Meteor (Raid AOE)
            ,2004  // Eisblume
            ,2020  // Eisblume
            ,425   // Großer Grabstein
            ,426   // Kleiner Grabstein
            ,453   // Tor zum Land der Toten
            ,465   // Riesenmuschel
            ,861   // Baumstumpf
            ,862   // Blütenbaum
            ,863   // Kovolt-Statue
            ,864   // Pentagramm
            ,865   // Kreis der Finsternis
            ,866   // Heiliger Strauch
            ,867   // Heiliger Altar
            ,869   // Schnüfflernest
            ,880   // Weizen
            ,881   // Eisenerz
            ,892   // Seltsame Spur
            ,893   // Mysteriöser Brunnen
            ,894   // Geheimnisvoller Brunnen
            ,895   // Vogelnest
            ,896   // Goldenes Nest
            ,897   // Magischer Brunnen
            ,898   // Eigenartiger Brunnen
            ,902   // Magischer Ast
            ,905   // Tafel mit Fahndungsliste
            ,906   // Shinebone-Statue
            ,907   // Zerstörtes Wachhaus
            ,908   // Weicher Boden
            ,909   // Harter Boden
            ,910   // Glänzender Stein
            ,911   // Besen
            ,912   // Reispflanze
            ,913   // Statue des Schreckens
            ,914   // Fruchtbaum
            ,915   // Punktetafel
            ,916   // Roter Stein
            ,917   // Zerstörte Shinebone-Statue
            ,920   // Kleiner Wegweiser
            ,921   // Großer Wegweiser
            ,922   // Großer Regenbogenkristall
            ,923   // Mittlerer Regenbogenkristall
            ,924   // Kleiner Regenbogenkristall
            ,928   // Gänseblümchen
            ,929   // Schwertlilie
            ,930   // Timespaceportal
            ,931   // Hinweis der Feen
            ,932   // Gelber Seelenstein
            ,933   // Roter Seelenstein
            ,934   // Blauer Seelenstein
            ,938   // Chrysosstatue
            ,941   // Großer rote Blume
            ,944   // Brunnen
            ,953   // Mysteriöser Seelenstein
            ,954   // Grauer Seelenstein
            ,955   // Teleportationsturm
            ,956   // Kleine Lagerfeuer
            ,957   // Großes Lagerfeuer
            ,959   // Eismaschine
            ,985   // Ostersack
            ,988   // Mysteriöses Ei
            ,1264  // Sack mit Kaktusfeigen
            ,1265  // Neils Postkasten
            ,1266  // Gefangener Humad
            ,1271  // Zurückgelassene Truhe der Akamurhändler
            ,1272  // Zerstörte Handelswaren
            ,1273  // Geplünderte Handelswaren
            ,1274  // Zerstörte Akamurfahne
            ,1275  // Erste Handelswaren
            ,1276  // Zweite Handelswaren
            ,1277  // Dritte Handelswaren
            ,1278  // Vierte Handelswaren
            ,1279  // Fünfte Handelswaren
            ,1280  // Sechste Handelswaren
            ,1281  // Truhe mit Befehlslisten
            ,1282  // Verdächtiger Trank
            ,1283  // Kerus merkwürde Truhe
            ,1284  // Gartons merkwürdige Truhe
            ,1285  // Kerus gestohlene Relikte
            ,1286  // Gartons gestohlene Relikte
            ,1287  // Gestohlene Relikte
            ,1288  // Truhe mit Schriftrollen
            ,1289  // Seltsames Reliktstück
            ,1308  // Zerbrochene Relikte
            ,1309  // Zerstörtes Relikt
            ,1310  // Versiegelte Truhe
            ,1311  // Zerbrochene Reliktstücke
            ,1312  // Zerbrochene Reliktstücke
            ,1313  // Zerbrochene Reliktstücke
            ,1314  // Zerbrochene Reliktstücke
            ,1315  // Erste Schrifttafel
            ,1316  // Zweite Schrifttafel
            ,1317  // Dritte Schrifttafel
            ,1318  // Zerbrochene Schrifttafel
            ,1319  // Zerbrochene Schrifttafel
            ,1320  // Zerbrochene Schrifttafel
            ,1321  // Zerbrochene Schrifttafel
            ,1322  // Toter Mönch
            ,1323  // Toter Kleriker
            ,1327  // Truhe mit Feenstaub
            ,1328  // Brief an den Wüstenräuberanführer
            ,1329  // Tagebuch des Reisenden
            ,1330  // Giftflasche
            ,1331  // Ledertasche einer Fee
            ,1332  // Feenstaub der Erde
            ,1333  // Feenstaub des Windes
            ,1334  // Feenstaub des Sandes
            ,1335  // Medizinflasche
            ,1337  // Truhe der Akamurhändler
            ,1338  // Verlorene Truhe (Kerus Lager)
            ,1339  // Verlorene Truhe (Gartons Lager)
            ,1340  // Verlorene Waren (Wüstenräuberlager)
            ,1341  // Verlorene Truhe (Räuberzentrum)
            ,1342  // Unentdeckte Merkwürdige Truhe
            ,1346  // Truhe der Räuberbande
            ,1347  // Schatztruhe des Vergessens
            ,1385  // Halloweenwegweiser
            ,2004  // Eisblume
            ,2320  // Seil
            ,2321  // Verängstigte Geschwister
            ,2350  //     Giftpflanze der Verdammnis
            ,1375  // Ruhende Transporteinheit
            ,1376  // Ruhende Patrouille
            ,1377  // Tote Eliteeinheit
            ,1083  // Ruhender Räuber
            ,1438  // Dunstwolke
            ,1439  // Riesenstrudel
            ,856   // Teleportstein
            ,955   // Teleportationsturm
            ,853   // Toter Kovolt
            ,2018  // Lavafontäne
            ,2345  // Verfaulte^Eierbombe
            ,4280  //
            ,4281  //
            ,4282  //
            ,1436  // Mobile Falle
            ,855   // Schriftrolle der Erleuchtung
            ,860   // Zerbrochene rote Tafel
            ,2352  // none (Erzmagier 20er Sternhagel 1 Stk)
            ,2353  // none (Erzmagier 20er Sternhagel 1 Stk)
            ,2328  // none
            ,2329  // Käsestück
            ,2330  // Grasbüschel
            ,3036 // Feuerobelisk
            ,510 // Small Hellduke
             ,3008 // Warrior
             ,3024 // Magier
            };

        public bool CanAttackEntity(BattleEntity receiver, bool isOwnerCheck = false)
        {
            if (receiver != null && this != receiver && (Hp > 0 || isOwnerCheck) && (receiver.Hp > 0 || isOwnerCheck))
            {
                if (MapMonster != null && CantAttackEntitiesList.Contains(MapMonster.MonsterVNum))
                {
                    return false;
                }
                if (MapNpc != null && CantAttackEntitiesList.Contains(MapNpc.NpcVNum))
                {
                    return false;
                }
                if (receiver.MapMonster != null && CantAttackToEntitiesList.Contains(receiver.MapMonster.MonsterVNum))
                {
                    return false;
                }
                if (receiver.MapNpc != null && CantAttackToEntitiesList.Contains(receiver.MapNpc.NpcVNum))
                {
                    return false;
                }
                if (MapInstance.InstanceBag?.EndState != 0)
                {
                    return false;
                }
                switch (EntityType)
                {
                    case EntityType.Player:
                        {
                            if (Character.Timespace != null && Character.Timespace.InstanceBag.EndState != 0)
                            {
                                return false;
                            }
                            // User in SafeZone
                            if (MapInstance.MapInstanceId == ServerManager.Instance.ArenaInstance.MapInstanceId
                             && ((MapInstance.Map.JaggedGrid[Character.PositionX][Character.PositionY]?.Value != 0 && (MapInstance.Map.JaggedGrid[Character.PositionX][Character.PositionY]?.Value != 16 || Character.PositionY == 35))
                             || (receiver.MapInstance.Map.JaggedGrid[receiver.PositionX][receiver.PositionY]?.Value != 0 && (MapInstance.Map.JaggedGrid[receiver.PositionX][receiver.PositionY]?.Value != 16 || receiver.PositionY == 35))))
                            {
                                return false;
                            }
                            if (MapInstance.MapInstanceId == ServerManager.Instance.FamilyArenaInstance.MapInstanceId
                             && (MapInstance.Map.JaggedGrid[Character.PositionX][Character.PositionY]?.Value != 0
                             || receiver.MapInstance.Map.JaggedGrid[receiver.PositionX][receiver.PositionY]?.Value != 0))
                            {
                                return false;
                            }

                            switch (receiver.EntityType)
                            {
                                case EntityType.Player:
                                    {
                                        if (receiver.Character.InvisibleGm)
                                        {
                                            return false;
                                        }
                                        if (MapInstance.Map.MapTypes.Any(s => s.MapTypeId == (short)MapTypeEnum.Act4))
                                        {
                                            if (Character.Faction != receiver.Character.Faction
                                                && MapInstance.Map.MapId != 130
                                                && MapInstance.Map.MapId != 131)
                                            {
                                                return true;
                                            }
                                        }
                                        else if (MapInstance.Map.MapTypes.Any(m => m.MapTypeId == (short)MapTypeEnum.PVPMap) || MapInstance.IsPVP
                                              || HasBuff(CardType.SpecialEffects, (byte)AdditionalTypes.SpecialEffects.AbleToFightPVP) && receiver.HasBuff(CardType.SpecialEffects, (byte)AdditionalTypes.SpecialEffects.AbleToFightPVP))
                                        {
                                            if (MapInstance == ServerManager.Instance.FamilyArenaInstance
                                                && ((Character.Family != null && receiver.Character.Family != null && Character.Family == receiver.Character.Family)
                                                || Character.Family == null && receiver.Character.Family == null))
                                            {
                                                return false;
                                            }

                                            ConcurrentBag<ArenaTeamMember> team = null;
                                            if (MapInstance.MapInstanceType == MapInstanceType.TalentArenaMapInstance)
                                            {
                                                team = ServerManager.Instance.ArenaTeams.ToList().FirstOrDefault(s => s.Any(o => o.Session == Character.Session));
                                            }

                                            ConcurrentBag<ClientSession> iceteam = null;
                                            if (MapInstance.MapInstanceType == MapInstanceType.IceBreakerInstance)
                                            {
                                                if (IceBreaker.FrozenPlayers.Contains(receiver.Character.Session))
                                                {
                                                    return false;
                                                }
                                                else if (IceBreaker.IceBreakerTeams.FirstOrDefault(t => t.Contains(Character.Session)) != null)
                                                {
                                                    iceteam = IceBreaker.IceBreakerTeams.FirstOrDefault(t => t.Contains(Character.Session));
                                                }
                                            }

                                            if (team != null && team.FirstOrDefault(s => s.Session == Character.Session)?.ArenaTeamType != team.FirstOrDefault(s => s.Session == receiver.Character.Session)?.ArenaTeamType
                                               || MapInstance.MapInstanceType != MapInstanceType.TalentArenaMapInstance &&
                                               (Character.Group == null || !Character.Group.IsMemberOfGroup(receiver.Character.CharacterId))
                                               && (iceteam == null || !iceteam.Contains(receiver.Character.Session)))
                                            {
                                                return true;
                                            }
                                        }
                                    }
                                    break;
                                case EntityType.Mate:
                                    {
                                        if ((receiver.Mate.Owner.IsVehicled || receiver.Mate.Owner.Invisible) && !receiver.Mate.IsTemporalMate)
                                        {
                                            return false;
                                        }
                                        return CanAttackEntity(receiver.Mate.Owner.BattleEntity, true);
                                    }
                                case EntityType.Monster:
                                    if (receiver.MapMonster.Owner == null || receiver.MapMonster.Owner.MapEntityId != MapEntityId && CanAttackEntity(receiver.MapMonster.Owner, true)
                                    || (receiver.MapMonster.Owner.MapEntityId == MapEntityId && MapInstance == Character.Miniland && IsMateTrainer(receiver.MapMonster.MonsterVNum)))
                                    {
                                        if (ServerManager.Instance.ChannelId != 51 || 
                                            receiver.MapMonster.Faction == FactionType.None ||
                                            receiver.MapMonster.Faction != Character.Faction)
                                        {
                                            if (receiver.MapMonster.IsDisabled || (!isOwnerCheck && receiver.MapMonster.IsJumping))
                                            {
                                                return false;
                                            }
                                            if (!IsMateTrainer(receiver.MapMonster.MonsterVNum) && (receiver.MapMonster.Owner?.Character != null || receiver.MapMonster.Owner?.Mate != null))
                                            {
                                                return false;
                                            }
                                            return true;
                                        }
                                    }
                                    break;
                                case EntityType.Npc:
                                    if (receiver.MapNpc.IsDisabled)
                                    {
                                        return false;
                                    }
                                    break;
                            }
                        }
                        break;

                    case EntityType.Mate:
                        {
                            if ((Mate.Owner.IsVehicled || Mate.Owner.Invisible) && !Mate.IsTemporalMate)
                            {
                                return false;
                            }
                            return Mate.Owner.BattleEntity.CanAttackEntity(receiver, true);
                        }

                    case EntityType.Monster:
                        {
                            if (MapMonster.IsDisabled || (!isOwnerCheck && MapMonster.IsJumping))
                            {
                                return false;
                            }
                            if (MapMonster.Owner != null)
                            {
                                if (IsMateTrainer(MapMonster.MonsterVNum))
                                {
                                    if (receiver.Mate != null && receiver.Mate.Owner.CharacterId == MapMonster.Owner.MapEntityId && MapInstance == receiver.Mate.Owner.Miniland)
                                    {
                                        return true;
                                    }
                                    else if (receiver.MapMonster?.Owner != null && receiver.MapMonster.Owner == MapMonster.Owner)
                                    {
                                        return false;
                                    }
                                }
                                return MapMonster.Owner.CanAttackEntity(receiver, true);
                            }
                            else
                            {
                                switch (receiver.EntityType)
                                {
                                    case EntityType.Player:
                                        {
                                            if (receiver.Character.Timespace != null && receiver.Character.Timespace.InstanceBag.EndState != 0)
                                            {
                                                return false;
                                            }
                                            if ((ServerManager.Instance.ChannelId != 51 || 
                                                MapMonster.Faction == FactionType.None ||
                                                MapMonster.Faction != receiver.Character.Faction)
                                              && !receiver.Character.InvisibleGm)
                                            {
                                                return true;
                                            }
                                        }
                                        break;
                                    case EntityType.Mate:
                                        {
                                            if ((receiver.Mate.Owner.IsVehicled || receiver.Mate.Owner.Invisible) && !receiver.Mate.IsTemporalMate)
                                            {
                                                return false;
                                            }
                                            return CanAttackEntity(receiver.Mate.Owner.BattleEntity, true);
                                        }
                                    case EntityType.Monster:
                                        {
                                            if (receiver.MapMonster.IsDisabled || receiver.MapMonster.IsJumping)
                                            {
                                                return false;
                                            }
                                            if (receiver.MapMonster.Owner?.Mate != null)
                                            {
                                                return false;
                                            }
                                            return CanAttackEntity(receiver.MapMonster.Owner, true);
                                        }
                                    case EntityType.Npc:
                                        {
                                            if (receiver.MapNpc.IsDisabled || receiver.MapNpc.Shop != null)
                                            {
                                                return false;
                                            }
                                            return true;
                                        }
                                }
                            }
                        }
                        break;
                    case EntityType.Npc:
                        {
                            if (MapNpc.IsDisabled || MapNpc.Shop != null)
                            {
                                return false;
                            }
                            /*if (MapNpc.Owner != null)
                            {
                                return MapNpc.Owner.BattleEntity.CanAttackEntity(receiver, true);
                            }
                            else*/
                            {
                                switch (receiver.EntityType)
                                {
                                    case EntityType.Player:
                                        return false;
                                        /*{
                                            
                                            if (!receiver.Character.InvisibleGm)
                                            {
                                                return true;
                                            }
                                        }
                                        break;*/
                                    case EntityType.Mate:
                                        {
                                            if ((receiver.Mate.Owner.IsVehicled || receiver.Mate.Owner.Invisible) && !receiver.Mate.IsTemporalMate)
                                            {
                                                return false;
                                            }
                                            return CanAttackEntity(receiver.Mate.Owner.BattleEntity, true);
                                        }
                                    case EntityType.Monster:
                                        {
                                            if (receiver.MapMonster.IsDisabled || receiver.MapMonster.IsJumping)
                                            {
                                                return false;
                                            }
                                            if (receiver.MapMonster.Owner?.Mate != null)
                                            {
                                                return false;
                                            }
                                            if (receiver.MapMonster.Owner != null)
                                            {
                                                return CanAttackEntity(receiver.MapMonster.Owner, true);
                                            }
                                            return true;
                                        }
                                    case EntityType.Npc:
                                        {
                                            if (receiver.MapNpc.IsDisabled)
                                            {
                                                return false;
                                            }
                                            return true;
                                        }
                                }
                            }
                        }
                        break;
                }
            }
            return false;
        }

        public bool IsMateTrainer(int vnum)
        {
            return vnum == 160 || vnum == 900 || vnum == 636 || vnum == 971;
        }

        public bool IsSignpost(int vnum)
        {
            return new int[] { 920, 921, 1385, 1428, 1499, 1519 }.Contains(vnum);
        }

        public bool IsCampfire(int vnum)
        {
            return new int[] { 956, 957, 959 }.Contains(vnum);
        }

        public string GenerateRc(int characterHealth) => $"rc {(short)UserType} {MapEntityId} {characterHealth} 0";

        public string GenerateDm(int dmg) => $"dm {(short)UserType} {MapEntityId} {dmg}";

        public string GenerateTp()
        {
            Character?.WalkDisposable?.Dispose();
            return $"tp {(short)UserType} {MapEntityId} {PositionX} {PositionY} 0";
        }

        public bool HasEntity => Character != null || Mate != null || MapMonster != null || MapNpc != null;

        public List<EventContainer> OnDeathEvents { get; set; }

        public bool IsInRange(int xCoordinate, int yCoordinate, int range = 50) => Math.Abs(PositionX - xCoordinate) <= range && Math.Abs(PositionY - yCoordinate) <= range;

        public void TeleportTo(MapCell mapCell, short distance = 0)
        {
            if (MapInstance?.Map == null || mapCell == null)
            {
                return;
            }

            MapCell mapCellTo = MapInstance.Map.GetRandomPositionByDistance(mapCell.X, mapCell.Y, distance, true) ?? mapCell;

            PositionX = mapCellTo.X;
            PositionY = mapCellTo.Y;
            MapInstance?.Broadcast(GenerateTp());

            RemoveBuff(620);
        }

        #endregion
    }
}