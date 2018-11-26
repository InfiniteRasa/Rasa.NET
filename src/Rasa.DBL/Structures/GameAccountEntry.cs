using System;

using MySql.Data.MySqlClient;

namespace Rasa.Structures
{
    public class GameAccountEntry
    {
        public uint Id { get; private set; }
        public string Email { get; private set; }
        public string Name { get; private set; }
        public byte Level { get; set; }
        public byte SelectedSlot { get; set; }
        public string FamilyName { get; set; }
        public bool CanSkipBootcamp { get; set; }
        public string LastIP { get; set; }
        public DateTime LastLogin { get; set; }
        public byte CharacterCount { get; set; }

        public static GameAccountEntry Read(MySqlDataReader reader, bool charCount = true)
        {
            if (!reader.Read())
                return null;

            var entry = new GameAccountEntry
            {
                Id = reader.GetUInt32("id"),
                Email = reader.GetString("email"),
                Name = reader.GetString("name"),
                Level = reader.GetByte("level"),
                FamilyName = reader.GetString("family_name"),
                SelectedSlot = reader.GetByte("selected_slot"),
                CanSkipBootcamp = reader.GetBoolean("can_skip_bootcamp"),
                LastIP = reader.GetString("last_ip"),
                LastLogin = reader.GetDateTime("last_login")
            };

            if (charCount)
            {
                entry.CharacterCount = (byte)reader.GetInt64("character_count");
            }

            if (string.IsNullOrWhiteSpace(entry.FamilyName))
                entry.FamilyName = null;

            return entry;
        }
    }
}
