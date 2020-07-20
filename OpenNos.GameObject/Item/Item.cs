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

using OpenNos.DAL;
using OpenNos.Data;
using OpenNos.Domain;
using System.Collections.Generic;
using System.Linq;

namespace OpenNos.GameObject
{
    public abstract class Item : ItemDTO
    {
        #region Instantiation

        protected Item()
        {
        }

        protected Item(ItemDTO item) => InitializeItem(item);

        #endregion

        #region Properties

        public List<BCard> BCards { get; set; }

        public List<RollGeneratedItemDTO> RollGeneratedItems { get; set; }

        // Vehicles

        public byte[] ActSpeedBoost { get; set; }

        public byte[] MapSpeedBoost { get; set; }

        public byte SpeedBoost { get; set; }

        public byte SpeedBoostDuration { get; set; }

        #endregion

        #region Methods

        public void InitializeItem(ItemDTO input)
        {
            BasicUpgrade = input.BasicUpgrade;
            CellonLvl = input.CellonLvl;
            Class = input.Class;
            CloseDefence = input.CloseDefence;
            Color = input.Color;
            Concentrate = input.Concentrate;
            CriticalLuckRate = input.CriticalLuckRate;
            CriticalRate = input.CriticalRate;
            DamageMaximum = input.DamageMaximum;
            DamageMinimum = input.DamageMinimum;
            DarkElement = input.DarkElement;
            DarkResistance = input.DarkResistance;
            DefenceDodge = input.DefenceDodge;
            DistanceDefence = input.DistanceDefence;
            DistanceDefenceDodge = input.DistanceDefenceDodge;
            Effect = input.Effect;
            EffectValue = input.EffectValue;
            Element = input.Element;
            ElementRate = input.ElementRate;
            EquipmentSlot = input.EquipmentSlot;
            FireElement = input.FireElement;
            FireResistance = input.FireResistance;
            Height = input.Height;
            HitRate = input.HitRate;
            Hp = input.Hp;
            HpRegeneration = input.HpRegeneration;
            IsBlocked = input.IsBlocked;
            IsColored = input.IsColored;
            IsConsumable = input.IsConsumable;
            IsDroppable = input.IsDroppable;
            IsHeroic = input.IsHeroic;
            IsHolder = input.IsHolder;
            IsMinilandObject = input.IsMinilandObject;
            IsSoldable = input.IsSoldable;
            IsTradable = input.IsTradable;
            ItemSubType = input.ItemSubType;
            ItemType = input.ItemType;
            ItemValidTime = input.ItemValidTime;
            LevelJobMinimum = input.LevelJobMinimum;
            LevelMinimum = input.LevelMinimum;
            LightElement = input.LightElement;
            LightResistance = input.LightResistance;
            MagicDefence = input.MagicDefence;
            MaxCellon = input.MaxCellon;
            MaxCellonLvl = input.MaxCellonLvl;
            MaxElementRate = input.MaxElementRate;
            MaximumAmmo = input.MaximumAmmo;
            MinilandObjectPoint = input.MinilandObjectPoint;
            MoreHp = input.MoreHp;
            MoreMp = input.MoreMp;
            Morph = input.Morph;
            Mp = input.Mp;
            MpRegeneration = input.MpRegeneration;
            Name = input.Name;
            Price = input.Price;
            SellToNpcPrice = input.SellToNpcPrice;
            PvpDefence = input.PvpDefence;
            PvpStrength = input.PvpStrength;
            ReduceOposantResistance = input.ReduceOposantResistance;
            ReputationMinimum = input.ReputationMinimum;
            ReputPrice = input.ReputPrice;
            SecondaryElement = input.SecondaryElement;
            Sex = input.Sex;
            Speed = input.Speed;
            SpType = input.SpType;
            Type = input.Type;
            VNum = input.VNum;
            WaitDelay = input.WaitDelay;
            WaterElement = input.WaterElement;
            WaterResistance = input.WaterResistance;
            Width = input.Width;
            BCards = new List<BCard>();
            DAOFactory.BCardDAO.LoadByItemVNum(input.VNum).ToList().ForEach(o => BCards.Add(new BCard(o)));
            RollGeneratedItems = DAOFactory.RollGeneratedItemDAO.LoadByItemVNum(input.VNum).ToList();
            LoadVehicleStats();
        }

