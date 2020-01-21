// This is used for CharacterOptions and UserOptions

namespace Rasa.Structures
{
    public partial class UserOptionsEntry
    {
        public int? AccountId { get; set; }
        public uint OptionId { get; set; }
        public string Value { get; set; }
    }
}
