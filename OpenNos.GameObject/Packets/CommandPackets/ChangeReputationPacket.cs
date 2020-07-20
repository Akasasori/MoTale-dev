﻿////<auto-generated <- Codemaid exclusion for now (PacketIndex Order is important for maintenance)

using OpenNos.Core;
using OpenNos.Domain;

namespace OpenNos.GameObject.CommandPackets
{
    [PacketHeader("$ChangeReputation", "$Reputation", "$Rep" , PassNonParseablePacket = true, Authorities = new AuthorityType[]{ AuthorityType.TGM } )]
    public class ChangeReputationPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0)]
        public long Reputation { get; set; }

        public static string ReturnHelp() => "$ChangeReputation | $Reputation <Value>";

        #endregion
    }
}