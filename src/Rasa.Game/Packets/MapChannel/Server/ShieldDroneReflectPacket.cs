namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// Recv_CallGameEffectMethod(effectId, 'ReflectDamage', (damageTargetId, rawInfo)): the
    /// Shield Drone's SHIELD_DRONE_SHIELD_SOURCE sent an attacker's own shot back at them.
    ///
    /// ShieldDroneShieldSourceEffect.Recv_ReflectDamage does the flight itself - it measures the
    /// distance from the drone to the target, divides by its own DAMAGE_REFLECTION_SPEED of 45
    /// and schedules AnnounceDamage to land then - so the server sends no delay, only who is
    /// being reflected at and what is coming. The effect's own tick marker goes out with it, so
    /// the drone plays the discharge.
    ///
    /// The method's own opcode (ReflectDamage, 577) is not how it is addressed: an effect's
    /// methods arrive through CallGameEffectMethod by name, as the announce methods do.
    /// </summary>
    public class ShieldDroneReflectPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.CallGameEffectMethod;

        public int EffectId { get; }
        public ulong DamageTargetId { get; }
        public DamageType DamageType { get; }
        public int Amount { get; }
        public bool DeathBlow { get; }

        /// <summary>What a shield on the one it hit took; Amount is what got through.</summary>
        public int Absorbed { get; }

        /// <summary>The one it hit was immune (Managers.DamageImmunity): "Immune" in place of the damage.</summary>
        public bool WasImmune { get; }

        public ShieldDroneReflectPacket(int effectId, ulong damageTargetId, DamageType damageType, int amount, bool deathBlow, int absorbed = 0, bool wasImmune = false)
        {
            EffectId = effectId;
            DamageTargetId = damageTargetId;
            DamageType = damageType;
            Amount = amount;
            DeathBlow = deathBlow;
            Absorbed = absorbed;
            WasImmune = wasImmune;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteInt(EffectId);
            pw.WriteString("ReflectDamage");
            pw.WriteTuple(2);                   // args = (damageTargetId, rawInfo)
            pw.WriteULong(DamageTargetId);
            DamageInfoWriter.WriteRawInfo(pw, DamageType, Amount, 0, false, DeathBlow, absorbed: Absorbed, wasImmune: WasImmune);
        }
    }
}
