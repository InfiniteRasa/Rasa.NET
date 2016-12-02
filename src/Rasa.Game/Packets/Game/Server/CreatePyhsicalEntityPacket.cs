namespace Rasa.Packets.Game.Server
{
    using Data;
    using Memory;

    public class CreatePyhsicalEntityPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.CreatePhysicalEntity;

        public int EntityId { get; set; }
        public int ClassId { get; set; }
        public object EntityData { get; set; }

        public CreatePyhsicalEntityPacket(int entityId, int classId)
        {
            EntityId = entityId;
            ClassId = classId;
        }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            EntityId = pr.ReadInt();
            ClassId = pr.ReadInt();

            // todo
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteInt(EntityId);
            pw.WriteInt(ClassId);

            if (EntityData != null)
            {
                // todo
            }
            else
                pw.WriteNoneStruct();
        }
    }
}
