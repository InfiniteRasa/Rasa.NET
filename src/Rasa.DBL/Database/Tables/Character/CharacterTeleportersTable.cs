using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.Character
{
    public class CharacterTeleportersTable
    {
        private static readonly MySqlCommand AddTeleporterCommand = new MySqlCommand("INSERT INTO character_teleporters (character_id, waypoint_id) VALUES (@CharacterId, @WaypointId)");
        private static readonly MySqlCommand GetTeleportersCommand = new MySqlCommand("SELECT waypoint_id FROM character_teleporters WHERE character_id = @CharacterId");

        public static void Initialize()
        {
            AddTeleporterCommand.Connection = GameDatabaseAccess.CharConnection;
            AddTeleporterCommand.Parameters.Add("@CharacterId", MySqlDbType.UInt32);
            AddTeleporterCommand.Parameters.Add("@WaypointId", MySqlDbType.UInt32);
            AddTeleporterCommand.Prepare();

            GetTeleportersCommand.Connection = GameDatabaseAccess.CharConnection;
            GetTeleportersCommand.Parameters.Add("@CharacterId", MySqlDbType.UInt32);
            GetTeleportersCommand.Prepare();
        }

        public static void AddTeleporter(uint characterId, uint waypointId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                AddTeleporterCommand.Parameters["@CharacterId"].Value = characterId;
                AddTeleporterCommand.Parameters["@WaypointId"].Value = waypointId;
                AddTeleporterCommand.ExecuteNonQuery();
            }
        }

        public static List<uint> GetTeleporters(uint characterId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                var teleporterList = new List<uint>();

                GetTeleportersCommand.Parameters["@CharacterId"].Value = characterId;

                using (var reader = GetTeleportersCommand.ExecuteReader())
                    while (reader.Read())
                        teleporterList.Add(reader.GetUInt32("waypoint_id"));

                return teleporterList;
            }
        }
    }
}
