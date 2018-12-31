using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class PerformRecoveryPacket : ServerPythonPacket
    {
        /*  PerformRecoveryPacket have few difrenst structures
         *  PerformRecoveryPacket(uint, uint)
         *  PerformRecoveryPacket(uint, uint, uint)
         *  PerformRecoveryPacket(uint, uint, List, List, List, List)
         */
        public override GameOpcode Opcode { get; } = GameOpcode.PerformRecovery;

        public PerformType PerformType { get; set; }
        public ActionId ActionId { get; set; }
        public uint ActionArgId { get; set; }
        public uint Arg { get; set; }
        public List<uint> Args = new List<uint>();

        public PerformRecoveryPacket(PerformType performType, ActionId actionId, uint actionArgId)
        {
            PerformType = performType;
            ActionId = actionId;
            ActionArgId = actionArgId;
        }

        public PerformRecoveryPacket(PerformType performType, ActionId actionId, uint actionArgId, List<uint> args)
        {
            PerformType = performType;
            ActionId = actionId;
            ActionArgId = actionArgId;
            Args = args;
        }

        public PerformRecoveryPacket(PerformType performType, ActionId actionId, uint actionArgId, uint arg)
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
                    pw.WriteUInt(Arg);
                    break;
                case PerformType.ListOfArgs:
                    pw.WriteTuple(6);
                    pw.WriteUInt((uint)ActionId);           // actionId
                    pw.WriteUInt(ActionArgId);              // actionargId
                    pw.WriteList(0);                        // List of hited entities
                    pw.WriteList(0);                        // List of missed entities
                    pw.WriteList(0);                        // List of misses data
                    pw.WriteList(0);                        // List of hits data
                    break;
                default:
                    Logger.WriteLog(LogType.Error, $"Unknown PerformType {PerformType}");
                    break;
            }
        }
    }
}
