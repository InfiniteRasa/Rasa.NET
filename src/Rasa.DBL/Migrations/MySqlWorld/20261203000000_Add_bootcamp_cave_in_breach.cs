using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.MySqlWorld
{
    using Services.Preloader;
    using Services.Preloader.Missions;

    /// <summary>
    /// The cave-in across the Proving Grounds' bridge and the Thrax who blow it during Capture
    /// the Flag (BootcampCaveInBreachV9): no Thrax at the bridge until then (the three bridge
    /// Thrax pools go), a pile of rocks in the doorwell from the start of bootcamp, and, when the
    /// player comes off the west end of the bridge with "Find a way out of the cave" open, the
    /// rocks exploding and six Thrax coming out of the doorwell at the checkpoint. The checkpoint's
    /// two Infantrymen with pistols and its Field Gunner (BootcampBaseNpcs) can now shoot back.
    /// No table changes.
    ///
    /// Down puts each back.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Add_bootcamp_cave_in_breach : Migration
    {
        /// <summary>The escorts' pistol (Place_bootcamp_base_npcs), and the AFS bridge soldiers' rifle shot.</summary>
        private const uint Pistol = 510218;
        private const uint Rifle = 2;

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            BootcampCaveInBreachV9.Up(migrationBuilder);
            migrationBuilder.Sql($"update creature set action1 = {Pistol} where id = {BootcampBaseNpcs.PistolInfantrymanId};");
            migrationBuilder.Sql($"update creature set action1 = {Rifle} where id = {BootcampBaseNpcs.FieldGunnerId};");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"update creature set action1 = 0 where id in ({BootcampBaseNpcs.PistolInfantrymanId}, {BootcampBaseNpcs.FieldGunnerId});");
            BootcampCaveInBreachV9.Down(migrationBuilder);
        }
    }
}
