namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class UpdatePowerPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.UpdatePower;

        public ActorAttributes Power { get; set; }
        public int WhoId { get; set; }

        public UpdatePowerPacket(ActorAttributes power, int whoId)
        {
            Power = power;
            WhoId = whoId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(4);
            pw.WriteInt(Power.Current);
            pw.WriteInt(Power.CurrentMax);
            pw.WriteInt(Power.RefreshAmount);
            pw.WriteInt(WhoId);
        }
    }
}
