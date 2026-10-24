namespace Rasa.Config
{
    /// <summary>
    /// appsettings.json's ControlPointDefence: how the control points' outposts defend themselves -
    /// their gate shields (Managers.ControlPointFields), their defenders (Managers.Defenders) and
    /// their turrets' bursts (Managers.Emplacements). Every value has a default, so a file without
    /// the section has them on. Picked up by a config reload, from the next think.
    /// </summary>
    public class ControlPointDefenceConfig
    {
        /// <summary>
        /// Whether a point's gate shields are its holder's: the AFS's while they hold it, the
        /// Bane's while they do, and back whole at the new holder's side when it changes hands.
        /// Off, each stands on its row's side whoever holds the point, as any placed field does.
        /// </summary>
        public bool FieldsFollowOwner { get; set; } = true;

        /// <summary>
        /// How long a point's shield stays down once destroyed before it stands again whole, in
        /// seconds. 0 or less: the 45-60 s every other placed field has (PlacedDestructibles).
        /// </summary>
        public int FieldRebuildSeconds { get; set; } = 120;

        /// <summary>How long a damaged point's shield must be left alone - no hit on it - before it powers up again, in seconds. 0 or less: it never does.</summary>
        public int FieldRechargeAfterSeconds { get; set; } = 60;

        /// <summary>How long a point's shield takes to power up from nothing to full once it has begun, in seconds.</summary>
        public int FieldRechargeFullSeconds { get; set; } = 300;

        /// <summary>
        /// Whether a fighting creature that a point's shield stops strikes at it, with its own
        /// attacks, through the ordinary missile path. Off, it stands at the shield, as before.
        /// </summary>
        public bool CreaturesStrikeFields { get; set; } = true;

        /// <summary>How far from a point's shield, across the ground, its side's defenders hear that someone it stops is hitting it, in metres. 0: nobody does.</summary>
        public float FieldAlertRange { get; set; } = 80f;

        /// <summary>
        /// Whether the defenders rules are on (Managers.Defenders): notice by role and turret
        /// reach, a fight called to the base's other defenders, the post held. Off, every
        /// creature notices out to 18 m and fights alone, as before.
        /// </summary>
        public bool Defenders { get; set; } = true;

        /// <summary>The least a defender that walks notices a fight from, in metres.</summary>
        public float DefenderNoticeRange { get; set; } = 30f;

        /// <summary>How much further than its longest attack a defender that walks notices, in metres.</summary>
        public float DefenderNoticeMargin { get; set; } = 4f;

        /// <summary>The most a defender that walks notices a fight from, in metres.</summary>
        public float DefenderNoticeMax { get; set; } = 40f;

        /// <summary>The most an AFS turret notices a fight from: its gun's reach, up to this, in metres; never under the 18 m every creature has.</summary>
        public float TurretNoticeMax { get; set; } = 30f;

        /// <summary>How far from the defender or turret that started a fight the others of its base are called to it, in metres. 0: nobody is.</summary>
        public float CallRange { get; set; } = 45f;

        /// <summary>How much further than its reach from its post a defender's target may go before it is let go, in metres.</summary>
        public float HoldPostMargin { get; set; } = 5f;

        /// <summary>
        /// Whether the AFS's turrets fire bursts (<see cref="BurstSizes"/> rounds,
        /// <see cref="BurstGapMs"/> apart, then a pause that keeps their rounds per minute). Off,
        /// one round each cooldown, as before.
        /// </summary>
        public bool TurretBursts { get; set; } = true;

        /// <summary>
        /// The sizes a burst is picked from, each as likely; none given: 3 or 5 (Emplacements.DefaultBurstSizes).
        /// No default array here: the configuration binder adds a file's list to one, rather than replacing it.
        /// </summary>
        public int[] BurstSizes { get; set; }

        /// <summary>The time between two rounds of a burst, in milliseconds: one creature think (250 ms) is the least that is ever kept.</summary>
        public int BurstGapMs { get; set; } = 250;
    }
}
