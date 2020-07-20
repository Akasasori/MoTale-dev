using OpenNos.Core;
using OpenNos.Domain;

namespace OpenNos.GameObject.CommandPackets
{
    [PacketHeader("$GiveMall", PassNonParseablePacket = true, Authorities = new AuthorityType[] { AuthorityType.Administrator })]
    public class GiveMallPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0)]
        public short Amount { get; set; }

        [PacketIndex(1)]
        public string CharacterName { get; set; }

        public static string ReturnHelp() => "$GiveMall <Amount> <Nickname>";

        #endregion
    }
}