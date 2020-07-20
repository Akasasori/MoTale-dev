using OpenNos.Core;
using OpenNos.Domain;

namespace OpenNos.GameObject.CommandPackets
{
    [PacketHeader("$DeleteLock", PassNonParseablePacket = true, Authorities = new AuthorityType[] { AuthorityType.User })]
    public class DeleteLockPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0)]
        public string oldlock { get; set; }

        public static string ReturnHelp()
        {
            return "$DeleteLock ACTUALCODE";
        }

        #endregion
    }
}