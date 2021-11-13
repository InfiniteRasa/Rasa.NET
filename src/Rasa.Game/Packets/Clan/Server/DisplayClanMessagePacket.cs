using System.Collections.Generic;

namespace Rasa.Packets.Clan.Server
{
    using Data;
    using Memory;

    public class DisplayClanMessagePacket : ServerPythonPacket
    {
        public override GameOpcode Opcode => GameOpcode.DisplayClanMessage;

        public int MsgId { get; set; }

        public Dictionary<string, string> Args { get; set; }

        public DisplayClanMessagePacket(int msgId, Dictionary<string, string> args)
        {
            MsgId = msgId;
            Args = args;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteInt(MsgId);
            pw.WriteDictionary(Args.Count);
            foreach(var arg in Args)
            {
                pw.WriteString(arg.Key);
                pw.WriteString(arg.Value);
            }            
        }
    }
}
