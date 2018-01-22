using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.World
{
    using Structures;

    public class CreatureAppearanceTable
    {
        private static readonly MySqlCommand GetCreatureAppearanceCommand = new MySqlCommand("SELECT slotId, classId, color FROM creature_appearance WHERE dbId = @CreatureDbId");
        private static readonly MySqlCommand SetCreatureAppearanceCommand = new MySqlCommand("INSERT INTO creature_appearance (dbId, slotId, classId, color) VALUES (@CreatureDbId, @SlotId, @ClassId, @Color)");
        private static readonly MySqlCommand UpdateCreatureAppearanceCommand = new MySqlCommand("Update creature_appearance SET classId = @ClassId, color = @Color WHERE dbId = @CreatureDbId AND slotId = @SlotId");

        public static void Initialize()
        {
            GetCreatureAppearanceCommand.Connection = GameDatabaseAccess.WorldConnection;
            GetCreatureAppearanceCommand.Parameters.Add("@CreatureDbId", MySqlDbType.Int32);
            GetCreatureAppearanceCommand.Prepare();

            SetCreatureAppearanceCommand.Connection = GameDatabaseAccess.WorldConnection;
            SetCreatureAppearanceCommand.Parameters.Add("@CreatureDbId", MySqlDbType.Int32);
            SetCreatureAppearanceCommand.Parameters.Add("@SlotId", MySqlDbType.Int32);
            SetCreatureAppearanceCommand.Parameters.Add("@ClassId", MySqlDbType.Int32);
            SetCreatureAppearanceCommand.Parameters.Add("@Color", MySqlDbType.Int32);
            SetCreatureAppearanceCommand.Prepare();

            UpdateCreatureAppearanceCommand.Connection = GameDatabaseAccess.WorldConnection;
            UpdateCreatureAppearanceCommand.Parameters.Add("@CreatureDbId", MySqlDbType.Int32);
            UpdateCreatureAppearanceCommand.Parameters.Add("@SlotId", MySqlDbType.Int32);
            UpdateCreatureAppearanceCommand.Parameters.Add("@ClassId", MySqlDbType.Int32);
            UpdateCreatureAppearanceCommand.Parameters.Add("@Color", MySqlDbType.Int32);
            UpdateCreatureAppearanceCommand.Prepare();
        }

        public static List<AppearanceEntry> GetCreatureAppearance(int creatureDbId)
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                var appearance = new List<AppearanceEntry>();

                GetCreatureAppearanceCommand.Parameters["@CreatureDbId"].Value = creatureDbId;
                using (var reader = GetCreatureAppearanceCommand.ExecuteReader())
                    while (reader.Read())
                        appearance.Add(AppearanceEntry.Read(reader));

                return appearance;
            }
        }

        public static void SetCreatureAppearance(int creatureDbId, int slotId, int classId, int color)
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                SetCreatureAppearanceCommand.Parameters["@CreatureDbId"].Value = creatureDbId;
                SetCreatureAppearanceCommand.Parameters["@SlotId"].Value = slotId;
                SetCreatureAppearanceCommand.Parameters["@ClassId"].Value = classId;
                SetCreatureAppearanceCommand.Parameters["@Color"].Value = color;
                SetCreatureAppearanceCommand.ExecuteNonQuery();
            }
        }

        public static void UpdateCreatureAppearance(int creatureDbId, int slotId, int classId, int color)
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                UpdateCreatureAppearanceCommand.Parameters["@CreatureDbId"].Value = creatureDbId;
                UpdateCreatureAppearanceCommand.Parameters["@SlotId"].Value = slotId;
                UpdateCreatureAppearanceCommand.Parameters["@ClassId"].Value = classId;
                UpdateCreatureAppearanceCommand.Parameters["@Color"].Value = color;
                UpdateCreatureAppearanceCommand.ExecuteNonQuery();
            }
        }
    }
}
