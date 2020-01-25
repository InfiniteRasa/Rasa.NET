using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rasa.Database.Configurations.World
{
    using Structures;

    internal class CreatureAppearanceConfiguration : IEntityTypeConfiguration<CreatureAppearanceEntry>
    {
        public void Configure(EntityTypeBuilder<CreatureAppearanceEntry> builder)
        {
            builder.HasNoKey();

            builder.ToTable("creature_appearance");

            builder.Property(e => e.Class)
                .HasColumnName("class_id")
                .HasColumnType("int(11) unsigned")
                .HasDefaultValueSql("0");

            builder.Property(e => e.Color)
                .HasColumnName("color")
                .HasColumnType("int(11) unsigned")
                .HasDefaultValueSql("0");

            builder.Property(e => e.CreatureId)
                .HasColumnName("creature_id")
                .HasColumnType("int(10) unsigned");

            builder.Property(e => e.Slot)
                .HasColumnName("slot_id")
                .HasColumnType("tinyint(2)");
        }
    }
}