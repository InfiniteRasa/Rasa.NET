using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.Character
{
    using Structures;

    public class GameAccountTable
    {
        private static readonly MySqlCommand CreateAccountIfNeededCommand = new MySqlCommand("INSERT INTO `account` (`id`, `name`, `email`) VALUE (@Id, @Name, @Email) ON DUPLICATE KEY UPDATE `name` = @Name, `email` = @Email");
        private static readonly MySqlCommand GetAccountCommand = new MySqlCommand("SELECT *, (SELECT COUNT(`id`) FROM `character` WHERE `account_id` = @Id) AS `character_count` FROM `account` WHERE `id` = @Id");
        private static readonly MySqlCommand GetAccountByFamilyNameCommand = new MySqlCommand("SELECT * FROM `account` WHERE `family_name` = @FamilyName");
        private static readonly MySqlCommand UpdateAccountCommand = new MySqlCommand("UPDATE `account` SET `level` = @Level, `family_name` = @FamilyName, `selected_slot` = @SelectedSlot, `can_skip_bootcamp` = @CanSkipBootcamp, `last_ip` = @LastIP, `last_login` = @LastLogin WHERE `id` = @Id");

        public static void Initialize()
        {
            CreateAccountIfNeededCommand.Connection = GameDatabaseAccess.CharConnection;
            CreateAccountIfNeededCommand.Parameters.Add("@Id", MySqlDbType.UInt32);
            CreateAccountIfNeededCommand.Parameters.Add("@Name", MySqlDbType.VarChar);
            CreateAccountIfNeededCommand.Parameters.Add("@Email", MySqlDbType.VarChar);
            CreateAccountIfNeededCommand.Prepare();

            GetAccountCommand.Connection = GameDatabaseAccess.CharConnection;
            GetAccountCommand.Parameters.Add("@Id", MySqlDbType.UInt32);
            GetAccountCommand.Prepare();

            GetAccountByFamilyNameCommand.Connection = GameDatabaseAccess.CharConnection;
            GetAccountByFamilyNameCommand.Parameters.Add("@FamilyName", MySqlDbType.VarChar);
            GetAccountByFamilyNameCommand.Prepare();

            UpdateAccountCommand.Connection = GameDatabaseAccess.CharConnection;
            UpdateAccountCommand.Parameters.Add("@Id", MySqlDbType.UInt32);
            UpdateAccountCommand.Parameters.Add("@Level", MySqlDbType.UByte);
            UpdateAccountCommand.Parameters.Add("@FamilyName", MySqlDbType.VarChar);
            UpdateAccountCommand.Parameters.Add("@SelectedSlot", MySqlDbType.UByte);
            UpdateAccountCommand.Parameters.Add("@CanSkipBootcamp", MySqlDbType.Bit);
            UpdateAccountCommand.Parameters.Add("@LastIP", MySqlDbType.VarChar);
            UpdateAccountCommand.Parameters.Add("@LastLogin", MySqlDbType.DateTime);
            UpdateAccountCommand.Prepare();
        }

        public static void CreateAccountDataIfNeeded(uint id, string name, string email)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                CreateAccountIfNeededCommand.Parameters["@Id"].Value = id;
                CreateAccountIfNeededCommand.Parameters["@Name"].Value = name;
                CreateAccountIfNeededCommand.Parameters["@Email"].Value = email;
                CreateAccountIfNeededCommand.ExecuteNonQuery();
            }
        }

        public static GameAccountEntry GetAccount(uint id)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                GetAccountCommand.Parameters["@Id"].Value = id;

                using (var reader = GetAccountCommand.ExecuteReader())
                    return GameAccountEntry.Read(reader);
            }
        }
        public static GameAccountEntry GetAccountByFamilyName(string familyName)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                GetAccountByFamilyNameCommand.Parameters["@FamilyName"].Value = familyName;

                using (var reader = GetAccountByFamilyNameCommand.ExecuteReader())
                    return GameAccountEntry.Read(reader, false);
            }
        }

        public static void UpdateAccount(GameAccountEntry entry)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                UpdateAccountCommand.Parameters["@Id"].Value = entry.Id;
                UpdateAccountCommand.Parameters["@Level"].Value = entry.Level;
                UpdateAccountCommand.Parameters["@FamilyName"].Value = entry.FamilyName ?? "";
                UpdateAccountCommand.Parameters["@SelectedSlot"].Value = entry.SelectedSlot;
                UpdateAccountCommand.Parameters["@CanSkipBootcamp"].Value = entry.CanSkipBootcamp;
                UpdateAccountCommand.Parameters["@LastIP"].Value = entry.LastIp;
                UpdateAccountCommand.Parameters["@LastLogin"].Value = entry.LastLogin;
                UpdateAccountCommand.ExecuteNonQuery();
            }
        }
    }
}
