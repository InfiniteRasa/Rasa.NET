using System.IO;

namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class AssignRadioMissionPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AssignRadioMission;
        public uint MissionId { get; set; }

        public override void Read(PythonReader pr)
        {
            if (pr.ReadTuple() != 1)
                throw new InvalidDataException("Radio mission assignment requires one mission field.");
            MissionId = pr.ReadUInt();
        }
    }
}
