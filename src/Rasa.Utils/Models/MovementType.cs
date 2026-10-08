namespace Rasa.Models
{
    /// <summary>
    /// The first byte of a movement block: what kind of movement it is. From the 1.16.5.0 client
    /// (ClientMovingEntityController and its states in tabula_rasa.exe).
    ///
    /// A client sends Normal and Jump and nothing else. Placed to Slide are the server's. The
    /// client never passes one of those over for a newer update, and Knockback, Rush and Slide
    /// each put the entity's controller into a state of its own, which runs to its end before
    /// anything sent after it is looked at.
    /// </summary>
    public enum MovementType : byte
    {
        /// <summary>An ordinary update: where the entity is and how it is moving.</summary>
        Normal = 0,

        /// <summary>The start of a jump: one Move from a player's client as they leave the ground (OnStartJump).</summary>
        Jump = 1,

        /// <summary>A placement, applied at once. No state of its own.</summary>
        Placed = 2,

        /// <summary>
        /// KnockbackState: thrown in an arc from where it stands to the position, at 15 m/s over
        /// the ground, facing back the way it came. Stunned through the flight and 0.85 s on the
        /// ground (OnStartStunPlusInAir to OnStopStunPlusInAir), then held 1.15 s more: the two
        /// seconds of KNOCKBACK_GETUP_TIME_MSEC. The velocity is not read.
        /// </summary>
        Knockback = 3,

        /// <summary>
        /// RushingMoveState: runs straight to the position at the velocity, no slower than 1 m/s,
        /// facing it. A player's own input is blocked until it arrives.
        /// </summary>
        Rush = 4,

        /// <summary>SlidingMoveState: slides to the position, a player's own input blocked.</summary>
        Slide = 5,

        /// <summary>An ordinary update with eight more bytes after it, an entity id. Neither read nor written here.</summary>
        WithEntity = 6
    }
}
