using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class PerformRecoveryPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.PerformRecovery;

        public ActionId ActionId { get; set; }
        public int ActionArgId { get; set; }
        public List<uint> Args = new List<uint>();

        public PerformRecoveryPacket(ActionId actionId, int actionArgId)
        {
            ActionId = actionId;
            ActionArgId = actionArgId;
        }

        public PerformRecoveryPacket(ActionId actionId, int actionArgId, List<uint> args)
        {
            ActionId = actionId;
            ActionArgId = actionArgId;
            Args = args;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteInt((int)ActionId);
            pw.WriteInt(ActionArgId);
            pw.WriteList(Args.Count);
            foreach (var arg in Args)
                pw.WriteUInt(arg);
        }
    }
}
