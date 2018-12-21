namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class CurrentCharacterIdPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.CurrentCharacterId;

        public uint CharacterId { get; set; }

        public CurrentCharacterIdPacket(uint characterId)
        {
            CharacterId = characterId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteUInt(CharacterId);
        }
    }
}
