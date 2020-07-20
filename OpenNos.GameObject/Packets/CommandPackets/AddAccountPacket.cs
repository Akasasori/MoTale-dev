using OpenNos.Core;
using OpenNos.Domain;

namespace OpenNos.GameObject.CommandPackets
{
    [PacketHeader("$AddAccount", PassNonParseablePacket = true, Authorities = new AuthorityType[] { AuthorityType.Administrator })]
    public class AddAccountPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0)]
        public string Name { get; set; }

        [PacketIndex(1)]
        public string Password { get; set; }

        [PacketIndex(2)]
        public int Authority { get; set; }

        public static string ReturnHelp() => "$AddAccount <Name> <Password> <Authority>";

        #endregion
    }
}