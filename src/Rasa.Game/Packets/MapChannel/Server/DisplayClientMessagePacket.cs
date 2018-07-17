using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class DisplayClientMessagePacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.DisplayClientMessage;

        public int MsgId { get; set; }                          // from playermessagelanguage.pyo
        public Dictionary<string, string> Args { get; set; }     
        public MsgFilterId Filterid { get; set; }

        public DisplayClientMessagePacket(int msgId, Dictionary<string, string> args, MsgFilterId filterId)
        {
            MsgId = msgId;
            Args = args;
            Filterid = filterId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteInt(MsgId);
            pw.WriteDictionary(Args.Count);
            foreach (var arg in Args)
            {
                pw.WriteString(arg.Key);
                pw.WriteString(arg.Value);
            }
            pw.WriteInt((int)Filterid);
        }
    }
}
