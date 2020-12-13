namespace Rasa.Repositories.Character
{
    using Structures;

    public interface ICharacterRepository
    {
        bool Create(GameAccountEntry account, byte slot, string characterName, byte race, double scale, byte gender);

        CharacterEntry Get(uint id);

        void Delete(uint id);

        void UpdateLoginData(uint id);
    }
}