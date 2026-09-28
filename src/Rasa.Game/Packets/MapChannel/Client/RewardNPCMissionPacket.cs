using System.IO;

namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class RewardNPCMissionPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RewardNPCMission;

        public ulong EntityId { get; set; }
        public uint MissionId { get; set; }
        public int? SelectionIdx { get; set; }
        public int? Rating { get; set; }

        public override void Read(PythonReader pr)
        {
            if (pr.ReadTuple() != 4)
                throw new InvalidDataException(
                    "NPC mission reward requires NPC, mission, selection and rating fields.");
            EntityId = pr.ReadULong();
            MissionId = pr.ReadUInt();
            SelectionIdx = pr.ReadNullableInt();
            Rating = pr.ReadNullableInt();
        }
    }
}
