using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// PhysicalEntity.Recv_PerformObjectAbility(actionId, argId, hits, misses, *args): an object
    /// performs an action - the client builds the action's class, plays its FX family from the
    /// object at the hits and calls DoAbility(object, *args). OBJ_EFFECT (abilities.obj_effect)
    /// takes one argument, the gameeffectdata id it announces on each hit. OBJ_DAMAGE and
    /// OBJ_KNOCKBACK (ObjDamageBase, ObjKnockback) take two, (hitdata, sourceId): a damage rawInfo
    /// for each hit, in the order of the hits, and the entity each is announced as dealt by
    /// (DamageHits, DamageSourceId).
    /// </summary>
    public class PerformObjectAbilityPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.PerformObjectAbility;

        public ActionId ActionId { get; }
        public uint ActionArgId { get; }
        public List<ulong> Hits { get; } = new List<ulong>();
        public List<ulong> Misses { get; } = new List<ulong>();
        public List<int> Args { get; } = new List<int>();

        /// <summary>For OBJ_DAMAGE and OBJ_KNOCKBACK: each hit's damage, written as the hitdata argument; null for an ability without one.</summary>
        public List<ObjectDamageHit> DamageHits { get; set; }

        /// <summary>The sourceId argument that follows DamageHits: whom the damage is announced as from.</summary>
        public ulong DamageSourceId { get; set; }

        public PerformObjectAbilityPacket(ActionId actionId, uint actionArgId)
        {
            ActionId = actionId;
            ActionArgId = actionArgId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(4 + Args.Count + (DamageHits != null ? 2 : 0));
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

            if (DamageHits == null)
                return;

            pw.WriteList(DamageHits.Count);
            foreach (var hit in DamageHits)
                DamageInfoWriter.WriteRawInfo(pw, hit.DamageType, hit.Amount, hit.Resisted, false, hit.DeathBlow, absorbed: hit.Absorbed, wasImmune: hit.Immune);

            pw.WriteULong(DamageSourceId);
        }
    }

    /// <summary>One hit of an object's blast, as its rawInfo carries it.</summary>
    public sealed class ObjectDamageHit
    {
        public ulong EntityId { get; set; }
        public DamageType DamageType { get; set; }
        public int Amount { get; set; }
        public int Resisted { get; set; }
        public int Absorbed { get; set; }
        public bool Immune { get; set; }
        public bool DeathBlow { get; set; }
    }
}
