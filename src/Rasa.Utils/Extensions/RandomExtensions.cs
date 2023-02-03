namespace Rasa.Extensions;

public static class RandomExtensions
{
    public static uint NextUInt(this Random rand)
    {
        return (uint) rand.Next();
    }
}
