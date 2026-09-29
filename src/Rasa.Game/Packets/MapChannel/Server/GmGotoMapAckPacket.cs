using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// The maps a GM can go to, for the /gotomap picker: <c>([(ordinal, mapId, templateId, ctxId), ...],)</c>.
    ///
    /// client/clientmethod.py's Recv_GmGotoMapAck hands the list to inputstate/gotomap.py, whose
    /// ProcessMapList names each row from ctxId (planet, continent and map, and the context type,
    /// with "(ordinal)" after it when ordinal is not None) and keeps mapId to send back as
    /// PrivilegedCommand('gotomap', '&lt;mapId&gt; &lt;startGroup&gt;'). templateId is unpacked and never
    /// used. There is one instance of each map here, so ordinal and templateId are None and mapId
    /// is the context id.
    /// </summary>
    public class GmGotoMapAckPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.GmGotoMapAck;

        public List<uint> ContextIds { get; set; }

        public GmGotoMapAckPacket(List<uint> contextIds)
        {
            ContextIds = contextIds;
        }

        public override void Write(PythonWriter pw) => WriteMapList(pw, ContextIds);

        /// <summary>The one argument GmGotoMapAck and GmKillMapAck both carry.</summary>
        public static void WriteMapList(PythonWriter pw, List<uint> contextIds)
        {
            pw.WriteTuple(1);
            pw.WriteList(contextIds.Count);

            foreach (var contextId in contextIds)
            {
                pw.WriteTuple(4);
                pw.WriteNoneStruct();       // ordinal
                pw.WriteUInt(contextId);    // mapId
                pw.WriteNoneStruct();       // templateId
                pw.WriteUInt(contextId);    // ctxId
            }
        }
    }
}
