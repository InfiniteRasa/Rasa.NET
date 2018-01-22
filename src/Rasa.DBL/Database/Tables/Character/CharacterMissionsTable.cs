using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.Character
{
    using Structures;

    public class CharacterMissionsTable
    {
        private static readonly MySqlCommand AddMissionCommand = new MySqlCommand("INSERT INTO character_missions (characterId, missionId, missionState) VALUES (@CharacterId, @MissionId, @MissionState)");
        private static readonly MySqlCommand GetMissionsCommand = new MySqlCommand("SELECT * FROM character_missions WHERE characterId = @CharacterId");
        private static readonly MySqlCommand UpdateMissionCommand = new MySqlCommand("UPDATE character_missions SET missionState = @MissionState WHERE characterId = @CharacterId AND missionId = @MissionId");

        public static void Initialize()
        {
            AddMissionCommand.Connection = GameDatabaseAccess.CharConnection;
            AddMissionCommand.Parameters.Add("@CharacterId", MySqlDbType.UInt32);
            AddMissionCommand.Parameters.Add("@MissionId", MySqlDbType.Int32);
            AddMissionCommand.Parameters.Add("@MissionState", MySqlDbType.Int16);
            AddMissionCommand.Prepare();

            GetMissionsCommand.Connection = GameDatabaseAccess.CharConnection;
            GetMissionsCommand.Parameters.Add("@CharacterId", MySqlDbType.UInt32);
            GetMissionsCommand.Prepare();

            UpdateMissionCommand.Connection = GameDatabaseAccess.CharConnection;
            UpdateMissionCommand.Parameters.Add("@CharacterId", MySqlDbType.UInt32);
            UpdateMissionCommand.Parameters.Add("@MissionId", MySqlDbType.Int32);
            UpdateMissionCommand.Parameters.Add("@MissionState", MySqlDbType.Int16);
            UpdateMissionCommand.Prepare();
        }

        public static void AddMission(uint characterId, int missionId, short missionState)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                AddMissionCommand.Parameters["@CharacterId"].Value = characterId;
                AddMissionCommand.Parameters["@MissionId"].Value = missionId;
                AddMissionCommand.Parameters["@MissionState"].Value = missionState;
                AddMissionCommand.ExecuteNonQuery();
            }
        }

        public static List<CharacterMissionsEntry> GetMissions(uint characterId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                var missions = new List<CharacterMissionsEntry>();

                GetMissionsCommand.Parameters["@CharacterId"].Value = characterId;

                using (var reader = GetMissionsCommand.ExecuteReader())
                    while (reader.Read())
                        missions.Add(CharacterMissionsEntry.Read(reader));

                return missions;
            }
        }

        public static void UpdateMission(uint characterId, int missionId, short missionState)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                UpdateMissionCommand.Parameters["@CharacterId"].Value = characterId;
                UpdateMissionCommand.Parameters["@MissionId"].Value = missionId;
                UpdateMissionCommand.Parameters["@MissionState"].Value = missionState;
                UpdateMissionCommand.ExecuteNonQuery();
            }
        }
    }
}
