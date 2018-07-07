using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class PerformRecoveryPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.PerformRecovery;

        public ActionId ActionId { get; set; }
        public int ActionArgId { get; set; }
        public List<int> Args = new List<int>();

        public PerformRecoveryPacket(ActionId actionId, int actionArgId)
        {
            ActionId = actionId;
            ActionArgId = actionArgId;
        }

        public PerformRecoveryPacket(ActionId actionId, int actionArgId, List<int> args)
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
            pw.WriteTuple(2 + Args.Count);
            pw.WriteInt((int)ActionId);
            pw.WriteInt(ActionArgId);
            foreach (var arg in Args)
                pw.WriteInt(arg);
        }
    }
}
