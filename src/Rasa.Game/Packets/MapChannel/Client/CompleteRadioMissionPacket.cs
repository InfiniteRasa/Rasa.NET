using System.IO;

namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public sealed class CompleteRadioMissionPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode => GameOpcode.CompleteRadioMission;
        public uint MissionId { get; set; }
        public int? SelectionIdx { get; set; }
        public int? Rating { get; set; }

        public override void Read(PythonReader pr)
        {
            if (pr.ReadTuple() != 3)
                throw new InvalidDataException("Radio mission completion requires mission, selection and rating fields.");
            MissionId = pr.ReadUInt();
            SelectionIdx = pr.ReadNullableInt();
            Rating = pr.ReadNullableInt();
        }
    }
}
