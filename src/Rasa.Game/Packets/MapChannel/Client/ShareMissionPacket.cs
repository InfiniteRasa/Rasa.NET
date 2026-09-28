using System.IO;

namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public sealed class ShareMissionPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ShareMission;
        public uint MissionId { get; set; }

        public override void Read(PythonReader pr)
        {
            if (pr.PeekType() != PythonType.Tuple || pr.ReadTuple() != 1 ||
                pr.PeekType() != PythonType.Int)
                throw new InvalidDataException("Mission sharing requires one integer mission field.");
            MissionId = pr.ReadUInt();
        }
    }
}
