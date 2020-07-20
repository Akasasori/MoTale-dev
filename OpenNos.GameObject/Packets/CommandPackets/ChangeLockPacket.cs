using OpenNos.Core;
using OpenNos.Domain;

namespace OpenNos.GameObject.CommandPackets
{
    [PacketHeader("$ChangeLock", PassNonParseablePacket = true, Authorities = new AuthorityType[] { AuthorityType.User })]
    public class ChangeLockPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0)]
        public string oldlock { get; set; }

        [PacketIndex(1)]
        public string newlock { get; set; }

        public static string ReturnHelp()
        {
            return "$ChangeLock ACTUALCODE NEWCODE";
        }

        #endregion
    }
}