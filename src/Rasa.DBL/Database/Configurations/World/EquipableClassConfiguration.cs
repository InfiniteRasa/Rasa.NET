using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rasa.Database.Configurations.World
{
    using Structures;

    internal class EquipableClassConfiguration : IEntityTypeConfiguration<EquipableClassEntry>
    {
        public void Configure(EntityTypeBuilder<EquipableClassEntry> builder)
        {
            builder.HasKey(e => e.ClassId)
                .HasName("PRIMARY");

            builder.ToTable("equipableclass");

            builder.Property(e => e.ClassId)
                .HasColumnName("classId")
                .HasColumnType("int(11) unsigned")
                .ValueGeneratedNever();

            builder.Property(e => e.SlotId)
                .HasColumnName("slotId")
                .HasColumnType("int(11)")
                .HasDefaultValueSql("0");

        }
    }
}