using System.Collections.Generic;
namespace Rasa.Services.Preloader
{
    /// <summary>Five-minute reinforcements for audited Wilderness combat posts; database values are tenths.</summary>
    public static class DefenderRespawns
    {
        public const uint ReinforcementTenths = 3000;
        // Match each original timer and primary template, preserving GM edits and repurposed pools.
        public static readonly (uint Pool, uint Written, uint Creature)[] Pools =
        {
            (49u, 25u, 39u), // Redshirt_Human_Soldier_Light_Male
            (50u, 25u, 39u), // Redshirt_Human_Soldier_Light_Male
            (51u, 25u, 39u), // Redshirt_Human_Soldier_Light_Male
            (63u, 20u, 52u), // NPC_Forean_Shaman
            (65u, 25u, 37u), // NPC_Forean_Spearman
            (66u, 25u, 51u), // NPC_Forean_Gunner
            (72u, 20u, 52u), // NPC_Forean_Shaman
            (78u, 20u, 52u), // NPC_Forean_Shaman
            (90u, 20u, 52u), // NPC_Forean_Shaman
            (92u, 100u, 52u), // NPC_Forean_Shaman
            (117u, 20u, 39u), // Redshirt_Human_Soldier_Light_Male
            (149u, 20u, 53u), // NPC_Forean_Archer
            (150u, 20u, 53u), // NPC_Forean_Archer
            (640002u, 20u, 640002u), // Infantryman at ease; rifle - Alia Das
            (640003u, 20u, 640002u), // Infantryman at ease; rifle - Alia Das
            (640013u, 20u, 640009u), // Infantryman; pistol - Oliver's post
            (640014u, 20u, 640010u), // Field Gunner; machine gun - Oliver's post
            (640015u, 20u, 640010u), // Field Gunner; machine gun - Oliver's post
            (640016u, 20u, 640011u), // Rifleman; laser rifle - Oliver's post
            (640017u, 20u, 640012u), // Field Gunner; machine gun - Landing Zone
            (640019u, 20u, 640014u), // Medic Bond - Landing Zone
            (640103u, 20u, 640103u), // Rocket Gunner; rocket launcher - Twin Pillars squad
            (640104u, 20u, 640103u), // Rocket Gunner; rocket launcher - Twin Pillars squad
            (640105u, 20u, 640104u), // Rifleman; rifle - Twin Pillars squad
            (640106u, 20u, 640104u), // Rifleman; rifle - Twin Pillars squad
            (640107u, 20u, 640105u), // Infantryman; pistol - Twin Pillars squad
        };
        public static IEnumerable<string> Up()
        {
            foreach(var p in Pools)
                yield return $"UPDATE spawnpool SET respown_time={ReinforcementTenths} WHERE id={p.Pool} AND map_context_id=1220 AND creature_1_Id={p.Creature} AND respown_time={p.Written};";
        }
        public static IEnumerable<string> Down()
        {
            foreach(var p in Pools)
                yield return $"UPDATE spawnpool SET respown_time={p.Written} WHERE id={p.Pool} AND map_context_id=1220 AND creature_1_Id={p.Creature} AND respown_time={ReinforcementTenths};";
        }
    }
}
