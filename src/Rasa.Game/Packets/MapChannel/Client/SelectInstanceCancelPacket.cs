namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    /// <summary>SelectInstanceCancel (688), clientmethod.py OnGotoInstanceCancel(): the instance picker closed without a pick. No arguments.</summary>
    public class SelectInstanceCancelPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SelectInstanceCancel;

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
        }
    }
}
