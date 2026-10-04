using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.MySqlWorld
{
    using Context.World;
    using Services.Preloader.Missions.Wilderness;

    /// <summary>
    /// The Wilderness missions' map and radar indicators showed other missions' indicator names
    /// ("Possible Food Crate Location" on Mortar By Numbers). Their rows are re-keyed as
    /// indicators without a client name, which the client names by their objective. Data only;
    /// see <see cref="WildernessUnnamedIndicatorsV1"/>. Down puts the keys back.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20261111000000_Wilderness_indicators_without_client_names")]
    public partial class Wilderness_indicators_without_client_names : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            WildernessUnnamedIndicatorsV1.Up(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder) =>
            WildernessUnnamedIndicatorsV1.Down(migrationBuilder);
    }
}
