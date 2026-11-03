using System.Collections.Generic;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// The Devil's Den's three local teleporters, linked as the walkthrough has them
    /// (Devils_den_teleporters). Each had a row of type 1 and no local_teleporter_link
    /// (LocalTeleporterLinks: the client's markers name each pad by where it stands, so they do
    /// not say where it goes), and took nobody anywhere.
    ///
    /// Ten Ton Hammer's guide to the Devil's Den says where they go:
    ///  - "You'll notice a transporter behind Caritas, this will take you from the first room in
    ///    the instance to this point if you die": the pad in the entrance chamber, Fithik
    ///    Hatchery (567), to the one in Caritas's cell, Bane Detention Center (568). The same
    ///    guide's "teleporter to the left [of the entrance room] ... a room with a lot of angry
    ///    Bane and no way out, so skip it for now" is that pad before the detention center's
    ///    laser door is down.
    ///  - In the building behind the howitzer bunkers, "the first door will lead to a teleporter,
    ///    which will take you back to the room where Caritas was": Bane Comm Tower (569) to the
    ///    detention center. The tower is a floor of its own past the Shard Impact Site, and this
    ///    is its way back.
    ///  - "Head back to the teleporter and return to Walk of Giants": the detention center's pad
    ///    back to the entrance chamber, by the way out to the Palisades.
    /// So 567 and 568 are linked both ways, and 569 to 568 alone; nothing goes to the tower.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. A link goes in only where both its ends are
    /// there as local teleporters on the Den and its pad has none yet; Down takes out these links
    /// alone.
    /// </summary>
    public static class DevilsDenTeleporters
    {
        public const uint MapContextId = 1394;          // adv_foreas_concordia_palisades_devilsden
        public const uint LocalTeleporterType = 1;
        public const uint FithikHatchery = 567, DetentionCenter = 568, CommTower = 569;

        /// <summary>Every link, from a pad to the pad it takes whoever steps onto it to.</summary>
        public static readonly (uint From, uint To)[] Links =
        {
            (FithikHatchery, DetentionCenter),
            (DetentionCenter, FithikHatchery),
            (CommTower, DetentionCenter),
        };

        public static IEnumerable<string> Up()
        {
            foreach (var (from, to) in Links)
                yield return $"INSERT INTO {LocalTeleporterLinkEntry.TableName} (from_teleporter_id, to_teleporter_id) " +
                    $"SELECT {from}, {to} FROM teleporter a, teleporter b " +
                    $"WHERE a.id = {from} AND a.type = {LocalTeleporterType} AND a.map_context_id = {MapContextId} " +
                    $"AND b.id = {to} AND b.type = {LocalTeleporterType} AND b.map_context_id = {MapContextId} " +
                    $"AND NOT EXISTS (SELECT 1 FROM {LocalTeleporterLinkEntry.TableName} x WHERE x.from_teleporter_id = {from});";
        }

        public static IEnumerable<string> Down()
        {
            foreach (var (from, to) in Links)
                yield return $"DELETE FROM {LocalTeleporterLinkEntry.TableName} WHERE from_teleporter_id = {from} AND to_teleporter_id = {to};";
        }
    }
}
