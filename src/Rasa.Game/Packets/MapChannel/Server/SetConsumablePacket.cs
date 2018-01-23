namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class SetConsumablePacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SetConsumable;

        public bool IsConsumable { get; set; }

        public SetConsumablePacket(bool isConsumable)
        {
            IsConsumable = isConsumable;
        }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteBool(IsConsumable);
        }
    }
}
