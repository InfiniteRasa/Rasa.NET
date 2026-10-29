using System.Collections.Generic;

namespace Rasa.Services.Preloader.Missions.Pools
{
    /// <summary>
    /// Corporal Sheen at the East Listening Post, Valverde Pools (map 1304), for the spy chain: a package for the public
    /// Sheen (510197), who gives Taking the Poison (1590), and her stand-in at her console for Spy Networking (1331).
    /// The server had Sheen with no package.
    ///
    /// Sourced: client name 9405 "Corporal Sheen" (creaturenamelanguage); her package 1524 (objectiveconversation 1331,25:
    /// "These consoles are for official use only. Is there something I can help you with?", choices "Send her to the
    /// first aid office." / "Goodbye."). TaRapedia Spy Networking walkthrough: "Instruct Corporal Sheen to go to the
    /// infirmary ... and use her computer terminal in her absence."; Taking the Poison: giver and receiver Corporal Sheen.
    /// TaRapedia's Sheen stands at (978, 656, 358), by the post's workstations; the server's at (1000.1, 656.5, 314).
    ///
    /// Reconstructed (ESTIMATE; the user's choice of 2026-10-25: a personal Sheen stand-in walks away for this character,
    /// the public Sheen stays):
    ///  - The stand-in: her own creature (so only she carries package 1524 and the 1331 talk), a copy of Sheen's row and
    ///    look (class, name, level, hit points, the six appearance slots). Spy Networking's scene sets her down by the
    ///    console and walks her to the post's hospital.
    ///  - The public Sheen: a server-only package (her id), as Riggins's, for her own mission (1590) only.
    ///
    /// Ids: creature 760212, free before. Plain SQL, valid on SQLite and MySQL (the copies are INSERT ... SELECT); down
    /// takes out only these rows.
    /// </summary>
    public static class PoolsSpyNpcsV1
    {
        public const uint Sheen = 510197, SheenPackage = 510197, SheenName = 9405;
        public const uint StandIn = 760212, StandInPackage = 1524;

        public static IEnumerable<string> Up()
        {
            yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                         "action1, action2, action3, action4, action5, action6, action7, action8) " +
                         $"SELECT {StandIn}, 'Corporal Sheen - East Listening Post, at her console (1331)', class_id, faction, level, " +
                         "max_hp, name_id, run_speed, walk_speed, action1, action2, action3, action4, action5, action6, action7, action8 " +
                         $"FROM creature WHERE id = {Sheen};";
            yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                         $"SELECT {StandIn}, slot_id, Class_id, color FROM creature_appearance WHERE id = {Sheen};";
            yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({StandIn}, {StandInPackage}, 'Corporal Sheen - at her console (1331)');";
            yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({Sheen}, {SheenPackage}, 'Corporal Sheen - East Listening Post');";
        }

        public static IEnumerable<string> Down()
        {
            yield return $"DELETE FROM npc_package WHERE id = {Sheen};";
            yield return $"DELETE FROM npc_package WHERE id = {StandIn};";
            yield return $"DELETE FROM creature_appearance WHERE id = {StandIn};";
            yield return $"DELETE FROM creature WHERE id = {StandIn};";
        }
    }
}
