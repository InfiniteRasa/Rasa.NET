using System;
using System.Collections.Generic;
using System.Numerics;

namespace Rasa.Managers
{
    using Config;
    using Data;
    using Game;
    using Packets.Communicator.Server;
    using Structures;

    /// <summary>
    /// Whether a weapon shot could have been aimed at its target. The target of a shot is the
    /// player's selection (SetTargetId), taken as sent: the server checked the target was an
    /// enemy and within 128 m, and nothing else. The retail client acquires a direct target
    /// under the reticle, but it does not hold the target it keeps to the weapon's range: Direct
    /// mode targets with checkRange 0 (targeting.py), and a lock keeps a target however far it
    /// walks. So a shot past the weapon's reach can come from an honest client, and a shot at
    /// something behind the shooter is more likely one that chose its own target.
    ///
    /// Three checks on a shot with a target (<see cref="WeaponChecksConfig"/>):
    ///
    ///  - <b>Facing.</b> The target is more than <see cref="FacingHalfAngle"/> from the way the
    ///    server has the shooter facing (their last Move's view direction). Wide, because the
    ///    retail client keeps shooting at a target the reticle has left: for the sticky-targeting
    ///    second after it leaves, and for as long as a target lock is on. Behind is behind
    ///    whatever the lock. Refused, the player is told the shot failed its line of sight
    ///    check, as the client tells them itself for a target not in front of them.
    ///  - <b>Range.</b> The target is past <see cref="RangeFalloff.FarMultiple"/> times the
    ///    weapon's range and <see cref="RangeSlack"/>: where the shot's damage has already
    ///    dropped to its floor. Log only by default: such a shot lands for that floor, a quarter,
    ///    and a locked target walking off is the commonest way to make one. Refused, the player
    ///    is told the target is out of range (<see cref="Tell"/>).
    ///  - <b>Sight.</b> Not one of the target's sample points is in the clear from the shooter's
    ///    eyes (Cover), and the target is further than <see cref="SightMinDistance"/>. Log only
    ///    by default: cover already takes the damage down to a tenth, and the server's mesh is
    ///    not the client's at an edge or a prop. Refused, the player is told the shot failed its
    ///    line of sight check.
    ///
    /// Cone weapons are not looked at: the server picks their targets itself, from the shooter's
    /// facing (ConeWeapons), whatever the client selected. A shot with no target is a blind
    /// shot and has nothing to check. A refused shot is not fired and costs nothing; the line is
    /// rate-limited per player as the movement checks' is.
    /// </summary>
    public static class WeaponChecks
    {
        /// <summary>Degrees either side of the shooter's facing a target may be.</summary>
        public const double FacingHalfAngle = 120;

        /// <summary>Metres past twice the weapon's range a shot is still let go: the target moved since the client looked.</summary>
        public const float RangeSlack = 5f;

        /// <summary>Within this of the target the sight check is not made: the mesh's own thickness is in the way.</summary>
        public const float SightMinDistance = 3f;

        /// <summary>Quiet time between one player's lines.</summary>
        public const long QuietMs = 5000;

        /// <summary>Quiet time between one player's messages for refused shots: a held trigger asks again every refire.</summary>
        public const long ToldQuietMs = 2000;

        public static WeaponChecksConfig Config { get; set; } = new WeaponChecksConfig();

        public enum Check
        {
            Facing,
            Range,
            Sight
        }

        public readonly struct Finding
        {
            public Finding(Check check, string what, bool refuse)
            {
                Check = check;
                What = what;
                Refuse = refuse;
            }

            public Check Check { get; }
            public string What { get; }
            public bool Refuse { get; }
        }

        /// <summary>
        /// What the checks find about a shot from the player at the target with a weapon of this
        /// range, or null. <paramref name="cover"/> is the map's cover mesh, null for none.
        ///
        /// Every check is made: one that only logs does not stand in for one that refuses (BR-225;
        /// a long shot's range finding, logged by default, used to hide facing and sight). The
        /// finding is the first that refuses, else the first; its line names the others too.
        /// </summary>
        public static Finding? Judge(Manifestation player, Actor target, float range, Navigation.CoverMesh cover)
        {
            if (player == null || target == null)
                return null;

            var config = Config ?? new WeaponChecksConfig();
            var distance = Vector3.Distance(player.Position, target.Position);
            var found = new List<Finding>();

            if (Mode(config.Range) != MovementChecksConfig.Off && range > 0)
            {
                var reach = range * (float)RangeFalloff.FarMultiple + RangeSlack;

                if (!float.IsFinite(distance) || distance > reach)
                    found.Add(new Finding(Check.Range, $"{distance:F0} m away with a weapon of range {range:F0}",
                        Mode(config.Range) == MovementChecksConfig.Refuse));
            }

            if (Mode(config.Facing) != MovementChecksConfig.Off && !IsFacing(player, target.Position, FacingHalfAngle))
                found.Add(new Finding(Check.Facing, $"{AngleTo(player, target.Position):F0} degrees from where they face",
                    Mode(config.Facing) == MovementChecksConfig.Refuse));

            if (Mode(config.Sight) != MovementChecksConfig.Off && cover != null && distance > SightMinDistance)
            {
                var eye = Cover.EyeOf(player);

                if (Cover.Visible(cover, eye, Cover.SamplePoints(target, eye)) <= 0)
                    found.Add(new Finding(Check.Sight, $"{distance:F0} m away, wholly behind cover",
                        Mode(config.Sight) == MovementChecksConfig.Refuse));
            }

            if (found.Count == 0)
                return null;

            var first = found.Find(finding => finding.Refuse);

            if (!first.Refuse)
                first = found[0];

            var others = found.FindAll(finding => finding.Check != first.Check);
            var what = $"at {target.EntityId}, {first.What}";

            foreach (var other in others)
                what += $"; also {other.What}{(other.Refuse ? "" : " (log)")}";

            return new Finding(first.Check, what, first.Refuse);
        }

