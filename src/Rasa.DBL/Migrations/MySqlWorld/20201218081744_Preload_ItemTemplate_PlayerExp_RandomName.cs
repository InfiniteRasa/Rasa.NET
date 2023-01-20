using System.Collections.Generic;

using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.MySqlWorld
{
    using Services.Preloader;
    using Structures.World;
    
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Preload_ItemTemplate_PlayerExp_RandomName : Migration
    {
        private readonly ICollection<IPreloader> _preloaders = new List<IPreloader>
        {
            new ItemTemplateItemClassPreloader(),
            new ExperienceForLevelPreloader(),
            new RandomNamePreloader()
        };

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var preloader in _preloaders)
            {
                preloader.Preload(migrationBuilder);
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            const string deleteTemplate = "delete from `{0}`;";
            migrationBuilder.Sql(string.Format(deleteTemplate, ItemTemplateItemClassEntry.TableName));
            migrationBuilder.Sql(string.Format(deleteTemplate, ExperienceForLevelEntry.TableName));
            migrationBuilder.Sql(string.Format(deleteTemplate, RandomNameEntry.TableName));
        }
    }
}
