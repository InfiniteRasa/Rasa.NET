using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// The maps a GM can kill, for the /killmap picker: the same list as GmGotoMapAck.
    /// client/clientmethod.py's Recv_GmKillMapAck hands it to inputstate/killmap.py, which shows
    /// it in the same DebugMapSelectWindow and sends the pick back as ('killmap', '&lt;mapId&gt;').
    /// </summary>
    public class GmKillMapAckPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.GmKillMapAck;

        public List<uint> ContextIds { get; set; }

        public GmKillMapAckPacket(List<uint> contextIds)
        {
            ContextIds = contextIds;
        }

        public override void Write(PythonWriter pw) => GmGotoMapAckPacket.WriteMapList(pw, ContextIds);
    }
}
