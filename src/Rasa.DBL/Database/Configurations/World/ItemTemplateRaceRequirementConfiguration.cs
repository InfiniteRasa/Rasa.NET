using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rasa.Database.Configurations.World
{
    using Structures;

    internal class ItemTemplateRaceRequirementConfiguration : IEntityTypeConfiguration<ItemTemplateRaceRequirementEntry>
    {
        public void Configure(EntityTypeBuilder<ItemTemplateRaceRequirementEntry> builder)
        {
            builder.HasNoKey();

            builder.ToTable("itemtemplate_racerequirement");

            builder.Property(e => e.ItemTemplateId)
                .HasColumnName("itemTemplateId")
                .HasColumnType("int(11)");

            builder.Property(e => e.RaceId)
                .HasColumnName("raceId")
                .HasColumnType("tinyint(4)");
        }
    }
}