﻿////<auto-generated <- Codemaid exclusion for now (PacketIndex Order is important for maintenance)

using OpenNos.Core;
using OpenNos.Domain;

namespace OpenNos.GameObject.CommandPackets
{
    [PacketHeader("$Position", PassNonParseablePacket = true, Authorities = new AuthorityType[]{ AuthorityType.GM } )]
    public class PositionPacket : PacketDefinition
    {
        public static string ReturnHelp() => "$Position";
    }
}