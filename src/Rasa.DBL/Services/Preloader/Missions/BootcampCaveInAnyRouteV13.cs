using System.Collections.Generic;

namespace Rasa.Services.Preloader.Missions
{
    /// <summary>
    /// The Proving Grounds' cave-in (BootcampCaveInBreachV9) blows whichever way the player comes
    /// to it.
    ///
    /// Capture the Flag (1994) blew the rocks only when the player came off the west end of the
    /// bridge (area 440, 7 m round 317, 65): objective 2's second transition, from incomplete to
    /// incomplete, starting the breach (scenario 5). A player who goes round the rock wall
    /// instead of over the bridge never enters that area. They reach the doorwell (area 439,
    /// 10 m round 279, 66), which completes "Find a way out of the cave" - and with it gone
    /// from incomplete, the bridge's transition can never fire. The pile stays shut, the Thrax
    /// never come, and Tizzik (objective 1, opened by that completion) is behind it.
    ///
    ///  - Reaching the doorwell starts the breach too: action 5 on objective 2's first
    ///    transition. One who came over the bridge has had it already, and every effect of it
    ///    is applied once, so it changes nothing for them.
    ///  - For a player already caught - "Find a way out of the cave" done, the pile shut - the
    ///    Tizzik objective gets a second transition, from incomplete to incomplete, that starts
    ///    the breach as well on entering a new area, 8 m round the pile itself (441). They walk
    ///    off from the pile and back to it. Its own area, not the doorwell's, so a second entry
    ///    into the doorwell still changes nothing of the mission.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. Each row goes in only where what it hangs
    /// from is there and it is not, and Down takes out only these rows.
    /// </summary>
    public static class BootcampCaveInAnyRouteV13
    {
        public const uint Mission = 1994;
        private const string Revision = BootcampMissionDataV1.Revision;

        public const uint TizzikObjective = 1, CaveExitObjective = 2;
        public const uint CaveExitTransition = 1, DoorwellAction = 5;
        public const uint TizzikDoorwellTransition = 2, TizzikDoorwellTrigger = 1, TizzikDoorwellAction = 1;
        public const uint PileArea = 441;
        public const float PileAreaRadius = 8f;
        public const uint AreaEntered = 4, StartScenario = 5;

        private const string Of = "mission_id = 1994 AND content_revision = '" + Revision + "'";

        public static IEnumerable<string> Up()
        {
            var pile = BootcampCaveInBreachV9.CaveInPosition;
            yield return "INSERT INTO mission_area (mission_id, content_revision, area_id, requirement, map_context_id, shape, pos_x, pos_y, pos_z, radius, comment) " +
                $"SELECT {Mission}, '{Revision}', {PileArea}, 1, 1985, 1, {F(pile.X)}, {F(pile.Y)}, {F(pile.Z)}, {F(PileAreaRadius)}, '1994 the cave-in pile' " +
                $"FROM mission_area r WHERE r.{Of} AND r.area_id = 439 " +
                $"AND NOT EXISTS (SELECT 1 FROM mission_area x WHERE x.{Of} AND x.area_id = {PileArea});";

            yield return "INSERT INTO mission_action (mission_id, content_revision, objective_id, transition_id, action_id, comment, kind, requirement, scenario_id, sequence) " +
                $"SELECT {Mission}, '{Revision}', {CaveExitObjective}, {CaveExitTransition}, {DoorwellAction}, 'Blow the cave-in, whichever way the player came', " +
                $"{StartScenario}, 1, {BootcampCaveInBreachV9.BreachScenario}, {DoorwellAction} FROM mission_objective_transition t " +
                $"WHERE t.{Of} AND t.objective_id = {CaveExitObjective} AND t.transition_id = {CaveExitTransition} " +
                $"AND NOT EXISTS (SELECT 1 FROM mission_action a WHERE a.{Of} AND a.objective_id = {CaveExitObjective} " +
                $"AND a.transition_id = {CaveExitTransition} AND a.action_id = {DoorwellAction});";

            yield return "INSERT INTO mission_objective_transition (mission_id, content_revision, objective_id, transition_id, requirement, sequence, from_state, to_state, comment) " +
                $"SELECT {Mission}, '{Revision}', {TizzikObjective}, {TizzikDoorwellTransition}, 1, 2, 1, 1, 'Cave-in pile reached, still shut' " +
                $"FROM mission_objective_transition t WHERE t.{Of} AND t.objective_id = {TizzikObjective} AND t.transition_id = 1 " +
                $"AND NOT EXISTS (SELECT 1 FROM mission_objective_transition x WHERE x.{Of} AND x.objective_id = {TizzikObjective} " +
                $"AND x.transition_id = {TizzikDoorwellTransition});";

            yield return "INSERT INTO mission_trigger (mission_id, content_revision, objective_id, transition_id, trigger_id, requirement, kind, sequence, area_id, comment) " +
                $"SELECT {Mission}, '{Revision}', {TizzikObjective}, {TizzikDoorwellTransition}, {TizzikDoorwellTrigger}, 1, {AreaEntered}, 1, {PileArea}, " +
                "'Come up to the cave-in pile' FROM mission_area r " +
                $"WHERE r.{Of} AND r.area_id = {PileArea} AND EXISTS (SELECT 1 FROM mission_objective_transition t WHERE t.{Of} " +
                $"AND t.objective_id = {TizzikObjective} AND t.transition_id = {TizzikDoorwellTransition}) " +
                $"AND NOT EXISTS (SELECT 1 FROM mission_trigger x WHERE x.{Of} AND x.objective_id = {TizzikObjective} " +
                $"AND x.transition_id = {TizzikDoorwellTransition} AND x.trigger_id = {TizzikDoorwellTrigger});";

            yield return "INSERT INTO mission_action (mission_id, content_revision, objective_id, transition_id, action_id, comment, kind, requirement, scenario_id, sequence) " +
                $"SELECT {Mission}, '{Revision}', {TizzikObjective}, {TizzikDoorwellTransition}, {TizzikDoorwellAction}, 'Blow the cave-in', " +
                $"{StartScenario}, 1, {BootcampCaveInBreachV9.BreachScenario}, 1 FROM mission_objective_transition t " +
                $"WHERE t.{Of} AND t.objective_id = {TizzikObjective} AND t.transition_id = {TizzikDoorwellTransition} " +
                $"AND NOT EXISTS (SELECT 1 FROM mission_action a WHERE a.{Of} AND a.objective_id = {TizzikObjective} " +
                $"AND a.transition_id = {TizzikDoorwellTransition} AND a.action_id = {TizzikDoorwellAction});";
        }

        public static IEnumerable<string> Down()
        {
            yield return $"DELETE FROM mission_action WHERE {Of} AND objective_id = {TizzikObjective} AND transition_id = {TizzikDoorwellTransition} AND action_id = {TizzikDoorwellAction};";
            yield return $"DELETE FROM mission_trigger WHERE {Of} AND objective_id = {TizzikObjective} AND transition_id = {TizzikDoorwellTransition} AND trigger_id = {TizzikDoorwellTrigger};";
            yield return $"DELETE FROM mission_objective_transition WHERE {Of} AND objective_id = {TizzikObjective} AND transition_id = {TizzikDoorwellTransition};";
            yield return $"DELETE FROM mission_action WHERE {Of} AND objective_id = {CaveExitObjective} AND transition_id = {CaveExitTransition} AND action_id = {DoorwellAction};";
            yield return $"DELETE FROM mission_area WHERE {Of} AND area_id = {PileArea};";
        }

        private static string F(float value) => value.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
    }
}
