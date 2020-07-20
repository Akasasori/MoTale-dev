﻿/*
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

using OpenNos.Domain;
using System;

namespace OpenNos.Data
{
    [Serializable]
    public class CharacterDTO
    {
        #region Properties

        public long AccountId { get; set; }

        public int Act4Dead { get; set; }

        public int Act4Kill { get; set; }

        public int Act4Points { get; set; }

        public int ArenaWinner { get; set; }

        public string Biography { get; set; }

        public bool BuffBlocked { get; set; }

        public long CharacterId { get; set; }

        public ClassType Class { get; set; }

        public short Compliment { get; set; }

        public float Dignity { get; set; }

        public bool EmoticonsBlocked { get; set; }

        public bool ExchangeBlocked { get; set; }

        public FactionType Faction { get; set; }

        public bool FamilyRequestBlocked { get; set; }

        public bool FriendRequestBlocked { get; set; }

        public GenderType Gender { get; set; }

        public long Gold { get; set; }

        public long GoldBank { get; set; }

        public bool GroupRequestBlocked { get; set; }

        public HairColorType HairColor { get; set; }

        public HairStyleType HairStyle { get; set; }

        public bool HeroChatBlocked { get; set; }

        public byte HeroLevel { get; set; }

        public long HeroXp { get; set; }

        public int Hp { get; set; }

        public bool HpBlocked { get; set; }

        public bool IsPetAutoRelive { get; set; }

        public bool IsPartnerAutoRelive { get; set; }

        public bool IsSeal { get; set; }

        public byte JobLevel { get; set; }

        public long JobLevelXp { get; set; }

        public long LastFamilyLeave { get; set; }

        public byte Level { get; set; }

        public long LevelXp { get; set; }

        public short MapId { get; set; }

        public short MapX { get; set; }

        public short MapY { get; set; }

        public int MasterPoints { get; set; }

        public int MasterTicket { get; set; }

        public byte MaxMateCount { get; set; }

        public byte MaxPartnerCount { get; set; }

        public bool MinilandInviteBlocked { get; set; }

        public string MinilandMessage { get; set; }

        public short MinilandPoint { get; set; }

        public MinilandState MinilandState { get; set; }

        public bool MouseAimLock { get; set; }

        public int Mp { get; set; }

        public string Name { get; set; }

        public bool QuickGetUp { get; set; }

        public long RagePoint { get; set; }

        public long Reputation { get; set; }

        public byte Slot { get; set; }

        public int SpAdditionPoint { get; set; }

        public int SpPoint { get; set; }

        public CharacterState State { get; set; }

        public int TalentLose { get; set; }

        public int TalentSurrender { get; set; }

        public int TalentWin { get; set; }

        public bool WhisperBlocked { get; set; }

    //    public string LockCode { get; set; }

    //    public bool VerifiedLock { get; set; }

        public short VisTit { get; set; }

        public short EffTit { get; set; }

        public byte SwitchLevel()
        {
            return Level;
        }

        public long SwitchHeroLevel()
        {
            return HeroLevel;
        }

        public int ItemShopShip { get; set; }

        public long ArenaDie { get; set; }

        public long ArenaKill { get; set; }

        public long ArenaTc { get; set; }

        #endregion
    }
}