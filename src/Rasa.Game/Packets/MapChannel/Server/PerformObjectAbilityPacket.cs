using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// PhysicalEntity.Recv_PerformObjectAbility(actionId, argId, hits, misses, *args): an object
    /// performs an action - the client builds the action's class, plays its FX family from the
    /// object at the hits and calls DoAbility(object, *args). OBJ_EFFECT (abilities.obj_effect)
    /// takes one argument, the gameeffectdata id it announces on each hit.
    /// </summary>
    public class PerformObjectAbilityPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.PerformObjectAbility;

        public ActionId ActionId { get; }
        public uint ActionArgId { get; }
        public List<ulong> Hits { get; } = new List<ulong>();
        public List<ulong> Misses { get; } = new List<ulong>();
        public List<int> Args { get; } = new List<int>();

        public PerformObjectAbilityPacket(ActionId actionId, uint actionArgId)
        {
            ActionId = actionId;
            ActionArgId = actionArgId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(4 + Args.Count);
            pw.WriteUInt((uint)ActionId);
            pw.WriteUInt(ActionArgId);

            pw.WriteList(Hits.Count);
            foreach (var hit in Hits)
                pw.WriteULong(hit);

            pw.WriteList(Misses.Count);
            foreach (var miss in Misses)
                pw.WriteULong(miss);

            foreach (var arg in Args)
                pw.WriteInt(arg);
        }
    }
}
