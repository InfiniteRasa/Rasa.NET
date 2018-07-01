namespace Rasa.Packets.Game.Server
{
    using Data;
    using Memory;

    public class CharacterCreateSuccessPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.CharacterCreateSuccess;

        public int SlotNum { get; set; }
        public string FamilyName { get; set; }

        public CharacterCreateSuccessPacket(int slotNum, string familyName)
        {
            SlotNum = slotNum;
            FamilyName = familyName;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteInt(SlotNum);
            pw.WriteUnicodeString(FamilyName);
        }
    }
}
