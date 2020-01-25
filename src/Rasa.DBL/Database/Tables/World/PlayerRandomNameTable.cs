using System.Linq;

namespace Rasa.Database.Tables.World
{
    public class PlayerRandomNameTable
    {
        public enum Gender : byte
        {
            Male    = 0,
            Female  = 1,
            Neutral = 2
        }

        public enum NameType : byte
        {
            First = 0,
            Last  = 1
        }

        public static string GetRandom(Gender gender, NameType type)
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                return (from prn in GameDatabaseAccess.WorldConnection.PlayerRandomName
                    where prn.Type == (uint) type
                          && (prn.Gender == (uint) gender || prn.Gender == (uint)Gender.Neutral)
                    orderby GameDatabaseAccess.WorldConnection.Rand()
                    select prn.Name).First();
            }
        }
    }
}
