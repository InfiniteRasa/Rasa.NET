using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// The Caves of Donn's Fithik given the lunge and acid spray of its kin
    /// (DonnFithikAbilities): creature_action 566103 and 566104 in its slots 3 and 4. Data only,
    /// in plain SQL: no model of its own.
    ///
    /// Down empties the two slots and deletes the rows.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270316000000_Donn_fithik_abilities")]
    public class Donn_fithik_abilities : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in DonnFithikAbilities.Up())
                migrationBuilder.Sql(sql);
            // Remove the original colliding history id after the guarded data migration reruns.
            migrationBuilder.Sql(DonnFithikAbilities.ForgetOldId);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in DonnFithikAbilities.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