        public void LoadVehicleStats()
        {
            if (ItemType == ItemType.Special && Effect == 1000)
            {
                ActSpeedBoost = new byte[100];
                MapSpeedBoost = new byte[100000];
                BCards.Add(new BCard
                {
                    FirstData = 100,
                    SecondData = 336,
                    Type = (byte)BCardType.CardType.Buff,
                    SubType = (byte)AdditionalTypes.Buff.ChanceCausing / 10
                });
                switch (Morph)
                {
                    case 2930: // Marco Pollo
                        ActSpeedBoost[1] = 6;
                        SpeedBoost = 2;
                        SpeedBoostDuration = 3;
                        break;

                    case 2513: // Camello mágico
                        ActSpeedBoost[51] = 6;
                        ActSpeedBoost[52] = 6;
                        SpeedBoost = 2;
                        SpeedBoostDuration = 5;
                        break;

                    case 2368: // Patinete mágico
                        SpeedBoost = 2;
                        SpeedBoostDuration = 3;
                        break;

                    case 2370: // Alfombra mágica
                        ActSpeedBoost[51] = 2;
                        SpeedBoost = 2;
                        SpeedBoostDuration = 3;
                        break;

                    case 2432: // Escobón
                        ActSpeedBoost[4] = 2;
                        SpeedBoost = 3;
                        SpeedBoostDuration = 5;
                        BCards.Add(new BCard
                        {
                            FirstData = 5,
                            Type = (byte)BCardType.CardType.SpecialEffects2,
                            SubType = (byte)AdditionalTypes.SpecialEffects2.TeleportInRadius / 10
                        });
                        break;

                    case 2520: // Bici "Billy Boneshaker"
                        ActSpeedBoost[51] = 2;
                        ActSpeedBoost[52] = 2;
                        SpeedBoost = 3;
                        SpeedBoostDuration = 5;
                        BCards.Add(new BCard
                        {
                            FirstData = 5,
                            Type = (byte)BCardType.CardType.SpecialEffects2,
                            SubType = (byte)AdditionalTypes.SpecialEffects2.TeleportInRadius / 10
                        });
                        break;

                    case 2522: // Patines "Blazing Blades"
                        ActSpeedBoost[51] = 2;
                        ActSpeedBoost[52] = 2;
                        SpeedBoost = 3;
                        SpeedBoostDuration = 5;
                        BCards.Add(new BCard
                        {
                            FirstData = 4,
                            Type = (byte)BCardType.CardType.LightAndShadow,
                            SubType = (byte)AdditionalTypes.LightAndShadow.RemoveBadEffects / 10
                        });
                        break;

                    case 2524: // Monopatín "Doni Darkslide"
                        ActSpeedBoost[51] = 2;
                        ActSpeedBoost[52] = 2;
                        SpeedBoost = 3;
                        SpeedBoostDuration = 5;
                        BCards.Add(new BCard
                        {
                            FirstData = 15,
                            IsLevelScaled = true,
                            Type = (byte)BCardType.CardType.HPMP,
                            SubType = (byte)AdditionalTypes.HPMP.HPRestored / 10
                        });
                        BCards.Add(new BCard
                        {
                            FirstData = 15,
                            IsLevelScaled = true,
                            Type = (byte)BCardType.CardType.HPMP,
                            SubType = (byte)AdditionalTypes.HPMP.MPRestored / 10
                        });
                        break;

                    case 1817: // Esquís mágicos "Iker Alud"
                        ActSpeedBoost[4] = 3;
                        SpeedBoost = 3;
                        SpeedBoostDuration = 5;
                        break;

                    case 1819: // Snowboard mágico
                        ActSpeedBoost[4] = 3;
                        SpeedBoost = 3;
                        SpeedBoostDuration = 5;
                        break;

                    case 2406: // Tigre blanco mágico
                        ActSpeedBoost[4] = 2;
                        SpeedBoost = 2;
                        SpeedBoostDuration = 3;
                        break;

                    case 2411: // Cabrio mágico
                        MapSpeedBoost[1] = 4;
                        MapSpeedBoost[145] = 4;
                        SpeedBoost = 2;
                        SpeedBoostDuration = 3;
                        break;

                    case 2429: // Nublín Núbez
                        SpeedBoost = 5;
                        SpeedBoostDuration = 3;
                        break;

                    case 2517: // Nossi, el dragón
                        ActSpeedBoost[4] = 2;
                        SpeedBoost = 3;
                        SpeedBoostDuration = 5;
                        BCards.Add(new BCard
                        {
                            FirstData = 4,
                            Type = (byte)BCardType.CardType.LightAndShadow,
                            SubType = (byte)AdditionalTypes.LightAndShadow.RemoveBadEffects / 10
                        });
                        break;

                    case 2928: // Ovni estrafalario
                        ActSpeedBoost[4] = 1;
                        SpeedBoostDuration = 5;
                        BCards.Add(new BCard
                        {
                            Type = (byte)BCardType.CardType.SpecialBehaviour,
                            SubType = (byte)AdditionalTypes.SpecialBehaviour.TeleportRandom / 10
                        });
                        break;

                    case 2526: // Unicornio blanco
                        ActSpeedBoost[4] = 1;
                        SpeedBoostDuration = 5;
                        BCards.Add(new BCard
                        {
                            Type = (byte)BCardType.CardType.SpecialBehaviour,
                            SubType = (byte)AdditionalTypes.SpecialBehaviour.TeleportRandom / 10
                        });
                        break;

                    case 2530: // Unicornio negro
                        ActSpeedBoost[51] = 2;
                        ActSpeedBoost[52] = 2;
                        SpeedBoostDuration = 5;
                        BCards.Add(new BCard
                        {
                            Type = (byte)BCardType.CardType.SpecialBehaviour,
                            SubType = (byte)AdditionalTypes.SpecialBehaviour.TeleportRandom / 10
                        });
                        break;

                    case 2528: // Unicornio rosa
                        MapSpeedBoost[1] = 3;
                        MapSpeedBoost[145] = 3;
                        SpeedBoostDuration = 5;
                        BCards.Add(new BCard
                        {
                            Type = (byte)BCardType.CardType.SpecialBehaviour,
                            SubType = (byte)AdditionalTypes.SpecialBehaviour.TeleportRandom / 10
                        });
                        break;

                    case 2936: // Jeep azul
                    case 2938: // Jeep rojo
                    case 2940: // Jeep turquesa
                    case 2942: // Jeep negro
                    case 2944: // Jeep rosa
                    case 2934: // Jeep amarillo
                        ActSpeedBoost[51] = 2;
                        ActSpeedBoost[52] = 2;
                        SpeedBoost = 2;
                        SpeedBoostDuration = 6;
                        break;

                    case 2932: // Tabla a vela
                        ActSpeedBoost[51] = 2;
                        ActSpeedBoost[52] = 2;
                        SpeedBoost = 3;
                        SpeedBoostDuration = 5;
                        BCards.Add(new BCard
                        {
                            FirstData = 4,
                            Type = (byte)BCardType.CardType.LightAndShadow,
                            SubType = (byte)AdditionalTypes.LightAndShadow.RemoveBadEffects / 10
                        });
                        break;              

                    case 3679: // Dragón de huesos mágico
                        ActSpeedBoost[51] = 2;
                        ActSpeedBoost[52] = 2;
                        MapSpeedBoost[1] = 1;
                        MapSpeedBoost[145] = 1;
                        SpeedBoostDuration = 5;
                        BCards.Add(new BCard
                        {
                            Type = (byte)BCardType.CardType.SpecialBehaviour,
                            SubType = (byte)AdditionalTypes.SpecialBehaviour.TeleportRandom / 10
                        });
                        break;

                    case 3693: // Jaguar mágico
                        ActSpeedBoost[51] = 2;
                        ActSpeedBoost[52] = 2;
                        ActSpeedBoost[4] = 3;
                        SpeedBoost = 4;
                        SpeedBoostDuration = 2;
                        BCards.Add(new BCard
                        {
                            FirstData = 4,
                            Type = (byte)BCardType.CardType.LightAndShadow,
                            SubType = (byte)AdditionalTypes.LightAndShadow.RemoveBadEffects / 10
                        });
                        BCards.Add(new BCard
                        {
                            FirstData = 50,
                            SecondData = 665,
                            ForceDelay = SpeedBoostDuration * 10,
                            Type = (byte)BCardType.CardType.Buff,
                            SubType = (byte)AdditionalTypes.Buff.ChanceCausing / 10
                        });
                        break;

                    case 2440:
                        ActSpeedBoost[4] = 1;
                        SpeedBoost = 3;
                        SpeedBoostDuration = 2;
                        break;

                    case 2442:
                        ActSpeedBoost[4] = 3;
                        SpeedBoost = 4;
                        SpeedBoostDuration = 2;
                        BCards.Add(new BCard // Should inflict blind debuff to enemies hitting vehicle while using speed booster, with a chance of 10%
                        {
                            FirstData = 10,
                            SecondData = 37,
                            Type = (byte)BCardType.CardType.Buff,
                            SubType = (byte)AdditionalTypes.Buff.ChanceCausing / 10
                        });
                        break;

                    case 2511:
                        MapSpeedBoost[2628] = 28;
                        SpeedBoost = 4;
                        SpeedBoostDuration = 20;                       
                        break;

                    default:
                        SpeedBoost = 2;
                        SpeedBoostDuration = 3;
                        break;
                }
            }
        }

        //TODO: Convert to PacketDefinition
        public abstract void Use(ClientSession session, ref ItemInstance inv, byte Option = 0, string[] packetsplit = null);

        #endregion
    }
}