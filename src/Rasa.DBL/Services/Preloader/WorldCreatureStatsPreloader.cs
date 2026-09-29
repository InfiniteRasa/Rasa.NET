using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// Stats for the garrison rows: body/mind/spirit 15 as the boss rows have them, health as the creature
    /// row, armour (10 x level - 20) times the tier's share (minion 0.5, thug 0.75, lieutenant 1.0).
    /// </summary>
    public class WorldCreatureStatsPreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, CreatureStatEntry.TableName, typeof(CreatureStatEntry));
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { 531001, 15, 15, 15, 6878, 195 };
            yield return new object[] { 531002, 15, 15, 15, 6878, 195 };
            yield return new object[] { 531003, 15, 15, 15, 10000, 300 };
            yield return new object[] { 531004, 15, 15, 15, 10000, 300 };
            yield return new object[] { 531005, 15, 15, 15, 20811, 420 };
            yield return new object[] { 531006, 15, 15, 15, 10000, 300 };
            yield return new object[] { 531007, 15, 15, 15, 10000, 300 };
            yield return new object[] { 531008, 15, 15, 15, 5783, 185 };
            yield return new object[] { 531009, 15, 15, 15, 5783, 185 };
            yield return new object[] { 531010, 15, 15, 15, 8409, 285 };
            yield return new object[] { 531011, 15, 15, 15, 8409, 285 };
            yield return new object[] { 531012, 15, 15, 15, 17500, 400 };
            yield return new object[] { 531013, 15, 15, 15, 8409, 285 };
            yield return new object[] { 531014, 15, 15, 15, 8409, 285 };
            yield return new object[] { 531015, 15, 15, 15, 8409, 285 };
            yield return new object[] { 531016, 15, 15, 15, 5783, 185 };
            yield return new object[] { 531017, 15, 15, 15, 5783, 185 };
            yield return new object[] { 531018, 15, 15, 15, 7500, 200 };
            yield return new object[] { 531019, 15, 15, 15, 7500, 200 };
            yield return new object[] { 531020, 15, 15, 15, 10905, 308 };
            yield return new object[] { 531021, 15, 15, 15, 10905, 308 };
            yield return new object[] { 531022, 15, 15, 15, 7500, 200 };
            yield return new object[] { 531023, 15, 15, 15, 10905, 308 };
            yield return new object[] { 531024, 15, 15, 15, 4089, 165 };
            yield return new object[] { 531025, 15, 15, 15, 4089, 165 };
            yield return new object[] { 531026, 15, 15, 15, 5946, 255 };
            yield return new object[] { 531027, 15, 15, 15, 5946, 255 };
            yield return new object[] { 531028, 15, 15, 15, 4089, 165 };
            yield return new object[] { 531029, 15, 15, 15, 8179, 205 };
            yield return new object[] { 531030, 15, 15, 15, 8179, 205 };
            yield return new object[] { 531031, 15, 15, 15, 11892, 315 };
            yield return new object[] { 531032, 15, 15, 15, 11892, 315 };
            yield return new object[] { 531033, 15, 15, 15, 24749, 440 };
            yield return new object[] { 531034, 15, 15, 15, 11892, 315 };
            yield return new object[] { 531035, 15, 15, 15, 8179, 205 };
            yield return new object[] { 531036, 15, 15, 15, 8179, 205 };
            yield return new object[] { 531037, 15, 15, 15, 7071, 270 };
            yield return new object[] { 531038, 15, 15, 15, 4863, 175 };
            yield return new object[] { 531039, 15, 15, 15, 4863, 175 };
            yield return new object[] { 531040, 15, 15, 15, 4863, 175 };
            yield return new object[] { 531041, 15, 15, 15, 4863, 175 };
            yield return new object[] { 531042, 15, 15, 15, 7071, 270 };
            yield return new object[] { 531043, 15, 15, 15, 7071, 270 };
            yield return new object[] { 531044, 15, 15, 15, 7071, 270 };
            yield return new object[] { 531045, 15, 15, 15, 3439, 155 };
            yield return new object[] { 531046, 15, 15, 15, 3439, 155 };
            yield return new object[] { 531047, 15, 15, 15, 5000, 240 };
            yield return new object[] { 531048, 15, 15, 15, 5000, 240 };
            yield return new object[] { 531049, 15, 15, 15, 10406, 340 };
            yield return new object[] { 531050, 15, 15, 15, 5000, 240 };
            yield return new object[] { 531051, 15, 15, 15, 3439, 155 };
            yield return new object[] { 531052, 15, 15, 15, 5000, 240 };
            yield return new object[] { 531053, 15, 15, 15, 10406, 340 };
            yield return new object[] { 531054, 15, 15, 15, 10406, 340 };
            yield return new object[] { 531055, 15, 15, 15, 4460, 170 };
            yield return new object[] { 531056, 15, 15, 15, 4460, 170 };
            yield return new object[] { 531057, 15, 15, 15, 6484, 262 };
            yield return new object[] { 531058, 15, 15, 15, 6484, 262 };
            yield return new object[] { 531059, 15, 15, 15, 13494, 370 };
            yield return new object[] { 531060, 15, 15, 15, 6484, 262 };
            yield return new object[] { 531061, 15, 15, 15, 4460, 170 };
            yield return new object[] { 531062, 15, 15, 15, 13494, 370 };
            yield return new object[] { 531063, 15, 15, 15, 1446, 105 };
            yield return new object[] { 531064, 15, 15, 15, 1446, 105 };
            yield return new object[] { 531065, 15, 15, 15, 2102, 165 };
            yield return new object[] { 531066, 15, 15, 15, 2102, 165 };
            yield return new object[] { 531067, 15, 15, 15, 1446, 105 };
            yield return new object[] { 531068, 15, 15, 15, 1446, 105 };
            yield return new object[] { 531069, 15, 15, 15, 4375, 240 };
            yield return new object[] { 531070, 15, 15, 15, 1446, 105 };
            yield return new object[] { 531071, 15, 15, 15, 4375, 240 };
            yield return new object[] { 531072, 15, 15, 15, 2102, 165 };
            yield return new object[] { 531073, 15, 15, 15, 4089, 165 };
            yield return new object[] { 531074, 15, 15, 15, 4089, 165 };
            yield return new object[] { 531075, 15, 15, 15, 4089, 165 };
            yield return new object[] { 531076, 15, 15, 15, 5946, 255 };
            yield return new object[] { 531077, 15, 15, 15, 5946, 255 };
            yield return new object[] { 531078, 15, 15, 15, 12375, 360 };
            yield return new object[] { 531079, 15, 15, 15, 5946, 255 };
            yield return new object[] { 531080, 15, 15, 15, 5946, 255 };
            yield return new object[] { 531081, 15, 15, 15, 4089, 165 };
            yield return new object[] { 531082, 15, 15, 15, 2045, 125 };
            yield return new object[] { 531083, 15, 15, 15, 2045, 125 };
            yield return new object[] { 531084, 15, 15, 15, 2973, 195 };
            yield return new object[] { 531085, 15, 15, 15, 2973, 195 };
            yield return new object[] { 531086, 15, 15, 15, 6187, 280 };
            yield return new object[] { 531087, 15, 15, 15, 2973, 195 };
            yield return new object[] { 531088, 15, 15, 15, 2973, 195 };
            yield return new object[] { 531089, 15, 15, 15, 2652, 140 };
            yield return new object[] { 531090, 15, 15, 15, 2652, 140 };
            yield return new object[] { 531091, 15, 15, 15, 3856, 218 };
            yield return new object[] { 531092, 15, 15, 15, 3856, 218 };
            yield return new object[] { 531093, 15, 15, 15, 8024, 310 };
            yield return new object[] { 531094, 15, 15, 15, 3856, 218 };
            yield return new object[] { 531095, 15, 15, 15, 2652, 140 };
            yield return new object[] { 531096, 15, 15, 15, 8024, 310 };
            yield return new object[] { 531097, 15, 15, 15, 3856, 218 };
            yield return new object[] { 531098, 15, 15, 15, 9170, 292 };
            yield return new object[] { 531099, 15, 15, 15, 6307, 190 };
            yield return new object[] { 531100, 15, 15, 15, 6307, 190 };
            yield return new object[] { 531101, 15, 15, 15, 6307, 190 };
            yield return new object[] { 531102, 15, 15, 15, 6307, 190 };
            yield return new object[] { 531103, 15, 15, 15, 9170, 292 };
            yield return new object[] { 531104, 15, 15, 15, 9170, 292 };
            yield return new object[] { 531105, 15, 15, 15, 19084, 410 };
            yield return new object[] { 531106, 15, 15, 15, 9170, 292 };
        }
    }
}
