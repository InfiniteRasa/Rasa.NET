using System.IO;

namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    /// <summary>
    /// ForceCompleteObjective (639), a user method: a GM's Complete button under an objective in
    /// another player's mission log (client/ui/currentmissionswindow.py OnGMCompleteObjective),
    /// <c>(userId, missionId, objectiveId)</c>. userId is the one GmShowUserMissionsAck carried,
    /// echoed back as the client got it: an int here, since this server sends the character id,
    /// but a long is read as well. Handled by GmMissionCommands.ForceCompleteObjective.
    /// </summary>
    public sealed class ForceCompleteObjectivePacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ForceCompleteObjective;
        public ulong UserId { get; set; }
        public uint MissionId { get; set; }
        public uint ObjectiveId { get; set; }

        public override void Read(PythonReader pr)
        {
            if (pr.PeekType() != PythonType.Tuple || pr.ReadTuple() != 3)
                throw new InvalidDataException("Forced objective completion requires user, mission and objective fields.");
            UserId = pr.PeekType() switch
            {
                PythonType.Int => pr.ReadUInt(),
                PythonType.Long => pr.ReadULong(),
                _ => throw new InvalidDataException("Forced objective completion requires an integer user field.")
            };
            if (pr.PeekType() != PythonType.Int)
                throw new InvalidDataException("Forced objective completion requires an integer mission field.");
            MissionId = pr.ReadUInt();
            if (pr.PeekType() != PythonType.Int)
                throw new InvalidDataException("Forced objective completion requires an integer objective field.");
            ObjectiveId = pr.ReadUInt();
        }
    }
}
