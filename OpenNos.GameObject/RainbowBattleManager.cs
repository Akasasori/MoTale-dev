using OpenNos.Domain;
using OpenNos.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace OpenNos.GameObject.RainbowBattle
{
    public class RainbowBattleManager
    {

        public static IDisposable ObservableFlag { get; set; }

        public static void GenerateScore(RainbowBattleTeam RainbowBattle)
        {
            var first = GetFlag(RainbowBattle, RainbowNpcType.First);
            var Second = GetFlag(RainbowBattle, RainbowNpcType.Second);
            var Last = GetFlag(RainbowBattle, RainbowNpcType.Last);

            var total = first + (Second * 2) + (Last * 5);

            RainbowBattle.Score += total;

            SendFbs();
        }

        public static void GenerateScoreForAll()
        {
            var RainbowTeam = ServerManager.Instance.RainbowBattleMembers?.First(s => s.TeamEntity == RainbowTeamBattleType.Blue);

            var RainbowTeam2 = ServerManager.Instance.RainbowBattleMembers?.First(s => s.TeamEntity == RainbowTeamBattleType.Red);

            GenerateScore(RainbowTeam);
            GenerateScore(RainbowTeam2);
        }

        public static void SendIcoFlagOnMinimap(ClientSession sess, long npcId, byte score, byte team)
        {
            sess.CurrentMapInstance?.Broadcast($"fbt 6 {npcId} {score} {team}");
        }

        public static void AddFlag(RainbowBattleTeam RainbowBattle, RainbowNpcType type, int npcId)
        {
            if (RainbowBattle == null)
            {
                return;
            }


            RainbowBattle.TotalFlag.Add(new Tuple<int, RainbowNpcType>(npcId, type));

            var RainbowTeam2 = ServerManager.Instance.RainbowBattleMembers.First(s => s != RainbowBattle);

            if (RainbowTeam2 == null)
            {
                return;
            }

            if (AlreadyHaveFlag(RainbowTeam2, type, npcId))
            {
                RemoveFlag(RainbowTeam2, type, npcId);
            }

            SendFbs();
        }

        public static void RemoveFlag(RainbowBattleTeam RainbowBattle, RainbowNpcType type, int NpcId)
        {
            if (RainbowBattle == null)
            {
                return;
            }

            RainbowBattle.TotalFlag.RemoveAll(s => s.Item1 == NpcId && s.Item2 == type);
        }

        private static int GetFlag(RainbowBattleTeam RainbowBattleTeam, RainbowNpcType type)
        {
            if (RainbowBattleTeam == null)
            {
                return 0;
            }

            return RainbowBattleTeam.TotalFlag.FindAll(s => s.Item2 == type).Count();
        }

        public static bool AlreadyHaveFlag(RainbowBattleTeam RainbowBattleTeam, RainbowNpcType type, int NpcId)
        {
            if (RainbowBattleTeam == null)
            {
                return false;
            }

            var a = RainbowBattleTeam.TotalFlag.FindAll(s => s.Item1 == NpcId && s.Item2 == type).Count();

            return a == 0 ? false : true;
        }

        public static void EndEvent(MapInstance map)
        {
            map.IsPVP = false;
            ObservableFlag.Dispose();

            var RainbowTeam = ServerManager.Instance.RainbowBattleMembers.First(s => s.TeamEntity == RainbowTeamBattleType.Blue);

            var RainbowTeam2 = ServerManager.Instance.RainbowBattleMembers.First(s => s.TeamEntity == RainbowTeamBattleType.Red);

            if (RainbowTeam == null) return;
            if (RainbowTeam2 == null) return;

            var teamWinner = (RainbowTeam.Score > RainbowTeam2.Score ? true : false);

            SendGift(RainbowTeam.Session, teamWinner);
            SendGift(RainbowTeam2.Session, !teamWinner);

            ServerManager.Instance.RainbowBattleMembers = null;
        }

        private static void SendGift(IEnumerable<ClientSession> sess, bool winner)
        {
            foreach (var ses in sess)
            {
                if (AreNotInMap(ses))
                {
                    continue;
                }

                ses.Character.Group?.LeaveGroup(ses);
                ServerManager.Instance.UpdateGroup(ses.Character.CharacterId);
                ses.SendPacket(ses.Character.GenerateSay($"You {(winner ? "win" : "lose")} this rainbow battle", 10));
                ses.SendPacket(ses.Character.GenerateRaid(2, true));

                // Winner team
                if (winner)
                {
                    ses.Character.GiftAdd(1, 1);
                }
                else // Loser team
                {
                    ses.Character.GiftAdd(1, 1);
                }

                Observable.Timer(TimeSpan.FromSeconds(5)).Subscribe(o =>
                {
                    ServerManager.Instance.ChangeMap(ses.Character.CharacterId, ses.Character.MapId, ses.Character.MapX, ses.Character.MapY);
                });
            }
        }

        public static bool AreNotInMap(ClientSession ses)
        {
            if (ses.CurrentMapInstance?.MapInstanceType != Domain.MapInstanceType.RainbowBattle)
            {
                return true;
            }
            return false;
        }

        public static void SendFbs()
        {
            var RainbowTeam = ServerManager.Instance.RainbowBattleMembers.First(s => s.TeamEntity == RainbowTeamBattleType.Blue);

            var RainbowTeam2 = ServerManager.Instance.RainbowBattleMembers.First(s => s.TeamEntity == RainbowTeamBattleType.Red);

            if (RainbowTeam == null)
            {
                return;
            }

            foreach (var bb in RainbowTeam.Session)
            {
                if (AreNotInMap(bb))
                {
                    continue;
                }

                bb.SendPacket(
                    $"fbs " +
                    $"{(byte)RainbowTeam.TeamEntity} " +
                    $"{RainbowTeam.Session.Count()} " +
                    $"{RainbowTeam2.Score} " +
                    $"{RainbowTeam.Score} " +
                    $"{GetFlag(RainbowTeam, RainbowNpcType.First)} " +
                    $"{GetFlag(RainbowTeam, RainbowNpcType.Second)} " +
                    $"{GetFlag(RainbowTeam, RainbowNpcType.Last)} " +
                    $"{RainbowTeam.TeamEntity}");
            }

            if (RainbowTeam2 == null)
            {
                return;
            }

            foreach (var bb in RainbowTeam2.Session)
            {
                if (AreNotInMap(bb))
                {
                    continue;
                }

                bb.SendPacket(
                    $"fbs " +
                    $"{(byte)RainbowTeam2.TeamEntity} " +
                    $"{RainbowTeam2.Session.Count()} " +
                    $"{RainbowTeam2.Score} " +
                    $"{RainbowTeam.Score} " +
                    $"{GetFlag(RainbowTeam2, RainbowNpcType.First)} " +
                    $"{GetFlag(RainbowTeam2, RainbowNpcType.Second)} " +
                    $"{GetFlag(RainbowTeam2, RainbowNpcType.Last)} " +
                    $"{RainbowTeam2.TeamEntity}");
            }
        }
    }
}
