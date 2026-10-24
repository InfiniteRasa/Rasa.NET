using System;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    public partial class ChatCommandsManager
    {
        /// <summary>
        /// .cpdef: a control point's defences (ControlPointFields, Defenders, Emplacements), to look at and to test.
        ///   .cpdef                  - the nearest point on your map: its holder, its gate shields, its garrisons
        ///   .cpdef &lt;id&gt;             - the same for that point
        ///   .cpdef &lt;id&gt; break [row]  - its shields (or the one of that world_destructible row) brought down
        ///   .cpdef &lt;id&gt; damage &lt;hp&gt;  - each of its standing shields takes that much
        ///   .cpdef &lt;id&gt; repair       - every one of its shields whole again, now
        ///   .cpdef config           - the ControlPointDefence settings in force
        /// </summary>
        private void ControlPointDefenceCommand(string[] parts)
        {
            const string usage = "usage: .cpdef | .cpdef <id> | .cpdef <id> break [row] | .cpdef <id> damage <hp> | .cpdef <id> repair | .cpdef config";
            var client = _client;
            var player = client.Player;
            var points = ControlPoints.Instance;

            void Say(string text) => CommunicatorManager.Instance.SystemMessage(client, text);

            if (parts.Length == 2 && parts[1] == "config")
            {
                var c = ControlPointFields.Config;

                Say($"shields: follow owner {c.FieldsFollowOwner}, rebuild {c.FieldRebuildSeconds} s, recharge after {c.FieldRechargeAfterSeconds} s over {c.FieldRechargeFullSeconds} s, " +
                    $"creatures strike {c.CreaturesStrikeFields}, alert {c.FieldAlertRange} m");
                Say($"defenders {c.Defenders}: notice {c.DefenderNoticeRange}-{c.DefenderNoticeMax} m (reach + {c.DefenderNoticeMargin}), turrets up to {c.TurretNoticeMax} m, " +
                    $"call {c.CallRange} m, hold post + {c.HoldPostMargin} m");
                Say($"turret bursts {c.TurretBursts}: {string.Join(" or ", c.BurstSizes is { Length: > 0 } ? c.BurstSizes : Emplacements.DefaultBurstSizes)} rounds {c.BurstGapMs} ms apart");
                return;
            }

            ControlPoints.Point point;

            if (parts.Length == 1)
            {
                point = points.OnMap(player.MapContextId).Where(p => !p.IsBattleground)
                    .OrderBy(p => Vector3.Distance(p.Object?.Position ?? p.Position, player.Position)).FirstOrDefault();

                if (point == null)
                {
                    Say($"No control points on map {player.MapContextId}.");
                    return;
                }
            }
            else if (!uint.TryParse(parts[1], out var id) || (point = points.ById(id)) == null)
            {
                Say(usage);
                return;
            }

            var fields = ControlPointFields.Of(point);

            if (parts.Length <= 2)
            {
                var map = point.Map;
                var pools = map?.SpawnPools?.Where(p => p != null).ToList();
                var afs = pools?.Where(p => point.AfsPools.Contains(p.DbId)).ToList();
                var bane = pools?.Where(p => point.BanePools.Contains(p.DbId)).ToList();

                Say($"#{point.Id} {point.Name}: {points.HolderName(point)}, {Vector3.Distance(point.Object?.Position ?? point.Position, player.Position):0.#} m; " +
                    $"{fields.Count(f => !f.IsDestroyed)} of {fields.Count} gate shield(s) up" +
                    (afs == null ? ", not placed" : $"; AFS garrison {afs.Sum(p => p.AliveCreatures)} alive in {afs.Count} pools, Bane {bane.Sum(p => p.AliveCreatures)} in {bane.Count}"));

                foreach (var field in fields)
                {
                    var row = PlacedDestructibles.LiveOf(field.Object)?.Placement.Id ?? 0;
                    var rebuild = field.IsDestroyed ? $", back in {ControlPointFields.RebuildsInMs(field) / 1000.0:0} s" : "";

                    Say($"  shield #{field.Id} (row {row}) {field.Class.Key}: side {field.Side}, {field.Health}/{field.MaxHealth} hp, {ForceFields.StateOf(field)}{rebuild}, " +
                        $"{Vector3.Distance(field.Position, player.Position):0.#} m");
                }

                return;
            }

            switch (parts[2])
            {
                case "break":
                {
                    uint row = 0;

                    if (parts.Length > 3 && !uint.TryParse(parts[3], out row))
                    {
                        Say(usage);
                        return;
                    }

                    var broken = 0;

                    foreach (var field in fields.Where(f => !f.IsDestroyed))
                        if (row == 0 || PlacedDestructibles.LiveOf(field.Object)?.Placement.Id == row)
                            broken += ForceFields.Damage(field, field.Health, player.EntityId) > 0 ? 1 : 0;

                    Say($"#{point.Id} {point.Name}: {broken} shield(s) brought down.");
                    return;
                }

                case "damage" when parts.Length == 4 && int.TryParse(parts[3], out var amount) && amount > 0:
                {
                    var taken = fields.Where(f => !f.IsDestroyed).Sum(f => ForceFields.Damage(f, amount, player.EntityId));

                    Say($"#{point.Id} {point.Name}: its shields took {taken} hp.");
                    return;
                }

                case "repair":
                {
                    foreach (var field in fields)
                    {
                        var live = PlacedDestructibles.LiveOf(field.Object);

                        if (field.IsDestroyed && live != null)
                            PlacedDestructibles.Respawn(live);
                        else
                            ForceFields.Repair(field, player.EntityId);
                    }

                    Say($"#{point.Id} {point.Name}: {fields.Count} shield(s) whole.");
                    return;
                }

                default:
                    Say(usage);
                    return;
            }
        }
    }
}
