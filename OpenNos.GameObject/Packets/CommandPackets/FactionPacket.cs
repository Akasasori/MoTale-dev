﻿////<auto-generated <- Codemaid exclusion for now (PacketIndex Order is important for maintenance)

using OpenNos.Core;
using OpenNos.Domain;

namespace OpenNos.GameObject.CommandPackets
{
    [PacketHeader("$Faction", PassNonParseablePacket = true, Authorities = new AuthorityType[]{ AuthorityType.TGM } )]
    public class FactionPacket : PacketDefinition
    {
        public static string ReturnHelp() => "$Faction";
    }
}