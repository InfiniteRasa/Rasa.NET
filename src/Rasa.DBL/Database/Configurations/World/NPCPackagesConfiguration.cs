using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rasa.Database.Configurations.World
{
    using Structures;

    internal class NPCPackagesConfiguration : IEntityTypeConfiguration<NPCPackagesEntry>
    {
        public void Configure(EntityTypeBuilder<NPCPackagesEntry> builder)
        {
            builder.HasNoKey();

            builder.ToTable("npc_packages");

            builder.Property(e => e.Comment)
                .IsRequired()
                .HasColumnName("comment")
                .HasColumnType("varchar(50)");

            builder.Property(e => e.CreatureDbId)
                .HasColumnName("creatureDbId")
                .HasColumnType("int(11)");

            builder.Property(e => e.NpcPackageId)
                .HasColumnName("packageId")
                .HasColumnType("int(11)");
        }
    }
}