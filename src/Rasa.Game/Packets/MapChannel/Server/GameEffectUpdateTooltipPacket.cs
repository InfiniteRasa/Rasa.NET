namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// Recv_GameEffectUpdateTooltip(effectId, tooltipDict) on the entity holding the effect.
    /// BaseGameEffect.SetTooltipDict takes the dictionary in place of the one it was attached
    /// with - every key the effect's tooltip format string names has to be there again - and
    /// starts its timer over from 'duration': the client's expiry is set from it, on the attach
    /// and here, and nowhere else. So an effect whose clock was stopped and started again
    /// (OnRestart) needs this to show the time it really has left. The dictionary is the
    /// attach's own (GameEffectAttachedPacket.WriteTooltip).
    /// </summary>
    public class GameEffectUpdateTooltipPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.GameEffectUpdateTooltip;

        /// <summary>The effect as an attach would describe it now; its id and tooltip are what is sent.</summary>
        public GameEffectAttachedPacket Effect { get; }

        public GameEffectUpdateTooltipPacket(GameEffectAttachedPacket effect)
        {
            Effect = effect;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteInt(Effect.EffectId);
            Effect.WriteTooltip(pw);
        }
    }
}
