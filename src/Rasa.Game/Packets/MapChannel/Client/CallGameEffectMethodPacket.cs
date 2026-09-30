namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    /// <summary>
    /// CallGameEffectMethod(effectId, methodName, args), from BaseGameEffect.SendMethodCall: a
    /// client effect calling its server-side counterpart. The arguments can be anything, so they
    /// are read past rather than read.
    /// </summary>
    public class CallGameEffectMethodPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.CallGameEffectMethod;

        public int EffectId { get; set; }
        public string MethodName { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            EffectId = pr.ReadInt();
            MethodName = pr.ReadString();
            pr.SkipValue();
        }
    }
}
