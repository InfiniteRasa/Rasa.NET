namespace Rasa.Packets.Party.Server
{
    using Data;
    using Memory;

    public class AddSquadMemberPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AddSquadMember;

        internal uint CharacterId { get; set; }
        internal ulong EntityId { get; set; }

        internal AddSquadMemberPacket(uint characterId, ulong entityId)
        {
            CharacterId = characterId;
            EntityId = entityId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteUInt(CharacterId);
            pw.WriteULong(EntityId);
        }
    }
}
