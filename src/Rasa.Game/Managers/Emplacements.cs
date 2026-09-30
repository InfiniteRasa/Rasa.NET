using System.Collections.Generic;

namespace Rasa.Managers
{
    using Data;
    using Structures;

    /// <summary>
    /// Emplacements: the guns the server stands on mounts the map builds for them. The mount is
    /// scenery the client draws on its own - the standard turret's platform
    /// (ArchHumanBaseTurretPlatformV01), the light turret's tripod or track
    /// (Creature_Human_Base_Turret_Mini_Base_Tripod / _Track) - and the gun on top is a creature
    /// of an Emplacement_* class. No map places one: they were always the server's to spawn.
    ///
    /// A gun bolted to a platform does not walk. An emplacement:
    /// - stands exactly where its pool puts it: the pool's height is the mount's top, and
    ///   snapped to the navmesh a light turret would drop into its tripod (SpawnPoolManager);
    /// - never chases, wanders, runs home or is pushed about by a crowd (BehaviorManager);
    /// - is not carried by a knockback or a pull (CrowdControl);
    /// - turns to face what it shoots, which is all the movement it sends.
    /// It notices a fight as any creature does (Creature.AggroRange) and shoots out to its
    /// weapon's reach once it is in one, or at whatever shoots it.
    /// </summary>
    public static class Emplacements
    {
        /// <summary>The client's Emplacement_* creature classes.</summary>
        public static readonly HashSet<EntityClasses> Classes = new HashSet<EntityClasses>
        {
            (EntityClasses)4064,    // Emplacement_AFS_Turret_Standard, "AFS Turret"
            (EntityClasses)11302,   // Emplacement_AFS_Turret_Mini, "AFS Light Turret"
            (EntityClasses)23902,   // Emplacement_AFS_Turret_Brann, "Brann Turret"
            (EntityClasses)7482,    // Emplacement_Bane_Turret_Standard
            (EntityClasses)10509,   // Emplacement_Bane_Turret_Mini
            (EntityClasses)20359    // Ability_Bane_Turret, a Technician's (CreatureSummons): it stands where it was set down
        };

        /// <summary>Whether this creature is an emplacement: it is if its class is one.</summary>
        public static bool Is(Creature creature) => creature != null && Classes.Contains(creature.EntityClass);
    }
}
