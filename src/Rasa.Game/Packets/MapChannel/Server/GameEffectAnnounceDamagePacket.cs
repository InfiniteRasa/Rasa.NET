using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// Recv_CallGameEffectMethod(effectId, 'AnnounceDamage', (damageData,)): asks the effect on
    /// this entity to announce damage it did to others - BaseGameEffect.Recv_AnnounceDamage
    /// takes [(targetId, rawInfo), ...] and floats each as damage from the effect's source, with
    /// the effect's tick marker if it ticks on damage. This is how an effect that hurts things
    /// around its holder (Scourge) shows its numbers: the effect's OnTick takes no data, so the
    /// tick is not the way, and the holder is not the one hurt, so a health update on the holder
    /// is not either. Each entry's rawInfo names the effects the blast put on that target quietly
    /// (TickEntry.TargetEffectIds, Managers.HitEffects), which the client announces with it.
    /// </summary>
    public class GameEffectAnnounceDamagePacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.CallGameEffectMethod;

        public int EffectId { get; }
        public List<TickEntry> Hits { get; } = new List<TickEntry>();

        /// <summary>
        /// The effect method called with (damageData,): AnnounceDamage, or DoExplosion for a
        /// BombEffect (Controlled Fission), which plays its blast at the holder and announces the
        /// same list.
        /// </summary>
        public string MethodName { get; }

        public GameEffectAnnounceDamagePacket(int effectId, string methodName = "AnnounceDamage")
        {
            EffectId = effectId;
            MethodName = methodName;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteInt(EffectId);
            pw.WriteString(MethodName);
            pw.WriteTuple(1);                   // args = (damageData,)
            pw.WriteList(Hits.Count);

            foreach (var hit in Hits)
            {
                pw.WriteTuple(2);
                pw.WriteULong(hit.EntityId);
                DamageInfoWriter.WriteRawInfo(pw, hit.DamageType, hit.Amount, hit.Resisted, hit.IsCritical, hit.DeathBlow, absorbed: hit.Absorbed, wasImmune: hit.WasImmune,
                    targetEffectIds: hit.TargetEffectIds);
            }
        }
    }
}
