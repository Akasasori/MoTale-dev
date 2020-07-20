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
using OpenNos.GameObject.Helpers;
using OpenNos.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;

namespace OpenNos.GameObject.Event
{
    public static class CaligorRaid
    {
        #region Properties

        public static int AngelDamage { get; set; }

        public static MapInstance CaligorMapInstance { get; set; }

        public static int DemonDamage { get; set; }

        public static bool IsLocked { get; set; }

        public static bool IsRunning { get; set; }

        public static int RemainingTime { get; set; }

        public static MapInstance UnknownLandMapInstance { get; set; }

        #endregion

        #region Methods

        public static void Run()
        {
            CaligorRaidThread raidThread = new CaligorRaidThread();
            Observable.Timer(TimeSpan.FromMinutes(0)).Subscribe(X => raidThread.Run());
        }

        #endregion
    }

    public class CaligorRaidThread
    {
        #region Methods

        public void Run()
        {
            CaligorRaid.RemainingTime = 3600;
            const int interval = 3;

            CaligorRaid.CaligorMapInstance = ServerManager.GenerateMapInstance(154, MapInstanceType.CaligorInstance, new InstanceBag());
            CaligorRaid.UnknownLandMapInstance = ServerManager.GetMapInstance(ServerManager.GetBaseMapInstanceIdByMapId(153));

            CaligorRaid.CaligorMapInstance.CreatePortal(new Portal
            {
                SourceMapId = 154,
                SourceX = 89,
                SourceY = 23,
                DestinationMapId = 153,
                DestinationX = 91,
                DestinationY = 35,
                Type = -1
            });
            CaligorRaid.CaligorMapInstance.CreatePortal(new Portal
            {
                SourceMapId = 154,
                SourceX = 128,
                SourceY = 174,
                DestinationMapId = 153,
                DestinationX = 110,
                DestinationY = 159,
                Type = -1
            });
            CaligorRaid.CaligorMapInstance.CreatePortal(new Portal
            {
                SourceMapId = 154,
                SourceX = 50,
                SourceY = 174,
                DestinationMapId = 153,
                DestinationX = 70,
                DestinationY = 159,
                Type = -1
            });

            CaligorRaid.UnknownLandMapInstance.CreatePortal(new Portal
            {
                SourceMapId = 153,
                SourceX = 70,
                SourceY = 159,
                DestinationMapInstanceId = CaligorRaid.CaligorMapInstance.MapInstanceId,
                Type = -1
            });
            CaligorRaid.UnknownLandMapInstance.CreatePortal(new Portal
            {
                SourceMapId = 153,
                SourceX = 110,
                SourceY = 159,
                DestinationMapInstanceId = CaligorRaid.CaligorMapInstance.MapInstanceId,
                Type = -1
            });
            CaligorRaid.UnknownLandMapInstance.CreatePortal(new Portal
            {
                SourceMapId = 153,
                SourceX = 91,
                SourceY = 36,
                DestinationMapInstanceId = CaligorRaid.CaligorMapInstance.MapInstanceId,
                DestinationX = 89,
                DestinationY = 23,
                Type = -1
            });

            List<EventContainer> onDeathEvents = new List<EventContainer>
            {
                new EventContainer(CaligorRaid.CaligorMapInstance, EventActionType.SCRIPTEND, (byte)1)
            };

            MapMonster caligor = CaligorRaid.CaligorMapInstance.Monsters.Find(s => s.Monster.NpcMonsterVNum == 2305);

            if (caligor != null)
            {
                caligor.BattleEntity.OnDeathEvents = onDeathEvents;
                caligor.IsBoss = true;
            }

            ServerManager.Shout(Language.Instance.GetMessageFromKey("CALIGOR_OPEN"), true);
            ServerManager.Instance.Broadcast(
    $"qnaml 3 #guri^506 {Language.Instance.GetMessageFromKey("HELP_FACTION")}");

            RefreshRaid();

            ServerManager.Instance.Act4RaidStart = DateTime.Now;

            for (int i = 0; i < CaligorRaid.RemainingTime; i += interval)
            {
                Observable.Timer(TimeSpan.FromSeconds(i)).Subscribe(observer =>
                {
                    CaligorRaid.RemainingTime -= interval;
                    RefreshRaid();
                });
            }

            Observable.Timer(TimeSpan.FromSeconds(CaligorRaid.RemainingTime)).Subscribe(observer => EndRaid());
        }

        private void TeleportPlayer(ClientSession sess, int delay)
        {
            Observable.Timer(TimeSpan.FromMilliseconds(delay)).Subscribe(observer =>
            {
                ServerManager.Instance.ChangeMapInstance(sess.Character.CharacterId,
                    CaligorRaid.UnknownLandMapInstance.MapInstanceId, sess.Character.MapX, sess.Character.MapY);
            });
        }

        private void EndRaid()
        {
            ServerManager.Shout(Language.Instance.GetMessageFromKey("CALIGOR_END"), true);

            int delay = 100;
            foreach (ClientSession sess in CaligorRaid.CaligorMapInstance.Sessions.ToList())
            {
                TeleportPlayer(sess, delay);
                delay += 100;
            }
            EventHelper.Instance.RunEvent(new EventContainer(CaligorRaid.CaligorMapInstance, EventActionType.DISPOSEMAP, null));
            CaligorRaid.IsRunning = false;
            CaligorRaid.AngelDamage = 0;
            CaligorRaid.DemonDamage = 0;
            ServerManager.Instance.StartedEvents.Remove(EventType.CALIGOR);
        }

        private void LockRaid()
        {
            foreach (Portal p in CaligorRaid.UnknownLandMapInstance.Portals.Where(s => s.DestinationMapInstanceId == CaligorRaid.CaligorMapInstance.MapInstanceId).ToList())
            {
                CaligorRaid.UnknownLandMapInstance.Portals.Remove(p);
                CaligorRaid.UnknownLandMapInstance.Broadcast(p.GenerateGp());
            }
            CaligorRaid.IsLocked = true;
        }

        private void RefreshRaid()
        {
            int maxHP = ServerManager.GetNpcMonster(2305).MaxHP;
            CaligorRaid.CaligorMapInstance.Broadcast(UserInterfaceHelper.GenerateCHDM(maxHP, CaligorRaid.AngelDamage, CaligorRaid.DemonDamage, CaligorRaid.RemainingTime));
            if (CaligorRaid.RemainingTime < 100)
            { LockRaid(); }
            if (((maxHP / 10) * 8 < CaligorRaid.AngelDamage + CaligorRaid.DemonDamage) && !CaligorRaid.IsLocked)
            {
                LockRaid();
            }
        }

        #endregion
    }
}