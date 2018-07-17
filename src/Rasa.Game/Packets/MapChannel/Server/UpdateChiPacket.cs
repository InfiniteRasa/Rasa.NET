namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class UpdateChiPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.UpdateChi;

        public ActorAttributes Chi { get; set; }
        public int WhoId { get; set; }

        public UpdateChiPacket(ActorAttributes chi, int whoId)
        {
            Chi = chi;
            WhoId = whoId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(4);
            pw.WriteInt(Chi.Current);
            pw.WriteInt(Chi.CurrentMax);
            pw.WriteInt(Chi.RefreshAmount);
            pw.WriteInt(WhoId);
        }
    }
}