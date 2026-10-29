using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Fort Defiance's murder case NPCs: Parkman, the suspect, Pendrik and his hostile copy, the Craps Players; Norton and Crais moved (PlateauCidNpcsV1). Data only.
    /// </summary>
    public partial class Plateau_cid_npcs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PlateauCidNpcsV1.Up())
                migrationBuilder.Sql(sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PlateauCidNpcsV1.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
