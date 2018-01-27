using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.Character
{
    using Structures;

    public class CharacterMissionsTable
    {
        private static readonly MySqlCommand AddMissionCommand = new MySqlCommand("INSERT INTO character_missions (accountId, characterSlot, missionId, missionState) VALUES (@AccountId, @CharacterSlot, @MissionId, @MissionState)");
        private static readonly MySqlCommand GetMissionsCommand = new MySqlCommand("SELECT * FROM character_missions WHERE accountId = @AccountId AND characterSlot = @CharacterSlot");
        private static readonly MySqlCommand UpdateMissionCommand = new MySqlCommand("UPDATE character_missions SET missionState = @MissionState WHERE accountId = @AccountId AND characterSlot = @CharacterSlot AND missionId = @MissionId");

        public static void Initialize()
        {
            AddMissionCommand.Connection = GameDatabaseAccess.CharConnection;
            AddMissionCommand.Parameters.Add("@AccountId", MySqlDbType.UInt32);
            AddMissionCommand.Parameters.Add("@CharacterSlot", MySqlDbType.UInt32);
            AddMissionCommand.Parameters.Add("@MissionId", MySqlDbType.Int32);
            AddMissionCommand.Parameters.Add("@MissionState", MySqlDbType.Int16);
            AddMissionCommand.Prepare();

            GetMissionsCommand.Connection = GameDatabaseAccess.CharConnection;
            GetMissionsCommand.Parameters.Add("@AccountId", MySqlDbType.UInt32);
            GetMissionsCommand.Parameters.Add("@CharacterSlot", MySqlDbType.UInt32);
            GetMissionsCommand.Prepare();

            UpdateMissionCommand.Connection = GameDatabaseAccess.CharConnection;
            UpdateMissionCommand.Parameters.Add("@AccountId", MySqlDbType.UInt32);
            UpdateMissionCommand.Parameters.Add("@CharacterSlot", MySqlDbType.UInt32);
            UpdateMissionCommand.Parameters.Add("@MissionId", MySqlDbType.Int32);
            UpdateMissionCommand.Parameters.Add("@MissionState", MySqlDbType.Int16);
            UpdateMissionCommand.Prepare();
        }

        public static void AddMission(uint accountId, uint characterSlot, int missionId, short missionState)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                AddMissionCommand.Parameters["@AccountId"].Value = accountId;
                AddMissionCommand.Parameters["@CharacterSlot"].Value = characterSlot;
                AddMissionCommand.Parameters["@MissionId"].Value = missionId;
                AddMissionCommand.Parameters["@MissionState"].Value = missionState;
                AddMissionCommand.ExecuteNonQuery();
            }
        }

        public static List<CharacterMissionsEntry> GetMissions(uint accountId, uint characterSlot)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                var missions = new List<CharacterMissionsEntry>();

                GetMissionsCommand.Parameters["@AccountId"].Value = accountId;
                GetMissionsCommand.Parameters["@CharacterSlot"].Value = characterSlot;

                using (var reader = GetMissionsCommand.ExecuteReader())
                    while (reader.Read())
                        missions.Add(CharacterMissionsEntry.Read(reader));

                return missions;
            }
        }

        public static void UpdateMission(uint accountId, uint characterSlot, int missionId, short missionState)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                UpdateMissionCommand.Parameters["@AccountId"].Value = accountId;
                UpdateMissionCommand.Parameters["@CharacterSlot"].Value = characterSlot;
                UpdateMissionCommand.Parameters["@MissionId"].Value = missionId;
                UpdateMissionCommand.Parameters["@MissionState"].Value = missionState;
                UpdateMissionCommand.ExecuteNonQuery();
            }
        }
    }
}
