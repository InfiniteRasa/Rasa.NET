namespace Rasa.Repositories.World
{
    public interface IPlayerRandomNameRepository
    {
        string GetFirstName(Gender gender);

        string GetLastName();
    }

    public enum Gender : byte
    {
        Male = 0,
        Female = 1,
        Neutral = 2
    }
}