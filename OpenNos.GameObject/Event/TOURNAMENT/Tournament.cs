using OpenNos.Core;
using OpenNos.Domain;
using OpenNos.GameObject.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using OpenNos.GameObject.Networking;

namespace OpenNos.GameObject.Event.TOURNAMENT
{
    internal class Tournament
    {
        #region Methods
        internal static void GenerateTournament()
        {
            ServerManager.Instance.Broadcast(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("TOURNAMENT_MINUTES"), 5), 0));
            ServerManager.Instance.Broadcast(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("TOURNAMENT_MINUTES"), 5), 1));
            Thread.Sleep(4 * 60 * 1000);
            ServerManager.Instance.Broadcast(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("TOURNAMENT_MINUTES"), 1), 0));
            ServerManager.Instance.Broadcast(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("TOURNAMENT_MINUTES"), 1), 1));
            Thread.Sleep(30 * 1000);
            ServerManager.Instance.Broadcast(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("TOURNAMENT_SECONDS"), 30), 0));
            ServerManager.Instance.Broadcast(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("TOURNAMENT_SECONDS"), 30), 1));
            Thread.Sleep(20 * 1000);
            ServerManager.Instance.Broadcast(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("TOURNAMENT_SECONDS"), 10), 0));
            ServerManager.Instance.Broadcast(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("TOURNAMENT_SECONDS"), 10), 1));
            Thread.Sleep(10 * 1000);
            ServerManager.Instance.Broadcast(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("TOURNAMENT_STARTED"), 1));
            ServerManager.Instance.Broadcast("qnaml 100 #guri^506 The Tournament is starting! Join now!");
            ServerManager.Instance.EventInWaiting = true;
            Thread.Sleep(30 * 1000);
            ServerManager.Instance.Sessions.Where(s => s.Character?.IsWaitingForEvent == false).ToList().ForEach(s => s.SendPacket("esf"));
            ServerManager.Instance.EventInWaiting = false;
            IEnumerable<ClientSession> sessions = ServerManager.Instance.Sessions.Where(s => s.Character?.IsWaitingForEvent == true && s.Character.MapInstance.MapInstanceType == MapInstanceType.BaseMapInstance);

            MapInstance map = ServerManager.GenerateMapInstance(2004, MapInstanceType.EventGameInstance, new InstanceBag());
            if (map != null)
            {
                foreach (ClientSession sess in sessions)
                {
                    ServerManager.Instance.TeleportOnRandomPlaceInMap(sess, map.MapInstanceId);
                }

                ServerManager.Instance.Sessions.Where(s => s.Character != null).ToList().ForEach(s => s.Character.IsWaitingForEvent = false);
                ServerManager.Instance.StartedEvents.Remove(EventType.TOURNAMENT);

                TournamentThread task = new TournamentThread();
                Observable.Timer(TimeSpan.FromSeconds(10)).Subscribe(X => task.Run(map));
            }
        }
        #endregion

        #region Classes
        public void SpawnUser()
        {

        }

        public class TournamentThread
        {
            #region Members

            private MapInstance _map;

            #endregion

            public void Run(MapInstance map)
            {
                _map = map;

                foreach (ClientSession session in _map.Sessions)
                {
                    ServerManager.Instance.TeleportOnRandomPlaceInMap(session, map.MapInstanceId);
                    session.Character.Speed = 20;
                    session.Character.IsCustomSpeed = true;
                    session.Character.ArenaWinner = 0;
                    session.Character.MorphUpgrade = 0;
                    session.Character.MorphUpgrade2 = 0;
                    session.CurrentMapInstance.IsPVP = true;
                    session.SendPacket(session.Character.GenerateCond());
                    session.Character.LastSpeedChange = DateTime.Now;
                    session.CurrentMapInstance?.Broadcast(session.Character.GenerateCMode());
                }

                int i = 0;

                while (_map?.Sessions?.Any() == true)
                {
                    runRound(i++);
                }

                //ended
            }

            private static IEnumerable<Tuple<short, int, short, short>> generateDrop(Map map, short vnum, int amountofdrop, int amount)
            {
                List<Tuple<short, int, short, short>> dropParameters = new List<Tuple<short, int, short, short>>();
                for (int i = 0; i < amountofdrop; i++)
                {
                    MapCell cell = map.GetRandomPosition();
                    dropParameters.Add(new Tuple<short, int, short, short>(vnum, amount, cell.X, cell.Y));
                }
                return dropParameters;
            }

            private void runRound(int number)
            {
                if (number == 6)
                {
                    endEvent();
                }

                int amount = 120 + (60 * number);

                int i = amount;
                while (i != 0)
                {
                    spawnCircle(number);
                    Thread.Sleep(60000 / amount);
                    i--;
                }
                Thread.Sleep(5000);
                string round = "";
                switch (number)
                {
                    case 1:
                        round = "first";
                        _map.Broadcast(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("TOURNAMENT_ROUND"), round), 0));
                        break;
                    case 2:
                        round = "second";
                        _map.Broadcast(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("TOURNAMENT_ROUND"), round), 0));
                        break;
                    case 3:
                        round = "three";
                        _map.Broadcast(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("TOURNAMENT_ROUND"), round), 0));
                        break;
                    case 4:
                        round = "fourth";
                        _map.Broadcast(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("TOURNAMENT_ROUND"), round), 0));
                        break;
                    case 5:
                        round = "ultimate";
                        _map.Broadcast(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("TOURNAMENT_ROUND"), round), 0));
                        break;
                }
                
                Thread.Sleep(5000);

                // Your dropped reward
                _map.DropItems(generateDrop(_map.Map, 1046, 20, 200 * ((number + 1) > 10 ? 10 : (number + 1))).ToList());
                _map.DropItems(generateDrop(_map.Map, 1030, 10, 3 * ((number + 1) > 10 ? 10 : (number + 1))).ToList());
                _map.DropItems(generateDrop(_map.Map, 2282, 10, 3 * ((number + 1) > 10 ? 10 : (number + 1))).ToList());
                _map.DropItems(generateDrop(_map.Map, 2514, 5, 1 * ((number + 1) > 10 ? 10 : (number + 1))).ToList());
                _map.DropItems(generateDrop(_map.Map, 2515, 5, 1 * ((number + 1) > 10 ? 10 : (number + 1))).ToList());
                _map.DropItems(generateDrop(_map.Map, 2516, 5, 1 * ((number + 1) > 10 ? 10 : (number + 1))).ToList());
                _map.DropItems(generateDrop(_map.Map, 2517, 5, 1 * ((number + 1) > 10 ? 10 : (number + 1))).ToList());
                _map.DropItems(generateDrop(_map.Map, 2518, 5, 1 * ((number + 1) > 10 ? 10 : (number + 1))).ToList());
                _map.DropItems(generateDrop(_map.Map, 2519, 5, 1 * ((number + 1) > 10 ? 10 : (number + 1))).ToList());
                _map.DropItems(generateDrop(_map.Map, 2520, 5, 1 * ((number + 1) > 10 ? 10 : (number + 1))).ToList());
                _map.DropItems(generateDrop(_map.Map, 2521, 5, 1 * ((number + 1) > 10 ? 10 : (number + 1))).ToList());
                foreach (ClientSession session in _map.Sessions)
                {
                    // Your reward that every player should get
                }

                Thread.Sleep(30000);
            }

            private void endEvent()
            {
                ServerManager.Instance.Broadcast(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("TOURNAMENT_FINISHED"), 15), 1));
                Thread.Sleep(15 * 1000);
                foreach (ClientSession session in _map.Sessions)
                {
                    ServerManager.Instance.ChangeMap(session.Character.CharacterId, 2628, 70, 69);
                }
            }

            private void spawnCircle(int round)
            {
                if (_map != null)
                {
                    MapCell cell = _map.Map.GetRandomPosition();

                    int circleId = _map.GetNextMonsterId();
                    short[] monster =
                    {
                        1, 1, 1, 1, 1
                    };
                    MapMonster circle = new MapMonster { MonsterVNum = monster[round], MapX = cell.X, MapY = cell.Y, MapMonsterId = circleId, IsHostile = false, IsMoving = false, ShouldRespawn = false };
                    circle.Initialize(_map);
                    circle.NoAggresiveIcon = false;
                    _map.AddMonster(circle);
                    _map.Broadcast(circle.GenerateIn());
                    _map.Broadcast(StaticPacketHelper.GenerateEff(UserType.Monster, circleId, 4660));
                    if (_map != null)
                    {
                        _map.Broadcast(StaticPacketHelper.SkillUsed(UserType.Monster, circleId, 3, circleId, 1220, 220, 0, 4983, cell.X, cell.Y, true, 0, 65535, 0, 0));
                        foreach (Character character in _map.GetCharactersInRange(cell.X, cell.Y, 2))
                        {
                            if (!_map.Sessions.Skip(3).Any())
                            {
                                //Regalo para los 3 ultimos supervivientes.
                                character.Inventory.AddNewToInventory(1, 1).FirstOrDefault();
                            }
                            character.IsCustomSpeed = false;
                            character.RemoveVehicle();
                            Observable.Timer(TimeSpan.FromMilliseconds(1000)).Subscribe(o => ServerManager.Instance.AskRevive(character.CharacterId));
                        }
                        _map.Broadcast(StaticPacketHelper.Out(UserType.Monster, circle.MapMonsterId));
                    }
                }
            }
        }
        #endregion
    }
}
