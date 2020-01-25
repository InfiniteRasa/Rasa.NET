using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rasa.Database.Configurations.World
{
    using Structures;

    internal class CreaturesConfiguration : IEntityTypeConfiguration<CreaturesEntry>
    {
        public void Configure(EntityTypeBuilder<CreaturesEntry> builder)
        {
            builder.HasKey(e => e.DbId)
                .HasName("PRIMARY");

            builder.ToTable("creatures");

            builder.Property(e => e.DbId)
                .HasColumnName("dbId")
                .HasColumnType("int(10) unsigned");

            builder.Property(e => e.Action1)
                .HasColumnName("action1")
                .HasColumnType("int(10) unsigned")
                .HasDefaultValueSql("0");

            builder.Property(e => e.Action2)
                .HasColumnName("action2")
                .HasColumnType("int(10) unsigned")
                .HasDefaultValueSql("0");

            builder.Property(e => e.Action3)
                .HasColumnName("action3")
                .HasColumnType("int(10) unsigned")
                .HasDefaultValueSql("0");

            builder.Property(e => e.Action4)
                .HasColumnName("action4")
                .HasColumnType("int(10) unsigned")
                .HasDefaultValueSql("0");

            builder.Property(e => e.Action5)
                .HasColumnName("action5")
                .HasColumnType("int(10) unsigned")
                .HasDefaultValueSql("0");

            builder.Property(e => e.Action6)
                .HasColumnName("action6")
                .HasColumnType("int(10) unsigned")
                .HasDefaultValueSql("0");

            builder.Property(e => e.Action7)
                .HasColumnName("action7")
                .HasColumnType("int(10) unsigned")
                .HasDefaultValueSql("0");

            builder.Property(e => e.Action8)
                .HasColumnName("action8")
                .HasColumnType("int(10) unsigned")
                .HasDefaultValueSql("0");

            builder.Property(e => e.ClassId)
                .HasColumnName("classId")
                .HasColumnType("int(10) unsigned");

            builder.Property(e => e.Comment)
                .HasColumnName("comment")
                .HasColumnType("varchar(50)")
                .HasDefaultValueSql("''");

            builder.Property(e => e.Faction)
                .HasColumnName("faction")
                .HasColumnType("int(10)")
                .HasDefaultValueSql("0");

            builder.Property(e => e.Level)
                .HasColumnName("level")
                .HasColumnType("int(10)")
                .HasDefaultValueSql("0");

            builder.Property(e => e.MaxHitPoints)
                .HasColumnName("maxHitPoints")
                .HasColumnType("int(10)")
                .HasDefaultValueSql("0");

            builder.Property(e => e.NameId)
                .HasColumnName("nameId")
                .HasColumnType("int(10)")
                .HasDefaultValueSql("0");

            builder.Property(e => e.RunSpeed)
                .HasColumnName("run_speed")
                .HasColumnType("int(10) unsigned")
                .HasDefaultValueSql("0");

            builder.Property(e => e.WalkSpeed)
                .HasColumnName("walk_speed")
                .HasColumnType("int(10) unsigned")
                .HasDefaultValueSql("0");
        }
    }
}