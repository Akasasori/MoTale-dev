﻿////<auto-generated <- Codemaid exclusion for now (PacketIndex Order is important for maintenance)

using OpenNos.Core;
using OpenNos.Domain;

namespace OpenNos.GameObject.CommandPackets
{
    [PacketHeader("$SPRefill", PassNonParseablePacket = true, Authorities = new AuthorityType[]{ AuthorityType.GS } )]
    public class SPRefillPacket : PacketDefinition
    {
        public static string ReturnHelp() => "$SPRefill";
    }
}