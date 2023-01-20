using System.Collections.Generic;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    using Services.Preloader;
    using Structures.World;
    public partial class Add_data_to_world : Migration
    {
        private readonly ICollection<IPreloader> _preloaders = new List<IPreloader>
        {
            new CreatureActionPreloader(),
            new CreatureAppearancePreloader(),
            new CreaturePreloader(),
            new CreatureStatsPreloader(),
            new EntityClassPreloader(),
            new EquipableClassPreloader(),
            new FootlockerPreloader(),
            new ItemClassPrelaoder(),
            new ItemTemplateArmorPrelaoder(),
            new ItemTemplatePreloader(),
            new ItemTemplateRequirementPreloader(),
            new ItemTemplateRequirementRasePreloader(),
            new ItemTemplateRequirementSkillPreloader(),
            new ItemTemplateResistancePreloader(),
            new ItemTemplateWeaponPreloader(),
            new LogosPreloader(),
            new MapInfoPreloader(),
            new NpcMissionPreloader(),
            new NpcPackagePrelader(),
            new SpawnpoolPreloader(),
            new TeleporterPreloader(),
            new VendorItemPreloader(),
            new VendorPreloader(),
            new WeaponClassPreloader()
        };

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            ArmorClassPreloader.Insert(migrationBuilder);
            foreach (var preloader in _preloaders)
            {
                preloader.Preload(migrationBuilder);
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            const string deleteTemplate = "delete from `{0}`;";
            migrationBuilder.Sql(string.Format(deleteTemplate, ArmorClassEntry.TableName));
            migrationBuilder.Sql(string.Format(deleteTemplate, CreatureActionEntry.TableName));
            migrationBuilder.Sql(string.Format(deleteTemplate, CreatureAppearanceEntry.TableName));
            migrationBuilder.Sql(string.Format(deleteTemplate, CreatureEntry.TableName));
            migrationBuilder.Sql(string.Format(deleteTemplate, CreatureStatEntry.TableName));
            migrationBuilder.Sql(string.Format(deleteTemplate, EntityClassEntry.TableName));
            migrationBuilder.Sql(string.Format(deleteTemplate, EquipableClassEntry.TableName));
            migrationBuilder.Sql(string.Format(deleteTemplate, ExperienceForLevelEntry.TableName));
            migrationBuilder.Sql(string.Format(deleteTemplate, FootlockerEntry.TableName));
            migrationBuilder.Sql(string.Format(deleteTemplate, ItemClassEntry.TableName));
            migrationBuilder.Sql(string.Format(deleteTemplate, ItemTemplateArmorEntry.TableName));
            migrationBuilder.Sql(string.Format(deleteTemplate, ItemTemplateItemClassEntry.TableName));
            migrationBuilder.Sql(string.Format(deleteTemplate, ItemTemplateEntry.TableName));
            migrationBuilder.Sql(string.Format(deleteTemplate, ItemTemplateRequirementEntry.TableName));
            migrationBuilder.Sql(string.Format(deleteTemplate, ItemTemplateRequirementRaceEntry.TableName));
            migrationBuilder.Sql(string.Format(deleteTemplate, ItemTemplateRequirementSkillEntry.TableName));
            migrationBuilder.Sql(string.Format(deleteTemplate, ItemTemplateResistanceEntry.TableName));
            migrationBuilder.Sql(string.Format(deleteTemplate, ItemTemplateWeaponEntry.TableName));
            migrationBuilder.Sql(string.Format(deleteTemplate, LogosEntry.TableName));
            migrationBuilder.Sql(string.Format(deleteTemplate, MapInfoEntry.TableName));
            migrationBuilder.Sql(string.Format(deleteTemplate, NpcMissionEntry.TableName));
            migrationBuilder.Sql(string.Format(deleteTemplate, NpcPackageEntry.TableName));
            migrationBuilder.Sql(string.Format(deleteTemplate, RandomNameEntry.TableName));
            migrationBuilder.Sql(string.Format(deleteTemplate, SpawnPoolEntry.TableName));
            migrationBuilder.Sql(string.Format(deleteTemplate, TeleporterEntry.TableName));
            migrationBuilder.Sql(string.Format(deleteTemplate, VendorEntry.TableName));
            migrationBuilder.Sql(string.Format(deleteTemplate, VendorItemEntry.TableName));
            migrationBuilder.Sql(string.Format(deleteTemplate, WeaponClassEntry.TableName));
        }
    }
}
