using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Map link 17, Wilderness to the AFS Arena (MapLinkPreloader), had its 8 m circle on the
    /// Alia Das barracks, from the client's map icon, and hospital 103 (750.07, 382.29) stood
    /// 1.4 m from its centre: a player revived there, or walking about the room, was taken to the
    /// arena (BR-157). The room has no door to the arena; what marks it is the Challenge Arena
    /// holosign hanging from the ceiling at (748.10, 383.98). The link is under the sign now, with
    /// the 1.5 m radius of the Edmund Range pads, which leaves the hospital 2.6 m outside it. The
    /// spot and the radius are ours. No table changes.
    ///
    /// Down puts the link back where it was.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Alia_Das_arena_link : Migration
    {
        private static readonly string[] Columns = { "pos_x", "pos_y", "pos_z", "radius" };

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData("map_link", "id", 17U, Columns, new object[] { 748.1018, 294.2103, 383.9836, 1.5 });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData("map_link", "id", 17U, Columns, new object[] { 751.5, 294.2103, 382.5, 8.0 });
        }
    }
}
