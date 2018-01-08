using MySql.Data.MySqlClient;

namespace Rasa.Structures
{
    public class CreatureAppearenceEntry
    {
        public int Helmet { get; set; }
        public int HelmetHue { get; set; }
        public int Shoes { get; set; }
        public int ShoesHue { get; set; }
        public int Gloves { get; set; }
        public int GlovesHue { get; set; }
        public int Weapon { get; set; }
        public int WeaponHue { get; set; }
        public int Hair { get; set; }
        public int HairHue { get; set; }
        public int Torso { get; set; }
        public int TorsoHue { get; set; }
        public int Legs { get; set; }
        public int LegsHue { get; set; }
        public int Face { get; set; }
        public int FaceHue { get; set; }
        public int EyeWear { get; set; }
        public int EyeWearHue { get; set; }
        public int Beard { get; set; }
        public int BeardHue { get; set; }
        public int Mask { get; set; }
        public int MaskHue { get; set; }

        public static CreatureAppearenceEntry Read(MySqlDataReader reader)
        {
            return new CreatureAppearenceEntry
            {
                Helmet = reader.GetInt32("helmet"),
                HelmetHue = reader.GetInt32("helmetHue"),
                Shoes = reader.GetInt32("shoes"),
                ShoesHue = reader.GetInt32("shoesHue"),
                Gloves = reader.GetInt32("gloves"),
                GlovesHue = reader.GetInt32("glovesHue"),
                Weapon = reader.GetInt32("weapon"),
                WeaponHue = reader.GetInt32("weaponHue"),
                Hair = reader.GetInt32("hair"),
                HairHue = reader.GetInt32("hairHue"),
                Torso = reader.GetInt32("torso"),
                TorsoHue = reader.GetInt32("torsoHue"),
                Legs = reader.GetInt32("legs"),
                LegsHue = reader.GetInt32("legsHue"),
                Face = reader.GetInt32("face"),
                FaceHue = reader.GetInt32("faceHue"),
                EyeWear = reader.GetInt32("eyeWear"),
                EyeWearHue = reader.GetInt32("eyeWearHue"),
                Beard = reader.GetInt32("beard"),
                BeardHue = reader.GetInt32("beardHue"),
                Mask = reader.GetInt32("mask"),
                MaskHue = reader.GetInt32("maskHue")
            };
        }
    }
}