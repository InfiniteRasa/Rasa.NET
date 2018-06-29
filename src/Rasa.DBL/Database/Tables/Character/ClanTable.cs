using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.Character
{
    using Structures;

    public static class ClanTable
    {
        private static readonly MySqlCommand GetClanDataCommand = new MySqlCommand("SELECT `id`, `name` FROM `clan` WHERE `id` = (SELECT `clan_id` FROM `clan_member` WHERE `character_id` = @CharacterId)");


        public static void Initialize()
        {
            GetClanDataCommand.Connection = GameDatabaseAccess.CharConnection;
            GetClanDataCommand.Parameters.Add("@CharacterId", MySqlDbType.UInt32);
            GetClanDataCommand.Prepare();
        }

        public static ClanEntry GetClanData(uint characterId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                GetClanDataCommand.Parameters["@CharacterId"].Value = characterId;

                using (var reader = GetClanDataCommand.ExecuteReader())
                    return ClanEntry.Read(reader);
            }
        }
    }
}
