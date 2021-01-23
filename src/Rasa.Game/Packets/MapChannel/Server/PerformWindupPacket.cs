using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class PerformWindupPacket : ServerPythonPacket
    {
        /*  PerformWindupPacket have few difrenst structures
         *  PerformWindupPacket(uint, uint)
         *  PerformWindupPacket(uint, uint, uint)
         */
        public override GameOpcode Opcode { get; } = GameOpcode.PerformWindup;

        public PerformType PerformType { get; set; }
        public ActionId ActionId { get; set; }
        public uint ActionArgId { get; set; }
        public ulong Arg { get; set; }
        public List<int> Args = new List<int>();

        public PerformWindupPacket(PerformType performType, ActionId actionId, uint actionArgId)
        {
            PerformType = performType;
            ActionId = actionId;
            ActionArgId = actionArgId;
        }

        public PerformWindupPacket(PerformType performType, ActionId actionId, uint actionArgId, ulong arg)
        {
            PerformType = performType;
            ActionId = actionId;
            ActionArgId = actionArgId;
            Arg = arg;
        }

        public override void Write(PythonWriter pw)
        {
            switch (PerformType)
            {
                case PerformType.TwoArgs:
                    pw.WriteTuple(2);
                    pw.WriteUInt((uint)ActionId);
                    pw.WriteUInt(ActionArgId);
                    break;
                case PerformType.ThreeArgs:
                    pw.WriteTuple(3);
                    pw.WriteUInt((uint)ActionId);
                    pw.WriteUInt(ActionArgId);
                    if (Arg != 0)
                        pw.WriteULong(Arg);
                    else
                        pw.WriteNoneStruct();
                    break;
                default:
                    Logger.WriteLog(LogType.Error, $"Unknown PerformType {PerformType}");
                    break;
            }
        }
    }
}
