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
using System.Reactive.Linq;
using System.Threading;

namespace OpenNos.GameObject.Event
{
    public static class LOD
    {
        #region Methods

        public static void GenerateLod()
        {
            const int LOD_TIME = 90;
            const int HORN_TIME = 60;
            const int HORN_STAY_TIME = 1;
            const int HORN_RESPAWN_TIME = 4;

            EventHelper.Instance.RunEvent(new EventContainer(ServerManager.GetMapInstance(ServerManager.GetBaseMapInstanceIdByMapId(98)), EventActionType.NPCSEFFECTCHANGESTATE, true));
            LODThread lodThread = new LODThread();
            Observable.Timer(TimeSpan.FromMinutes(0)).Subscribe(s => lodThread.Run(LOD_TIME * 60, (HORN_TIME + 1) * 60, (HORN_RESPAWN_TIME + HORN_STAY_TIME) * 60, HORN_STAY_TIME * 60));
        }

        #endregion
    }

    public class LODThread
    {
        #region Members

        public bool IsOpen { get; set; } = true;

        #endregion

        #region Methods

        public void Run(int lodTime, int hornTime, int hornRespawn, int hornStay)
        {
            ChangePortalEffect(855);

            const int interval = 30;
            int dhspawns = 0;

            while (lodTime > 0)
            {
                refreshLOD(lodTime);

                if (lodTime == hornTime || (lodTime == hornTime - (hornRespawn * dhspawns)))
                {
                    foreach (Family fam in ServerManager.Instance.FamilyList.GetAllItems())
                    {
                        if (fam.LandOfDeath != null)
                        {
                            EventHelper.Instance.RunEvent(new EventContainer(fam.LandOfDeath, EventActionType.CHANGEXPRATE, 2));
                            EventHelper.Instance.RunEvent(new EventContainer(fam.LandOfDeath, EventActionType.CHANGEDROPRATE, 2));
                            spawnDH(fam.LandOfDeath);
                        }
                    }
                }
                else if (lodTime == hornTime - (hornRespawn * dhspawns) - hornStay)
                {
                    foreach (Family fam in ServerManager.Instance.FamilyList.GetAllItems())
                    {
                        if (fam.LandOfDeath != null)
                        {
                            despawnDH(fam.LandOfDeath);
                        }
                    }

                    dhspawns++;
                }

                lodTime -= interval;
                Thread.Sleep(interval * 1000);
            }

            endLOD();
        }

        private void ChangePortalEffect(short effectId)
        {
            ServerManager.Instance.GetMapNpcsByVNum(453).ForEach(mapNpc => mapNpc.Effect = effectId);
        }

        private void despawnDH(MapInstance LandOfDeath)
        {
            EventHelper.Instance.RunEvent(new EventContainer(ServerManager.GetMapInstance(ServerManager.GetBaseMapInstanceIdByMapId(98)), EventActionType.NPCSEFFECTCHANGESTATE, false));
            EventHelper.Instance.RunEvent(new EventContainer(LandOfDeath, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("HORN_DISAPEAR"), 0)));
            EventHelper.Instance.RunEvent(new EventContainer(LandOfDeath, EventActionType.UNSPAWNMONSTERS, 443));

            if (IsOpen)
            {
                IsOpen = false;

                ChangePortalEffect(0);
            }
        }

        private void endLOD()
        {
            foreach (Family fam in ServerManager.Instance.FamilyList.GetAllItems())
            {
                if (fam.LandOfDeath != null)
                {
                    EventHelper.Instance.RunEvent(new EventContainer(fam.LandOfDeath, EventActionType.DISPOSEMAP, null));
                    fam.LandOfDeath = null;
                }
            }

            ServerManager.Instance.StartedEvents.Remove(EventType.LOD);
        }

        private void refreshLOD(int remaining)
        {
            foreach (Family fam in ServerManager.Instance.FamilyList.GetAllItems())
            {
                if (fam.LandOfDeath == null)
                {
                    fam.LandOfDeath = ServerManager.GenerateMapInstance(150, MapInstanceType.LodInstance, new InstanceBag());
                }

                EventHelper.Instance.RunEvent(new EventContainer(fam.LandOfDeath, EventActionType.CLOCK, remaining * 10));
                EventHelper.Instance.RunEvent(new EventContainer(fam.LandOfDeath, EventActionType.STARTCLOCK, new Tuple<List<EventContainer>, List<EventContainer>>(new List<EventContainer>(), new List<EventContainer>())));
            }
        }

        private void spawnDH(MapInstance LandOfDeath)
        {
            EventHelper.Instance.RunEvent(new EventContainer(LandOfDeath, EventActionType.SPAWNONLASTENTRY, 443));
            EventHelper.Instance.RunEvent(new EventContainer(LandOfDeath, EventActionType.SENDPACKET, "df 2"));
            EventHelper.Instance.RunEvent(new EventContainer(LandOfDeath, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("HORN_APPEAR"), 0)));
        }

        #endregion
    }
}