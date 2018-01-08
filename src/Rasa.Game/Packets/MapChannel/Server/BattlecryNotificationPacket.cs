namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class BattlecryNotificationPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.BattlecryNotification;

        public int PackageId { get; set; }
        public int TypeId { get; set; }
        public int Seed { get; set; }

        public BattlecryNotificationPacket(int packageId, int typeId, int seed)
        {
            PackageId = packageId;
            TypeId = typeId;
            Seed = seed;
        }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteInt(PackageId);
            pw.WriteInt(TypeId);
            pw.WriteInt(Seed);
        }
    }
}
