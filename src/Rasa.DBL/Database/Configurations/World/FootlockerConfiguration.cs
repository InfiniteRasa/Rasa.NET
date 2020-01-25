using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rasa.Database.Configurations.World
{
    using Structures;

    internal class FootlockerConfiguration : IEntityTypeConfiguration<FootlockerEntry>
    {
        public void Configure(EntityTypeBuilder<FootlockerEntry> builder)
        {
            builder.ToTable("footlockers");

            builder.Property(e => e.Id)
                .HasColumnName("id")
                .HasColumnType("int(11) unsigned");

            builder.Property(e => e.Comment)
                .HasColumnName("comment")
                .HasColumnType("varchar(64)")
                .HasDefaultValueSql("NULL");

            builder.Property(e => e.CoordX)
                .HasColumnName("coord_x")
                .HasDefaultValueSql("0");

            builder.Property(e => e.CoordY)
                .HasColumnName("coord_y")
                .HasDefaultValueSql("0");

            builder.Property(e => e.CoordZ)
                .HasColumnName("coord_z")
                .HasDefaultValueSql("0");

            builder.Property(e => e.EntityClassId)
                .HasColumnName("entity_class_id")
                .HasColumnType("int(11) unsigned")
                .HasDefaultValueSql("0");

            builder.Property(e => e.MapContextId)
                .HasColumnName("map_context_id")
                .HasColumnType("int(10) unsigned")
                .HasDefaultValueSql("0");

            builder.Property(e => e.Orientation)
                .HasColumnName("orientation")
                .HasDefaultValueSql("0");
        }
    }
}