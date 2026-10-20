using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Six Targets of Opportunity missions given by their officers, and the bosses of two of them
    /// (TargetsOfOpportunityGivers, which says where each comes from): Agent Garrett and
    /// Surveyor Savinelli at Foreas Base, Sergeant Dekay at Irendas Penal Colony, Captain
    /// Caruthers at Raintree Post, Commander Tumlinson in the Incline tunnel and Lieutenant
    /// Colonel Brigman at Icarus Post; the seven Mires officers and the Plains' Stalker Overlord;
    /// the Four Horsemen moved off Raintree Post and the Warnet Queen taken off Foreas Base. Data
    /// only, in plain SQL: no model of its own.
    ///
    /// Down puts it all back.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270111000000_Targets_of_opportunity_givers")]
    public class Targets_of_opportunity_givers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in TargetsOfOpportunityGivers.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in TargetsOfOpportunityGivers.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
