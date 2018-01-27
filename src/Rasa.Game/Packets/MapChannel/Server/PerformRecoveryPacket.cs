using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class PerformRecoveryPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.PerformRecovery;

        public int ActionId { get; set; }
        public int ActionArgId { get; set; }
        public List<int> Args { get; set; }

        public PerformRecoveryPacket(int actionId, int actionArgId, List<int> args)
        {
            ActionId = actionId;
            ActionArgId = actionArgId;
            Args = args;
        }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteInt(ActionId);
            pw.WriteInt(ActionArgId);
            if (Args.Count > 0)
            {
                pw.WriteList(Args.Count);
                foreach (var arg in Args)
                    pw.WriteInt(arg);
            }
            else
                pw.WriteNoneStruct();
        }
    }
}
