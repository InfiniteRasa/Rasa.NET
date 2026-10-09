using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.MySqlWorld
{
    using Services.Preloader;
    using Services.Preloader.Missions;
    using Structures.World;

    /// <summary>
    /// The Proving Grounds' third lot of people (BootcampBaseNpcs, BootcampBaseNpcScenesV8):
    /// Field Officer Oceana, two Infantrymen with pistols at the sandbag post west of the bridge,
    /// a Field Gunner walking the road by it, and four ambient figures (a woman drinking on a
    /// chair, a man drinking, a man at a console, a man with a data pad). The firing range's two
    /// soldiers move to have their targets between their lane's far sandbags, and the two
    /// practice targets in front of those targets go. Capture the Flag's three escorts become
    /// Infantrymen with pistols where the GM stood for them. Corporal DeSimone moves, Major
    /// McAllister's departure ends on the command deck, and the bootcamp quest NPCs' officer
    /// clothes go from blue to #68562c. No table changes.
    ///
    /// Down puts each back.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Place_bootcamp_base_npcs : Migration
    {
        /// <summary>The escorts: id, comment, class, name, first and second attack, weapon.</summary>
        private static readonly (uint Id, string Comment, uint Class, uint Name, uint Action1, uint Action2, uint Weapon)[] Escorts =
        {
            (510213, "Bootcamp AFS escort, pistol", 29423, 8716, EscortPistol, 0, 6271),
            (510214, "Bootcamp AFS escort, pistol", 29423, 8716, EscortPistol, 0, 6271),
            (510215, "Bootcamp AFS escort, pistol", 29423, 8716, EscortPistol, 0, 6271)
        };

        private static readonly (uint Id, string Comment, uint Class, uint Name, uint Action1, uint Action2, uint Weapon)[] EscortsWere =
        {
            (510213, "Bootcamp Forean Guardsman Initiate", 7034, 7874, 5, 17, 6042),
            (510214, "Bootcamp Forean Shaman Initiate", 7035, 7890, 510214, 0, 6164),
            (510215, "Bootcamp Forean Archer Initiate", 7036, 7986, 28, 0, 6453)
        };

        /// <summary>The escorts' pistol: the bridge Thrax initiates' shot (510216), the AFS's own.</summary>
        private const uint EscortPistol = 510218;

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            new BootcampBaseNpcCreaturePreloader().Preload(migrationBuilder);
            new BootcampBaseNpcAppearancePreloader().Preload(migrationBuilder);
            new BootcampBaseNpcStatsPreloader().Preload(migrationBuilder);
            new BootcampBaseNpcSpawnpoolPreloader().Preload(migrationBuilder);
            new BootcampBaseNpcPosePreloader().Preload(migrationBuilder);
            new BootcampBaseNpcPatrolPreloader().Preload(migrationBuilder);
            new BootcampBaseNpcAmbientPreloader().Preload(migrationBuilder);

            migrationBuilder.Sql(
                "insert into creature_action (id, description, action_id, action_arg_id, range_min, range_max, cooldown, windup, min_damage, max_damage, damage_type) " +
                $"values ({EscortPistol}, 'Bootcamp AFS escort pistol', 1, 1, 0.5, 24, 1300, 0, 8, 12, 1);");
            Escort(migrationBuilder, Escorts);

            Shooter(migrationBuilder, 1, BootcampBaseNpcs.MiddleLaneX);
            Shooter(migrationBuilder, 2, BootcampBaseNpcs.WestLaneX);

            migrationBuilder.Sql($"update {SpawnPoolEntry.TableName} set {BootcampBaseNpcs.DeSimoneIs} where id = {BootcampBaseNpcs.DeSimonePoolId};");
            Tint(migrationBuilder, BootcampBaseNpcs.OfficerBlue, BootcampBaseNpcs.OfficerOlive);

            BootcampBaseNpcScenesV8.Up(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            BootcampBaseNpcScenesV8.Down(migrationBuilder);

            Tint(migrationBuilder, BootcampBaseNpcs.OfficerOlive, BootcampBaseNpcs.OfficerBlue);
            migrationBuilder.Sql($"update {SpawnPoolEntry.TableName} set {BootcampBaseNpcs.DeSimoneWas} where id = {BootcampBaseNpcs.DeSimonePoolId};");

            migrationBuilder.Sql($"update {AmbientNpcEntry.TableName} set {BootcampBaseNpcs.MiddleShooterWas} where id = 1;");
            migrationBuilder.Sql($"update {AmbientNpcEntry.TableName} set {BootcampBaseNpcs.WestShooterWas} where id = 2;");

            Escort(migrationBuilder, EscortsWere);
            migrationBuilder.Sql($"delete from creature_action where id = {EscortPistol};");

            migrationBuilder.Sql($"delete from {AmbientNpcEntry.TableName} where id between {BootcampBaseNpcs.FirstAmbientId} and {BootcampBaseNpcs.LastAmbientId};");
            migrationBuilder.Sql($"delete from {SpawnPoolPatrolEntry.TableName} where pool_id = {BootcampBaseNpcs.FieldGunnerPoolId};");

            foreach (var table in new[] { SpawnPoolPoseEntry.TableName, SpawnPoolEntry.TableName })
                migrationBuilder.Sql($"delete from {table} where id between {BootcampBaseNpcs.FirstId} and {BootcampBaseNpcs.LastPoolId};");

            foreach (var table in new[] { CreatureStatEntry.TableName, CreatureAppearanceEntry.TableName, CreatureEntry.TableName })
                migrationBuilder.Sql($"delete from {table} where id between {BootcampBaseNpcs.FirstId} and {BootcampBaseNpcs.LastCreatureId};");
        }

        private static void Escort(MigrationBuilder migrationBuilder,
            (uint Id, string Comment, uint Class, uint Name, uint Action1, uint Action2, uint Weapon)[] escorts)
        {
            foreach (var escort in escorts)
            {
                migrationBuilder.Sql(
                    $"update {CreatureEntry.TableName} set comment = '{escort.Comment}', class_id = {escort.Class}, name_id = {escort.Name}, " +
                    $"action1 = {escort.Action1}, action2 = {escort.Action2} where id = {escort.Id};");
                migrationBuilder.Sql(
                    $"update {CreatureAppearanceEntry.TableName} set class_id = {escort.Weapon} where id = {escort.Id} and slot_id = 13;");
            }
        }

        private static void Shooter(MigrationBuilder migrationBuilder, uint id, double laneX) =>
            migrationBuilder.Sql(System.FormattableString.Invariant(
                $"update {AmbientNpcEntry.TableName} set pos_x = {laneX}, pos_z = {BootcampBaseNpcs.ShooterZ}, rotation = {BootcampBaseNpcs.ShooterRotation} where id = {id};"));

        private static void Tint(MigrationBuilder migrationBuilder, uint from, uint to) =>
            migrationBuilder.Sql(
                $"update {CreatureAppearanceEntry.TableName} set color = {to} where id between {BootcampBaseNpcs.FirstQuestNpcId} and {BootcampBaseNpcs.LastQuestNpcId} " +
                $"and slot_id in (2, 15, 16) and color = {from};");
    }
}
