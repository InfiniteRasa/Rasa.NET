using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader.Missions.Wilderness
{
    public static class WildernessRewardEquipmentV1
    {
        public static void Up(MigrationBuilder migration) =>
            migration.InsertData("itemtemplate_armor", new[] { "id", "armor_value" },
                new object[,]
                {
                    { 20250U, 71 },
                    { 35486U, 94 },
                    { 26996U, 54 },
                    { 20697U, 118 },
                    { 20399U, 47 },
                    { 20846U, 141 },
                    { 20548U, 94 },
                    { 36083U, 189 },
                    { 12827U, 91 },
                    { 12855U, 60 },
                    { 11567U, 83 },
                    { 11568U, 139 },
                    { 12887U, 187 },
                    { 12831U, 140 },
                    { 12943U, 281 },
                    { 12915U, 234 },
                    { 13388U, 250 },
                    { 35784U, 126 },
                    { 35933U, 157 },
                    { 13744U, 167 },
                    { 28692U, 66 },
                    { 13739U, 100 }
                });

        public static void Down(MigrationBuilder migration)
        {
            foreach (var id in new uint[]
            {
                20250, 35486, 26996, 20697, 20399, 20846, 20548, 36083,
                12827, 12855, 11567, 11568, 12887, 12831, 12943, 12915, 13388,
                35784, 35933, 13744, 28692, 13739
            })
                migration.DeleteData("itemtemplate_armor", "id", id);
        }
    }
}
