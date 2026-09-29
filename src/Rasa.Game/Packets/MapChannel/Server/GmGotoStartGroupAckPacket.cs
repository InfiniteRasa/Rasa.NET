using System.Collections.Generic;
using System.Numerics;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// The start groups on the GM's map, for the /gotostartgroup picker: <c>([(name, (x, y, z)), ...],)</c>.
    ///
    /// client/clientmethod.py's Recv_GmGotoStartGroupAck makes each one a (name, name, pos) row of
    /// the waypoint window in its start group mode (UI_SHOW_START_GROUPS), which marks pos on the
    /// map and sends the chosen name back as PrivilegedCommand('gotostartgroup', name). None shows
    /// an empty list.
    /// </summary>
    public class GmGotoStartGroupAckPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.GmGotoStartGroupAck;

        public List<(string Name, Vector3 Position)> StartGroups { get; set; }

        public GmGotoStartGroupAckPacket(List<(string Name, Vector3 Position)> startGroups)
        {
            StartGroups = startGroups;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteList(StartGroups.Count);

            foreach (var (name, position) in StartGroups)
            {
                pw.WriteTuple(2);
                pw.WriteUnicodeString(name);
                pw.WriteTuple(3);
                pw.WriteDouble(position.X);
                pw.WriteDouble(position.Y);
                pw.WriteDouble(position.Z);
            }
        }
    }
}
