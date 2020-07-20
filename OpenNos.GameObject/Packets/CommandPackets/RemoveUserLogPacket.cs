using OpenNos.Core;
using OpenNos.Domain;

namespace OpenNos.GameObject.CommandPackets
{
    [PacketHeader("$RemoveUserLog", PassNonParseablePacket = true, Authorities = new AuthorityType[] { AuthorityType.Administrator })]
    public class RemoveUserLogPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0)]
        public string Username { get; set; }

        #endregion

        #region Methods

        public static string ReturnHelp() => "$RemoveUserLog <Username>";

        #endregion
    }
}