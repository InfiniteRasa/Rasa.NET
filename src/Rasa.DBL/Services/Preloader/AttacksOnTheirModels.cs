using System.Collections.Generic;
using System.Globalization;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Attacks that the creature's model had no animation for (the client's animationdata, as
    /// CreatureAttackAnimationTests reads it), put on ones it has:
    ///  - Magma Caverns' Ember and Quicksilver Magmonix: Creature_Magmonix and
    ///    Creature_Magmonix_Boss are mesh 17734, on the Treeback's skeleton (13975), which
    ///    animates the Treeback's howl (192/1) and stomp (190/1) and no weapon attack. Their
    ///    slams (1/1) become the howl, and Ember's Flaregasher fireball (237/1) the stomp, at
    ///    their damage, in fire.
    ///  - Magma Caverns' Pacified Thrax (Thrax soldier, skeleton 13999): the grenadier's 1/79
    ///    plays family 512, which that skeleton lacks; it fires the Thrax soldier's own 1/1, at
    ///    the grenadier's Crucible damage.
    ///  - Queen Kardash (Creature_Atta_Queen_Boss, mesh 17409, skeleton 13916): the harvester's
    ///    acid spit (475/1) plays family 1378, which the Queen lacks; it becomes the Atta's
    ///    melee 174/25 (family 875), at the spit's damage. Her rock throw (292/1) she animates.
    /// </summary>
    public static class AttacksOnTheirModels
    {
        public const uint EmberHowl = MagmaCavernsContent.EmberSlam, MagmonixHowl = MagmaCavernsContent.MagmonixSlam;
        public const uint EmberStomp = 678106, SubjectRifle = 678107;
        public const uint QueenSpit = 781113;

        /// <summary>An action row as it is and as it was: id, description, action, arg, range, cooldown, windup, damage, type.</summary>
        public sealed record Row(uint Id, string Description, uint ActionId, uint ArgId, double RangeMin, double RangeMax,
            uint Cooldown, uint Windup, uint MinDamage, uint MaxDamage, uint DamageType);

        /// <summary>The rows changed in place: (now, before).</summary>
        public static readonly (Row Now, Row Was)[] Changed =
        {
            (new(EmberHowl, "Ember - Magma Caverns lava howl 192/1", 192, 1, 1, 40, 4000, 1000, 1500, 2200, 2),
             new(EmberHowl, "Ember - Magma Caverns slam 1/1", 1, 1, 0.5, 6, 2500, 0, 1500, 2200, 1)),
            (new(MagmonixHowl, "Magmonix - Magma Caverns lava howl 192/1", 192, 1, 1, 40, 4000, 1000, 600, 850, 2),
             new(MagmonixHowl, "Magmonix - Magma Caverns slam 1/1", 1, 1, 0.5, 4, 1600, 0, 600, 850, 1)),
            (new(QueenSpit, "Queen Kardash claw 174/25", 174, 25, 0.5, 4, 2500, 0, 86, 120, 4),
             new(QueenSpit, "Queen Kardash acid spit", 475, 1, 1, 30, 4000, 0, 86, 120, 4))
        };

        /// <summary>The new rows.</summary>
        public static readonly Row[] Added =
        {
            new(EmberStomp, "Ember - Magma Caverns lava stomp 190/1", 190, 1, 1, 20, 30000, 3299, 2400, 3300, 2),
            new(SubjectRifle, "Pacified Thrax - Magma Caverns weapon 1/1", 1, 1, 1, 20, 800, 0, 320, 480, 1)
        };

        /// <summary>Which creature's slot takes a new row, and what it had.</summary>
        public static readonly (uint Creature, string Slot, uint Now, uint Was)[] Slots =
        {
            (MagmaCavernsContent.Ember, "action2", EmberStomp, MagmaCavernsContent.FlaregasherFireball),
            (MagmaCavernsContent.PacifiedThrax, "action1", SubjectRifle, MagmaCavernsContent.GrenadierRifle)
        };

        public static IEnumerable<string> Up()
        {
            foreach (var a in Added)
                yield return "INSERT INTO creature_action (id, description, action_id, action_arg_id, range_min, range_max, cooldown, windup, min_damage, max_damage, damage_type) " +
                    $"SELECT {a.Id}, {S(a.Description)}, {a.ActionId}, {a.ArgId}, {N(a.RangeMin)}, {N(a.RangeMax)}, {a.Cooldown}, {a.Windup}, {a.MinDamage}, {a.MaxDamage}, {a.DamageType} " +
                    $"FROM creature c WHERE c.id = {MagmaCavernsContent.Ember} AND NOT EXISTS (SELECT 1 FROM creature_action x WHERE x.id = {a.Id});";

            foreach (var (now, was) in Changed)
                yield return Set(now, was);

            foreach (var (creature, slot, now, was) in Slots)
                yield return $"UPDATE creature SET {slot} = {now} WHERE id = {creature} AND {slot} = {was} " +
                    $"AND EXISTS (SELECT 1 FROM creature_action a WHERE a.id = {now});";
        }

        public static IEnumerable<string> Down()
        {
            foreach (var (creature, slot, now, was) in Slots)
                yield return $"UPDATE creature SET {slot} = {was} WHERE id = {creature} AND {slot} = {now};";

            foreach (var (now, was) in Changed)
                yield return Set(was, now);

            foreach (var a in Added)
                yield return $"DELETE FROM creature_action WHERE id = {a.Id} AND action_id = {a.ActionId} " +
                    $"AND NOT EXISTS (SELECT 1 FROM creature c WHERE {a.Id} IN (c.action1, c.action2, c.action3, c.action4, c.action5, c.action6, c.action7, c.action8));";
        }

        /// <summary>The row made <paramref name="to"/>, where it is still <paramref name="from"/>.</summary>
        private static string Set(Row to, Row from) =>
            $"UPDATE creature_action SET description = {S(to.Description)}, action_id = {to.ActionId}, action_arg_id = {to.ArgId}, " +
            $"range_min = {N(to.RangeMin)}, range_max = {N(to.RangeMax)}, cooldown = {to.Cooldown}, windup = {to.Windup}, " +
            $"min_damage = {to.MinDamage}, max_damage = {to.MaxDamage}, damage_type = {to.DamageType} " +
            $"WHERE id = {to.Id} AND action_id = {from.ActionId} AND action_arg_id = {from.ArgId};";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
        private static string S(string value) => "'" + value.Replace("'", "''") + "'";
    }
}
