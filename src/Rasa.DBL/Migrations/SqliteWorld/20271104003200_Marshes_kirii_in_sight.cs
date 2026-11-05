using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Marshes;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Valverde Marshes: Forean Elder Kirii (891, 894) 3.2 m north-east, out of the Village Ruins rock pile's shadow, so the
    /// client sees him from the west, the village side (MarshesKiriiInSightV1); his 891 marker with him. Down puts both back.
    /// Data only.
    /// </summary>
    public partial class Marshes_kirii_in_sight : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in MarshesKiriiInSightV1.Up())
                migrationBuilder.Sql(MarshesSql.Guard(sql));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in MarshesKiriiInSightV1.Down())
                migrationBuilder.Sql(MarshesSql.Guard(sql));
        }
    }
}
