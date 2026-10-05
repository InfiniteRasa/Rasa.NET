namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    /// <summary>BuryMe(): Actor.OnRequestBurial, the death window's Revive button (shown only when canRevive).</summary>
    public class BuryMePacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.BuryMe;

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
        }
    }
}
