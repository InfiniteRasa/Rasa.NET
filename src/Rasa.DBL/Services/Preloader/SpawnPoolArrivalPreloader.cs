using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// 62 Bane arrival points for 13 camps and 22 squads: 22 landing pads, 7 dropship bays, 33
    /// teleporters. Kind 1 lands the Bane dropship there, kind 2 switches the map's teleporter
    /// (entity_id) on and brings the creatures through it; a pool with several uses one at random.
    /// The position is the walkable surface at the point (the pad's deck, the bay's floor, the
    /// teleporter's plate) from the map's navmesh. bane_arrivals.csv has every point, used or not.
    /// </summary>
    public class SpawnPoolArrivalPreloader : PreloaderBase, IPreloader
    {
        private static readonly string[] Columns =
        {
            "id", "pool_id", "kind", "pos_x", "pos_y", "pos_z", "rotation", "entity_id", "comment"
        };

        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, SpawnPoolArrivalEntry.TableName, Columns);
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { 1, 531032, 1, -101.9800f, 286.0500f, 117.8600f, -1.1011f, 0UL, "arieki_ligo_ashendesert pad" };
            yield return new object[] { 2, 531032, 1, -50.4700f, 284.3500f, 128.1000f, 0.7854f, 0UL, "arieki_ligo_ashendesert pad" };
            yield return new object[] { 3, 531032, 1, -47.8900f, 286.0500f, 172.8900f, 0.8607f, 0UL, "arieki_ligo_ashendesert pad" };
            yield return new object[] { 4, 531085, 1, -360.0000f, 173.6300f, -288.0000f, -1.5708f, 0UL, "arieki_ligo_ashendesert_indracaverns pad" };
            yield return new object[] { 5, 531137, 1, 453.0000f, 470.4000f, 318.0000f, -1.5708f, 0UL, "foreas_valverde_descent pad" };
            yield return new object[] { 6, 531206, 1, 945.3000f, 252.2000f, 721.7000f, -1.5708f, 0UL, "foreas_howlingmaw1 pad" };
            yield return new object[] { 7, 531233, 2, -444.1700f, 251.1600f, -32.0500f, -2.3562f, 0x7A4100004B11UL, "arieki_torden_incline teleporter" };
            yield return new object[] { 8, 531242, 1, 186.4900f, 281.7900f, 353.7200f, 3.1416f, 0UL, "arieki_torden_incline bay" };
            yield return new object[] { 9, 531430, 1, 191.2500f, 109.2900f, -37.2500f, -2.3562f, 0UL, "arieki_torden_mires_banefluxitemines pad" };
            yield return new object[] { 10, 531430, 1, 217.5000f, 109.2500f, -72.7500f, -1.5708f, 0UL, "arieki_torden_mires_banefluxitemines pad" };
            yield return new object[] { 11, 531507, 2, 302.8200f, 130.0000f, 505.3800f, -1.5708f, 0x79B600001D0EUL, "foreas_concordia_palisades_devilsden teleporter" };
            yield return new object[] { 12, 531507, 2, 302.8700f, 129.8200f, 438.4000f, 1.6132f, 0x79B600001D0FUL, "foreas_concordia_palisades_devilsden teleporter" };
            yield return new object[] { 13, 531507, 2, 316.7900f, 130.0700f, 477.8100f, -2.3562f, 0x79B600001D11UL, "foreas_concordia_palisades_devilsden teleporter" };
            yield return new object[] { 14, 531507, 2, 317.0500f, 129.9900f, 463.1100f, 2.1085f, 0x79B600001D10UL, "foreas_concordia_palisades_devilsden teleporter" };
            yield return new object[] { 15, 531653, 1, 227.8800f, 177.3100f, 117.9300f, -1.2873f, 0UL, "foreas_valverde_plateau_ustoryard pad" };
            yield return new object[] { 16, 531694, 1, 476.0000f, 432.6000f, 533.0000f, 3.1416f, 0UL, "arieki_ligo_thunderhead pad" };
            yield return new object[] { 17, 531698, 1, 91.0000f, 379.4000f, 123.0000f, 1.5708f, 0UL, "arieki_ligo_thunderhead pad" };
            yield return new object[] { 18, 531750, 1, 63.4000f, 12.7700f, 56.8000f, 1.0432f, 0UL, "arieki_ligo_thunderhead_quassostation pad" };
            yield return new object[] { 19, 531760, 1, -36.9500f, 12.7700f, 50.4800f, 1.5183f, 0UL, "arieki_ligo_thunderhead_quassostation pad" };
            yield return new object[] { 20, 531760, 1, 8.4000f, 12.9000f, 13.4000f, -2.0733f, 0UL, "arieki_ligo_thunderhead_quassostation pad" };
            yield return new object[] { 21, 560001, 2, 0.0000f, 378.3800f, 134.0000f, 1.5708f, 0x7A4100000790UL, "arieki_ligo_thunderhead teleporter" };
            yield return new object[] { 22, 560002, 1, -186.0000f, 427.6000f, 639.0000f, 3.1416f, 0UL, "arieki_ligo_thunderhead bay" };
            yield return new object[] { 23, 560003, 1, 364.0000f, 429.6000f, 704.0000f, 3.1416f, 0UL, "arieki_torden_plains bay" };
            yield return new object[] { 24, 560004, 1, 100.9000f, 107.7700f, -648.7000f, -1.3823f, 0UL, "foreas_concordia_divide bay" };
            yield return new object[] { 25, 560005, 1, -54.5000f, 107.2000f, -643.4000f, -0.2513f, 0UL, "foreas_concordia_divide bay" };
            yield return new object[] { 26, 560006, 2, 186.8300f, 4.6200f, -22.6400f, 1.3129f, 0x79870000130AUL, "foreas_concordia_divide_minoscaverns teleporter" };
            yield return new object[] { 27, 560006, 2, 215.7200f, 12.0700f, -64.0300f, -0.0000f, 0x798700001303UL, "foreas_concordia_divide_minoscaverns teleporter" };
            yield return new object[] { 28, 560006, 2, 224.5000f, 12.0700f, -91.7500f, 3.1416f, 0x7987000011D3UL, "foreas_concordia_divide_minoscaverns teleporter" };
            yield return new object[] { 29, 560006, 2, 224.2700f, 12.0000f, -100.1900f, 2.8849f, 0x7987000011D4UL, "foreas_concordia_divide_minoscaverns teleporter" };
            yield return new object[] { 30, 560006, 2, 224.8000f, 12.0400f, -64.0000f, 3.1416f, 0x798700001304UL, "foreas_concordia_divide_minoscaverns teleporter" };
            yield return new object[] { 31, 560007, 2, 156.0100f, 4.0700f, 152.0700f, -1.5929f, 0x7987000011F5UL, "foreas_concordia_divide_minoscaverns teleporter" };
            yield return new object[] { 32, 560008, 2, 158.1100f, 4.6200f, 58.7600f, -3.0776f, 0x798700001365UL, "foreas_concordia_divide_minoscaverns teleporter" };
            yield return new object[] { 33, 560008, 2, 158.3200f, 4.6300f, 39.1200f, 3.1195f, 0x798700001364UL, "foreas_concordia_divide_minoscaverns teleporter" };
            yield return new object[] { 34, 560008, 2, 178.9700f, 3.8600f, 63.9000f, 0.1139f, 0x7987000012FBUL, "foreas_concordia_divide_minoscaverns teleporter" };
            yield return new object[] { 35, 560008, 2, 213.3000f, 2.5200f, 117.6100f, -2.6038f, 0x79870000131CUL, "foreas_concordia_divide_minoscaverns teleporter" };
            yield return new object[] { 36, 560008, 2, 213.7700f, 2.4700f, 106.5400f, 2.6506f, 0x79870000131BUL, "foreas_concordia_divide_minoscaverns teleporter" };
            yield return new object[] { 37, 560009, 1, 754.2500f, 134.5000f, -675.7500f, -2.3562f, 0UL, "foreas_concordia_palisades pad" };
            yield return new object[] { 38, 560009, 1, 763.2500f, 134.5200f, -640.5000f, -1.5708f, 0UL, "foreas_concordia_palisades pad" };
            yield return new object[] { 39, 560010, 2, 158.2400f, 144.9000f, 699.9100f, -0.0148f, 0x79B600001E0DUL, "foreas_concordia_palisades_devilsden teleporter" };
            yield return new object[] { 40, 560010, 2, 177.9700f, 144.9900f, 680.4000f, 1.5708f, 0x79B600001E0CUL, "foreas_concordia_palisades_devilsden teleporter" };
            yield return new object[] { 41, 560010, 2, 178.0300f, 144.9900f, 719.6000f, -1.5356f, 0x79B600001E0EUL, "foreas_concordia_palisades_devilsden teleporter" };
            yield return new object[] { 42, 560010, 2, 194.9700f, 151.7900f, 655.7500f, -0.1279f, 0x79B6000020E3UL, "foreas_concordia_palisades_devilsden teleporter" };
            yield return new object[] { 43, 560010, 2, 220.3700f, 144.5500f, 704.5300f, -1.6869f, 0x79B6000020FCUL, "foreas_concordia_palisades_devilsden teleporter" };
            yield return new object[] { 44, 560010, 2, 224.8000f, 144.5700f, 694.1700f, -0.1946f, 0x79B600001EAEUL, "foreas_concordia_palisades_devilsden teleporter" };
            yield return new object[] { 45, 560010, 2, 230.2400f, 144.4200f, 681.8100f, 0.0504f, 0x79B6000020FDUL, "foreas_concordia_palisades_devilsden teleporter" };
            yield return new object[] { 46, 560010, 2, 236.9000f, 144.4200f, 689.8500f, -2.3843f, 0x79B6000020F9UL, "foreas_concordia_palisades_devilsden teleporter" };
            yield return new object[] { 47, 560011, 2, -107.9700f, 184.8200f, 509.0400f, -0.6283f, 0x79B600001FA5UL, "foreas_concordia_palisades_devilsden teleporter" };
            yield return new object[] { 48, 560011, 2, -82.0600f, 182.7800f, 500.2900f, 3.1324f, 0x79B600001FA8UL, "foreas_concordia_palisades_devilsden teleporter" };
            yield return new object[] { 49, 560012, 1, 134.0000f, 118.8000f, 112.0000f, 3.1416f, 0UL, "foreas_concordia_palisades_treebackcamp bay" };
            yield return new object[] { 50, 560013, 1, 52.2000f, 39.1400f, -193.4000f, 3.1416f, 0UL, "foreas_concordia_wilderness_pravusresearch bay" };
            yield return new object[] { 51, 560014, 2, 144.0000f, 10.5200f, 184.5600f, -0.0000f, 0x79DB0000C1A6UL, "foreas_concordia_wilderness_pravusresearch teleporter" };
            yield return new object[] { 52, 560014, 2, 144.2200f, 10.5200f, 151.4000f, -0.0000f, 0x79DB0000C1A5UL, "foreas_concordia_wilderness_pravusresearch teleporter" };
            yield return new object[] { 53, 560014, 2, 184.2600f, 8.8200f, 128.1000f, 0.7854f, 0x79DB0000C2DAUL, "foreas_concordia_wilderness_pravusresearch teleporter" };
            yield return new object[] { 54, 560014, 2, 184.1600f, 8.7400f, 207.7700f, -0.7869f, 0x79DB0000C2DDUL, "foreas_concordia_wilderness_pravusresearch teleporter" };
            yield return new object[] { 55, 560015, 2, 263.7700f, 8.7400f, 128.2900f, 2.3562f, 0x79DB0000C2DBUL, "foreas_concordia_wilderness_pravusresearch teleporter" };
            yield return new object[] { 56, 560016, 2, 263.7700f, 8.7400f, 207.8400f, -2.3577f, 0x79DB0000C2DCUL, "foreas_concordia_wilderness_pravusresearch teleporter" };
            yield return new object[] { 57, 560017, 1, -562.7400f, 210.0000f, 313.1300f, 3.1416f, 0UL, "foreas_howlingmaw1 pad" };
            yield return new object[] { 58, 560018, 1, -5.5000f, 209.5400f, -425.5000f, 3.1416f, 0UL, "foreas_howlingmaw_cuthahbase pad" };
            yield return new object[] { 59, 560019, 1, 54.5000f, 62.7700f, 291.0000f, 1.5708f, 0UL, "foreas_valverde_marshes_villageruins pad" };
            yield return new object[] { 60, 560020, 1, -8.7500f, 62.7700f, 291.0000f, -1.5708f, 0UL, "foreas_valverde_marshes_villageruins pad" };
            yield return new object[] { 61, 560021, 1, 348.7900f, 285.2000f, -230.0500f, 3.1416f, 0UL, "foreas_valverde_plateau pad" };
            yield return new object[] { 62, 560022, 1, 160.3100f, 176.7000f, -44.3800f, 1.0689f, 0UL, "foreas_valverde_plateau_ustoryard pad" };
        }
    }
}
