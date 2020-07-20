using OpenNos.Domain;
using System;
using System.Collections.Generic;

namespace OpenNos.GameObject.RainbowBattle
{
    public class RainbowBattleTeam
    {
        public RainbowBattleTeam(IEnumerable<ClientSession> session, RainbowTeamBattleType RmbTeamType)
        {
            Session = session;
            TeamEntity = RmbTeamType;
            TotalFlag = new List<Tuple<int, RainbowNpcType>>();
        }

        public IEnumerable<ClientSession> Session { get; set; }

        public RainbowTeamBattleType TeamEntity { get; set; }

        public List<Tuple<int, RainbowNpcType>> TotalFlag { get; set; }

        public long Score { get; set; }

    }
}