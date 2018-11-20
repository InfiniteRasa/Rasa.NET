using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.World
{
    using Structures;

    public class CreatureAppearanceTable
    {
        private static readonly MySqlCommand GetCreatureAppearanceCommand = new MySqlCommand("SELECT creature_id, slot_id, class_id, color FROM creature_appearance WHERE creature_id = @CreatureId");
        private static readonly MySqlCommand SetCreatureAppearanceCommand = new MySqlCommand("INSERT INTO creature_appearance (creature_id, slot_id, class_id, color) VALUES (@CreatureId, @Slot, @Class, @Color)");
        private static readonly MySqlCommand UpdateCreatureAppearanceCommand = new MySqlCommand("Update creature_appearance SET class_id = @Class, color = @Color WHERE creature_id = @CreatureId AND slot_id = @Slot");

        public static void Initialize()
        {
            GetCreatureAppearanceCommand.Connection = GameDatabaseAccess.WorldConnection;
            GetCreatureAppearanceCommand.Parameters.Add("@CreatureId", MySqlDbType.UInt32);
            GetCreatureAppearanceCommand.Prepare();

            SetCreatureAppearanceCommand.Connection = GameDatabaseAccess.WorldConnection;
            SetCreatureAppearanceCommand.Parameters.Add("@CreatureId", MySqlDbType.UInt32);
            SetCreatureAppearanceCommand.Parameters.Add("@Slot", MySqlDbType.UInt32);
            SetCreatureAppearanceCommand.Parameters.Add("@Class", MySqlDbType.UInt32);
            SetCreatureAppearanceCommand.Parameters.Add("@Color", MySqlDbType.UInt32);
            SetCreatureAppearanceCommand.Prepare();

            UpdateCreatureAppearanceCommand.Connection = GameDatabaseAccess.WorldConnection;
            UpdateCreatureAppearanceCommand.Parameters.Add("@CreatureId", MySqlDbType.UInt32);
            UpdateCreatureAppearanceCommand.Parameters.Add("@Slot", MySqlDbType.UInt32);
            UpdateCreatureAppearanceCommand.Parameters.Add("@Class", MySqlDbType.UInt32);
            UpdateCreatureAppearanceCommand.Parameters.Add("@Color", MySqlDbType.UInt32);
            UpdateCreatureAppearanceCommand.Prepare();
        }

        public static List<CreatureAppearanceEntry> GetCreatureAppearance(uint creatureId)
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                var appearance = new List<CreatureAppearanceEntry>();

                GetCreatureAppearanceCommand.Parameters["@CreatureId"].Value = creatureId;
                using (var reader = GetCreatureAppearanceCommand.ExecuteReader())
                    while (reader.Read())
                        appearance.Add(CreatureAppearanceEntry.Read(reader));

                return appearance;
            }
        }

        public static void SetCreatureAppearance(uint creatureId, uint slot, uint classId, uint color)
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                SetCreatureAppearanceCommand.Parameters["@CreatureId"].Value = creatureId;
                SetCreatureAppearanceCommand.Parameters["@Slot"].Value = slot;
                SetCreatureAppearanceCommand.Parameters["@Class"].Value = classId;
                SetCreatureAppearanceCommand.Parameters["@Color"].Value = color;
                SetCreatureAppearanceCommand.ExecuteNonQuery();
            }
        }

        public static void UpdateCreatureAppearance(uint creatureId, uint slot, uint classId, uint color)
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                UpdateCreatureAppearanceCommand.Parameters["@CreatureId"].Value = creatureId;
                UpdateCreatureAppearanceCommand.Parameters["@Slot"].Value = slot;
                UpdateCreatureAppearanceCommand.Parameters["@Class"].Value = classId;
                UpdateCreatureAppearanceCommand.Parameters["@Color"].Value = color;
                UpdateCreatureAppearanceCommand.ExecuteNonQuery();
            }
        }
    }
}
