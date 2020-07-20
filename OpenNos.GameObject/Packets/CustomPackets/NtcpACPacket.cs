﻿using OpenNos.Core;

namespace OpenNos.GameObject
{
    [PacketHeader("ntcp_ac")]
    public class NtcpAcPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0)]
        public byte Type { get; set; }

        [PacketIndex(1)]
        public byte TrapValue { get; set; }

        [PacketIndex(2)]
        public string Data { get; set; }

        [PacketIndex(3)]
        public string EncryptedKey { get; set; }

        [PacketIndex(4)]
        public string Crc32 { get; set; }

        [PacketIndex(5)]
        public string Signature { get; set; }

        #endregion
    }
}
