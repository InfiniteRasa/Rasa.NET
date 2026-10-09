namespace Rasa.Data
{
    /// <summary>
    /// How an NPC stands at its post: spawnpool_pose.pose, applied by Managers.NpcPoses.
    ///
    /// The client has no pose to be sent. It picks an actor's animation from the actor's states
    /// and from the class in its weapon slot (generated.client.animationdata), so each of these
    /// is one of those:
    ///
    ///  - Standing: nothing changed - the at-ease idle every creature has. It is a pose only in
    ///    that the creature keeps its post and its facing.
    ///  - WeaponOut: TOOL_READY and no combat stance, the client's "ready for combat" - a rifle
    ///    held across the chest ("Weapon - Rifle - Idle"), a Forean's spear at the ready
    ///    ("Idle - Combat - Ready"). Nothing for a creature with no weapon.
    ///  - Crouched, CrouchedWeaponOut: the CROUCHED posture, for the 19 skeletons that have one -
    ///    humans, the Forean gunner and archer, Thrax soldiers and a few more. Not the Forean
    ///    Warrior or the elder.
    ///  - Leaning, Sitting, LyingDown, AtConsole, HandTool: the client's five ambient animations,
    ///    each forced by an invisible class in the weapon slot (AnimCondForcer_Avatar_*), so the
    ///    creature holds nothing else while it is posed. Humans only; AtConsole and HandTool have
    ///    animations for the female skeleton alone.
    ///
    /// Nothing checks that a creature's skeleton has the animation. Without it the creature
    /// stands at ease - and for an ambient pose, with its hands empty.
    /// </summary>
    public enum NpcPose : byte
    {
        /// <summary>No pose: the creature idles and strolls as any other.</summary>
        None = 0,
        Standing = 1,
        WeaponOut = 2,
        Crouched = 3,
        CrouchedWeaponOut = 4,
        Leaning = 5,
        Sitting = 6,
        LyingDown = 7,
        AtConsole = 8,
        HandTool = 9
    }
}
