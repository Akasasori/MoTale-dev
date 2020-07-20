using OpenNos.Core;
using OpenNos.Domain;
using OpenNos.GameObject.Helpers;
using OpenNos.GameObject.Networking;
using OpenNos.Master.Library.Client;
using OpenNos.Master.Library.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenNos.GameObject.Event.GAMES
{

    public static class WorldRad
    {
        #region Properties

        public static int AngelDamage { get; set; }

        public static MapInstance WorldMapinstance { get; set; }

        public static int DemonDamage { get; set; }

        public static bool IsLocked { get; set; }

        public static bool IsRunning { get; set; }

        public static int RemainingTime { get; set; }

        public static MapInstance UnknownLandMapInstance { get; set; }

        #endregion

        #region Methods

        public static void Run()
        {
            WolrdBoss raidThread = new WolrdBoss();
            Observable.Timer(TimeSpan.FromMinutes(0)).Subscribe(X => raidThread.Run());
        }

        #endregion
    }

    public class WolrdBoss
    {
        #region Methods

        public void Run()
        {
            CommunicationServiceClient.Instance.SendMessageToCharacter(new SCSCharacterMessage
            {
                DestinationCharacterId = null,
                SourceCharacterId = 0,
                SourceWorldId = ServerManager.Instance.WorldId,
                Message = "A random world boss has appeared! Portal opened in Olorun Village!",
                Type = MessageType.Shout
            });

            WorldRad.RemainingTime = 3600;
            const int interval = 1;

            WorldRad.WorldMapinstance = ServerManager.GenerateMapInstance(405, MapInstanceType.WorldBossInstance, new InstanceBag());
            WorldRad.UnknownLandMapInstance = ServerManager.GetMapInstance(ServerManager.GetBaseMapInstanceIdByMapId(2628));
          

            WorldRad.UnknownLandMapInstance.CreatePortal(new Portal
            {
                SourceMapInstanceId = WorldRad.UnknownLandMapInstance.MapInstanceId,
                SourceX = 56,
                SourceY = 39,
                DestinationMapId = 0,
                DestinationX = 40,
                DestinationY = 72,
                DestinationMapInstanceId = WorldRad.WorldMapinstance.MapInstanceId,
                Type = 4
            });

            WorldRad.WorldMapinstance.CreatePortal(new Portal
            {
                SourceMapInstanceId = WorldRad.WorldMapinstance.MapInstanceId,
                SourceX = 40,
                SourceY = 72,
                DestinationMapId = 0,
                DestinationX = 56,
                DestinationY = 39,
                DestinationMapInstanceId = WorldRad.UnknownLandMapInstance.MapInstanceId,
                Type = 4
            });
            List<EventContainer> onDeathEvents = new List<EventContainer>
            {
               new EventContainer(WorldRad.WorldMapinstance, EventActionType.SCRIPTEND, (byte)1)                
        };

            #region Fafnir
            short[] vnums = { 286, 284, 282, 1046, 1044, 2574, 2514, 2504, 2552, 2553 };
            short vnum = vnums[ServerManager.RandomNumber(0, 10)];

            MapMonster FafnirMonster = new MapMonster
            {
                MonsterVNum = vnum,
                MapY = 30,
                MapX = 40,
                MapId = WorldRad.WorldMapinstance.Map.MapId,
                Position = 2,
                IsMoving = true,
                MapMonsterId = WorldRad.WorldMapinstance.GetNextMonsterId(),
                ShouldRespawn = false
            };
            FafnirMonster.Initialize(WorldRad.WorldMapinstance);
            WorldRad.WorldMapinstance.AddMonster(FafnirMonster);
            MapMonster Fafnir = WorldRad.WorldMapinstance.Monsters.Find(s => s.Monster.NpcMonsterVNum == vnum);
            if (Fafnir != null)
            {
                Fafnir.BattleEntity.OnDeathEvents = onDeathEvents;
                Fafnir.IsBoss = true;
            }
            #endregion
            try
            {
                Observable.Timer(TimeSpan.FromMinutes(15)).Subscribe(X => LockRaid());
                Observable.Timer(TimeSpan.FromMinutes(60)).Subscribe(X => EndRaid());
            }
            catch(Exception ex)
            {

            }



        }

        public void EndRaid()
        {
            try
            {
             //   ServerManager.Shout(Language.Instance.GetMessageFromKey("WORDLBOSS_END"), true);

                foreach (ClientSession sess in WorldRad.WorldMapinstance.Sessions.ToList())
                {
                    ServerManager.Instance.ChangeMapInstance(sess.Character.CharacterId, WorldRad.UnknownLandMapInstance.MapInstanceId, sess.Character.MapX, sess.Character.MapY);
                    Thread.Sleep(100);
                }
                EventHelper.Instance.RunEvent(new EventContainer(WorldRad.WorldMapinstance, EventActionType.DISPOSEMAP, null));
                WorldRad.IsRunning = false;
                WorldRad.AngelDamage = 0;
                WorldRad.DemonDamage = 0;
                ServerManager.Instance.StartedEvents.Remove(EventType.WORLDBOSS);
            }
            catch(Exception ex)
            {

            }

        }

        private void LockRaid()
        {
            foreach (Portal p in WorldRad.UnknownLandMapInstance.Portals.Where(s => s.DestinationMapInstanceId == WorldRad.WorldMapinstance.MapInstanceId).ToList())
            {
                WorldRad.UnknownLandMapInstance.Portals.Remove(p);
                WorldRad.UnknownLandMapInstance.Broadcast(p.GenerateGp());
            }
           // ServerManager.Shout(Language.Instance.GetMessageFromKey("WORLDBOSS_LOCKED"), true);
            WorldRad.IsLocked = true;
        }

        #endregion
    }
}