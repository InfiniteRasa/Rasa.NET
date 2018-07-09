using MySql.Data.MySqlClient;

namespace Rasa.Structures
{
    public class UserOptionsEntry
    {
        public uint OptionId { get; set; }
        public string Value { get; set; }

        public static UserOptionsEntry Read(MySqlDataReader reader)
        {
            return new UserOptionsEntry
            {
                OptionId = reader.GetUInt32("option_id"),
                Value = reader.GetString("value")
            };
        }
    }
}
