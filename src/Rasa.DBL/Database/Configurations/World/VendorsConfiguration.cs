using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rasa.Database.Configurations.World
{
    using Structures;

    internal class VendorsConfiguration : IEntityTypeConfiguration<VendorsEntry>
    {
        public void Configure(EntityTypeBuilder<VendorsEntry> builder)
        {
            builder.HasKey(e => e.CreatureDbId)
                .HasName("PRIMARY");

            builder.ToTable("vendors");

            builder.Property(e => e.CreatureDbId)
                .HasColumnName("creatureDbId")
                .HasColumnType("int(11)");

            builder.Property(e => e.PackageId)
                .HasColumnName("packageId")
                .HasColumnType("int(11)");
        }
    }
}