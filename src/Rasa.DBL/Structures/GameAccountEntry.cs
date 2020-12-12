using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using MySql.Data.MySqlClient;

namespace Rasa.Structures
{
    using Interfaces;

    [Table(GameAccountEntry.TableName)]
    public class GameAccountEntry : IHasId
    {
        public const string TableName = "account";

        [Key]
        [Column("id")]
        public uint Id { get; set; }

        [Column("email", TypeName = "varchar(255)")]
        [Required]
        public string Email { get; set; }

        [Column("name", TypeName = "varchar(64)")]
        [Required]
        public string Name { get; set; }

        [Column("level")]
        [Required]
        public byte Level { get; set; }

        [Column("family_name", TypeName = "varchar(64)")]
        [Required]
        public string FamilyName { get; set; }

        [Column("selected_slot")]
        [Required]
        public byte SelectedSlot { get; set; }

        [Column("can_skip_bootcamp", TypeName = "bit")]
        [Required]
        public bool CanSkipBootcamp { get; set; }

        [Column("last_ip", TypeName = "varchar(15)")]
        [Required]
        public string LastIp { get; set; }

        [Column("last_login")]
        [Required]
        public DateTime LastLogin { get; set; }

        [Column("character_count")]
        [Required]
        public byte CharacterCount { get; set; }

        [Column("created_at")]
        [Required]
        public DateTime CreatedAt { get; set; }

        public static GameAccountEntry Read(MySqlDataReader reader, bool charCount = true)
        {
            if (!reader.Read())
                return null;

            var entry = new GameAccountEntry
            {
                Id = reader.GetUInt32("id"),
                Email = reader.GetString("name"),
                Name = reader.GetString("name"),
                Level = reader.GetByte("level"),
                FamilyName = reader.GetString("family_name"),
                SelectedSlot = reader.GetByte("selected_slot"),
                CanSkipBootcamp = reader.GetBoolean("can_skip_bootcamp"),
                LastIp = reader.GetString("last_ip"),
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
