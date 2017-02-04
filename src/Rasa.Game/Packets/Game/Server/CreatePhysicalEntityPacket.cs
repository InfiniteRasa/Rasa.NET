using System;

namespace Rasa.Packets.Game.Server
{
    using Data;
    using Memory;

    public class CreatePhysicalEntityPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.CreatePhysicalEntity;

        public uint EntityId { get; set; }
        public int ClassId { get; set; }
        public object EntityData { get; set; }

        public CreatePhysicalEntityPacket(uint entityId, int classId)
        {
            EntityId = entityId;
            ClassId = classId;
        }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            EntityId = (uint)pr.ReadInt();
            ClassId = pr.ReadInt();
            // todo
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteInt((int)EntityId);
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
