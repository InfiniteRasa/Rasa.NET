using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rasa.Database.Configurations.World
{
    using Structures;

    internal class ArmorClassConfiguration : IEntityTypeConfiguration<ArmorClassEntry>
    {
        public void Configure(EntityTypeBuilder<ArmorClassEntry> builder)
        {
            builder.HasNoKey();

            builder.ToTable("armorClass");

            builder.Property(e => e.ClassId)
                .HasColumnName("classId")
                .HasColumnType("int(11)");

            builder.Property(e => e.MaxDamageAbsorbed)
                .HasColumnName("maxDamageAbsorbed")
                .HasColumnType("int(11)");

            builder.Property(e => e.MinDamageAbsorbed)
                .HasColumnName("minDamageAbsorbed")
                .HasColumnType("int(11)");

            builder.Property(e => e.RegenRate)
                .HasColumnName("regenRate")
                .HasColumnType("int(11)");
        }
    }
}