using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader.Missions.Pools;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// The scenes of the Pools missions played in the Turpis Refinery, the Lamna Armory and the Retread Caves are
    /// bound to their instance's map (PoolsInstanceScenesBoundV1), as the Plateau's instance missions are: a run goes
    /// with its owner to a later copy of the instance (after a restart, a new squad), and a mission taken outside waits
    /// for its owner to come in. The scene bindings are written again; Down writes them without the bound map. Data only.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20271101000300_Pools_instance_scenes_bound")]
    public class Pools_instance_scenes_bound : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) => PoolsInstanceScenesBoundV1.Up(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder) => PoolsInstanceScenesBoundV1.Down(migrationBuilder);
    }
}
