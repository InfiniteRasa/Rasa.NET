using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Marshes;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Valverde Marshes set 14, P'reo Das: Forean Elder Qunle, the hidden encampment's Forean Elder and Operation Master Myrquar
    /// in the Village Ruins (MarshesPreoDasNpcsV1), with The Lost Ones (1076, MarshesTheLostOnesV1), Needle in a Bane Camp (893,
    /// MarshesNeedleInABaneCampV1), The Choices We Make (1078, MarshesTheChoicesWeMakeV1) and Dirty Secrets (1077,
    /// MarshesDirtySecretsV1). Data only.
    /// </summary>
    public partial class Marshes_preo_das : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in MarshesPreoDasNpcsV1.Up())
                migrationBuilder.Sql(sql);
            MarshesTheLostOnesV1.Up(migrationBuilder);
            MarshesNeedleInABaneCampV1.Up(migrationBuilder);
            MarshesTheChoicesWeMakeV1.Up(migrationBuilder);
            MarshesDirtySecretsV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            MarshesDirtySecretsV1.Down(migrationBuilder);
            MarshesTheChoicesWeMakeV1.Down(migrationBuilder);
            MarshesNeedleInABaneCampV1.Down(migrationBuilder);
            MarshesTheLostOnesV1.Down(migrationBuilder);
            foreach (var sql in MarshesPreoDasNpcsV1.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
