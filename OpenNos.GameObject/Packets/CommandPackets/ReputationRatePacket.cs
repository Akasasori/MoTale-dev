﻿////<auto-generated <- Codemaid exclusion for now (PacketIndex Order is important for maintenance)

using OpenNos.Core;
using OpenNos.Domain;

namespace OpenNos.GameObject.CommandPackets
{
    [PacketHeader("$ReputationRate", PassNonParseablePacket = true, Authorities = new AuthorityType[] { AuthorityType.Administrator })]
    public class ReputationRatePacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0)]
        public int Value { get; set; }

        public static string ReturnHelp() => "$ReputationRate <Value>";

        #endregion
    }
}