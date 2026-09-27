using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    /// <summary>
    /// GmShowUserMissionsAck (461), on SysEntity.ClientMethodId: another player's mission log for a
    /// GM, <c>(userId, displayName, missionInfo)</c>. client/clientmethod.py
    /// Recv_GmShowUserMissionsAck filters missionInfo as the player's own MissionStatusInfo is
    /// filtered and, unless it is empty, opens the intel window's mission log on it with
    /// " (displayName)" after the title. There the current missions window leaves out Share,
    /// Abandon and Radio and puts a GM Complete button (MissionTextRow_GM_Complete) under every
    /// objective not yet completed, which sends ForceCompleteObjective(userId, missionId,
    /// objectiveId). userId is only ever echoed back; this server sends the character id. The view
    /// is a snapshot - the client never refreshes it - so the server sends it again after a change.
    /// Sent by GmMissionCommands. Until then this class was sent by nothing and wrote one MissionInfo
    /// where the client reads a dictionary of them by mission id.
    /// </summary>
    public class GmShowUserMissionsAckPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.GmShowUserMissionsAck;

        public uint UserId { get; }
        public string DisplayName { get; }
        public IReadOnlyDictionary<uint, MissionInfo> MissionInfo { get; }

        public GmShowUserMissionsAckPacket(uint userId, string displayName, IReadOnlyDictionary<uint, MissionInfo> missionInfo)
        {
            UserId = userId;
            DisplayName = displayName ?? "";
            MissionInfo = missionInfo ?? new Dictionary<uint, MissionInfo>();
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteUInt(UserId);
            pw.WriteUnicodeString(DisplayName);
            pw.WriteDictionary(MissionInfo.Count);
            foreach (var entry in MissionInfo)
            {
                pw.WriteUInt(entry.Key);
                pw.WriteStruct(entry.Value);
            }
        }
    }
}
