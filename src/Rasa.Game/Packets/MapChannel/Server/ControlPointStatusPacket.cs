using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    /// <summary>
    /// ControlPointStatus (814): client/controlpointmanager.py Recv_ControlPointStatus(statusList),
    /// on the client's control point manager (SysEntity.ClientControlPointManagerId). Each entry is
    /// a ControlPointStatus struct; the client keeps them by control point id and posts
    /// UI_CONTROL_POINT_STATUS_UPDATED. The answer to RequestControlPointStatus (817)
    /// (Battlegrounds.RequestControlPointStatus).
    ///
    /// On the record rather than of use, as PvPEnabled is: the request and the list were the
    /// challenge board's, the window clans were to bid for control points in. In the 1.16.5.0
    /// client that window never registers for the event that opens it, so the retail client does
    /// not ask; nothing handles UI_CONTROL_POINT_STATUS_UPDATED; and the window's own reader
    /// expects a later form of the struct (bids, ownership, clanId) than the one the client
    /// still unpacks.
    /// </summary>
    public class ControlPointStatusPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ControlPointStatus;

        public List<ControlPointStatus> StatusList { get; }

        public ControlPointStatusPacket(List<ControlPointStatus> statusList)
        {
            StatusList = statusList ?? new List<ControlPointStatus>();
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteList(StatusList.Count);

            foreach (var status in StatusList)
                pw.WriteStruct(status);
        }
    }
}
