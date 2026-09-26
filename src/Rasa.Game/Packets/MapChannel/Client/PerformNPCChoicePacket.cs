using System.IO;

namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public sealed class PerformNPCChoicePacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.PerformNPCChoice;

        public ulong EntityId { get; set; }
        public uint MissionId { get; set; }
        public uint ObjectiveId { get; set; }
        public uint PlayerFlagId { get; set; }
        public int ChoiceIdx { get; set; }

        public override void Read(PythonReader pr)
        {
            if (pr.ReadTuple() != 5)
                throw new InvalidDataException("NPC choice requires NPC, mission, objective, player flag and choice index.");
            EntityId = pr.ReadULong();
            MissionId = pr.ReadUInt();
            ObjectiveId = pr.ReadUInt();
            PlayerFlagId = pr.ReadUInt();
            ChoiceIdx = pr.ReadInt();
            if (EntityId == 0 || MissionId is 0 or > int.MaxValue ||
                ObjectiveId is 0 or > int.MaxValue || PlayerFlagId > int.MaxValue || ChoiceIdx is < 1 or > 3)
                throw new InvalidDataException("NPC choice requires valid native IDs and an index from 1 through 3.");
        }
    }
}
