using System;

namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;
    using Structures;

    public class LeaveClanPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode => GameOpcode.LeaveClan;

        public uint CharacterId { get; set; }
        public uint ClanId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            CharacterId = pr.ReadUInt();
            ClanId = pr.ReadUInt();
        }
    }
}
