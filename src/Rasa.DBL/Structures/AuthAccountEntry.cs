using System;
using System.Security.Cryptography;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MySql.Data.MySqlClient;

namespace Rasa.Structures
{
    using Microsoft.EntityFrameworkCore;

    [Table(AuthAccountEntry.TableName)]
    [Index(nameof(AuthAccountEntry.Email), IsUnique = true, Name = "email_UNIQUE")]
    [Index(nameof(AuthAccountEntry.Username), IsUnique = true, Name = "email_UNIQUE")]
    public class AuthAccountEntry
    {
        public const string TableName = "account";

        [Key]
        [Column(TypeName = "int(11)")]
        public uint Id { get; set; }

        [EmailAddress]
        [Column(TypeName = "varchar(255)")]
        public string Email { get; set; }

        [Column(TypeName = "varchar(64)")]
        public string Username { get; set; }

        [Column(TypeName = "varchar(64)")]
        public string Password { get; set; }

        [Column(TypeName = "varchar(40)")]
        public string Salt { get; set; }

        [Column(TypeName = "tinyint(3)")]
        public byte Level { get; set; }

        [Column("last_ip", TypeName = "varchar(45)")]
        public string LastIp { get; set; }

        [Column("last_server_id", TypeName = "tinyint(3)")]
        public byte LastServerId { get; set; }

        public bool Locked { get; set; }

        public bool Validated { get; set; }

        public static AuthAccountEntry Read(MySqlDataReader reader)
        {
            if (!reader.Read())
                return null;

            return new AuthAccountEntry
            {
                Id = reader.GetUInt32("id"),
                Email = reader.GetString("email"),
                Username = reader.GetString("username"),
                Password = reader.GetString("password"),
                Salt = reader.GetString("salt"),
                Level = reader.GetByte("level"),
                LastServerId = reader.GetByte("last_server_id"),
                Locked = reader.GetBoolean("locked"),
                Validated = reader.GetBoolean("validated")
            };
        }

        public void HashPassword()
        {
            Password = Hash(Password, Salt);
        }

        public bool CheckPassword(string password)
        {
            return Hash(password, Salt) == Password;
        }

        public static string Hash(string password, string salt)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes($"{salt}:{password}"))).Replace("-", "").ToLower();
        }
    }
}
