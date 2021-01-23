namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class CurrentCharacterIdPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.CurrentCharacterId;

        public ulong CharacterId { get; set; }

        public CurrentCharacterIdPacket(ulong characterId)
        {
            CharacterId = characterId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteULong(CharacterId);
        }
    }
}
