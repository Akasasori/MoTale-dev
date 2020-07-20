using OpenNos.Core;
using OpenNos.Domain;

namespace OpenNos.GameObject.Packets.CommandPackets
{
    [PacketHeader("$Buy", PassNonParseablePacket = true, Authorities = new AuthorityType[] { AuthorityType.Donator })]
    public class BuyPacket : PacketDefinition
    {
        #region Properties
        [PacketIndex(0)]
        public string Item { get; set; }
        [PacketIndex(1)]
        public ushort Amount { get; set; }
        public static string ReturnHelp()
        {
            return "$Buy <Item> <Amount (max 999)>";
        }
        #endregion
    }
}
