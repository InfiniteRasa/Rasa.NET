using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Marshes;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Valverde Marshes set 1, Paludos: Ranger Alia (an NPC squad of one on the road), Shaman Siva, Warrior Veska and the
    /// six mission pools east of Paludos (MarshesPaludosNpcsV1), with Elder Toida's Roll out the Welcome Wagon (541,
    /// MarshesWelcomeWagonV1) and Divided We Fall (917, MarshesDividedWeFallV1), Sergeant Starling's Cannister Collection
    /// (498, MarshesCannisterCollectionV1) and Shaman Geli's What's that Gas? (539, MarshesWhatsThatGasV1). Data only.
    /// </summary>
    public partial class Marshes_paludos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in MarshesPaludosNpcsV1.Up())
                migrationBuilder.Sql(MarshesSql.Guard(sql));
            MarshesWelcomeWagonV1.Up(migrationBuilder);
            MarshesCannisterCollectionV1.Up(migrationBuilder);
            MarshesWhatsThatGasV1.Up(migrationBuilder);
            MarshesDividedWeFallV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            MarshesDividedWeFallV1.Down(migrationBuilder);
            MarshesWhatsThatGasV1.Down(migrationBuilder);
            MarshesCannisterCollectionV1.Down(migrationBuilder);
            MarshesWelcomeWagonV1.Down(migrationBuilder);
            foreach (var sql in MarshesPaludosNpcsV1.Down())
                migrationBuilder.Sql(MarshesSql.Guard(sql));
        }
    }
}
