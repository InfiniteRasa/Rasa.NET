using MySql.Data.MySqlClient;

namespace Rasa.Structures
{
    public class CharacterEquipment
    {
        public uint Id { get; set; }
        public int Helmet { get; set; }
        public int HelmetHue { get; set; }
        public int Shoes { get; set; }
        public int ShoesHue { get; set; }
        public int Gloves { get; set; }
        public int GlovesHue { get; set; }
        public int Slot4 { get; set; }
        public int Slot4Hue { get; set; }
        public int Slot5 { get; set; }
        public int Slot5Hue { get; set; }
        public int Slot6 { get; set; }
        public int Slot6Hue { get; set; }
        public int Slot7 { get; set; }
        public int Slot7Hue { get; set; }
        public int Slot8 { get; set; }
        public int Slot8Hue { get; set; }
        public int Slot9 { get; set; }
        public int Slot9Hue { get; set; }
        public int Slot10 { get; set; }
        public int Slot10Hue { get; set; }
        public int Slot11 { get; set; }
        public int Slot11Hue { get; set; }
        public int Slot12 { get; set; }
        public int Slot12Hue { get; set; }
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
        public int Wing { get; set; }
        public int WingHue { get; set; }
        public int EyeWeare { get; set; }
        public int EyeWeareHue { get; set; }
        public int Beard { get; set; }
        public int BeardHue { get; set; }
        public int Mask { get; set; }
        public int MaskHue { get; set; }

        public static CharacterEquipment Read(MySqlDataReader reader)
        {
            if (!reader.Read())
                return null;

            return new CharacterEquipment
            {
                //Id = reader.GetUInt32("id"),
                Helmet = reader.GetInt32("helmet"),
                HelmetHue = reader.GetInt32("helmetHue"),
                Shoes = reader.GetInt32("shoes"),
                ShoesHue = reader.GetInt32("shoesHue"),
                Gloves = reader.GetInt32("gloves"),
                GlovesHue = reader.GetInt32("glovesHue"),
                Slot4 = reader.GetInt32("slot4"),
                Slot4Hue = reader.GetInt32("slot4Hue"),
                Slot5 = reader.GetInt32("slot5"),
                Slot5Hue = reader.GetInt32("slot5Hue"),
                Slot6 = reader.GetInt32("slot6"),
                Slot6Hue = reader.GetInt32("slot6Hue"),
                Slot7 = reader.GetInt32("slot7"),
                Slot7Hue = reader.GetInt32("slot7Hue"),
                Slot8 = reader.GetInt32("slot8"),
                Slot8Hue = reader.GetInt32("slot8Hue"),
                Slot9 = reader.GetInt32("slot9"),
                Slot9Hue = reader.GetInt32("slot9Hue"),
                Slot10 = reader.GetInt32("slot10"),
                Slot10Hue = reader.GetInt32("slot10Hue"),
                Slot11 = reader.GetInt32("slot11"),
                Slot11Hue = reader.GetInt32("slot11Hue"),
                Slot12 = reader.GetInt32("slot12"),
                Slot12Hue = reader.GetInt32("slot12Hue"),
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
                Wing = reader.GetInt32("wing"),
                WingHue = reader.GetInt32("wingHue"),
                EyeWeare = reader.GetInt32("eyeWeare"),
                EyeWeareHue = reader.GetInt32("eyeWeareHue"),
                Beard = reader.GetInt32("beard"),
                BeardHue = reader.GetInt32("beardHue"),
                Mask = reader.GetInt32("mask"),
                MaskHue = reader.GetInt32("maskHue")
            };
        }
    }
}
