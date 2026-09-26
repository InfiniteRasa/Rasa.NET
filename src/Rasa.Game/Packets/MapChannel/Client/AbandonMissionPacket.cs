using System.IO;

namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class AbandonMissionPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AbandonMission;

        public uint MissionId { get; set; }

        public override void Read(PythonReader pr)
        {
            if (pr.ReadTuple() != 1)
                throw new InvalidDataException("Mission abandonment requires one mission field.");
            MissionId = pr.ReadUInt();
        }
    }
}
