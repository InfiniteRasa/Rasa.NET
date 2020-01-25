using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rasa.Database.Configurations.World
{
    using Structures;

    internal class TeleporterConfiguration : IEntityTypeConfiguration<TeleporterEntry>
    {
        public void Configure(EntityTypeBuilder<TeleporterEntry> builder)
        {
            builder.ToTable("teleporter");

            builder.Property(e => e.Id)
                .HasColumnName("id")
                .HasColumnType("int(11) unsigned")
                .ValueGeneratedNever();

            builder.Property(e => e.CoordX).HasColumnName("coord_x");

            builder.Property(e => e.CoordY).HasColumnName("coord_y");

            builder.Property(e => e.CoordZ).HasColumnName("coord_z");

            builder.Property(e => e.Description)
                .IsRequired()
                .HasColumnName("description")
                .HasColumnType("varchar(50)");

            builder.Property(e => e.EntityClassId)
                .HasColumnName("entity_class_id")
                .HasColumnType("int(11) unsigned");

            builder.Property(e => e.MapContextId)
                .HasColumnName("map_context_id")
                .HasColumnType("int(10) unsigned");

            builder.Property(e => e.Orientation).HasColumnName("orientation");

            builder.Property(e => e.Type)
                .HasColumnName("type")
                .HasColumnType("int(11) unsigned");
        }
    }
}