using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Marshes;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Valverde Marshes: Forean Elder Kirii (891, 894) 5 m north into the open, clear of the Village Ruins rock pile by the
    /// client's own five-line sight test, so the client sees him from the way in from the village (MarshesKiriiInOpenV1); his
    /// 891 marker with him. Down puts both back at MarshesKiriiInSightV1's place. Data only.
    /// </summary>
    public partial class Marshes_kirii_in_open : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in MarshesKiriiInOpenV1.Up())
                migrationBuilder.Sql(MarshesSql.Guard(sql));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in MarshesKiriiInOpenV1.Down())
                migrationBuilder.Sql(MarshesSql.Guard(sql));
        }
    }
}
