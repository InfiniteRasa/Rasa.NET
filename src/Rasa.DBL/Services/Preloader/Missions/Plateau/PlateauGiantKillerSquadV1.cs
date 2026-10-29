using System.Collections.Generic;
using System.Globalization;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    using Structures.World;

    /// <summary>
    /// Giant Killer (939): Goliath and his two to four Kael walk a beat east of Camp Resistance as
    /// one NPC squad (in Plateau_giant_killer; the game server's NpcSquads), not as his pool.
    ///
    /// Sourced: TaRapedia (Giant_Killer) "Goliath patrols east of Camp Resistance with two to four
    /// other Kael", "The pack tends to patrol between the ruins and about halfway to Camp
    /// Resistence. They are best attacked at the furthest point west of their patrol area.";
    /// Ellatha (boss NPC Goliath) "Patrols east of Camp Resistance along the north ridge, near
    /// -250, -350".
    ///
    /// The squad: Goliath (creature 520051, "Goliath - Plateau") leads, one of him; two to four
    /// "Kael - Plateau" (531088) behind him in a wedge 5 m apart (5 m: the user's spacing after a
    /// playtest, 2026-10-22, at 3 m the big models walked too close together). Each looks as far
    /// as its own aggro range (engage_range 0); a Kael fights at most 30 m from Goliath (tether).
    /// It walks at its slowest member's walk speed: 2.4 m/s. Goliath's and the Kael's walk_speed is
    /// the seed's 5 (upstream's rows, kept), a placeholder the game server puts on their classes'
    /// rate (EntityMovementRates; the client's entitymovementrate, Bane_Kael_Standard 4046 and
    /// Bane_Kael_Standard_Boss 25413 both walk 2.4 m/s).
    ///
    /// Reconstructed (ESTIMATE): the beat goes from the west end (-440, -230), Goliath's pool
    /// point, to (-254, -346) near the ruins, the east end, 261 m, and back the same way (mode 1,
    /// Back: at the last point the squad turns and walks the points back). Measured on the
    /// Plateau navmesh and the client's geometry (terrain and entity collision meshes, as
    /// StandingHeights measures a floor): every leg is a clear straight line on the navmesh with
    /// at least 5.5 m to a wall on each side; the east end is 4 m short of (-250, -350), where
    /// there is 3.8 m. The points are the floor's height, with a point wherever the floor leaves
    /// the straight line by more than 0.3 m. The squad stands 10 s at each end (user 2026-10-22).
    /// It is sent at the west end, all at once, and sent again 50 s after the last of it falls
    /// (ESTIMATE). Goliath's pool (520051) goes scripted, so it no longer spawns him on its own.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike; each row goes in only if its id is free, the
    /// pool changes only while it is still upstream's, and Down takes out only the rows that are
    /// still these.
    /// </summary>
    public static class PlateauGiantKillerSquadV1
    {
        public const uint Map = PlateauGiantKillerV1.MapContextId;

        public const uint Goliath = PlateauGiantKillerV1.Goliath, Kael = PlateauGiantKillerV1.KaelGuard;
        public const uint GoliathPool = PlateauGiantKillerV1.GoliathPool;

        /// <summary>The squad type, its route and its spawn share the one id.</summary>
        public const uint Squad = 750111;

        public const string Name = "Giant Killer Goliath's pack", Route = "Goliath's beat, Camp Resistance to the ruins";

        /// <summary>A wedge, 5 m apart; its own creatures' aggro range; the Kael no further than 30 m from Goliath.</summary>
        public const byte Formation = 0;
        public const double Spacing = 5, EngageRange = 0, Tether = 30;

        /// <summary>There and back: at either end the squad turns and walks the points back.</summary>
        public const byte Mode = 1;

        public const uint EndPauseMs = 10000;

        /// <summary>ESTIMATE.</summary>
        public const uint RespawnSeconds = 50;

        /// <summary>All of it at once, in its places (NpcSquads.Arrival.None).</summary>
        public const byte Arrival = 0;

        /// <summary>spawnpool.mode: automatic (upstream's) and scripted (SpawnPoolManager.ModeScripted).</summary>
        public const byte PoolAutomatic = 0, PoolScripted = 2;

        public const uint EvidenceId = 4;
        public const string EvidenceUri = "repository:PlateauGiantKillerSquadV1";

        public sealed record Member(byte Slot, uint Creature, byte Min, byte Max);

        /// <summary>Goliath leads, one of him; two to four Kael.</summary>
        public static readonly Member[] Members =
        {
            new(0, Goliath, 1, 1),
            new(1, Kael, (byte)PlateauGiantKillerV1.GuardsMin, (byte)PlateauGiantKillerV1.GuardsMax)
        };

        /// <summary>The way out, west end to east end: x, floor height, z.</summary>
        public static readonly (double X, double Y, double Z)[] Points =
        {
            (-440.0, 442.37, -230.0),   // west end, Goliath's pool point
            (-436.7, 442.50, -232.2),
            (-434.2, 443.33, -233.9),
            (-429.1, 443.12, -237.2),
            (-425.0, 442.37, -239.9),
            (-397.4, 443.10, -258.1),
            (-372.4, 441.87, -274.6),
            (-364.0, 442.86, -280.1),
            (-358.2, 442.87, -284.0),
            (-354.0, 443.79, -286.7),
            (-350.7, 443.22, -288.9),
            (-349.0, 443.62, -290.0),
            (-346.0, 443.37, -292.0),
            (-340.8, 442.44, -287.3),
            (-339.3, 442.78, -285.9),
            (-324.0, 442.37, -272.0),
            (-314.0, 442.62, -266.0),
            (-310.0, 443.87, -266.0),
            (-302.0, 443.62, -266.0),
            (-291.4, 442.30, -276.6),
            (-287.2, 442.30, -280.8),
            (-285.0, 443.04, -283.0),
            (-282.0, 442.80, -286.0),
            (-279.9, 442.82, -289.4),
            (-277.4, 443.77, -293.7),
            (-276.0, 443.62, -296.0),
            (-272.8, 442.62, -303.3),
            (-267.9, 442.63, -314.3),
            (-265.9, 443.12, -318.9),
            (-262.7, 442.44, -326.2),
            (-254.0, 442.87, -346.0),   // east end, near the ruins
        };

        /// <summary>The west end, facing the first leg (east-south-east; NpcSquads' yaw, AbilityManager.YawTowards).</summary>
        public const double SpawnX = -440.0, SpawnY = 442.37, SpawnZ = -230.0, SpawnRotation = -0.9828;

        /// <summary>10 s at each end, none between.</summary>
        public static uint PauseAt(int step) => step == 0 || step == Points.Length - 1 ? EndPauseMs : 0;

        /// <summary>The statements, one each, as MigrationBuilder.Sql takes them.</summary>
        public static IEnumerable<string> Up()
        {
            yield return $"INSERT INTO {NpcSquadTypeEntry.TableName} (id, name, formation, spacing, engage_range, tether) " +
                $"SELECT {Squad}, {S(Name)}, {Formation}, {N(Spacing)}, {N(EngageRange)}, {N(Tether)} FROM creature c WHERE c.id = {Goliath} " +
                $"AND NOT EXISTS (SELECT 1 FROM {NpcSquadTypeEntry.TableName} x WHERE x.id = {Squad});";

            foreach (var m in Members)
                yield return $"INSERT INTO {NpcSquadMemberEntry.TableName} (type_id, slot, creature_id, min_count, max_count) " +
                    $"SELECT {Squad}, {m.Slot}, {m.Creature}, {m.Min}, {m.Max} FROM {NpcSquadTypeEntry.TableName} t WHERE t.id = {Squad} AND t.name = {S(Name)} " +
                    $"AND EXISTS (SELECT 1 FROM creature c WHERE c.id = {m.Creature}) " +
                    $"AND NOT EXISTS (SELECT 1 FROM {NpcSquadMemberEntry.TableName} x WHERE x.type_id = {Squad} AND x.slot = {m.Slot});";

            yield return $"INSERT INTO {NpcSquadPathEntry.TableName} (id, map_context_id, name, mode) " +
                $"SELECT {Squad}, {Map}, {S(Route)}, {Mode} FROM {NpcSquadTypeEntry.TableName} t WHERE t.id = {Squad} AND t.name = {S(Name)} " +
                $"AND NOT EXISTS (SELECT 1 FROM {NpcSquadPathEntry.TableName} x WHERE x.id = {Squad});";

            for (var i = 0; i < Points.Length; i++)
            {
                var p = Points[i];
                yield return $"INSERT INTO {NpcSquadPathPointEntry.TableName} (path_id, step, pos_x, pos_y, pos_z, pause_ms, run, engage_range) " +
                    $"SELECT {Squad}, {i}, {N(p.X)}, {N(p.Y)}, {N(p.Z)}, {PauseAt(i)}, 0, 0 FROM {NpcSquadPathEntry.TableName} r " +
                    $"WHERE r.id = {Squad} AND r.name = {S(Route)} " +
                    $"AND NOT EXISTS (SELECT 1 FROM {NpcSquadPathPointEntry.TableName} x WHERE x.path_id = {Squad} AND x.step = {i});";
            }

            yield return $"INSERT INTO {NpcSquadSpawnEntry.TableName} (id, map_context_id, type_id, path_id, pos_x, pos_y, pos_z, rotation, respawn_seconds, enabled, comment, arrival) " +
                $"SELECT {Squad}, {Map}, {Squad}, {Squad}, {N(SpawnX)}, {N(SpawnY)}, {N(SpawnZ)}, {N(SpawnRotation)}, {RespawnSeconds}, 1, {S(Name)}, {Arrival} " +
                $"FROM {NpcSquadPathEntry.TableName} r WHERE r.id = {Squad} AND r.name = {S(Route)} " +
                $"AND NOT EXISTS (SELECT 1 FROM {NpcSquadSpawnEntry.TableName} x WHERE x.id = {Squad});";

            // Goliath's pool no longer spawns him on its own: the squad does. Only while the pool
            // is still upstream's (his, automatic) and the squad's spawn is ours.
            yield return $"UPDATE spawnpool SET mode = {PoolScripted} WHERE id = {GoliathPool} AND mode = {PoolAutomatic} " +
                $"AND creature_1_Id = {Goliath} " +
                $"AND EXISTS (SELECT 1 FROM {NpcSquadSpawnEntry.TableName} x WHERE x.id = {Squad} AND x.comment = {S(Name)});";

            yield return "INSERT INTO mission_evidence (mission_id, content_revision, evidence_id, owner_kind, owner_id, " +
                "source_kind, source_uri, local_client_path, confidence, reconstruction_note) " +
                $"SELECT {PlateauGiantKillerV1.MissionId}, {S(PlateauMissionDataV1.Revision)}, {EvidenceId}, " +
                $"{(byte)MissionEvidenceOwnerKind.Mission}, {PlateauGiantKillerV1.MissionId}, " +
                $"{(byte)MissionEvidenceSourceKind.Reconstruction}, {S(EvidenceUri)}, NULL, 0.3, {S(EvidenceNote)} " +
                $"FROM {NpcSquadTypeEntry.TableName} t WHERE t.id = {Squad} AND t.name = {S(Name)} " +
                "AND NOT EXISTS (SELECT 1 FROM mission_evidence x WHERE " +
                $"x.mission_id = {PlateauGiantKillerV1.MissionId} AND x.content_revision = {S(PlateauMissionDataV1.Revision)} AND x.evidence_id = {EvidenceId});";
        }

        public static IEnumerable<string> Down()
        {
            yield return $"DELETE FROM mission_evidence WHERE mission_id = {PlateauGiantKillerV1.MissionId} " +
                $"AND content_revision = {S(PlateauMissionDataV1.Revision)} AND evidence_id = {EvidenceId} AND source_uri = {S(EvidenceUri)};";
            yield return $"UPDATE spawnpool SET mode = {PoolAutomatic} WHERE id = {GoliathPool} AND mode = {PoolScripted} " +
                $"AND creature_1_Id = {Goliath} " +
                $"AND EXISTS (SELECT 1 FROM {NpcSquadSpawnEntry.TableName} x WHERE x.id = {Squad} AND x.comment = {S(Name)});";
            yield return $"DELETE FROM {NpcSquadSpawnEntry.TableName} WHERE id = {Squad} AND type_id = {Squad} AND comment = {S(Name)};";
            yield return $"DELETE FROM {NpcSquadPathPointEntry.TableName} WHERE path_id = {Squad} " +
                $"AND EXISTS (SELECT 1 FROM {NpcSquadPathEntry.TableName} r WHERE r.id = {Squad} AND r.name = {S(Route)});";
            yield return $"DELETE FROM {NpcSquadPathEntry.TableName} WHERE id = {Squad} AND name = {S(Route)};";
            yield return $"DELETE FROM {NpcSquadMemberEntry.TableName} WHERE type_id = {Squad} " +
                $"AND EXISTS (SELECT 1 FROM {NpcSquadTypeEntry.TableName} t WHERE t.id = {Squad} AND t.name = {S(Name)});";
            yield return $"DELETE FROM {NpcSquadTypeEntry.TableName} WHERE id = {Squad} AND name = {S(Name)};";
        }

        /// <summary>mission_evidence 4: what the squad is reconstructed from.</summary>
        public const string EvidenceNote =
            "Goliath520051 leads two to four Kael531088 as an NPC squad (npc_squad 750111): wedge, 5m apart, tether 30m, " +
            "walks at its slowest walk speed, 2.4m/s (Kael classes 4046/25413, client entitymovementrate). Beat there and back (path mode Back) from (-440,-230) to (-254,-346), " +
            "261m each way, 31 points measured on the navmesh and client floor, >=5.5m clear each side; 10s at each end. " +
            "Route from Ellatha 'Patrols east of Camp Resistance along the north ridge, near -250, -350' and TaRapedia " +
            "'The pack tends to patrol between the ruins and about halfway to Camp Resistence'. Sent again 50s after the " +
            "last falls (ESTIMATE). Pool520051 scripted.";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string S(string value) => "'" + value.Replace("'", "''") + "'";
    }
}
