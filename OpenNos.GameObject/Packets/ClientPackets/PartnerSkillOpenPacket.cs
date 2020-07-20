using OpenNos.Core;

namespace OpenNos.GameObject.Packets.ClientPackets
{
    [PacketHeader("ps_op", PassNonParseablePacket = true)]
    public class PartnerSkillOpenPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0)]
        public byte PetId { get; set; }

        [PacketIndex(1)]
        public byte CastId { get; set; }

        [PacketIndex(2)]
        public bool JustDoIt { get; set; }

        #endregion
    }
}
