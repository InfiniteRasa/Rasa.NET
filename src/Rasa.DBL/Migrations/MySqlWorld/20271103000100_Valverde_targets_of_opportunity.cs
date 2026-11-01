using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.MySqlWorld
{
    using Context.World;
    using Services.Preloader;

    /// <summary>
    /// The rest of the Plateau (1586) and Valverde Pools (1594) Targets of Opportunity: the objectives
    /// the client gives them past their kills, the "all" objective as their sum, a turn-in reward, Doss
    /// as Plateau's giver and receiver, the three special Bane Pools had no row for, and a pool of
    /// Striders for Pools' "Kill 10 Striders" (TargetsOfOpportunitySeed.Valverde, which says what and
    /// why). No table changes.
    ///
    /// Down deletes the rows and puts the two bindings and Plateau's radio policy back. The character
    /// database has the other half (its own Valverde_targets_of_opportunity).
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20271103000100_Valverde_targets_of_opportunity")]
    public partial class Valverde_targets_of_opportunity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var statement in TargetsOfOpportunitySeed.Valverde.WorldUp)
                migrationBuilder.Sql(statement);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var statement in TargetsOfOpportunitySeed.Valverde.WorldDown)
                migrationBuilder.Sql(statement);
        }
    }
}
