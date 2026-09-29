namespace Rasa.Packets.Game.Client
{
    using Data;
    using Memory;

    /// <summary>
    /// CreateCharacter(familyName, characterName, gender, height, appearanceData, raceId)
    /// (client/inputstate/charactercreation.py OnCreateCharacter): RequestCreateCharacterInSlot's
    /// arguments without the slot, sent for the first character of an account that had no family
    /// name when character selection began. CharacterManager.CreateCharacter picks the slot.
    /// </summary>
    public class CreateCharacterPacket : RequestCreateCharacterInSlotPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.CreateCharacter;

        public override void Read(PythonReader pr)
        {
            ReadFields(pr, false);
        }
    }
}
