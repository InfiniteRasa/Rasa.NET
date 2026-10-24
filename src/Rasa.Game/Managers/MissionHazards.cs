using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Rasa.Managers
{
    using Config;
    using Data;
    using Game;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// The radiation and toxic gas some missions send their players into. The client has the effects and nothing else:
    ///  - RADIATION_POISON 200, a DamageOverTime the tray names for radiation;
    ///  - SCRIPTED_PERCENT_DOT 423, a DamageOverTime whose level 2 tooltip is "Exposure to an experimental toxic gas is
    ///    causing damage over time."
    /// Each one's OnTick floats the damage it is sent. Retail put them on from the server, and where its volumes were and
    /// how hard they hurt is not in anything we have: the volumes and the numbers here are ours (<see cref="Zones"/>).
    /// Lava is not here; it burns as LavaDamage has it, at the client's own numbers.
    ///
    /// Once a second per map, next to the lava (MapChannelManager), each player standing in an enabled volume - and, for a
    /// mission's, holding that mission - has the volume's effect put on, or kept on: its end is pushed out to the volume's
    /// LingerMs from now, so it stays a little after they step out and then runs out by itself. One effect of a kind at a
    /// time: two overlapping volumes of radiation hurt once. The effect is nobody's and the world's
    /// (GameEffect.Environmental, as lava's): Cure neither keeps it off nor takes it off. It does its work on its own
    /// OnTick: every TickMs a roll of the volume's DamageMin-DamageMax plus PercentMaxHealth of the victim's maximum health,
    /// times appsettings.json's Hazards.DamageScale, the victim's resistance to the damage type taken off
    /// (GameEffectManager.ApplyResist: Hazmat Armor against Virulent, an effect's resistance, an immunity), dealt as a
    /// periodic hit with no attacker (ActorManager.Damage: a shield's share, armour first, "Immune") and told in the tick
    /// (GameEffectTick, one (target, rawInfo) entry). A hit that takes the last of the player's health kills them as lava
    /// does, with nobody's kill.
    ///
    /// Not touched: the dead and the dying, anyone immune to all damage (.immune all) and a GM who has not said .allowdeath
    /// (PlayerDeath.IsDeathless). Creatures are never touched, as by lava. Any of these, the volume switched off (.hazard),
    /// the mission no longer held or the section switched off, and the effect comes off at its next tick.
    ///
    /// GMs place more for a test (.hazard place), held until the next restart, and list and switch them (.hazard).
    /// </summary>
    public static class MissionHazards
    {
        public const int RadiationEffectTypeId = 200;   // RADIATION_POISON
        public const int ToxicGasEffectTypeId = 423;    // SCRIPTED_PERCENT_DOT
        public const uint ToxicGasEffectLevel = 2;      // "Exposure to an experimental toxic gas ..."

        /// <summary>Ours: how long an effect stays on after leaving its volume.</summary>
        public const int LingerMs = 4000;

        /// <summary>Ours: how often a volume hurts.</summary>
        public const int TickMs = 2000;

        public const uint MislaidPlansMissionId = 965;
        public const uint ChambersOfHorrorMissionId = 1344;

        /// <summary>A GM's volumes have ids from here.</summary>
        public const uint FirstPlacedId = 1000;

        /// <summary>appsettings.json's Hazards (Game.Server.ConfigLoaded).</summary>
        public static HazardsConfig Config { get; set; } = new HazardsConfig();

        /// <summary>A hurt's roll between the two, both included; a test's to fix.</summary>
        internal static Func<int, int, int> Roll { get; set; } = (min, max) => Random.Shared.Next(min, max + 1);

        /// <summary>
        /// The missions' volumes, drawn round where the missions happen. Retail's numbers are lost; these are invented.
        ///  - 965 Mislaid Plans (adv_foreas_valverde_plateau, 1497): the radiation round the courier bag on the far side
        ///    of the wrecked Pinnacle Watchtower ("RADIATION WARNING!", objective 3). RADIATION_POISON at level 1,
        ///    Environmental: a 40 m circle, and every 2 s 70 to 105 - 2 to 3 percent of a level 30 player's 3,535 health
        ///    (the mission's level, the baseline with no points spent) - through armour.
        ///  - 1344 Chambers Of Horror (adv_arieki_torden_abyss, 2028): the experimental toxic gas of Penumbra Section
        ///    Five. SCRIPTED_PERCENT_DOT at level 2, Virulent: a box over the rooms the twenty Machina chambers stand in,
        ///    and every 2 s 4 percent of the victim's own maximum health.
        /// No zone was electrified in retail, and none is here.
        /// </summary>
        public static readonly IReadOnlyList<HazardVolume> Zones = new[]
        {
            new HazardVolume
            {
                Id = 1, MapContextId = 1497, Kind = HazardVolume.HazardKind.Radiation, Shape = HazardVolume.HazardShape.Circle,
                Position = new Vector3(-588.4f, 397.7f, 194.7f), Radius = 40, MinY = 377.7f, MaxY = 437.7f,
                EffectTypeId = RadiationEffectTypeId, EffectLevel = 1, DamageType = DamageType.Environmental, TickMs = TickMs,
                DamageMin = 70, DamageMax = 105, LingerMs = LingerMs, MissionId = MislaidPlansMissionId,
                Comment = "Mislaid Plans: radiation round the courier bag (numbers ours)"
            },
            new HazardVolume
            {
                Id = 2, MapContextId = 2028, Kind = HazardVolume.HazardKind.ToxicGas, Shape = HazardVolume.HazardShape.Box,
                Position = new Vector3(-742.5f, 544.5f, -156f), HalfX = 67.5f, HalfZ = 74f, MinY = 538f, MaxY = 556f,
                EffectTypeId = ToxicGasEffectTypeId, EffectLevel = ToxicGasEffectLevel, DamageType = DamageType.Virulent, TickMs = TickMs,
                PercentMaxHealth = 4, LingerMs = LingerMs, MissionId = ChambersOfHorrorMissionId,
                Comment = "Chambers Of Horror: Penumbra Section Five's toxic gas (numbers ours)"
            }
        };

        private static readonly object Gate = new object();
        private static readonly List<HazardVolume> PlacedVolumes = new List<HazardVolume>();
        private static uint _nextPlacedId = FirstPlacedId;

        /// <summary>The volume behind each effect this put on: the one the player last stood in.</summary>
        private static readonly ConditionalWeakTable<GameEffect, HazardVolume> Driving = new ConditionalWeakTable<GameEffect, HazardVolume>();

        /// <summary>Every volume: the missions', then a GM's.</summary>
        public static List<HazardVolume> All()
        {
            lock (Gate)
                return Zones.Concat(PlacedVolumes).ToList();
        }

        public static HazardVolume Find(uint id) => All().FirstOrDefault(volume => volume.Id == id);

        /// <summary>The volumes on a map, nearest to the position first.</summary>
        public static List<HazardVolume> OnMap(uint mapContextId, Vector3 position) =>
            All().Where(volume => volume.MapContextId == mapContextId).OrderBy(volume => volume.Distance(position)).ThenBy(volume => volume.Id).ToList();

        /// <summary>Once a second per map, from MapChannelWorker.</summary>
        public static void Worker(MapChannel mapChannel)
        {
            if (mapChannel?.MapInfo == null || !Config.Enabled || mapChannel.ClientList.Count == 0)
                return;

            var mapContextId = mapChannel.MapInfo.MapContextId;
            var volumes = All().Where(volume => volume.MapContextId == mapContextId && volume.Enabled).ToList();

            if (volumes.Count == 0)
                return;

            var now = Environment.TickCount64;

            foreach (var client in mapChannel.ClientList.ToArray())
            {
                var player = client?.Player;

                if (player == null || !ReferenceEquals(player.MapChannel, mapChannel) || Spared(mapChannel, player))
                    continue;

                foreach (var volume in At(volumes, player))
                    Expose(mapChannel, player, volume, now);
            }
        }

        /// <summary>The volumes of the list that hold the player now and apply to them, one per effect (the lowest id).</summary>
        public static List<HazardVolume> At(IEnumerable<HazardVolume> volumes, Manifestation player)
        {
            var result = new List<HazardVolume>();

            foreach (var volume in volumes.OrderBy(v => v.Id))
            {
                if (!volume.Enabled || result.Any(taken => taken.EffectTypeId == volume.EffectTypeId))
                    continue;

                if (volume.Contains(player.Position) && HoldsMission(player, volume))
                    result.Add(volume);
            }

            return result;
        }

        /// <summary>A mission's volume touches a player who holds the mission: taken, and not yet turned in, failed or abandoned.</summary>
        public static bool HoldsMission(Manifestation player, HazardVolume volume)
        {
            if (!volume.MissionId.HasValue)
                return true;

            return player.Missions.TryGetValue(volume.MissionId.Value, out var log) && log.State == MissionState.Active;
        }

        /// <summary>Whom no volume touches: the dead and the dying, anyone immune to all damage, a GM who may not die.</summary>
        public static bool Spared(MapChannel mapChannel, Manifestation player)
        {
            if (player == null || player.State == CharacterState.Dead || player.State == CharacterState.Dying)
                return true;

            if (!player.Attributes.TryGetValue(Attributes.Health, out var health) || health.Current <= 0)
                return true;

            if (player.ImmuneToAllDamage)
                return true;

            var client = GameEffectManager.ClientOf(mapChannel, player);

            return client != null && PlayerDeath.IsDeathless(client);
        }

        /// <summary>
        /// The volume's effect on the player from now until its LingerMs after: the one already on (from any volume of the
        /// same effect), its end pushed out and this volume behind it, or a fresh one. Null when none could go on.
        /// </summary>
        public static GameEffect Expose(MapChannel mapChannel, Manifestation player, HazardVolume volume, long now)
        {
            var existing = player.ActiveEffects.Values.FirstOrDefault(effect => effect.TypeId == volume.EffectTypeId && Driving.TryGetValue(effect, out _));

            if (existing != null)
            {
                existing.ExpiresTick = Math.Max(existing.ExpiresTick, now + volume.LingerMs);
                Driving.AddOrUpdate(existing, volume);

                return existing;
            }

            var effect = new GameEffect
            {
                TypeId = volume.EffectTypeId,
                EffectId = GameEffectManager.Instance.NextEffectId(mapChannel),
                EffectLevel = volume.EffectLevel,
                SourceId = 0,
                Source = null,
                SourceLevel = player.Level,
                IsBuff = false,
                Environmental = true,
                AllowDetach = false,
                ShowsDuration = false,
                ExpiresTick = now + volume.LingerMs,
                TickIntervalMs = volume.TickMs,
                NextTickTick = now,
                OnTick = Tick
            };

            effect.Tooltip["dmgMin"] = volume.DamageMin;
            effect.Tooltip["dmgMax"] = volume.DamageMax;
            effect.Tooltip["interval"] = volume.TickMs / 1000;

            Driving.AddOrUpdate(effect, volume);
            GameEffectManager.Instance.Attach(mapChannel, player, effect);

            return player.ActiveEffects.ContainsKey(effect.EffectId) ? effect : null;
        }

        /// <summary>Whether this effect was put on by a volume, and by which last.</summary>
        public static bool TryGetVolumeOf(GameEffect effect, out HazardVolume volume) => Driving.TryGetValue(effect, out volume);

        /// <summary>One hurt from the volume behind the effect, or the effect taken off when it no longer applies.</summary>
        private static void Tick(MapChannel mapChannel, Actor holder, GameEffect effect)
        {
            if (!Driving.TryGetValue(effect, out var volume) || !Config.Enabled || !volume.Enabled
                || !(holder is Manifestation player) || Spared(mapChannel, player) || !HoldsMission(player, volume))
            {
                GameEffectManager.Instance.DettachEffect(mapChannel, holder, effect);
                return;
            }

            var amount = DamageOf(volume, player);

            if (amount <= 0)
                return;

            var dealt = GameEffectManager.ApplyResist(player, amount, out var resisted, volume.DamageType);
            var taken = ActorManager.Instance.Damage(mapChannel, player, dealt, null, out var outcome, volume.DamageType, isPeriodic: true);

            // After the damage, so that a death blow is known; the client still has the entity, dead or not.
            var tick = new GameEffectTickPacket(effect.EffectId, GameEffectTickPacket.TickKind.Damage);

            tick.Entries.Add(new TickEntry
            {
                EntityId = player.EntityId,
                Amount = outcome.Delivered,
                Absorbed = outcome.Absorbed,
                WasImmune = outcome.Immune,
                Resisted = resisted,
                DamageType = volume.DamageType,
                DeathBlow = taken > 0 && player.Attributes.TryGetValue(Attributes.Health, out var health) && health.Current <= 0
            });

            CellManager.Instance.CellCallMethod(mapChannel, player, tick);
        }

        /// <summary>One hurt before resistance: the roll, plus the share of the victim's maximum health, times DamageScale.</summary>
        public static int DamageOf(HazardVolume volume, Actor victim)
        {
            var amount = volume.DamageMax > 0 ? (double)Roll(volume.DamageMin, volume.DamageMax) : 0;

            if (volume.PercentMaxHealth > 0 && victim.Attributes.TryGetValue(Attributes.Health, out var health))
                amount += health.CurrentMax * volume.PercentMaxHealth / 100.0;

            return (int)Math.Round(amount * Math.Max(0, Config.DamageScale) / 100.0, MidpointRounding.AwayFromZero);
        }

        #region GM

        /// <summary>A GM's volume: a circle of the kind round the position, at the missions' numbers, held until the next restart.</summary>
        public static HazardVolume Place(uint mapContextId, Vector3 at, HazardVolume.HazardKind kind, float radius, uint? missionId)
        {
            var radiation = kind == HazardVolume.HazardKind.Radiation;

            lock (Gate)
            {
                var volume = new HazardVolume
                {
                    Id = _nextPlacedId++, MapContextId = mapContextId, Kind = kind, Shape = HazardVolume.HazardShape.Circle,
                    Position = at, Radius = radius, MinY = at.Y - 10, MaxY = at.Y + 20,
                    EffectTypeId = radiation ? RadiationEffectTypeId : ToxicGasEffectTypeId,
                    EffectLevel = radiation ? 1 : ToxicGasEffectLevel,
                    DamageType = radiation ? DamageType.Environmental : DamageType.Virulent, TickMs = TickMs,
                    DamageMin = radiation ? 70 : 0, DamageMax = radiation ? 105 : 0, PercentMaxHealth = radiation ? 0 : 4,
                    LingerMs = LingerMs, MissionId = missionId, Placed = true, Comment = "placed by a GM"
                };

                PlacedVolumes.Add(volume);

                return volume;
            }
        }

        /// <summary>Takes a GM's volume away. False for one of the missions' (switch those off instead) or none.</summary>
        public static bool Remove(uint id)
        {
            lock (Gate)
                return PlacedVolumes.RemoveAll(volume => volume.Id == id) > 0;
        }

        /// <summary>Takes every GM volume away; a test's.</summary>
        internal static void ClearPlaced()
        {
            lock (Gate)
            {
                PlacedVolumes.Clear();
                _nextPlacedId = FirstPlacedId;
            }

            foreach (var zone in Zones)
                zone.Enabled = true;
        }

        public const string GmUsage = "usage: .hazard | .hazard place radiation|toxic [radius] [missionId] | .hazard <id> on|off | .hazard remove <id>";

        /// <summary>
        /// .hazard: the volumes nearest to you on this map, and which you stand in. .hazard place radiation|toxic [radius]
        /// [missionId]: a volume round you (radius 10 by default), for everyone or for a mission's holders. .hazard id on|off:
        /// switches one. .hazard remove id: takes a placed one away. Everything is held until the next restart.
        /// </summary>
        public static void GmCommand(Client client, string[] parts)
        {
            var player = client?.Player;

            if (player == null)
                return;

            void Say(string text) => CommunicatorManager.Instance.SystemMessage(client, text);

            if (parts.Length >= 2 && parts[1].Equals("place", StringComparison.OrdinalIgnoreCase))
            {
                HazardVolume.HazardKind kind;

                if (parts.Length >= 3 && parts[2].Equals("radiation", StringComparison.OrdinalIgnoreCase))
                    kind = HazardVolume.HazardKind.Radiation;
                else if (parts.Length >= 3 && parts[2].Equals("toxic", StringComparison.OrdinalIgnoreCase))
                    kind = HazardVolume.HazardKind.ToxicGas;
                else
                {
                    Say(GmUsage);
                    return;
                }

                var radius = 10f;
                uint? mission = null;

                if (parts.Length >= 4 && (!float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out radius) || radius <= 0 || radius > 500))
                {
                    Say(GmUsage);
                    return;
                }

                if (parts.Length >= 5)
                {
                    if (!uint.TryParse(parts[4], out var missionId))
                    {
                        Say(GmUsage);
                        return;
                    }

                    mission = missionId;
                }

                var placed = Place(player.MapContextId, player.Position, kind, radius, mission);
                Say($"Placed {placed.Describe()}");
                return;
            }

            if (parts.Length >= 3 && parts[1].Equals("remove", StringComparison.OrdinalIgnoreCase))
            {
                Say(uint.TryParse(parts[2], out var removeId) && Remove(removeId)
                    ? $"Hazard volume #{removeId} removed."
                    : "No GM-placed hazard volume of that id (the missions' are switched off with .hazard <id> off).");
                return;
            }

            if (parts.Length >= 3)
            {
                var on = parts[2].Equals("on", StringComparison.OrdinalIgnoreCase);

                if (!uint.TryParse(parts[1], out var id) || !(on || parts[2].Equals("off", StringComparison.OrdinalIgnoreCase)) || Find(id) is not { } volume)
                {
                    Say(GmUsage);
                    return;
                }

                volume.Enabled = on;
                Say($"{volume.Describe()} (until the next restart)");
                return;
            }

            if (parts.Length == 2)
            {
                Say(GmUsage);
                return;
            }

            var volumes = OnMap(player.MapContextId, player.Position);
            var off = Config.Enabled ? "" : " Hazards are off (appsettings.json Hazards.Enabled).";

            if (volumes.Count == 0)
            {
                Say($"No hazard volumes on map {player.MapContextId}.{off}");
                return;
            }

            var standing = At(volumes, player);
            var spared = Spared(player.MapChannel, player) ? " You are spared (dead, immune, or a GM without .allowdeath)." : "";

            Say($"{volumes.Count} hazard volume(s) on map {player.MapContextId}, nearest first; damage at {Config.DamageScale}%.{off}{spared}");

            foreach (var volume in volumes.Take(8))
            {
                var inside = standing.Contains(volume) ? " <- you are in it"
                    : volume.Contains(player.Position) ? " <- in it, but not for you (mission)" : "";
                Say(FormattableString.Invariant($"{volume.Distance(player.Position),6:0.#} m  {volume.Describe()}{inside}"));
            }

            if (volumes.Count > 8)
                Say($"... and {volumes.Count - 8} more.");
        }

        #endregion
    }
}
