using System.IO;

namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    /// <summary>
    /// DeclineSharedMission (440), a user method: the recipient of a shared mission pressed Decline.
    /// client/ui/conversationwindow.py OnDeclineMissionBtn -> missionlog.py DeclineSharedMission,
    /// with the same (playerId, missionId) AssignSharedMission sends: the sharer's entity id as
    /// DispenseSharedMission gave it (a long), then the mission. The client hides the window and
    /// waits for nothing. Closing the window without choosing sends nothing at all, so an offer can
    /// still end only by expiring.
    /// </summary>
    public sealed class DeclineSharedMissionPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.DeclineSharedMission;
        public ulong SourcePlayerEntityId { get; set; }
        public uint MissionId { get; set; }

        public override void Read(PythonReader pr)
        {
            if (pr.PeekType() != PythonType.Tuple || pr.ReadTuple() != 2 ||
                pr.PeekType() != PythonType.Long)
                throw new InvalidDataException("Shared decline requires a source actor and mission.");
            SourcePlayerEntityId = pr.ReadULong();
            if (pr.PeekType() != PythonType.Int)
                throw new InvalidDataException("Shared decline requires an integer mission field.");
            MissionId = pr.ReadUInt();
        }
    }
}
