using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Pools;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// John McKormic, Jayson Venma, Austin Buckley and Retread Rickert at the Retread Camp (PoolsRetreadCampNpcsV1), with
    /// the Retread chain: A Cult Divided (560, PoolsCultDividedV1), Testing the Pools (561, PoolsTestingThePoolsV1), One
    /// Man's Trash (562, PoolsOneMansTrashV1), Retread Planning (563, PoolsRetreadPlanningV1) and Retread Planning Part II
    /// (1330, PoolsRetreadPlanningPartTwoV1). Data only.
    /// </summary>
    public partial class Pools_retread_chain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PoolsRetreadCampNpcsV1.Up())
                migrationBuilder.Sql(sql);
            PoolsCultDividedV1.Up(migrationBuilder);
            PoolsTestingThePoolsV1.Up(migrationBuilder);
            PoolsOneMansTrashV1.Up(migrationBuilder);
            PoolsRetreadPlanningV1.Up(migrationBuilder);
            PoolsRetreadPlanningPartTwoV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PoolsRetreadPlanningPartTwoV1.Down(migrationBuilder);
            PoolsRetreadPlanningV1.Down(migrationBuilder);
            PoolsOneMansTrashV1.Down(migrationBuilder);
            PoolsTestingThePoolsV1.Down(migrationBuilder);
            PoolsCultDividedV1.Down(migrationBuilder);
            foreach (var sql in PoolsRetreadCampNpcsV1.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