        /// <summary>Whether the point is within the half-angle of the way the actor faces, over the ground. Standing on it counts as facing it.</summary>
        public static bool IsFacing(Actor actor, Vector3 point, double halfAngleDegrees)
        {
            return AngleTo(actor, point) <= halfAngleDegrees;
        }

        /// <summary>Degrees between the way the actor faces and the point, over the ground; 0 when standing on it.</summary>
        public static double AngleTo(Actor actor, Vector3 point)
        {
            var facing = Facing.Of(actor);
            var toPoint = new Vector2(point.X - actor.Position.X, point.Z - actor.Position.Z);

            if (toPoint.LengthSquared() < 0.01f)
                return 0;

            var cos = Vector2.Dot(facing, Vector2.Normalize(toPoint));

            return Math.Acos(Math.Clamp(cos, -1.0, 1.0)) * 180.0 / Math.PI;
        }

        /// <summary>The finding's Security line, one per <see cref="QuietMs"/> per player, with how many went unreported.</summary>
        public static void Report(Client client, Finding finding, long now)
        {
            var player = client?.Player;

            if (player == null)
                return;

            player.WeaponCheckHits++;

            if (player.WeaponCheckLogTick != 0 && now < player.WeaponCheckLogTick + QuietMs)
                return;

            var repeat = player.WeaponCheckHits > 1 ? $" ({player.WeaponCheckHits} since the last of these)" : "";
            var action = finding.Refuse ? "not fired" : "fired (log mode)";

            Logger.WriteLog(LogType.Security,
                $"{player.FamilyName} shot {finding.What} on map {player.MapContextId}{repeat}; {action}.");

            player.WeaponCheckLogTick = now;
            player.WeaponCheckHits = 0;
        }

        /// <summary>
        /// Tells the shooter why a shot was refused, at most once per <see cref="ToldQuietMs"/>.
        /// The client has played the shot already and nothing else says it did not go.
        ///
        ///  - Range: the message an ability out of range gets, PM_TARGET_OUT_OF_RANGE.
        ///  - Facing and sight: PM_FAILED_LINE_OF_SIGHT, "Action failed line of sight check." The
        ///    client has no facing message of its own; its targeted actions show this one for a
        ///    target that is not in front of the player as well as for one out of sight
        ///    (targetedaction.py CheckAction, bFailedFrontal and bFailedLOS).
        ///
        /// Both are log only as shipped, so a server sees these only once it sets them to refuse.
        /// </summary>
        public static void Tell(Client client, Finding finding, long now)
        {
            var player = client?.Player;

            if (player == null || !finding.Refuse)
                return;

            var message = finding.Check switch
            {
                Check.Range => PlayerMessage.PmTargetOutOfRange,
                Check.Facing => PlayerMessage.PmFailedLineOfSight,
                Check.Sight => PlayerMessage.PmFailedLineOfSight,
                _ => (PlayerMessage?)null
            };

            if (message == null)
                return;

            if (player.WeaponRangeToldTick != 0 && now < player.WeaponRangeToldTick + ToldQuietMs)
                return;

            player.WeaponRangeToldTick = now;
            client.CallMethod(SysEntity.CommunicatorId, new DisplayClientMessagePacket(message.Value,
                new Dictionary<string, string>(), MsgFilterId.GeneralSystemMessages));
        }

        private static string Mode(string value)
        {
            return value?.Trim().ToLowerInvariant() switch
            {
                MovementChecksConfig.Refuse => MovementChecksConfig.Refuse,
                MovementChecksConfig.Off => MovementChecksConfig.Off,
                _ => MovementChecksConfig.Log
            };
        }
    }
}
