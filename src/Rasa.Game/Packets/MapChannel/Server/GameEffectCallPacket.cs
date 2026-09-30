namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// Recv_CallGameEffectMethod(effectId, methodName, ()): calls a method of the effect on this
    /// entity that takes nothing but its holder - MiasmaDissipateEffect.Recv_Dissipate and
    /// Recv_Coalesce, which turn a Miasma into a cloud nothing can target and back.
    /// </summary>
    public class GameEffectCallPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.CallGameEffectMethod;

        public int EffectId { get; }
        public string MethodName { get; }

        public GameEffectCallPacket(int effectId, string methodName)
        {
            EffectId = effectId;
            MethodName = methodName;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteInt(EffectId);
            pw.WriteString(MethodName);
            pw.WriteTuple(0);                   // args = ()
        }
    }
}
