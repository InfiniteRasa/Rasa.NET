using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// 22 Bane squads at arrival points no camp owns: Thrax soldiers 2-3 and a grenadier 0-1, the
    /// zone's rows (Divide's and the world spawns'; two new ones for Wilderness), respawn 90 s, round
    /// the arrival point. bane_arrival_squads.csv lists them.
    /// </summary>
    public class BaneSquadSpawnpoolPreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, SpawnPoolEntry.TableName, typeof(SpawnPoolEntry));
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { 560001, 0, 0, 900, 0.0000f, 378.3800f, 134.0000f, 0.0, 1911, 531101, 2, 3, 531102, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 10.0 };
            yield return new object[] { 560002, 0, 0, 900, -186.0000f, 427.6000f, 639.0000f, 0.0, 1911, 531101, 2, 3, 531102, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 10.0 };
            yield return new object[] { 560003, 0, 0, 900, 364.0000f, 429.6000f, 704.0000f, 0.0, 1764, 531074, 2, 3, 531075, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 10.0 };
            yield return new object[] { 560004, 0, 0, 900, 100.9000f, 107.7700f, -648.7000f, 0.0, 1148, 530001, 2, 3, 530002, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 10.0 };
            yield return new object[] { 560005, 0, 0, 900, -54.5000f, 107.2000f, -643.4000f, 0.0, 1148, 530001, 2, 3, 530002, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 10.0 };
            yield return new object[] { 560006, 0, 0, 900, 215.7200f, 12.0700f, -64.0300f, 0.0, 1347, 530001, 2, 3, 530002, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 25.0 };
            yield return new object[] { 560007, 0, 0, 900, 156.0100f, 4.0700f, 152.0700f, 0.0, 1347, 530001, 2, 3, 530002, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 10.0 };
            yield return new object[] { 560008, 0, 0, 900, 178.9700f, 3.8600f, 63.9000f, 0.0, 1347, 530001, 2, 3, 530002, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 25.0 };
            yield return new object[] { 560009, 0, 0, 900, 754.2500f, 134.5000f, -675.7500f, 0.0, 1244, 531063, 2, 3, 531064, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 25.0 };
            yield return new object[] { 560010, 0, 0, 900, 224.8000f, 144.5700f, 694.1700f, 0.0, 1394, 531063, 2, 3, 531064, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 25.0 };
            yield return new object[] { 560011, 0, 0, 900, -107.9700f, 184.8200f, 509.0400f, 0.0, 1394, 531063, 2, 3, 531064, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 25.0 };
            yield return new object[] { 560012, 0, 0, 900, 134.0000f, 118.8000f, 112.0000f, 0.0, 1397, 531063, 2, 3, 531064, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 10.0 };
            yield return new object[] { 560013, 0, 0, 900, 52.2000f, 39.1400f, -193.4000f, 0.0, 1430, 560001, 2, 3, 560002, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 10.0 };
            yield return new object[] { 560014, 0, 0, 900, 144.2200f, 10.5200f, 151.4000f, 0.0, 1430, 560001, 2, 3, 560002, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 25.0 };
            yield return new object[] { 560015, 0, 0, 900, 263.7700f, 8.7400f, 128.2900f, 0.0, 1430, 560001, 2, 3, 560002, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 10.0 };
            yield return new object[] { 560016, 0, 0, 900, 263.7700f, 8.7400f, 207.8400f, 0.0, 1430, 560001, 2, 3, 560002, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 10.0 };
            yield return new object[] { 560017, 0, 0, 900, -562.7400f, 210.0000f, 313.1300f, 0.0, 2051, 531029, 2, 3, 531030, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 10.0 };
            yield return new object[] { 560018, 0, 0, 900, -5.5000f, 209.5400f, -425.5000f, 0.0, 2162, 531029, 2, 3, 531030, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 10.0 };
            yield return new object[] { 560019, 0, 0, 900, 54.5000f, 62.7700f, 291.0000f, 0.0, 1743, 531045, 2, 3, 531046, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 10.0 };
            yield return new object[] { 560020, 0, 0, 900, -8.7500f, 62.7700f, 291.0000f, 0.0, 1743, 531045, 2, 3, 531046, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 10.0 };
            yield return new object[] { 560021, 0, 0, 900, 348.7900f, 285.2000f, -230.0500f, 0.0, 1497, 531082, 2, 3, 531083, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 10.0 };
            yield return new object[] { 560022, 0, 0, 900, 160.3100f, 176.7000f, -44.3800f, 0.0, 1502, 531082, 2, 3, 531083, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 10.0 };
        }
    }
}
