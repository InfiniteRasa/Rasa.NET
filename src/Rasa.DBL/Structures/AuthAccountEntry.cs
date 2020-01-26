using System;
using System.Security.Cryptography;
using System.Text;

namespace Rasa.Structures
{
    public partial class AuthAccountEntry
    {
        public uint Id { get; set; }
        public string Email { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Salt { get; set; }
        public byte Level { get; set; }
        public string LastIp { get; set; }
        public byte LastServerId { get; set; }
        public DateTime? LastLogin { get; set; }
        public DateTime JoinDate { get; set; }
        public bool Locked { get; set; }
        public bool Validated { get; set; }
        public string ValidationToken { get; set; }

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
