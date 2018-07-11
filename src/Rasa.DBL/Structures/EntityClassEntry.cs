using MySql.Data.MySqlClient;

namespace Rasa.Structures
{
    public class EntityClassEntry
    {
        public uint ClassId { get; set; }
        public string ClassName { get; set; }
        public int MeshId { get; set; }
        public short ClassCollisionRole { get; set; }
        public bool TargetFlag { get; set; }
        public string Augmentations { get; set; }

        public static EntityClassEntry Read(MySqlDataReader reader)
        {
            return new EntityClassEntry
            {
                ClassId = reader.GetUInt32("classId"),
                ClassName = reader.GetString("className"),
                MeshId = reader.GetInt32("meshId"),
                ClassCollisionRole = reader.GetInt16("classCollisionRole"),
                TargetFlag = reader.GetBoolean("targetFlag"),
                Augmentations = reader.GetString("augList")
            };
        }
    }
}
