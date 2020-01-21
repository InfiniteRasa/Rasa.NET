using System;

namespace Rasa.Database
{
    public class GameDatabaseAccess
    {
        public static WorldContext WorldConnection { get; private set; }
        public static CharacterContext CharConnection { get; private set; }

        public static object WorldLock { get; } = new object();
        public static object CharLock { get; } = new object();

        public static void Initialize(WorldContext worldContext, CharacterContext charContext)
        {
            WorldConnection = worldContext ?? throw new ArgumentNullException(nameof(worldContext));
            CharConnection = charContext ?? throw new ArgumentNullException(nameof(charContext));
        }
    }
}
