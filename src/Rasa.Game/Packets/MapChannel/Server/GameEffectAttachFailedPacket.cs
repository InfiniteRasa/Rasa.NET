namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// PhysicalEntity.Recv_GameEffectAttachFailed(typeId, reason, sourceId, delayMs): an effect
    /// did not take on this entity. The client floats "Immune" or "Resisted" over it from the
    /// source (COMBAT_IMMUNE_ANNOUNCED / COMBAT_RESIST_ANNOUNCED), after delayMs if one is given.
    /// </summary>
    public class GameEffectAttachFailedPacket : ServerPythonPacket
    {
        /// <summary>gameconstants.EFFECT_ATTACH_FAIL_*: IncSeed from a ResetSeed(), so 0, 1, 2.</summary>
        public enum FailReason { Immune = 0, Resist = 1, NoDuration = 2 }

        public override GameOpcode Opcode { get; } = GameOpcode.GameEffectAttachFailed;

        public int EffectTypeId { get; }
        public FailReason Reason { get; }
        public ulong SourceId { get; }
        public int DelayMs { get; }

        public GameEffectAttachFailedPacket(int effectTypeId, FailReason reason, ulong sourceId, int delayMs = 0)
        {
            EffectTypeId = effectTypeId;
            Reason = reason;
            SourceId = sourceId;
            DelayMs = delayMs;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(4);
            pw.WriteInt(EffectTypeId);
            pw.WriteInt((int)Reason);
            pw.WriteULong(SourceId);
            pw.WriteInt(DelayMs);
        }
    }
}
