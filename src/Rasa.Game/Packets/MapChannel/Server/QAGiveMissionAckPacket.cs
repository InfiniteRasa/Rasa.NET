using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// QAGiveMissionAck (503), on SysEntity.ClientMethodId: the Give Mission picker,
    /// <c>([(missionId, missionStatus), ...],)</c>. client/clientmethod.py Recv_QAGiveMissionAck
    /// shows it in the generic list box (GenericListBoxWindow), captioned "Give Mission", as ID,
    /// the mission's name from the client's own text and "Available" - False for ACTIVE, SUCCESS
    /// and COMPLETED. That column is only text: every row can be picked, and the pick is sent as
    /// PrivilegedCommand('givemission', str(missionId)). An empty list shows nothing. The status
    /// values are the client's missionstate ones, which MissionState matches. Sent by
    /// GmMissionCommands for /givemission with no argument.
    /// </summary>
    public class QAGiveMissionAckPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.QAGiveMissionAck;

        public IReadOnlyList<(uint MissionId, MissionState State)> Missions { get; }

        public QAGiveMissionAckPacket(IReadOnlyList<(uint MissionId, MissionState State)> missions)
        {
            Missions = missions ?? new List<(uint, MissionState)>();
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteList(Missions.Count);
            foreach (var (missionId, state) in Missions)
            {
                pw.WriteTuple(2);
                pw.WriteUInt(missionId);
                pw.WriteInt((int)state);
            }
        }
    }
}
