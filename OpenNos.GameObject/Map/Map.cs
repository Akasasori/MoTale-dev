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

using OpenNos.Core.ArrayExtensions;
using OpenNos.DAL;
using OpenNos.Data;
using OpenNos.PathFinder;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenNos.GameObject.Networking;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace OpenNos.GameObject
{
    public class Map : IMapDTO
    {
        #region Members

        //private readonly Random _random;

        //Function to get a random number 
        private static readonly Random random = new Random();
        private static readonly object syncLock = new object();
        public static int RandomNumber(int min, int max)
        {
            lock (syncLock)
            { // synchronize
                return random.Next(min, max);
            }
        }
        #endregion

        #region Instantiation

        public Map(short mapId, short secondMapId, byte[] data)
        {
            MapId = mapId;
            SecondMapId = secondMapId;
            Data = data;
            loadZone();
            MapTypes = new List<MapTypeDTO>();
            foreach (MapTypeMapDTO maptypemap in DAOFactory.MapTypeMapDAO.LoadByMapId(mapId).ToList())
            {
                MapTypeDTO maptype = DAOFactory.MapTypeDAO.LoadById(maptypemap.MapTypeId);
                MapTypes.Add(maptype);
            }

            if (MapTypes.Count > 0 && MapTypes[0].RespawnMapTypeId != null)
            {
                long? respawnMapTypeId = MapTypes[0].RespawnMapTypeId;
                long? returnMapTypeId = MapTypes[0].ReturnMapTypeId;
                if (respawnMapTypeId != null)
                {
                    DefaultRespawn = DAOFactory.RespawnMapTypeDAO.LoadById((long)respawnMapTypeId);
                }
                if (returnMapTypeId != null)
                {
                    DefaultReturn = DAOFactory.RespawnMapTypeDAO.LoadById((long)returnMapTypeId);
                }
            }
        }

        #endregion

        #region Properties

        public byte[] Data { get; set; }

        public RespawnMapTypeDTO DefaultRespawn { get; }

        public RespawnMapTypeDTO DefaultReturn { get; }

        public GridPos[][] JaggedGrid { get; set; }

        public short MapId { get; set; }

        public short SecondMapId { get; set; }

        public List<MapTypeDTO> MapTypes { get; }

        public int Music { get; set; }

        public string Name { get; set; }

        public bool ShopAllowed { get; set; }

        private ConcurrentBag<MapCell> Cells { get; set; }

        internal int XLength { get; set; }

        internal int YLength { get; set; }

        public byte XpRate { get; set; }

        #endregion

        #region Methods
        public static MapCell GetNextStep(MapCell start, MapCell end, double steps)
        {
            MapCell futurPoint;
            double newX = start.X;
            double newY = start.Y;

            if (start.X < end.X)
            {
                newX = start.X + (steps);
                if (newX > end.X)
                    newX = end.X;
            }
            else if (start.X > end.X)
            {
                newX = start.X - (steps);
                if (newX < end.X)
                    newX = end.X;
            }
            if (start.Y < end.Y)
            {
                newY = start.Y + (steps);
                if (newY > end.Y)
                    newY = end.Y;
            }
            else if (start.Y > end.Y)
            {
                newY = start.Y - (steps);
                if (newY < end.Y)
                    newY = end.Y;
            }

            futurPoint = new MapCell { X = (short)newX, Y = (short)newY };
            return futurPoint;
        }

        public static int GetDistance(Character character1, Character character2) => GetDistance(new MapCell { X = character1.PositionX, Y = character1.PositionY }, new MapCell { X = character2.PositionX, Y = character2.PositionY });

        public static int GetDistance(MapCell p, MapCell q) => (int)Heuristic.Octile(Math.Abs(p.X - q.X), Math.Abs(p.Y - q.Y));

        public IEnumerable<MonsterToSummon> GenerateMonsters(short vnum, short amount, bool move, List<EventContainer> deathEvents, bool isBonus = false, bool isHostile = true, bool isBoss = false)
        {
            List<MonsterToSummon> SummonParameters = new List<MonsterToSummon>();
            for (int i = 0; i < amount; i++)
            {
                MapCell cell = GetRandomPosition();
                SummonParameters.Add(new MonsterToSummon(vnum, cell, null, move, isBonus: isBonus, isHostile: isHostile, isBoss: isBoss) { DeathEvents = deathEvents });
            }
            return SummonParameters;
        }

        public List<NpcToSummon> GenerateNpcs(short vnum, short amount, List<EventContainer> deathEvents, bool isMate, bool isProtected, bool move, bool isHostile)
        {
            List<NpcToSummon> SummonParameters = new List<NpcToSummon>();
            for (int i = 0; i < amount; i++)
            {
                MapCell cell = GetRandomPosition();
                SummonParameters.Add(new NpcToSummon(vnum, cell, -1, isProtected, isMate, move, isHostile) { DeathEvents = deathEvents });
            }
            return SummonParameters;
        }

        public MapCell GetRandomPosition()
        {
            if (Cells == null)
            {
                Cells = new ConcurrentBag<MapCell>();
                Parallel.For(0, YLength, y => Parallel.For(0, XLength, x =>
                {
                    if (!IsBlockedZone(x, y) && CanWalkAround(x, y))
                    {
                        Cells.Add(new MapCell { X = (short)x, Y = (short)y });
                    }
                }));
            }
            return Cells.OrderBy(s => RandomNumber(0, int.MaxValue)).FirstOrDefault();
        }

        public bool CanWalkAround(int x, int y)
        {
            for (int dX = -1; dX <= 1; dX++)
            {
                for (int dY = -1; dY <= 1; dY++)
                {
                    if (dX == 0 && dY == 0)
                    {
                        continue;
                    }

                    if (!IsBlockedZone(x + dX, y + dY))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        
        public MapCell GetRandomPositionByDistance(short xPos, short yPos, short distance, bool randomInRange = false)
        {
            if (Cells == null)
            {
                Cells = new ConcurrentBag<MapCell>();
                Parallel.For(0, YLength, y => Parallel.For(0, XLength, x =>
                {
                    if (!IsBlockedZone(x, y))
                    {
                        Cells.Add(new MapCell { X = (short)x, Y = (short)y });
                    }
                }));
            }
            if (randomInRange)
            {
                return Cells.Where(s => GetDistance(new MapCell { X = xPos, Y = yPos }, new MapCell { X = s.X, Y = s.Y }) <= distance && !isBlockedZone(xPos, yPos, s.X, s.Y)).OrderBy(s => RandomNumber(0, int.MaxValue)).FirstOrDefault();
            }
            else
            {
                return Cells.Where(s => GetDistance(new MapCell { X = xPos, Y = yPos }, new MapCell { X = s.X, Y = s.Y }) <= distance && !isBlockedZone(xPos, yPos, s.X, s.Y)).OrderBy(s => RandomNumber(0, int.MaxValue)).OrderByDescending(s => GetDistance(new MapCell { X = xPos, Y = yPos }, new MapCell { X = s.X, Y = s.Y })).FirstOrDefault();
            }
        }

        public bool IsBlockedZone(int x, int y)
        {
            try
            {
                if (JaggedGrid == null
                    || (MapId == 2552 && y > 38)
                    || x < 0
                    || y < 0
                    || x >= JaggedGrid.Length
                    || JaggedGrid[x] == null
                    || y >= JaggedGrid[x].Length
                    || JaggedGrid[x][y] == null
                    )
                {
                    return true;
                }

                return !JaggedGrid[x][y].IsWalkable();
            }
            catch
            {
                return true;
            }
        }

        internal bool GetFreePosition(ref short firstX, ref short firstY, byte xpoint, byte ypoint)
        {
            short MinX = (short)(-xpoint + firstX);
            short MaxX = (short)(xpoint + firstX);

            short MinY = (short)(-ypoint + firstY);
            short MaxY = (short)(ypoint + firstY);

            List<MapCell> cells = new List<MapCell>();
            for (short y = MinY; y <= MaxY; y++)
            {
                for (short x = MinX; x <= MaxX; x++)
                {
                    if (x != firstX || y != firstY)
                    {
                        cells.Add(new MapCell { X = x, Y = y });
                    }
                }
            }
            foreach (MapCell cell in cells.OrderBy(s => RandomNumber(0, int.MaxValue)))
            {
                if (!isBlockedZone(firstX, firstY, cell.X, cell.Y))
                {
                    firstX = cell.X;
                    firstY = cell.Y;
                    return true;
                }
            }
            return false;
        }

        public bool isBlockedZone(int firstX, int firstY, int mapX, int mapY)
        {
            if (IsBlockedZone(mapX, mapY) || !CanWalkAround(mapX, mapY))
            {
                return true;
            }
            for (int i = 1; i <= Math.Abs(mapX - firstX); i++)
            {
                if (IsBlockedZone(firstX + (Math.Sign(mapX - firstX) * i), firstY))
                {
                    return true;
                }
            }
            for (int i = 1; i <= Math.Abs(mapY - firstY); i++)
            {
                if (IsBlockedZone(firstX, firstY + (Math.Sign(mapY - firstY) * i)))
                {
                    return true;
                }
            }
            return false;
        }

        private void loadZone()
        {
            using (BinaryReader reader = new BinaryReader(new MemoryStream(Data)))
            {
                XLength = reader.ReadInt16();
                YLength = reader.ReadInt16();

                JaggedGrid = JaggedArrayExtensions.CreateJaggedArray<GridPos>(XLength, YLength);
                for (short i = 0; i < YLength; ++i)
                {
                    for (short t = 0; t < XLength; ++t)
                    {
                        JaggedGrid[t][i] = new GridPos
                        {
                            Value = reader.ReadByte(),
                            X = t,
                            Y = i,
                        };
                    }
                }
            }
        }

        #endregion
    }
}