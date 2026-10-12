using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.SqliteWorld
{
    using Context.World;

    /// <summary>
    /// Spawn pool 540127, a Flaregasher pack on Thunderhead (RegionSpawnpoolPreloader), was
    /// entered with a height of 0. The region pools' heights are their region marker's, and the
    /// spawn point looks for the ground 200 m up and down from them; this one's ground is 437 m
    /// up, at (-810.8, 437.0, -530.9) on the map's navmesh, so nothing was found and the pack
    /// was put down in the void. The height is the ground's now. No table changes.
    ///
    /// Down puts the 0 back.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Thunderhead_pool_height : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData("spawnpool", "id", 540127U, "pos_y", 437.0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData("spawnpool", "id", 540127U, "pos_y", 0.0);
        }
    }
}
