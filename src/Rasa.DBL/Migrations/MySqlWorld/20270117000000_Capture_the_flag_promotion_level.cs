using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Capture the Flag's promotion at Corporal DeSimone pays 1750 experience, not 500, so it
    /// takes the recruit to level 2 (CaptureTheFlagPromotionLevel). Data only, in plain SQL: no
    /// model of its own.
    ///
    /// Down puts back 500 where it is still 1750.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270117000000_Capture_the_flag_promotion_level")]
    public class Capture_the_flag_promotion_level : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in CaptureTheFlagPromotionLevel.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in CaptureTheFlagPromotionLevel.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
