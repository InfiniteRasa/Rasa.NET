namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class SetConsumablePacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SetConsumable;

        public byte IsConsumable { get; set; }

        public SetConsumablePacket(byte isConsumable)
        {
            IsConsumable = isConsumable;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteBool(IsConsumable != 0);
        }
    }
}
