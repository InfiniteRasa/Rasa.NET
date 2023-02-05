using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{

    using Structures.World;

    public class FootlockerPreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, FootlockerEntry.TableName, typeof(FootlockerEntry));
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { 1, 21030, 1220, 756.3, 294.2, 379.3, 0.0, "Alia Das Footlocker" };
            yield return new object[] { 2, 21030, 1220, -91.5, 221.3, -529.5, 0.0, "Twin Pillars Footlocker" };
            yield return new object[] { 3, 21030, 1148, -128.2, 125.0, 554.1, 0.0, "Foreas Base Footlocker1" };
            yield return new object[] { 4, 21030, 1148, -129.7, 125.0, 522.7, 0.0, "Foreas Base Footlocker 2" };
            yield return new object[] { 5, 21030, 1244, -752.8, 139.0, 612.8, 0.0, "Cumbria Research Footlocker" };
            yield return new object[] { 6, 21030, 1244, -158.3, 172.5, -763.3, 0.0, "Fort Dew Footlocker" };
            yield return new object[] { 7, 21030, 1244, 894.1, 125.1, -245.9, 0.0, "Staging Point Footlocker" };
            yield return new object[] { 8, 21030, 2047, -242.4, 738.2, -666.3, 0.0, "Fort Virgil1 Footlocker" };
            yield return new object[] { 9, 21030, 2047, -271.7, 738.2, -661.08, 0.0, "Fort Virgil 2 Footlocker" };
            yield return new object[] { 10, 21030, 2047, -321.6, 714.0, -647.2, 0.0, "Fort Virgil Underground 1 Footlocker" };
            yield return new object[] { 11, 21030, 2047, -322.1, 714.0, -639.1, 0.0, "Fort Virgil Underground 2 Footlocker" };
            yield return new object[] { 12, 21030, 2051, -549.8, 199.6, -1064.1, 0.0, "Gangus Outpost Tent Footlocker" };
            yield return new object[] { 13, 21030, 2051, -591.4, 200.8, -1008.0, 0.0, "Gangus Outpost Footlocker" };
            yield return new object[] { 14, 21030, 2051, 1023.6, 190.3, -890.1, 0.0, "Dia Uyona Footlocker" };
            yield return new object[] { 15, 21030, 1454, -320.5, 218.3, 103.8, 0.0, "Paludos Footlocker" };
            yield return new object[] { 16, 21030, 1454, -314.8, 218.3, 137.5, 0.0, "Paludos 2 Footlocker" };
            yield return new object[] { 17, 21030, 1497, 30.2, 414.0, 836.3, 0.0, "Fort Defiance Footlocker" };
            yield return new object[] { 18, 21030, 1304, -961.0, 919.2, 470.4, 0.0, "The Snake Pit Footlocker" };
            yield return new object[] { 19, 21030, 1304, 1031.4, 656.5, 375.3, 3.7, "Retread Outpost Footlocker" };
            yield return new object[] { 20, 21030, 1734, -84.8, 297.0, -792.7, 4.68, "Shadows Edge 1 Footlocker" };
            yield return new object[] { 21, 21030, 1734, -171.3, 297.0, -790.9, 4.68, "Shadows Edge 2 Footlocker" };
            yield return new object[] { 22, 21030, 1993, 1019.8, 144.5, 64.9, 0.0, "Outpost Intrepid Footlocker" };
            yield return new object[] { 23, 21030, 1993, -921.2, 309.5, 240.3, 0.0, "Awol Camp Footlocker" };
            yield return new object[] { 24, 21030, 1911, 446.3, 687.2, 340.02, 0.0, "Thunderhead Base Footlocker" };
            yield return new object[] { 25, 21030, 2028, -402.3, 534.5, -342.4, 0.0, "Tantulas Base Footlocker South" };
            yield return new object[] { 26, 21030, 2028, -468.9, 538.8, -26.9, 0.0, "Tantulas Base Footlocker North" };
            yield return new object[] { 27, 21030, 2028, -600.1, 536.5, -107.6, 0.0, "Penumbra Footlocker" };
            yield return new object[] { 28, 21030, 1761, 346.7, 260.1, 13.5, 0.0, "Ortho Footlocker" };
            yield return new object[] { 29, 21030, 1761, 314.2, 260.2, 24.6, 0.0, "Ortho Footlocker" };
            yield return new object[] { 30, 21030, 1761, 136.2, 232.2, -296.5, 0.0, "Plains Post Footlocker" };
            yield return new object[] { 31, 21030, 1759, -627.4, 228.5, -513.6, 0.0, "Baylor Base Footlocker" };
            yield return new object[] { 32, 21030, 1764, -601.5, 439.0, 421.6, 0.0, "MT Hellas Outpost Footlocker" };
            yield return new object[] { 33, 21030, 2375, 149.56, 209.5, -157.3, 0.0, "AFSOutpost Lexington Footlocker" };
            yield return new object[] { 34, 10000063, 1454, -316.67656, 218.30469, 111.66, 2.89, "Clan Lockbox Paludos" };
        }
    }
}
