﻿using System;
using System.Security.Cryptography;
using System.Text;

using MySql.Data.MySqlClient;

namespace Rasa.Structures
{
    public class AuthAccountEntry
    {
        public uint Id { get; set; }
        public string Email { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Salt { get; set; }
        public byte Level { get; set; }
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
