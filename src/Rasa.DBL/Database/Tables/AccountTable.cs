using System.Net;

using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables
{
    using Structures;

    public static class AccountTable
    {
        private static readonly MySqlCommand GetAccountByNameCommand = new MySqlCommand("SELECT `id`, `username`, `password`, `salt`, `level`, `last_server_id`, `locked`, `validated` FROM `account` WHERE `username` = @AccountName;");
        private static readonly MySqlCommand GetAccountByIdCommand = new MySqlCommand("SELECT `id`, `username`, `password`, `salt`, `level`, `last_server_id` FROM `account` WHERE `id` = @AccountId;");
        private static readonly MySqlCommand UpdateLoginDataCommand = new MySqlCommand("UPDATE `account` SET `last_login` = NOW(), `last_ip` = @LastIP WHERE `id` = @AccountId;");
        private static readonly MySqlCommand UpdateLastServerCommand = new MySqlCommand("UPDATE `account` SET `last_server_id` = @LastServerId WHERE `id` = @AccountId;");
        private static readonly MySqlCommand InsertAccountCommand = new MySqlCommand("INSERT INTO `account` (`email`, `username`, `password`, `salt`, `validated`) VALUES (@Email, @Username, @Password, @Salt, 1);");

        public static void Initialize()
        {
            GetAccountByNameCommand.Connection = DatabaseAccess.Connection;
            GetAccountByNameCommand.Parameters.Add("@AccountName", MySqlDbType.VarChar);
            GetAccountByNameCommand.Prepare();

            GetAccountByIdCommand.Connection = DatabaseAccess.Connection;
            GetAccountByIdCommand.Parameters.Add("@AccountId", MySqlDbType.UInt32);
            GetAccountByIdCommand.Prepare();

            UpdateLoginDataCommand.Connection = DatabaseAccess.Connection;
            UpdateLoginDataCommand.Parameters.Add("@AccountId", MySqlDbType.UInt32);
            UpdateLoginDataCommand.Parameters.Add("@LastIP", MySqlDbType.VarChar);
            UpdateLoginDataCommand.Prepare();

            UpdateLastServerCommand.Connection = DatabaseAccess.Connection;
            UpdateLastServerCommand.Parameters.Add("@AccountId", MySqlDbType.UInt32);
            UpdateLastServerCommand.Parameters.Add("@LastServerId", MySqlDbType.UByte);
            UpdateLastServerCommand.Prepare();

            InsertAccountCommand.Connection = DatabaseAccess.Connection;
            InsertAccountCommand.Parameters.Add("@Email", MySqlDbType.VarChar);
            InsertAccountCommand.Parameters.Add("@Username", MySqlDbType.VarChar);
            InsertAccountCommand.Parameters.Add("@Password", MySqlDbType.VarChar);
            InsertAccountCommand.Parameters.Add("@Salt", MySqlDbType.VarChar);
            InsertAccountCommand.Prepare();
        }

        public static AccountEntry GetAccount(string accountName)
        {
            lock (DatabaseAccess.Lock)
            {
                GetAccountByNameCommand.Parameters["@AccountName"].Value = accountName;

                using (var reader = GetAccountByNameCommand.ExecuteReader())
                    return AccountEntry.Read(reader);
            }
        }

        public static AccountEntry GetAccount(uint accountId)
        {
            lock (DatabaseAccess.Lock)
            {
                GetAccountByIdCommand.Parameters["@AccountId"].Value = accountId;

                using (var reader = GetAccountByIdCommand.ExecuteReader())
                    return AccountEntry.Read(reader);
            }
        }

        public static void UpdateLoginData(uint accountId, IPAddress ipa)
        {
            lock (DatabaseAccess.Lock)
            {
                UpdateLoginDataCommand.Parameters["@LastIP"].Value = ipa.ToString();
                UpdateLoginDataCommand.Parameters["@AccountId"].Value = accountId;
                UpdateLoginDataCommand.ExecuteNonQuery();
            }
        }

        public static void UpdateLastServer(uint accountId, byte lastServerId)
        {
            lock (DatabaseAccess.Lock)
            {
                UpdateLastServerCommand.Parameters["@LastServerId"].Value = lastServerId;
                UpdateLastServerCommand.Parameters["@AccountId"].Value = accountId;
                UpdateLastServerCommand.ExecuteNonQuery();
            }
        }

        public static void InsertAccount(AccountEntry entry)
        {
            lock (DatabaseAccess.Lock)
            {
                InsertAccountCommand.Parameters["@Email"].Value = entry.Email;
                InsertAccountCommand.Parameters["@Username"].Value = entry.Username;
                InsertAccountCommand.Parameters["@Password"].Value = entry.Password;
                InsertAccountCommand.Parameters["@Salt"].Value = entry.Salt;
                InsertAccountCommand.ExecuteNonQuery();
            }
        }
    }
}
