using System.Collections.Generic;
using System.Globalization;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Map link 41, the Valverde Marshes door to the Bane Supply Depot (MapLinkPreloader, 1454 to 1451), put the player at
    /// (-370.5371, 149.7653, 3.1254): the trigger point of link 39, the depot's own way out (6 m radius), 6.5 m behind the
    /// door frame, where the client has no floor. The player fell; the safety floor put them back inside link 39's
    /// circle, and link 39 sent them back to the Marshes (missions 663, 664 and 919 need the depot).
    ///
    /// The arrival moves 14.5 m into the depot, to (-356, 149.61, 3.1254): on the depot's navmesh and on the client's
    /// collision floor (149.61), outside link 39's circle. Height and rotation of the arrival are otherwise kept. The spot is
    /// ours (ESTIMATE), measured on the client floor and the navmesh.
    ///
    /// Data only, plain SQL valid on SQLite and MySQL. Up changes the row only where it still has the old arrival, so a GM
    /// edit is left alone and a second Up changes nothing; Down puts back only the arrival Up wrote.
    /// </summary>
    public static class BaneSupplyDepotDoorArrival
    {
        public const uint LinkId = 41;
        public const uint MapContextId = 1454;      // adv_foreas_valverde_marshes
        public const uint DestMapContextId = 1451;  // adv_foreas_valverde_marshes_banesupplydepot

        public const double X = -356.0, Y = 149.61, Z = 3.1254;
        public const double OldX = -370.5371, OldY = 149.7653;

        public static IEnumerable<string> Up()
        {
            yield return Move(OldX, OldY, X, Y);
        }

        public static IEnumerable<string> Down()
        {
            yield return Move(X, Y, OldX, OldY);
        }

        private static string Move(double fromX, double fromY, double toX, double toY) =>
            $"UPDATE map_link SET dest_pos_x = {F(toX)}, dest_pos_y = {F(toY)} " +
            $"WHERE id = {LinkId} AND map_context_id = {MapContextId} AND dest_map_context_id = {DestMapContextId} " +
            $"AND ABS(dest_pos_x - ({F(fromX)})) < 0.001 AND ABS(dest_pos_y - ({F(fromY)})) < 0.001 " +
            $"AND ABS(dest_pos_z - ({F(Z)})) < 0.001;";

        private static string F(double value) => value.ToString("0.####", CultureInfo.InvariantCulture);
    }
}
