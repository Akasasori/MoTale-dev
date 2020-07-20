using OpenNos.Domain;

namespace OpenNos.Core.Interfaces.Packets.ClientPackets
{
    public interface ICharacterCreatePacket
    {
        string OriginalContent { get; set; }

        string Name { get; set; }

        byte Slot { get; set; }

        GenderType Gender { get; set; }

        HairStyleType HairStyle { get; set; }

        HairColorType HairColor { get; set; }
    }
}
