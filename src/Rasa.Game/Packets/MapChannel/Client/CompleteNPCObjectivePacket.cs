using System.IO;

namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class CompleteNPCObjectivePacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.CompleteNPCObjective;

        public ulong EntityId { get; set; }
        public uint MissionId { get; set; }
        public uint ObjectiveId { get; set; }
        public uint PlayerFlagId { get; set; }

        public override void Read(PythonReader pr)
        {
            if (pr.ReadTuple() != 4)
                throw new InvalidDataException(
                    "NPC objective completion requires NPC, mission, objective and player flag fields.");
            EntityId = pr.ReadULong();
            MissionId = pr.ReadUInt();
            ObjectiveId = pr.ReadUInt();
            PlayerFlagId = pr.ReadUInt();
        }
    }
}
