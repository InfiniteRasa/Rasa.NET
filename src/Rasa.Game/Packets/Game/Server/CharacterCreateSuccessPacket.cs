namespace Rasa.Packets.Game.Server
{
    using Data;
    using Memory;

    public class CharacterCreateSuccessPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.CharacterCreateSuccess;

        public uint SlotNum { get; set; }
        public string FamilyName { get; set; }

        public CharacterCreateSuccessPacket(uint slotNum, string familyName)
        {
            SlotNum = slotNum;
            FamilyName = familyName;
        }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteUInt(SlotNum);
            pw.WriteUnicodeString(FamilyName);
        }
    }
}
