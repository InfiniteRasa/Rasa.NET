using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rasa.Database.Configurations.World
{
    using Structures;

    internal class CreatureStatsConfiguration : IEntityTypeConfiguration<CreatureStatsEntry>
    {
        public void Configure(EntityTypeBuilder<CreatureStatsEntry> builder)
        {
            builder.HasKey(e => e.CreatureDbId)
                .HasName("PRIMARY");

            builder.ToTable("creature_stats");

            builder.Property(e => e.CreatureDbId)
                .HasColumnName("creatureDbId")
                .HasColumnType("int(10) unsigned")
                .HasComment("dbId from cratures table");

            builder.Property(e => e.Armor)
                .HasColumnName("armor")
                .HasColumnType("int(10) unsigned");

            builder.Property(e => e.Body)
                .HasColumnName("body")
                .HasColumnType("int(10) unsigned");

            builder.Property(e => e.Health)
                .HasColumnName("health")
                .HasColumnType("int(10) unsigned");

            builder.Property(e => e.Mind)
                .HasColumnName("mind")
                .HasColumnType("int(10) unsigned");

            builder.Property(e => e.Spirit)
                .HasColumnName("spirit")
                .HasColumnType("int(10) unsigned");
        }
    }
}