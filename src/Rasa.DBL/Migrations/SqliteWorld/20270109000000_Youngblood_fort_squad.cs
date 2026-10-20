using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader.Missions;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Capture the Flag's fort, once its Bane boss is dead (BootcampFortSquadV10): Captain
    /// Youngblood comes in through the east gate with four Riflemen, a Shotgunner and a Field
    /// Gunner, who take posts at the gate and about him, and the fort's seven Bane pools spawn
    /// nothing more on the player's map. Three creature rows, their shotgun's attack, mission
    /// 1994's spawn group 5 and the scene and experience bindings. No table changes.
    ///
    /// Down puts each back.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270109000000_Youngblood_fort_squad")]
    public class Youngblood_fort_squad : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) => BootcampFortSquadV10.Up(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder) => BootcampFortSquadV10.Down(migrationBuilder);
    }
}
