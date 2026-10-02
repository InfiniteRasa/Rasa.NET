using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// ChooseInstanceList (685), clientmethod.py Recv_ChooseInstanceList(instances): opens the
    /// waypoint window as the instance picker - "Instance Selection: Select the instance of the
    /// world map to enter." Each row is (ordinal, instanceId, mapTemplateId, startGroup,
    /// overloadedStatus) (ui/waypointwindow.py ShowInstances): the row reads as the map's name -
    /// found from its template - with "(ordinal)" and the population word for the status after
    /// it, the first row is the one selected, and instanceId and startGroup are what
    /// SelectInstance sends back. Closing the window sends SelectInstanceCancel.
    /// </summary>
    public class ChooseInstanceListPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ChooseInstanceList;

        public sealed class Row
        {
            /// <summary>The copy's number as players see it: 1 for the map's own channel.</summary>
            public int Ordinal { get; set; }
            public uint InstanceId { get; set; }
            public uint MapTemplateId { get; set; }
            public int StartGroup { get; set; }
            public MapInstanceStatus Status { get; set; }
        }

        public List<Row> Rows { get; }

        public ChooseInstanceListPacket(List<Row> rows)
        {
            Rows = rows ?? new List<Row>();
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteList(Rows.Count);

            foreach (var row in Rows)
            {
                pw.WriteTuple(5);
                pw.WriteInt(row.Ordinal);
                pw.WriteUInt(row.InstanceId);
                pw.WriteUInt(row.MapTemplateId);
                pw.WriteInt(row.StartGroup);
                pw.WriteUInt((uint)row.Status);
            }
        }
    }
}
