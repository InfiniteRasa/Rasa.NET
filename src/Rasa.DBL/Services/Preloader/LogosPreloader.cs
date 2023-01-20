using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{

    using Structures.World;

    public class LogosPreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, LogosEntry.TableName, typeof(LogosEntry));
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { 1, 7361, 1220, 363.872, 214.211, 619.304, "Area" };
            yield return new object[] { 2, 9538, 1220, 41.0646, 193.485, -332.566, "Attack" };
            yield return new object[] { 6, 7290, 1220, 406, 246.168, 441, "Damage" };
            yield return new object[] { 9, 7292, 1220, -436, 188.325, -620, "Enemy" };
            yield return new object[] { 10, 7364, 1220, 832, 161.838, 960, "Enchance" };
            yield return new object[] { 23, 7302, 1220, 493.078, 289.531, 319.682, "Power" };
            yield return new object[] { 24, 7367, 1220, 187.877, 174.58, 255.913, "Projectile" };
            yield return new object[] { 28, 12698, 1220, 795.107, 311.797, 70.8783, "Time" };
            yield return new object[] { 38, 12693, 1220, -448.368, 164.678, -108.112, "Self" };
            yield return new object[] { 49, 12697, 1220, -906.358, 190.186, -649.29, "Target" };
            yield return new object[] { 53, 12684, 1220, -979, 286.096, 744, "Here" };
            yield return new object[] { 56, 12689, 1220, -110.341, 219.371, 111.142, "Mind" };
            yield return new object[] { 408, 30408, 1220, -828.544, 143, -738.22, "Earth" };
        }
    }
}