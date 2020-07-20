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

using OpenNos.Core;
using OpenNos.Data;
using OpenNos.Domain;
using OpenNos.GameObject.Event;
using OpenNos.GameObject.Helpers;
using OpenNos.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenNos.GameObject
{
    public class Group : IDisposable
    {
        #region Members

        private readonly object _syncObj = new object();
        private bool _disposed;
        private int _order;

        #endregion

        #region Instantiation

        public Group()
        {
            Sessions = new ThreadSafeGenericList<ClientSession>();
            GroupId = ServerManager.Instance.GetNextGroupId();
            _order = 0;
        }

        #endregion

        #region Properties

        public int SessionCount => Sessions.Count;

        public ThreadSafeGenericList<ClientSession> Sessions { get; }

        public long GroupId { get; set; }

        public GroupType GroupType { get; set; }

        public ScriptedInstance Raid { get; set; }

        public byte SharingMode { get; set; }

        public TalentArenaBattle TalentArenaBattle { get; set; }

        #endregion

        #region Methods

        public void Dispose()
        {
            if (!_disposed)
            {
                Dispose(true);
                GC.SuppressFinalize(this);
                _disposed = true;
            }
        }

        public List<string> GeneratePst(ClientSession player)
        {
            List<string> str = new List<string>();
            int i = 0;
            str.AddRange(player.Character.Mates.Where(s => s.IsTeamMember).OrderByDescending(s => s.MateType).Select(mate => $"pst 2 {mate.MateTransportId} {((short)mate.MateType == 1 ? ++i : 0)} {(int)(mate.Hp / mate.MaxHp * 100)} {(int)(mate.Mp / mate.MaxMp * 100)} {mate.Hp} {mate.Mp} 0 0 0 {mate.Buff.GetAllItems().Aggregate("", (current, buff) => current + $" {buff.Card.CardId}")}"));
            Sessions.Where(s => s != player).ForEach(session =>
            {
                str.Add($"pst 1 {session.Character.CharacterId} {++i} {(int)(session.Character.Hp / session.Character.HPLoad() * 100)} {(int)(session.Character.Mp / session.Character.MPLoad() * 100)} {session.Character.HPLoad()} {session.Character.MPLoad()} {(byte)session.Character.Class} {(byte)session.Character.Gender} {(session.Character.UseSp ? session.Character.Morph : 0)}{session.Character.Buff.GetAllItems().Where(s => !s.StaticBuff || new short[] { 339, 340 }.Contains(s.Card.CardId)).Aggregate("", (current, buff) => current + $" {buff.Card.CardId}")}");
            });
            Sessions.Where(s => s == player).ForEach(session =>
            {
                i = session.Character.Mates.Count(s => s.IsTeamMember);
                str.Add($"pst 1 {session.Character.CharacterId} {++i} {(int)(session.Character.Hp / session.Character.HPLoad() * 100)} {(int)(session.Character.Mp / session.Character.MPLoad() * 100)} {session.Character.HPLoad()} {session.Character.MPLoad()} {(byte)session.Character.Class} {(byte)session.Character.Gender} {(session.Character.UseSp ? session.Character.Morph : 0)}");
            });
            return str;
        }

        public string GenerateRdlst()
        {
            string result;

            if (GroupType != GroupType.GiantTeam)
            {
                result = $"rdlst {Raid?.LevelMinimum ?? 1} {Raid?.LevelMaximum ?? 99} 0";
            }
            else
            {
                result = $"rdlstf {Raid?.LevelMinimum ?? 1} {Raid?.LevelMaximum ?? 99} 0 0";
            }

            try
            {
                Sessions.ForEach(session => result += $" {session.Character.Level}.{(session.Character.UseSp || session.Character.IsVehicled ? session.Character.Morph : -1)}.{(short)session.Character.Class}.{Raid?.InstanceBag.DeadList.Count(s => s == session.Character.CharacterId) ?? 0}.{session.Character.Name}.{(short)session.Character.Gender}.{session.Character.CharacterId}.{session.Character.HeroLevel}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, nameof(GenerateRdlst));
            }

            return result;
        }
        public string GenerateFblst()
        {
            string result = string.Empty;
            result = $"fblst ";
            try
            {
                Sessions.ForEach(session => result += $" {session.Character.SwitchLevel()}.{(session.Character.UseSp || session.Character.IsVehicled ? session.Character.Morph : -1)}.{(short)session.Character.Class}.{Raid?.InstanceBag.DeadList.Count(s => s == session.Character.CharacterId) ?? 0}.{session.Character.Name}.{(short)session.Character.Gender}.{session.Character.CharacterId}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, nameof(GenerateRdlst));
            }
            return result;
        }

        public string GeneraterRaidmbf(ClientSession session) => $"raidmbf {session?.CurrentMapInstance?.InstanceBag?.MonsterLocker.Initial} {session?.CurrentMapInstance?.InstanceBag?.MonsterLocker.Current} {session?.CurrentMapInstance?.InstanceBag?.ButtonLocker.Initial} {session?.CurrentMapInstance?.InstanceBag?.ButtonLocker.Current} {Raid?.InstanceBag?.Lives - Raid?.InstanceBag?.DeadList.Count} {Raid?.InstanceBag?.Lives} {(GroupType == GroupType.GiantTeam ? 0 : 25)}";

        public long? GetNextOrderedCharacterId(Character character)
        {
            lock (_syncObj)
            {
                _order++;
                List<ClientSession> sessions = Sessions.Where(s => Map.GetDistance(s.Character, character) < 50);
                if (_order > sessions.Count - 1) // if order wents out of amount of ppl, reset it -> zero based index
                {
                    _order = 0;
                }

                if (sessions.Count == 0) // group seems to be empty
                {
                    return null;
                }

                return sessions[_order].Character.CharacterId;
            }
        }

        public bool IsLeader(ClientSession session)
        {
            if (Sessions.Count > 0)
            {
                return Sessions.FirstOrDefault() == session;
            }
            else
            {
                return false;
            }
        }

        public bool IsMemberOfGroup(long entityId) => 
            Sessions?.Any(s => s?.Character != null && (s.Character.CharacterId == entityId || s.Character.Mates.Any(m => m.IsTeamMember && m.MateTransportId == entityId))) == true;

        public bool IsMemberOfGroup(ClientSession session) => Sessions?.Any(s => s?.Character?.CharacterId == session.Character.CharacterId) == true;

        public void JoinGroup(long characterId)
        {
            ClientSession session = ServerManager.Instance.GetSessionByCharacterId(characterId);
            if (session != null)
            {
                JoinGroup(session);
            }
        }

        public bool JoinGroup(ClientSession session)
        {
            if (Sessions.Count > 0 && session.Character.IsBlockedByCharacter(Sessions.FirstOrDefault().Character.CharacterId))
            {
                session.SendPacket(
                    UserInterfaceHelper.GenerateInfo(
                        Language.Instance.GetMessageFromKey("BLACKLIST_BLOCKED")));
                return false;
            }
            if (Raid != null)
            {
                var entries = Raid.DailyEntries - session.Character.GeneralLogs.CountLinq(s => s.LogType == "InstanceEntry" && short.Parse(s.LogData) == Raid.Id && s.Timestamp.Date == DateTime.Today);
                if (Raid.DailyEntries > 0 && entries <= 0)
                {
                    session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("INSTANCE_NO_MORE_ENTRIES"), 0));
                    session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("INSTANCE_NO_MORE_ENTRIES"), 10));
                    return false;
                }
                if (Raid.RequiredItems != null)
                {
                    foreach (Gift requiredItem in Raid.RequiredItems)
                    {
                        if (ServerManager.GetItem(requiredItem.VNum).Type == InventoryType.Equipment
                        && !session.Character.Inventory.Any(s => s.ItemVNum == requiredItem.VNum && s.Type == InventoryType.Wear))
                        {
                            session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                string.Format(Language.Instance.GetMessageFromKey("ITEM_NOT_EQUIPPED"),
                                    ServerManager.GetItem(requiredItem.VNum).Name), 0));
                            return false;
                        }
                    }
                }
            }

            session.Character.Group = this;
            Sessions.Add(session);
            if (GroupType == GroupType.Group)
            {
                if (Sessions.Find(c => c.Character.IsCoupleOfCharacter(session.Character.CharacterId)) is ClientSession couple)
                {
                    session.Character.AddStaticBuff(new StaticBuffDTO { CardId = 319 }, true);
                    couple.Character.AddStaticBuff(new StaticBuffDTO { CardId = 319 }, true);
                }
            }

            return true;
        }

        public void LeaveGroup(ClientSession session)
        {
            session.Character.Group = null;
            if (Sessions.Find(c => c.Character.IsCoupleOfCharacter(session.Character.CharacterId)) is ClientSession couple)
            {
                session.Character.RemoveBuff(319, true);
                couple.Character.RemoveBuff(319, true);
            }
            Sessions.RemoveAll(s => s?.Character.CharacterId == session.Character.CharacterId);
            if (IsLeader(session) && GroupType != GroupType.Group && Sessions.Count > 1)
            {
                Sessions.ForEach(s => s.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("TEAM_LEADER_CHANGE"), Sessions.ElementAt(0).Character?.Name), 0)));
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Sessions.Dispose();
            }
        }

        #endregion
    }
}