using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

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
        public MissileArgs MissileArgs { get; set; }

        public PerformRecoveryPacket(PerformType performType, ActionId actionId, uint actionArgId)
        {
            PerformType = performType;
            ActionId = actionId;
            ActionArgId = actionArgId;
        }

        public PerformRecoveryPacket(PerformType performType, ActionId actionId, uint actionArgId, MissileArgs args)
        {
            PerformType = performType;
            ActionId = actionId;
            ActionArgId = actionArgId;
            MissileArgs = args;
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
                    if (Arg != 0)
                        pw.WriteUInt(Arg);
                    else
                        pw.WriteNoneStruct();
                    break;
                case PerformType.ListOfArgs:
                    pw.WriteTuple(6);
                    pw.WriteUInt((uint)ActionId);                   // actionId
                    pw.WriteUInt(ActionArgId);                      // actionargId
                    pw.WriteList(MissileArgs.HitEntities.Count);    // List of hited entities
                    foreach (var entity in MissileArgs.HitEntities)
                        pw.WriteULong(entity);
                    pw.WriteList(MissileArgs.MisstEntities.Count);  // List of missed entities
                    foreach (var entity in MissileArgs.MisstEntities)
                        pw.WriteULong(entity);
                    pw.WriteList(0);                                // ToDo: List of misses data
                    pw.WriteList(MissileArgs.HitData.Count);        // List of hits data
                    foreach (var hit in MissileArgs.HitData)
                    {
                        pw.WriteTuple(3);
                            pw.WriteULong(hit.EntityId);         // target entityId
                            pw.WriteTuple(12);                   // rawInfo start
                                pw.WriteUInt((uint)hit.DamageType); // self.damageType
                                pw.WriteUInt(hit.Reflected);        // self.reflected
                                pw.WriteUInt(hit.Filtered);         // self.filtered
                                pw.WriteUInt(hit.Absorbed);         // self.absorbed
                                pw.WriteUInt(hit.Resisted);         // self.resisted
                                pw.WriteLong(hit.FinalAmt);         // self.finalAmt
                                pw.WriteInt(hit.IsCritical);        // self.isCrit
                                pw.WriteInt(hit.DeathBlow);         // self.deathBlow    ToDo => maybe bool
                                pw.WriteUInt(hit.CoverModifier);    // self.coverModifier
                                pw.WriteInt(hit.WasImune);          // self.wasImmune
                                pw.WriteList(0);                    // ToDo: targetEffectIds
                                pw.WriteList(0);                    // ToDo: sourceEffectIds
                            pw.WriteNoneStruct();               // OnHitData
                    }
                    break;
                default:
                    Logger.WriteLog(LogType.Error, $"Unknown PerformType {PerformType}");
                    break;
            }
        }
    }
}
