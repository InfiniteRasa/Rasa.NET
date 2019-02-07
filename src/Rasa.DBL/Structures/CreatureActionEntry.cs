using MySql.Data.MySqlClient;

namespace Rasa.Structures
{
    public class CreatureActionEntry
    {
        public uint Id { get; set; }
        public string Description { get; set; }
        public uint ActionId { get; set; }
        public uint ActionArgId { get; set; }
        public float RangeMin { get; set; }
        public float RangeMax { get; set; }
        public uint Cooldown { get; set; }
        public uint WindupTime { get; set; }
        public uint MinDamage { get; set; }
        public uint MaxDamage { get; set; }

        public static CreatureActionEntry Read(MySqlDataReader reader)
        {
            return new CreatureActionEntry
            {
                Id = reader.GetUInt32("id"),
                Description = reader.GetString("description"),
                ActionId = reader.GetUInt32("action_id"),
                ActionArgId = reader.GetUInt32("action_arg_id"),
                RangeMin = reader.GetFloat("range_min"),
                RangeMax = reader.GetFloat("range_max"),
                Cooldown = reader.GetUInt32("cooldown"),
                WindupTime = reader.GetUInt32("windup_time"),
                MinDamage = reader.GetUInt32("min_damage"),
                MaxDamage = reader.GetUInt32("max_damage")
            };
        }
    }
}
