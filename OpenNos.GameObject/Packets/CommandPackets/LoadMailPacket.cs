using OpenNos.Core;
using OpenNos.Domain;

namespace OpenNos.GameObject.CommandPackets
{
    [PacketHeader("$LoadMail", PassNonParseablePacket = true, Authorities = new AuthorityType[] { AuthorityType.User })]
    public class LoadMailPacket : PacketDefinition
    {
        #region Methods

        public static string ReturnHelp() => "$LoadMail";

        #endregion
    }
}