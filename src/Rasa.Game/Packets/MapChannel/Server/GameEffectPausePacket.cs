namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// Recv_CallGameEffectMethod(effectId, 'OnPaused' or 'OnRestart', ()) on the entity holding
    /// the effect. BaseGameEffect.Recv_OnPaused marks it paused (a second one is only a warning
    /// in the client's log) and Recv_OnRestart clears the mark; each then calls a hook no effect
    /// class overrides. The mark is read in one place, the effect's tooltip, which says "Paused"
    /// (tooltip 1880) where the timer would be. Neither touches the timer itself, so a restart
    /// is followed by a GameEffectUpdateTooltip with the time left.
    ///
    /// These are methods of the effect, not of the entity: the OnPaused (713) and OnRestart
    /// (714) opcodes name them in the client's method table, but an entity has no Recv_OnPaused
    /// to answer one sent to it directly.
    /// </summary>
    public class GameEffectPausePacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.CallGameEffectMethod;

        public int EffectId { get; }
        public bool Paused { get; }

        public GameEffectPausePacket(int effectId, bool paused)
        {
            EffectId = effectId;
            Paused = paused;
        }

        public string MethodName => Paused ? "OnPaused" : "OnRestart";

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteInt(EffectId);
            pw.WriteString(MethodName);
            pw.WriteTuple(0);                   // args
        }
    }
}
