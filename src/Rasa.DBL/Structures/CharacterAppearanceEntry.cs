namespace Rasa.Structures
{
    public partial class CharacterAppearanceEntry
    {
        public uint CharacterId { get; set; }
        public uint Slot { get; set; }
        public uint Class { get; set; }
        public uint Color { get; set; }

        public virtual CharacterEntry Character { get; set; }

        public CharacterAppearanceEntry()
        {

        }

        public CharacterAppearanceEntry(uint characterId, uint slot, uint classId, uint color)
        {
            CharacterId = characterId;
            Slot = slot;
            Class = classId;
            Color = color;
        }
    }
}
