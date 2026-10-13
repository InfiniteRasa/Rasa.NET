namespace Rasa.Structures
{
    using Data;

    public class Missile
    {
        public int DamageA { get; set; }
        public int DamageB { get; set; }
        public ActionId ActionId { get; set; }
        public uint ActionArgId { get; set; }
        public bool IsAbility { get; set; }         // set to true to use PerformAbility instead of Windup/Recovery
        public ulong TargetEntityId { get; set; }    // the entityId of the destination (it is possible that the object does no more exist on arrival)
        public Actor TargetActor { get; set; }
        internal DynamicObject TargetObject { get; set; }
        public Actor Source { get; set; }
        internal Game.Missions.World.ScriptedCombatAuthorization SourceCombatAuthorization { get; set; }
        /// <summary>Percent of DamageA that skips armour and comes straight off health (Torqueshell and Injection Guns skills).</summary>
        public int ArmorBypassPercent { get; set; }
        /// <summary>
        /// A player's weapon attack: what the modules in the weapon do on a hit - a steal, a
        /// resist debuff - as the weapon had them when it was fired (ItemModuleBonuses.OnWeaponHit).
        /// </summary>
        public System.Collections.Generic.IReadOnlyList<Managers.ModuleProc> WeaponProcs { get; set; }
        /// <summary>The attack's damage type, as reported to the clients; 0 is treated as physical.</summary>
        public DamageType DamageType { get; set; }
        /// <summary>A melee swing rather than a shot: crouching helps a shot crit and helps a swing crit the one crouching.</summary>
        public bool IsMelee { get; set; }
        /// <summary>The shooter's crit chance in percent, worked out when it was fired; the target's part is added when it lands.</summary>
        public double CritChance { get; set; }
        /// <summary>Whether it landed as a critical hit, DamageA already multiplied.</summary>
        public bool IsCritical { get; set; }
        /// <summary>Chance in percent that the hit stuns a creature (Hand to Hand, grenades), and for how long.</summary>
        public int StunChance { get; set; }
        public int StunMs { get; set; }
        /// <summary>How long the hit holds a creature where it stands (net guns); 0 for not at all.</summary>
        public int RootMs { get; set; }
        /// <summary>Chance in percent that the hit knocks a creature back (Firearms' shotguns, Hand to Hand).</summary>
        public int KnockbackChance { get; set; }
        /// <summary>How much longer a creature this knocks back stays down (Hand to Hand's "Stun Duration").</summary>
        public int KnockbackStunMs { get; set; }
        /// <summary>Metres around the target that a launcher's splash reaches (Splash); 0 for none.</summary>
        public float SplashRadius { get; set; }
        /// <summary>What each splashed creature takes, worked out from the damage before the crit roll.</summary>
        public int SplashDamage { get; set; }
        /// <summary>A player's weapon shot: the weapon's optimal range, past which its damage drops (RangeFalloff); 0 for no drop.</summary>
        public float OptimalRange { get; set; }
        /// <summary>A cone weapon's other victims, found when it was fired (ConeWeapons); each takes ConeDamage as a hit of its own.</summary>
        public System.Collections.Generic.List<Actor> ConeTargets { get; set; }
        /// <summary>What each creature in the cone takes before its own crit roll: the shot's damage as fired.</summary>
        public int ConeDamage { get; set; }
        /// <summary>
        /// A creature's attack: what each player caught in its area (CreatureAreaAttacks) takes
        /// before their own crit roll - the attack's damage as launched.
        /// </summary>
        public int AreaDamage { get; set; }
        /// <summary>The creature_action row a creature's attack was made with: its damage is an effect's tick where the attack is a damage over time (CreatureEffectAttacks).</summary>
        public CreatureAction CreatureAction { get; set; }

        /// <summary>A creature ability held for its windup (CreatureWindups): it does not land if the creature is dead by then.</summary>
        public bool AfterWindup { get; set; }
        /// <summary>A creature attack's area when the attack names it rather than its action data (KaelRushingBlow's EFFECT_RADIUS).</summary>
        public Managers.CreatureArea? AreaOverride { get; set; }
        /// <summary>Where AreaOverride is centred, in place of the target's position.</summary>
        public System.Numerics.Vector3? AreaCentre { get; set; }
        /// <summary>
        /// Whether the to-hit roll went against it (ToHit): made as a weapon is fired, and null
        /// for a missile that has not been rolled for, which is rolled for as it lands.
        /// </summary>
        public bool? Missed { get; set; }
        /// <summary>A player's shot the server is holding for its flight (ShotFlight): it does not land if the shooter has left the map by then.</summary>
        public bool HeldForFlight { get; set; }

        /// <summary>A player's weapon shot: the weapon class's velocity, which their client flies it at (Managers.ShotFlight). Null for a swing or a creature's attack.</summary>
        public int? FlightVelocity { get; set; }
        public long TriggerTime { get; set; }       // amount of milliseconds left before the missile is triggered, is decreased on every tick
        public MissileArgs Args = new MissileArgs();
    }
}
