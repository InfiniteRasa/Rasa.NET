using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rasa.Database.Configurations.World
{
    using Structures;

    internal class SpawnPoolConfiguration : IEntityTypeConfiguration<SpawnPoolEntry>
    {
        public void Configure(EntityTypeBuilder<SpawnPoolEntry> builder)
        {
            builder.HasKey(e => e.DbId)
                .HasName("PRIMARY");

            builder.ToTable("spawnpool");

            builder.Property(e => e.DbId)
                .HasColumnName("dbId")
                .HasColumnType("int(10) unsigned")
                .ValueGeneratedOnAdd();

            builder.Property(e => e.AnimType)
                .HasColumnName("animType")
                .HasColumnType("tinyint(2)")
                .HasDefaultValueSql("0");

            builder.Property(e => e.MapContextId)
                .HasColumnName("contextId")
                .HasColumnType("int(5)");

            builder.Property(e => e.CreatureId1)
                .HasColumnName("creatureId1")
                .HasColumnType("int(11) unsigned")
                .HasDefaultValueSql("0")
                .HasComment("dbId from creatures table");

            builder.Property(e => e.CreatureId2)
                .HasColumnName("creatureId2")
                .HasColumnType("int(10) unsigned")
                .HasDefaultValueSql("0")
                .HasComment("dbId from creatures table");

            builder.Property(e => e.CreatureId3)
                .HasColumnName("creatureId3")
                .HasColumnType("int(10) unsigned")
                .HasDefaultValueSql("0")
                .HasComment("dbId from creatures table");

            builder.Property(e => e.CreatureId4)
                .HasColumnName("creatureId4")
                .HasColumnType("int(10) unsigned")
                .HasDefaultValueSql("0")
                .HasComment("dbId from creatures table");

            builder.Property(e => e.CreatureId5)
                .HasColumnName("creatureId5")
                .HasColumnType("int(10) unsigned")
                .HasDefaultValueSql("0")
                .HasComment("dbId from creatures table");

            builder.Property(e => e.CreatureId6)
                .HasColumnName("creatureId6")
                .HasColumnType("int(10) unsigned")
                .HasDefaultValueSql("0")
                .HasComment("dbId from creatures table");

            builder.Property(e => e.CreatureMaxCount1)
                .HasColumnName("creatureMaxCount1")
                .HasColumnType("tinyint(4)")
                .HasDefaultValueSql("0");

            builder.Property(e => e.CreatureMaxCount2)
                .HasColumnName("creatureMaxCount2")
                .HasColumnType("tinyint(4)")
                .HasDefaultValueSql("0");

            builder.Property(e => e.CreatureMaxCount3)
                .HasColumnName("creatureMaxCount3")
                .HasColumnType("tinyint(4)")
                .HasDefaultValueSql("0");

            builder.Property(e => e.CreatureMaxCount4)
                .HasColumnName("creatureMaxCount4")
                .HasColumnType("tinyint(4)")
                .HasDefaultValueSql("0");

            builder.Property(e => e.CreatureMaxCount5)
                .HasColumnName("creatureMaxCount5")
                .HasColumnType("tinyint(4)")
                .HasDefaultValueSql("0");

            builder.Property(e => e.CreatureMaxCount6)
                .HasColumnName("creatureMaxCount6")
                .HasColumnType("tinyint(4)")
                .HasDefaultValueSql("0");

            builder.Property(e => e.CreatureMinCount1)
                .HasColumnName("creatureMinCount1")
                .HasColumnType("tinyint(4)")
                .HasDefaultValueSql("0");

            builder.Property(e => e.CreatureMinCount2)
                .HasColumnName("creatureMinCount2")
                .HasColumnType("tinyint(4)")
                .HasDefaultValueSql("0");

            builder.Property(e => e.CreatureMinCount3)
                .HasColumnName("creatureMinCount3")
                .HasColumnType("tinyint(4)")
                .HasDefaultValueSql("0");

            builder.Property(e => e.CreatureMinCount4)
                .HasColumnName("creatureMinCount4")
                .HasColumnType("tinyint(4)")
                .HasDefaultValueSql("0");

            builder.Property(e => e.CreatureMinCount5)
                .HasColumnName("creatureMinCount5")
                .HasColumnType("tinyint(4)")
                .HasDefaultValueSql("0");

            builder.Property(e => e.CreatureMinCount6)
                .HasColumnName("creatureMinCount6")
                .HasColumnType("tinyint(4)")
                .HasDefaultValueSql("0");

            builder.Property(e => e.Mode)
                .HasColumnName("mode")
                .HasColumnType("tinyint(2)")
                .HasDefaultValueSql("0");

            builder.Property(e => e.Orientation).HasColumnName("orientation");

            builder.Property(e => e.PosX).HasColumnName("posX");

            builder.Property(e => e.PosY).HasColumnName("posY");

            builder.Property(e => e.PosZ).HasColumnName("posZ");

            builder.Property(e => e.RespawnTime)
                .HasColumnName("respawnTime")
                .HasColumnType("int(11)")
                .HasDefaultValueSql("100")
                .HasComment("in secounds");
        }
    }
}