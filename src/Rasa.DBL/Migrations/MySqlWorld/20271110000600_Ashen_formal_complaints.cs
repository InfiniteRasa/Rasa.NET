using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Ashen;
using Rasa.Services.Preloader.Missions.Crucible;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Ligo Ashen Desert set 6, Formal Complaints and its AFS side: Scavenger Vakiel Latan, Shinichiro Ito and Megumi Yamamoto
    /// new (AshenFormalComplaintsNpcsV1), with Latan's Formal Complaints (1254, AshenFormalComplaintsV1: the choice that sets
    /// player flag 250 or 251), Ito's The Human Perspective (1258, AshenHumanPerspectiveV1), Latan's A Bribe for Your Thoughts
    /// (1259, AshenBribeForYourThoughtsV1), Captain Akio Yamamoto's Court Martial Arts (1261, AshenCourtMartialArtsV1) and
    /// Megumi's Dearly Departed (1285, AshenDearlyDepartedV1, which also makes the Boneyard's Thrax pools 531038, 531039 and
    /// 531041 point pools on open ground; Down puts upstream's rows back). Data only, plain SQL (CrucibleSql).
    /// </summary>
    public partial class Ashen_formal_complaints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in AshenFormalComplaintsNpcsV1.Up())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
            AshenFormalComplaintsV1.Up(migrationBuilder);
            AshenHumanPerspectiveV1.Up(migrationBuilder);
            AshenBribeForYourThoughtsV1.Up(migrationBuilder);
            AshenCourtMartialArtsV1.Up(migrationBuilder);
            AshenDearlyDepartedV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            AshenDearlyDepartedV1.Down(migrationBuilder);
            AshenCourtMartialArtsV1.Down(migrationBuilder);
            AshenBribeForYourThoughtsV1.Down(migrationBuilder);
            AshenHumanPerspectiveV1.Down(migrationBuilder);
            AshenFormalComplaintsV1.Down(migrationBuilder);
            foreach (var sql in AshenFormalComplaintsNpcsV1.Down())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
        }
    }
}
