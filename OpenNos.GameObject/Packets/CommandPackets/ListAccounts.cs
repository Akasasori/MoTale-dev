﻿////<auto-generated <- Codemaid exclusion for now (PacketIndex Order is important for maintenance)

using OpenNos.Core;
using OpenNos.Domain;

namespace OpenNos.GameObject.CommandPackets
{
    [PacketHeader("$ListAccounts", PassNonParseablePacket = true, Authorities = new AuthorityType[] { AuthorityType.Administrator })]
    public class ListAccountsPacket : PacketDefinition
    {
        #region Properties
        public long AccountId { get; set; }
        public static string ReturnHelp() => "$ListAccounts ACCID";

        #endregion
    }
}