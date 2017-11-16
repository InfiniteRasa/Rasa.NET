namespace Rasa.Entities
{
    using Data;
    using Structures;

    public class Character
    {
        public int ClassId { get; set; }
        public int GameContextId { get; set; }
        public Position Position { get; set; }
        public double Rotatation { get; set; }
        public int Level { get; set; }
        public string Logos { get; set; }
        
        public Character CreateCharacter()
        {
            var newCharacter = new Character
            {
                ClassId = (int)CharacterClass.Recruit,
                GameContextId = 1220,
                Position = new Position(894D, 347D, 306D),
                Rotatation = 1D,
                Level = 1,
                Logos = "00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000"   // 410 logos
        };

            return newCharacter;
        }
    }
}
