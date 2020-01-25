using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rasa.Database.Configurations.World
{
    using Structures;

    internal class EntityClassConfiguration : IEntityTypeConfiguration<EntityClassEntry>
    {
        public void Configure(EntityTypeBuilder<EntityClassEntry> builder)
        {
            builder.HasKey(e => e.ClassId)
                .HasName("PRIMARY");

            builder.ToTable("entityClass");

            builder.Property(e => e.ClassId)
                .HasColumnName("classId")
                .HasColumnType("int(11)");

            builder.Property(e => e.Augmentations)
                .IsRequired()
                .HasColumnName("augList")
                .HasColumnType("char(11)");

            builder.Property(e => e.ClassCollisionRole)
                .HasColumnName("classCollisionRole")
                .HasColumnType("tinyint(4)");

            builder.Property(e => e.ClassName)
                .IsRequired()
                .HasColumnName("className")
                .HasColumnType("char(58)");

            builder.Property(e => e.MeshId)
                .HasColumnName("meshId")
                .HasColumnType("int(11)");

            builder.Property(e => e.TargetFlag)
                .HasColumnName("targetFlag")
                .HasColumnType("bit(1)");
        }
    }
}