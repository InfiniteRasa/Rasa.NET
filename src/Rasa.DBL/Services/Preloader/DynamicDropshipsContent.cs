using System;
using System.Linq;
using System.Numerics;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Every zone's control point plans by dropship - the Plateau's (PlateauDynamicDropshipsContent)
    /// and the Pools' (PoolsDynamicDropshipsContent) - and what a plan is made of.
    /// </summary>
    public static class DynamicDropshipsContent
    {
        public readonly struct Troop
        {
            public uint Creature { get; }
            public int Min { get; }
            public int Max { get; }
            public Troop(uint creature, int min, int max) { Creature = creature; Min = min; Max = max; }
        }

        public sealed class Assault
        {
            public uint MapId { get; init; }
            public uint PointId { get; init; }
            /// <summary>From the server start, or from the AFS taking the point, to the first assault.</summary>
            public int FirstDelaySeconds { get; init; }
            public int PeriodSeconds { get; init; }
            public int WaveSeconds { get; init; }
            public int LifetimeSeconds { get; init; }
            public LandingZoneAssaultContent.Drop[] Drops { get; init; }
            /// <summary>Per wave, per ship: what the ship brings.</summary>
            public Troop[][][] Waves { get; init; }
            /// <summary>Comes with one ship of the last wave and takes the point if the defence falls.</summary>
            public uint Boss { get; init; }
            /// <summary>From the assault's start to the AFS relief ship.</summary>
            public int ReliefSeconds { get; init; } = 480;
            public LandingZoneAssaultContent.Drop ReliefPad { get; init; }
            public Vector3 ReliefPost { get; init; }
            public Troop[] Relief { get; init; }
            /// <summary>While the Bane hold the point: one AFS ship with this squad now and then (AfsSorties).</summary>
            public Troop[] Sortie { get; init; } = Array.Empty<Troop>();
            public int SortieFirstDelaySeconds { get; init; }
            public int SortiePeriodSeconds { get; init; } = 1800;
            public int SortieLifetimeSeconds { get; init; } = 600;
            /// <summary>
            /// Each assault the AFS hold off makes the next one bigger by this share of its troops
            /// (LandingZoneAssaults), until the point falls; it starts again from nothing when the
            /// point changes hands. 0: no growth.
            /// </summary>
            public double EscalationStep { get; init; } = Escalation;
            /// <summary>Held-off assaults that count towards the growth: at most twice the troops.</summary>
            public int MaxEscalation { get; init; } = 4;
            /// <summary>One of each, however big the assault grows: a named creature.</summary>
            public uint[] NotEscalated { get; init; } = Array.Empty<uint>();
        }

        // Each zone's escalation (Plateau and Pools, tuned on the Plateau). "Allowing the Bane to control these points may give them the means to drop an ever
        // increasing number of troops" (Ten Ton Hammer, E3 2005); "as Bane are killed, even more are
        // called in for backup" (TaRapedia, Control Point); "The game had a built in 'ramp up' for the
        // number of Bane that would spawn in and assault so that EVENTUALLY the Control Point would get
        // overrun" (a retail player, City of Titans forum, "CONTROL POINTS ... how Tabula Rasa did it").
        // 25% more troops (Commander and Kael one each) for each assault held off: ESTIMATE, so that a
        // point no player defends holds off the first two and falls in the third (live run without
        // Shield Drones: Northwest and a captured Southwest both fell in the third). Capped at twice the
        // troops (MaxEscalation, ESTIMATE) so that a point players keep defending does not grow without end.
        public const double Escalation = 0.25;

        public static readonly Assault[] Assaults =
            PlateauDynamicDropshipsContent.Assaults.Concat(PoolsDynamicDropshipsContent.Assaults).ToArray();
    }
}
