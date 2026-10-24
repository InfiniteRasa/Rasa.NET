using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// The defences of the open world's control points (ControlPointDefences): a shield in each
    /// gate of their outposts, linked to its point, and AFS soldiers with their zones' weapons at
    /// the gates, sandbags and counters, in each point's AFS garrison; Imperial Valley's soldiers;
    /// the Wilderness posts given the same roles; no Rushing Blow for the AFS soldiers. Data only,
    /// in plain SQL: no model of its own.
    ///
    /// Down undoes what is still as Up left it.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270301000001_Control_point_defences")]
    public class Control_point_defences : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in ControlPointDefences.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in ControlPointDefences.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
