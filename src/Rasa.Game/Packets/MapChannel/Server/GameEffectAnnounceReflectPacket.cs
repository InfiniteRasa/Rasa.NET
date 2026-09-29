namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// Recv_CallGameEffectMethod(effectId, 'AnnounceReflect', (entityId, rawInfo, delayMs)):
    /// the reflecting effect on this entity (Reflective Armor's MEDIUM_ARMOR_SKILL, the Guardian's
    /// REFLECTION) sent damage back at entityId. The effect's Recv_AnnounceReflect schedules
    /// DoReflection, which flies the reflection from the wearer to the attacker and announces the
    /// damage on the attacker when it arrives. Sent by Managers.Reflection.
    /// </summary>
    public class GameEffectAnnounceReflectPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.CallGameEffectMethod;

        public int EffectId { get; }
        public ulong AttackerId { get; }
        public DamageType DamageType { get; }
        public int Amount { get; }
        public bool DeathBlow { get; }

        /// <summary>What a shield on the one it hit took; Amount is what got through.</summary>
        public int Absorbed { get; }

        /// <summary>The one it hit was immune (Managers.DamageImmunity): "Immune" when the reflection arrives.</summary>
        public bool WasImmune { get; }

        public int DelayMs { get; }

        public GameEffectAnnounceReflectPacket(int effectId, ulong attackerId, DamageType damageType, int amount, bool deathBlow, int delayMs = 0, int absorbed = 0, bool wasImmune = false)
        {
            EffectId = effectId;
            AttackerId = attackerId;
            DamageType = damageType;
            Amount = amount;
            DeathBlow = deathBlow;
            Absorbed = absorbed;
            WasImmune = wasImmune;
            DelayMs = delayMs;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteInt(EffectId);
            pw.WriteString("AnnounceReflect");
            pw.WriteTuple(3);                   // args = (entityId, rawInfo, delayMs)
            pw.WriteULong(AttackerId);
            DamageInfoWriter.WriteRawInfo(pw, DamageType, Amount, 0, false, DeathBlow, absorbed: Absorbed, wasImmune: WasImmune);
            pw.WriteInt(DelayMs);
        }
    }
}
