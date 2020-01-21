using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rasa.Structures;

namespace Rasa.Database.Configurations.Character
{
    internal class CharacterTeleportersConfiguration : IEntityTypeConfiguration<CharacterTeleportersEntry>
    {
        public void Configure(EntityTypeBuilder<CharacterTeleportersEntry> builder)
        {
            builder.HasKey(e => new { e.CharacterId, e.WaypointId });

            builder.ToTable("character_teleporters");

            builder.HasComment("holds data about all waypoints and wormholes that character gained");

            builder.Property(e => e.CharacterId)
                .HasColumnName("character_id")
                .HasColumnType("int(10) unsigned")
                .HasDefaultValueSql("'NULL'");

            builder.Property(e => e.WaypointId)
                .HasColumnName("waypoint_id")
                .HasColumnType("int(10) unsigned")
                .HasDefaultValueSql("'NULL'");
        }
    }
}