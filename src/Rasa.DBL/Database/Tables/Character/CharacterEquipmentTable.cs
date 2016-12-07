using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.Character
{
    using Structures;
    public class CharacterEquipmentTable
    {
        private static readonly MySqlCommand BasicEquipmentCommand = new MySqlCommand(
            "INSERT INTO character_equipment (id, helmet, helmetHue, shoes, shoesHue, gloves, glovesHue, hair, hairHue, " +
            "torso, torsoHue, legs, legsHue, face, faceHue, eyeWeare, eyeWeareHue, beard, beardHue) VALUES " +
            "(@Id, @Helmet, @HelmetHue, @Shoes, @ShoesHue, @Gloves, @GlovesHue, @Hair, @HairHue, " +
            "@Torso, @TorsoHue, @Legs, @LegsHue, @Face, @FaceHue, @EyeWeare, @EyeWeareHue, @Beard, @BeardHue)");
        private static readonly MySqlCommand GetEquipmentCommand = new MySqlCommand(
                "SELECT helmet, helmetHue, shoes, shoesHue, gloves, glovesHue, slot4, slot4Hue, slot5, slot5Hue, " +
                "slot6, slot6Hue, slot7, slot7Hue, slot8, slot8Hue, slot9, slot9Hue, slot10, slot10Hue, " +
                "slot11, slot11Hue, slot12, slot12Hue, weapon, weaponHue, hair, hairHue, torso, torsoHue, " +
                "legs, legsHue, face, faceHue, wing, wingHue, eyeWeare, eyeWeareHue, beard, beardHue, mask, maskHue " +
                "FROM character_equipment WHERE Id = @Id");
        private static readonly MySqlCommand UpdateEquipmentCommand = new MySqlCommand(
            "INSERT INTO character_equipment (id, helmet, helmetHue, shoes, shoesHue, gloves, glovesHue, slot4, slot4Hue, slot5, slot5Hue, " +
            "slot6, slot6Hue, slot7, slot7Hue, slot8, slot8Hue, slot9, slot9Hue, slot10, slot10Hue, " +
            "slot11, slot11Hue, slot12, slot12Hue, weapon, weaponHue, hair, hairHue, torso, torsoHue, " +
            "legs, legsHue, face, faceHue, wing, wingHue, eyeWeare, eyeWeareHue, beard, beardHue, mask, maskHue) VALUES " +
            "(@Id, @Helmet, @HelmetHue, @Shoes, @ShoesHue, @Gloves, @GlovesHue, @Slot4, @Slot4Hue, @Slot5, @Slot5Hue," +
            "@Slot6, @Slot6Hue, @Slot7, @Slot7Hue, @Slot8, @Slot8Hue, @Slot9, @Slot9Hue, @Slot10, @Slot10Hue, " +
            "@Slot11, @Slot11Hue, @Slot12, @Slot12Hue, @Weapon, @WeaponHue, @Hair, @HairHue, @Torso, @TorsoHue, " +
            "@Legs, @LegsHue, @Face, @FaceHue, @Wing, @WingHue, @EyeWeare, @EyeWeareHue, @Beard, @BeardHue, @Mask, @MaskHue)");
        public static void Initialize()
        {
            BasicEquipmentCommand.Connection = GameDatabaseAccess.CharConnection;
            BasicEquipmentCommand.Parameters.Add("@Id", MySqlDbType.UInt32);
            BasicEquipmentCommand.Parameters.Add("@Helmet", MySqlDbType.Int32);
            BasicEquipmentCommand.Parameters.Add("@HelmetHue", MySqlDbType.Int32);
            BasicEquipmentCommand.Parameters.Add("@Shoes", MySqlDbType.Int32);
            BasicEquipmentCommand.Parameters.Add("@ShoesHue", MySqlDbType.Int32);
            BasicEquipmentCommand.Parameters.Add("@Gloves", MySqlDbType.Int32);
            BasicEquipmentCommand.Parameters.Add("@GlovesHue", MySqlDbType.Int32);
            BasicEquipmentCommand.Parameters.Add("@Hair", MySqlDbType.Int32);
            BasicEquipmentCommand.Parameters.Add("@HairHue", MySqlDbType.Int32);
            BasicEquipmentCommand.Parameters.Add("@Torso", MySqlDbType.Int32);
            BasicEquipmentCommand.Parameters.Add("@TorsoHue", MySqlDbType.Int32);
            BasicEquipmentCommand.Parameters.Add("@Legs", MySqlDbType.Int32);
            BasicEquipmentCommand.Parameters.Add("@LegsHue", MySqlDbType.Int32);
            BasicEquipmentCommand.Parameters.Add("@Face", MySqlDbType.Int32);
            BasicEquipmentCommand.Parameters.Add("@FaceHue", MySqlDbType.Int32);
            BasicEquipmentCommand.Parameters.Add("@EyeWeare", MySqlDbType.Int32);
            BasicEquipmentCommand.Parameters.Add("@EyeWeareHue", MySqlDbType.Int32);
            BasicEquipmentCommand.Parameters.Add("@Beard", MySqlDbType.Int32);
            BasicEquipmentCommand.Parameters.Add("@BeardHue", MySqlDbType.Int32);

            GetEquipmentCommand.Connection = GameDatabaseAccess.CharConnection;
            GetEquipmentCommand.Parameters.Add("@Id", MySqlDbType.UInt32);
            GetEquipmentCommand.Prepare();

            UpdateEquipmentCommand.Connection = GameDatabaseAccess.CharConnection;
            UpdateEquipmentCommand.Parameters.Add("@Id", MySqlDbType.UInt32);
            UpdateEquipmentCommand.Parameters.Add("@Helmet", MySqlDbType.Int32);
            UpdateEquipmentCommand.Parameters.Add("@HelmetHue", MySqlDbType.Int32);
            UpdateEquipmentCommand.Parameters.Add("@Shoes", MySqlDbType.Int32);
            UpdateEquipmentCommand.Parameters.Add("@ShoesHue", MySqlDbType.Int32);
            UpdateEquipmentCommand.Parameters.Add("@Gloves", MySqlDbType.Int32);
            UpdateEquipmentCommand.Parameters.Add("@GlovesHue", MySqlDbType.Int32);
            UpdateEquipmentCommand.Parameters.Add("@Slot4", MySqlDbType.Int32);
            UpdateEquipmentCommand.Parameters.Add("@Slot4Hue", MySqlDbType.Int32);
            UpdateEquipmentCommand.Parameters.Add("@Slot5", MySqlDbType.Int32);
            UpdateEquipmentCommand.Parameters.Add("@Slot5Hue", MySqlDbType.Int32);
            UpdateEquipmentCommand.Parameters.Add("@Slot6", MySqlDbType.Int32);
            UpdateEquipmentCommand.Parameters.Add("@Slot6Hue", MySqlDbType.Int32);
            UpdateEquipmentCommand.Parameters.Add("@Slot7", MySqlDbType.Int32);
            UpdateEquipmentCommand.Parameters.Add("@Slot7Hue", MySqlDbType.Int32);
            UpdateEquipmentCommand.Parameters.Add("@Slot8", MySqlDbType.Int32);
            UpdateEquipmentCommand.Parameters.Add("@Slot8Hue", MySqlDbType.Int32);
            UpdateEquipmentCommand.Parameters.Add("@Slot9", MySqlDbType.Int32);
            UpdateEquipmentCommand.Parameters.Add("@Slot9Hue", MySqlDbType.Int32);
            UpdateEquipmentCommand.Parameters.Add("@Slot10", MySqlDbType.Int32);
            UpdateEquipmentCommand.Parameters.Add("@Slot10Hue", MySqlDbType.Int32);
            UpdateEquipmentCommand.Parameters.Add("@Slot11", MySqlDbType.Int32);
            UpdateEquipmentCommand.Parameters.Add("@Slot11Hue", MySqlDbType.Int32);
            UpdateEquipmentCommand.Parameters.Add("@Slot12", MySqlDbType.Int32);
            UpdateEquipmentCommand.Parameters.Add("@Slot12Hue", MySqlDbType.Int32);
            UpdateEquipmentCommand.Parameters.Add("@Weapon", MySqlDbType.Int32);
            UpdateEquipmentCommand.Parameters.Add("@WeaponHue", MySqlDbType.Int32);
            UpdateEquipmentCommand.Parameters.Add("@Hair", MySqlDbType.Int32);
            UpdateEquipmentCommand.Parameters.Add("@HairHue", MySqlDbType.Int32);
            UpdateEquipmentCommand.Parameters.Add("@Torso", MySqlDbType.Int32);
            UpdateEquipmentCommand.Parameters.Add("@TorsoHue", MySqlDbType.Int32);
            UpdateEquipmentCommand.Parameters.Add("@Legs", MySqlDbType.Int32);
            UpdateEquipmentCommand.Parameters.Add("@LegsHue", MySqlDbType.Int32);
            UpdateEquipmentCommand.Parameters.Add("@Face", MySqlDbType.Int32);
            UpdateEquipmentCommand.Parameters.Add("@FaceHue", MySqlDbType.Int32);
            UpdateEquipmentCommand.Parameters.Add("@Wing", MySqlDbType.Int32);
            UpdateEquipmentCommand.Parameters.Add("@WingHue", MySqlDbType.Int32);
            UpdateEquipmentCommand.Parameters.Add("@EyeWeare", MySqlDbType.Int32);
            UpdateEquipmentCommand.Parameters.Add("@EyeWeareHue", MySqlDbType.Int32);
            UpdateEquipmentCommand.Parameters.Add("@Beard", MySqlDbType.Int32);
            UpdateEquipmentCommand.Parameters.Add("@BeardHue", MySqlDbType.Int32);
            UpdateEquipmentCommand.Parameters.Add("@Mask", MySqlDbType.Int32);
            UpdateEquipmentCommand.Parameters.Add("@MaskHue", MySqlDbType.Int32);
        }

        public static void BasicEquipment(uint id, int helmet, int helmetHue, int shoes, int shoesHue, int gloves, int glovesHue, int hair, int hairHue,
                                            int torso, int torsoHue, int legs, int legsHue, int face, int faceHue, int eyeWeare, int eyeWeareHue, int beard, int beardHue)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                BasicEquipmentCommand.Parameters["@Id"].Value = id;
                BasicEquipmentCommand.Parameters["@Helmet"].Value = helmet;
                BasicEquipmentCommand.Parameters["@HelmetHue"].Value = helmetHue;
                BasicEquipmentCommand.Parameters["@Shoes"].Value = shoes;
                BasicEquipmentCommand.Parameters["@ShoesHue"].Value = shoesHue;
                BasicEquipmentCommand.Parameters["@Gloves"].Value = gloves;
                BasicEquipmentCommand.Parameters["@GlovesHue"].Value = glovesHue;
                BasicEquipmentCommand.Parameters["@Hair"].Value = hair;
                BasicEquipmentCommand.Parameters["@HairHue"].Value = hairHue;
                BasicEquipmentCommand.Parameters["@Torso"].Value = torso;
                BasicEquipmentCommand.Parameters["@TorsoHue"].Value = torsoHue;
                BasicEquipmentCommand.Parameters["@Legs"].Value = legs;
                BasicEquipmentCommand.Parameters["@LegsHue"].Value = legsHue;
                BasicEquipmentCommand.Parameters["@Face"].Value = face;
                BasicEquipmentCommand.Parameters["@FaceHue"].Value = faceHue;
                BasicEquipmentCommand.Parameters["@EyeWeare"].Value = eyeWeare;
                BasicEquipmentCommand.Parameters["@EyeWeareHue"].Value = eyeWeareHue;
                BasicEquipmentCommand.Parameters["@Beard"].Value = beard;
                BasicEquipmentCommand.Parameters["@BeardHue"].Value = beardHue;
                BasicEquipmentCommand.ExecuteNonQuery();
            }
        }

        public static CharacterEquipment GetEquipment(uint id)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                GetEquipmentCommand.Parameters["@Id"].Value = id;

                using (var reader = GetEquipmentCommand.ExecuteReader())
                    return CharacterEquipment.Read(reader);
            }
        }

        public static void UpdateEquipment(uint id, int helmet, int helmetHue, int shoes, int shoesHue, int gloves, int glovesHue, int slot4, int slot4Hue, int slot5, int slot5Hue,
                                            int slot6, int slot6Hue, int slot7, int slot7Hue, int slot8, int slot8Hue, int slot9, int slot9Hue, int slot10, int slot10Hue,
                                            int slot11, int slot11Hue, int slot12, int slot12Hue, int weapon, int weaponHue, int hair, int hairHue, int torso, int torsoHue,
                                            int legs, int legsHue, int face, int faceHue, int wing, int wingHue, int eyeWeare, int eyeWeareHue, int beard, int beardHue, int mask, int maskHue)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                UpdateEquipmentCommand.Parameters["@Id"].Value = id;
                UpdateEquipmentCommand.Parameters["@Helmet"].Value = helmet;
                UpdateEquipmentCommand.Parameters["@HelmetHue"].Value = helmetHue;
                UpdateEquipmentCommand.Parameters["@Shoes"].Value = shoes;
                UpdateEquipmentCommand.Parameters["@ShoesHue"].Value = shoesHue;
                UpdateEquipmentCommand.Parameters["@Gloves"].Value = gloves;
                UpdateEquipmentCommand.Parameters["@GlovesHue"].Value = glovesHue;
                UpdateEquipmentCommand.Parameters["@Slot4"].Value = slot4;
                UpdateEquipmentCommand.Parameters["@Slot4Hue"].Value = slot4Hue;
                UpdateEquipmentCommand.Parameters["@Slot5"].Value = slot5;
                UpdateEquipmentCommand.Parameters["@Slot5Hue"].Value = slot5Hue;
                UpdateEquipmentCommand.Parameters["@Slot6"].Value = slot6;
                UpdateEquipmentCommand.Parameters["@Slot6Hue"].Value = slot6Hue;
                UpdateEquipmentCommand.Parameters["@Slot7"].Value = slot7;
                UpdateEquipmentCommand.Parameters["@Slot7Hue"].Value = slot7Hue;
                UpdateEquipmentCommand.Parameters["@Slot8"].Value = slot8;
                UpdateEquipmentCommand.Parameters["@Slot8Hue"].Value = slot8Hue;
                UpdateEquipmentCommand.Parameters["@Slot9"].Value = slot9;
                UpdateEquipmentCommand.Parameters["@Slot9Hue"].Value = slot9Hue;
                UpdateEquipmentCommand.Parameters["@Slot10"].Value = slot10;
                UpdateEquipmentCommand.Parameters["@Slot10Hue"].Value = slot10Hue;
                UpdateEquipmentCommand.Parameters["@Slot11"].Value = slot11;
                UpdateEquipmentCommand.Parameters["@Slot11Hue"].Value = slot11Hue;
                UpdateEquipmentCommand.Parameters["@Slot12"].Value = slot12;
                UpdateEquipmentCommand.Parameters["@Slot12Hue"].Value = slot12Hue;
                UpdateEquipmentCommand.Parameters["@Weapon"].Value = weapon;
                UpdateEquipmentCommand.Parameters["@WeaponHue"].Value = weaponHue;
                UpdateEquipmentCommand.Parameters["@Hair"].Value = hair;
                UpdateEquipmentCommand.Parameters["@HairHue"].Value = hairHue;
                UpdateEquipmentCommand.Parameters["@Torso"].Value = torso;
                UpdateEquipmentCommand.Parameters["@TorsoHue"].Value = torsoHue;
                UpdateEquipmentCommand.Parameters["@Legs"].Value = legs;
                UpdateEquipmentCommand.Parameters["@LegsHue"].Value = legsHue;
                UpdateEquipmentCommand.Parameters["@Face"].Value = face;
                UpdateEquipmentCommand.Parameters["@FaceHue"].Value = faceHue;
                UpdateEquipmentCommand.Parameters["@Wing"].Value = wing;
                UpdateEquipmentCommand.Parameters["@WingHue"].Value = wingHue;
                UpdateEquipmentCommand.Parameters["@EyeWeare"].Value = eyeWeare;
                UpdateEquipmentCommand.Parameters["@EyeWeareHue"].Value = eyeWeareHue;
                UpdateEquipmentCommand.Parameters["@Beard"].Value = beard;
                UpdateEquipmentCommand.Parameters["@BeardHue"].Value = beardHue;
                UpdateEquipmentCommand.Parameters["@Mask"].Value = mask;
                UpdateEquipmentCommand.Parameters["@MaskHue"].Value = maskHue;
                UpdateEquipmentCommand.ExecuteNonQuery();
                }
        }
    }
}