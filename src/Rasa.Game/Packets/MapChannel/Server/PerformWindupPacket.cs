using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class PerformWindupPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.PerformWindup;

        public ActionId ActionId { get; set; }
        public int ActionArgId { get; set; }
        public List<int> Args = new List<int>();

        public PerformWindupPacket(ActionId actionId, int actionArgId)
        {
            ActionId = ActionId;
            ActionArgId = actionArgId;
        }

        public PerformWindupPacket(ActionId actionId, int actionArgId, List<int> args)
        {
            ActionId = ActionId;
            ActionArgId = actionArgId;
            Args = args;
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
