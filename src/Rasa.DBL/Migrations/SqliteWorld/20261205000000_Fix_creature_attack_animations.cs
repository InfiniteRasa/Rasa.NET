using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.SqliteWorld
{
    using Services.Preloader;

    /// <summary>
    /// Creatures whose attack the client has no animation for on their model, so they stood still
    /// while the hit landed, attack with one of their own that it does animate; and the Proving
    /// Grounds' AFS rifle soldiers fire their rifles rather than a pistol shot. The rows, and why
    /// each, are in CreatureAttackAnimations:
    ///
    ///  - new rows 72001-72008, for creatures that shared row 2 (the AFS light soldier's pistol)
    ///    or row 8 (the AFS mini turret's gun) with creatures who animate it: the Winged Fithik
    ///    (174/34), the Hominis Machina (1/97), the AFS rifle soldiers 510217 and 510227
    ///    (1/134), the Wilderness Bane mortars 630076-630079 (411/1) and the four Bot
    ///    Construction minions (1/261, 141/11, 1/255, 1/152); and Warrior Aprika (72009, 1/116),
    ///    whose goo gunner's model animates neither the AFS pistol nor the spear she held, and who
    ///    now holds the goo gun (creature_appearance slot 13, 10532 to 10530);
    ///  - the Forean spearmen to row 5, the spear's melee (174/11), from row 17 (1/305, the
    ///    spear's ranged attack); Ranger Milpas to row 6, CR_FOREAN_LIGHTNING;
    ///  - the thirteen Thrax Soldier rows 174/48 to 174/46, Horntail's row 33 1/1 to 1/190, the
    ///    Barb Ticks' rows 174/17 to 1/153.
    ///
    /// Range, cooldown and damage are the replaced rows'. Down puts each back.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Fix_creature_attack_animations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            CreatureAttackAnimations.Up(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            CreatureAttackAnimations.Down(migrationBuilder);
        }
    }
}
