using System.IO;

namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class AssignNPCMissionPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AssignNPCMission;

        public ulong NpcEntityId { get; set; }
        public uint MissionId { get; set; }

        public override void Read(PythonReader pr)
        {
            if (pr.ReadTuple() != 2)
                throw new InvalidDataException("NPC mission assignment requires NPC and mission fields.");
            NpcEntityId = pr.ReadULong();
            MissionId = pr.ReadUInt();
        }
    }
}
