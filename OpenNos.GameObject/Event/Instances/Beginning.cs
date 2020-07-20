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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using OpenNos.GameObject.Networking;

namespace OpenNos.GameObject.Event
{
    public static class JOBMAP
    {
        #region Methods

        public static void GenerateJOBMAP(int Vnum, ClientSession host)
        {
            List<ClientSession> sessions = new List<ClientSession>();
            if (host.Character.Group != null)
            {
                sessions = host.Character.Group.Sessions.Where(s => s.Character.MapInstance.MapInstanceType == MapInstanceType.Beginner);
            }
            else
            {
                sessions.Add(host);
            }
            List<Tuple<MapInstance, byte>> maps = new List<Tuple<MapInstance, byte>>();
            MapInstance map = null;
            byte instancelevel = 1;
            if (Vnum == 1)
            {
                instancelevel = 1;
            }
            map = ServerManager.GenerateMapInstance(4421, MapInstanceType.Beginner, new InstanceBag());
            maps.Add(new Tuple<MapInstance, byte>(map, instancelevel));

            if (map != null)
            {
                foreach (ClientSession s in sessions)
                {
                    ServerManager.Instance.TeleportOnRandomPlaceInMap(s, map.MapInstanceId);
                }
            }

            foreach (Tuple<MapInstance, byte> mapinstance in maps)
            {
                PSTTask task = new PSTTask();
                Observable.Timer(TimeSpan.FromMinutes(0)).Subscribe(X => PSTTask.Run(mapinstance));
            }
        }

        #endregion

        #region Classes

        public class PSTTask
        {
            #region Methods

            public static void Run(Tuple<MapInstance, byte> mapinstance)
            {
               
               
            }

            #endregion
        }

        #endregion
    }
}