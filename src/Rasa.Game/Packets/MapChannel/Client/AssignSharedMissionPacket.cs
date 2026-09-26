using System.IO;

namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public sealed class AssignSharedMissionPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AssignSharedMission;
        public ulong SourcePlayerEntityId { get; set; }
        public uint MissionId { get; set; }

        public override void Read(PythonReader pr)
        {
            if (pr.PeekType() != PythonType.Tuple || pr.ReadTuple() != 2 ||
                pr.PeekType() != PythonType.Long)
                throw new InvalidDataException("Shared assignment requires a source actor and mission.");
            SourcePlayerEntityId = pr.ReadULong();
            if (pr.PeekType() != PythonType.Int)
                throw new InvalidDataException("Shared assignment requires an integer mission field.");
            MissionId = pr.ReadUInt();
        }
    }
}
