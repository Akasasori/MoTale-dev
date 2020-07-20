using OpenNos.Core;
using OpenNos.Domain;

namespace OpenNos.GameObject.CommandPackets
{
    [PacketHeader("$Unlock", PassNonParseablePacket = true, Authorities = new AuthorityType[] { AuthorityType.User })]
    public class UnlockPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0)]
        public string lockcode { get; set; }

        public static string ReturnHelp()
        {
            return "$Unlock CODE";
        }

        #endregion
    }
}