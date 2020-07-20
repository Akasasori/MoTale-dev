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

    public static class ShopShipev
    {
        #region Properties

        public static MapInstance ShopMapInstance { get; set; }

        public static MapInstance UnknownLandMapInstance { get; set; }

        #endregion

        #region Methods

        public static void Run()
        {
            ShopShip raidThread = new ShopShip();
            Observable.Timer(TimeSpan.FromMinutes(0)).Subscribe(X => raidThread.Run());
        }

        #endregion
    }

    public class ShopShip
    {
        #region Methods

        public void Run()
        {
            CommunicationServiceClient.Instance.SendMessageToCharacter(new SCSCharacterMessage
            {
                DestinationCharacterId = null,
                SourceCharacterId = 0,
                SourceWorldId = ServerManager.Instance.WorldId,
                Message = "The Mystic Traveler is here, look for him in Olorun Village!",
                Type = MessageType.Shout
            });

            ShopShipev.ShopMapInstance = ServerManager.GenerateMapInstance(5404, MapInstanceType.ShopShip, new InstanceBag());
            ShopShipev.UnknownLandMapInstance = ServerManager.GetMapInstance(ServerManager.GetBaseMapInstanceIdByMapId(2628));

            MapNpc CaptainEnter = new MapNpc
            {
                NpcVNum = 2306,
                MapX = 73,
                MapY = 69,
                MapId = 2628,
                ShouldRespawn = false,
                IsMoving = false,
                MapNpcId = ShopShipev.UnknownLandMapInstance.GetNextNpcId(),
                Position = 1,
                Name = $"Mystic-Traveler"
            };
            CaptainEnter.Initialize(ShopShipev.UnknownLandMapInstance);
            ShopShipev.UnknownLandMapInstance.AddNPC(CaptainEnter);
            ShopShipev.UnknownLandMapInstance.Broadcast(CaptainEnter.GenerateIn());

            MapNpc CaptainExit = new MapNpc
            {
                NpcVNum = 2306,
                MapX = 15,
                MapY = 5,
                MapId = 5404,
                ShouldRespawn = false,
                IsMoving = false,
                MapNpcId = ShopShipev.ShopMapInstance.GetNextNpcId(),
                Position = 2,
                Name = $"Mystic-Traveler"
            };
            CaptainExit.Initialize(ShopShipev.ShopMapInstance);
            ShopShipev.ShopMapInstance.AddNPC(CaptainExit);
            ShopShipev.ShopMapInstance.Broadcast(CaptainExit.GenerateIn());

            void RemoveNpc()
            {
                ShopShipev.ShopMapInstance.RemoveNpc(CaptainExit);
                CaptainExit.MapInstance.Broadcast(StaticPacketHelper.Out(UserType.Npc, CaptainExit.MapNpcId));

                ShopShipev.UnknownLandMapInstance.RemoveNpc(CaptainEnter);
                CaptainEnter.MapInstance.Broadcast(StaticPacketHelper.Out(UserType.Npc, CaptainEnter.MapNpcId));
            }
            List<EventContainer> onDeathEvents = new List<EventContainer>
            {
               new EventContainer(ShopShipev.ShopMapInstance, EventActionType.SCRIPTEND, (byte)1)
            };

            try
            {
                Observable.Timer(TimeSpan.FromMinutes(1)).Subscribe(X => FirstAnounce());
                Observable.Timer(TimeSpan.FromMinutes(2)).Subscribe(X => FirstAnounce2());
                Observable.Timer(TimeSpan.FromMinutes(3)).Subscribe(X => FirstAnounce3());
                Observable.Timer(TimeSpan.FromMinutes(4)).Subscribe(X => FirstAnounce4());
                Observable.Timer(TimeSpan.FromSeconds(270)).Subscribe(X => FirstAnounce5());

                Observable.Timer(TimeSpan.FromMinutes(5)).Subscribe(X => EndRaid());
                Observable.Timer(TimeSpan.FromSeconds(295)).Subscribe(X => RemoveNpc());
            }
            catch (Exception ex)
            {

            }



        }

        public void EndRaid()
        {
            try
            {
                CommunicationServiceClient.Instance.SendMessageToCharacter(new SCSCharacterMessage
                {
                    DestinationCharacterId = null,
                    SourceCharacterId = 0,
                    SourceWorldId = ServerManager.Instance.WorldId,
                    Message = "The Mystic Traveler vanished!",
                    Type = MessageType.Shout
                });

                foreach (ClientSession sess in ShopShipev.ShopMapInstance.Sessions.ToList())
                {
                    ServerManager.Instance.ChangeMapInstance(sess.Character.CharacterId, ShopShipev.UnknownLandMapInstance.MapInstanceId, sess.Character.MapX, sess.Character.MapY);
                    Thread.Sleep(100);
                }
                EventHelper.Instance.RunEvent(new EventContainer(ShopShipev.ShopMapInstance, EventActionType.DISPOSEMAP, null));
                ServerManager.Instance.StartedEvents.Remove(EventType.SHOPSHIP);
            }
            catch (Exception ex)
            {

            }

        }


        #region Anuncios
        public void FirstAnounce()
        {
            CommunicationServiceClient.Instance.SendMessageToCharacter(new SCSCharacterMessage
            {
                DestinationCharacterId = null,
                SourceCharacterId = 0,
                SourceWorldId = ServerManager.Instance.WorldId,
                Message = "The Mystic Traveler will vanish in 4 minutes.",
                Type = MessageType.Shout
            });
        }

        public void FirstAnounce2()
        {
            CommunicationServiceClient.Instance.SendMessageToCharacter(new SCSCharacterMessage
            {
                DestinationCharacterId = null,
                SourceCharacterId = 0,
                SourceWorldId = ServerManager.Instance.WorldId,
                Message = "The Mystic Traveler will vanish in 3 minutes.",
                Type = MessageType.Shout
            });
        }

        public void FirstAnounce3()
        {
            CommunicationServiceClient.Instance.SendMessageToCharacter(new SCSCharacterMessage
            {
                DestinationCharacterId = null,
                SourceCharacterId = 0,
                SourceWorldId = ServerManager.Instance.WorldId,
                Message = "The Mystic Traveler will vanish in 2 minutes.",
                Type = MessageType.Shout
            });
        }

        public void FirstAnounce4()
        {
            CommunicationServiceClient.Instance.SendMessageToCharacter(new SCSCharacterMessage
            {
                DestinationCharacterId = null,
                SourceCharacterId = 0,
                SourceWorldId = ServerManager.Instance.WorldId,
                Message = "The Mystic Traveler will vanish in 1 minutes.",
                Type = MessageType.Shout
            });
        }

        public void FirstAnounce5()
        {
            CommunicationServiceClient.Instance.SendMessageToCharacter(new SCSCharacterMessage
            {
                DestinationCharacterId = null,
                SourceCharacterId = 0,
                SourceWorldId = ServerManager.Instance.WorldId,
                Message = "The Mystic Traveler will vanish in 30 seconds.",
                Type = MessageType.Shout
            });
        }
        #endregion
        #endregion
    }
}