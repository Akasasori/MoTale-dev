﻿////<auto-generated <- Codemaid exclusion for now (PacketIndex Order is important for maintenance)

using OpenNos.Core;
using OpenNos.Domain;

namespace OpenNos.GameObject.CommandPackets
{
    [PacketHeader("$HeroLvl", PassNonParseablePacket = true, Authorities = new AuthorityType[]{ AuthorityType.TGM } )]
    public class ChangeHeroLevelPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0)]
        public byte HeroLevel { get; set; }

        public static string ReturnHelp() => "$HeroLvl <Value>";

        #endregion
    }
}